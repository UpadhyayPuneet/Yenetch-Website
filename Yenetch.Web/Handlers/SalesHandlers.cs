using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Handlers
{
    /// <summary>/api/site-data: the scripts' content as one cached JavaScript file (see SiteDataScript).</summary>
    public class SiteDataHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            var built = SiteDataScript.Current;
            ctx.Response.ContentType = "application/javascript";
            ctx.Response.ContentEncoding = Encoding.UTF8;
            ctx.Response.AppendHeader("X-Content-Type-Options", "nosniff");
            // Versioned links (?v=) never change, so browsers keep them; the bare address is checked again after 5 minutes.
            var cache = ctx.Response.Cache;
            cache.SetCacheability(HttpCacheability.Public);
            cache.SetMaxAge(ctx.Request.QueryString["v"] == built.Version ? TimeSpan.FromDays(30) : TimeSpan.FromMinutes(5));
            cache.SetETag("\"" + built.Version + "\"");
            if (ctx.Request.Headers["If-None-Match"] == "\"" + built.Version + "\"") { ctx.Response.StatusCode = 304; return; }
            ctx.Response.Write(built.Script);
        }
    }

    /// <summary>/api/geo: the visitor's country and the currency to show prices in (from Cloudflare or the location cache).</summary>
    public class GeoHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetCacheability(HttpCacheability.Private);
            ctx.Response.Cache.SetMaxAge(TimeSpan.FromHours(6));
            string country = null, currency = null;
            try { country = Fx.Country(ctx.Request); currency = Fx.ForCountry(country); } catch { }
            ctx.Response.Write(new JavaScriptSerializer().Serialize(new { country, currency }));
        }
    }

    /// <summary>Base for the JSON POST endpoints: method, size, origin and body parsing.</summary>
    public abstract class JsonPostHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }
        protected virtual int MaxBody { get { return 20000; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            ctx.Response.TrySkipIisCustomErrors = true;
            if (ctx.Request.HttpMethod != "POST") { ctx.Response.StatusCode = 405; return; }
            if (!Guard.SameOrigin(ctx.Request)) { ctx.Response.StatusCode = 403; return; }
            if (ctx.Request.ContentLength > MaxBody) { ctx.Response.StatusCode = 413; return; }
            string body;
            using (var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) body = sr.ReadToEnd();
            Dictionary<string, object> d;
            try { d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(body) ?? new Dictionary<string, object>(); }
            catch { Write(ctx, 400, new { ok = false, error = "Bad request." }); return; }
            Db.EnsureSchema();
            Handle(ctx, d);
        }

        protected abstract void Handle(HttpContext ctx, Dictionary<string, object> d);

        protected static void Write(HttpContext ctx, int status, object o)
        {
            ctx.Response.StatusCode = status;
            ctx.Response.Write(new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(o));
        }

        protected static string S(Dictionary<string, object> d, string k, int max)
        {
            object v;
            var s = d.TryGetValue(k, out v) && v != null ? Convert.ToString(v).Trim() : "";
            return s.Length > max ? s.Substring(0, max) : s;
        }
    }

    /// <summary>
    /// /api/quote. mode "price": prices a selection (and checks a coupon) for the plan builder. mode "submit": saves the
    /// quote, creates or updates the lead, emails the visitor a copy and alerts the team.
    /// </summary>
    public class QuoteHandler : JsonPostHandler
    {
        protected override void Handle(HttpContext ctx, Dictionary<string, object> d)
        {
            var picks = new List<Yenetch.Data.Pricing.Pick>();
            object items;
            if (d.TryGetValue("items", out items) && items is System.Collections.IEnumerable)
                foreach (var x in (System.Collections.IEnumerable)items)
                {
                    var it = x as Dictionary<string, object>;
                    if (it == null) continue;
                    int qty; int.TryParse(S(it, "qty", 6), out qty);
                    picks.Add(new Yenetch.Data.Pricing.Pick { Kind = S(it, "kind", 10) == "addon" ? "addon" : "plan", Id = S(it, "id", 80), Qty = qty });
                }
            var q = Yenetch.Data.Pricing.Calc(picks, S(d, "currency", 3), S(d, "code", 40));
            if (S(d, "mode", 10) != "submit")
            {
                if (!Guard.Allow(ctx.Request, "quote-price", 300, TimeSpan.FromHours(1))) { Write(ctx, 429, new { ok = false }); return; }
                Write(ctx, 200, View(q));
                return;
            }

            var problem = Guard.Check(ctx.Request, "quote", 10, S(d, "website", 200), S(d, "t", 20), S(d, "captcha", 4096));
            if (problem == "bot") { Write(ctx, 200, new { ok = true }); return; }
            if (problem != null) { Write(ctx, 429, new { ok = false, error = problem }); return; }
            var name = S(d, "name", 120); var email = S(d, "email", 160); var phone = S(d, "phone", 40);
            if (name.Length < 2) { Write(ctx, 422, new { ok = false, error = "Enter your name." }); return; }
            if (!Newsletter.IsEmail(email)) { Write(ctx, 422, new { ok = false, error = "Enter a valid email address so we can send your quote." }); return; }
            if (Util.Digits(phone).Length < 8) { Write(ctx, 422, new { ok = false, error = "Enter a phone number we can reach you on." }); return; }
            if (q.Lines.Count == 0) { Write(ctx, 422, new { ok = false, error = "Add at least one plan or extra." }); return; }

            var quote = Quotes.Create(q, name, email, phone, S(d, "company", 160), S(d, "notes", 2000), Analytics.CookieId(ctx.Request, Analytics.VisitorCookie), Analytics.ClientIp(ctx.Request), S(d, "page", 300));
            Write(ctx, 200, new { ok = true, token = quote.Token, total = Yenetch.Data.Pricing.ShowPrices ? q.Money(q.DueNow) : null });
        }

        /// <summary>The priced quote in the shape the plan builder shows.</summary>
        public static object View(QuoteResult q)
        {
            var show = Yenetch.Data.Pricing.ShowPrices;
            Func<decimal, string> m = v => show ? q.Money(v) : "";
            return new
            {
                ok = true, currency = q.Currency, show,
                lines = q.Lines.Select(l => new { kind = l.Kind, id = l.Id, name = l.Name, service = l.ServiceName, qty = l.Qty, unit = l.Unit, billing = l.Billing, from = l.IsFrom, unitPrice = m(l.UnitPrice), amount = m(l.Amount), setup = l.Setup > 0 ? m(l.Setup) : null }),
                oneTime = m(q.OneTime), monthly = q.Monthly > 0 ? m(q.Monthly) : null, yearly = q.Yearly > 0 ? m(q.Yearly) : null,
                discount = q.Discount > 0 ? m(q.Discount) : null, offer = q.OfferTitle, offerNote = q.OfferNote, codeError = q.CodeError,
                dueNow = m(q.DueNow), hasFrom = q.HasFrom, tax = Yenetch.Data.Pricing.TaxName, taxPct = Yenetch.Data.Pricing.TaxPct
            };
        }
    }

    /// <summary>/api/chat: the AI assistant. Returns {fallback:true} when AI is off or no key can answer, so the chat uses its built-in answers.</summary>
    public class ChatHandler : JsonPostHandler
    {
        protected override int MaxBody { get { return 40000; } }

        protected override void Handle(HttpContext ctx, Dictionary<string, object> d)
        {
            if (!Ai.Enabled) { Write(ctx, 200, new { fallback = true }); return; }
            if (!Guard.Allow(ctx.Request, "chat", Ai.PerVisitorPerHour, TimeSpan.FromHours(1)))
            {
                Write(ctx, 200, new { reply = "You've asked a lot of questions! For anything else, a specialist can help you directly.", chips = new[] { "Talk to an expert", "Book a call" }, links = new object[0], lead = false });
                return;
            }
            var turns = new List<AiTurn>();
            object msgs;
            if (d.TryGetValue("messages", out msgs) && msgs is System.Collections.IEnumerable)
                foreach (var x in (System.Collections.IEnumerable)msgs)
                {
                    var m = x as Dictionary<string, object>;
                    if (m != null) turns.Add(new AiTurn { Role = S(m, "role", 10), Text = S(m, "text", 1500) });
                }
            if (turns.Count == 0 || turns.Count > 40) { Write(ctx, 400, new { fallback = true }); return; }
            var page = S(d, "page", 300);
            var reply = Ai.Ask(turns, page, S(d, "currency", 3));
            if (reply.Fallback) { Write(ctx, 200, new { fallback = true }); return; }
            var key = S(d, "key", 40);
            if (key.Length >= 8) Ai.Record(key, Analytics.CookieId(ctx.Request, Analytics.VisitorCookie), Analytics.ClientIp(ctx.Request), page, turns.Last().Text, reply.Text);
            Write(ctx, 200, new { reply = reply.Text, chips = reply.Chips, links = reply.Links.Select(l => new { label = l.Label, url = l.Url }), lead = reply.OfferLead });
        }
    }
}
