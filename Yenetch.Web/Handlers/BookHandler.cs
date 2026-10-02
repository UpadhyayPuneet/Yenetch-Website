using System;
using System.Web;
using System.Web.Caching;
using System.Web.Script.Serialization;
using Yenetch.Crm;

namespace Yenetch.Web.Handlers
{
    /// <summary>GET /api/slots: open booking slots for /book.</summary>
    public class SlotsHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            try { Db.EnsureSchema(); ctx.Response.Write(Bookings.SlotsJson()); }
            catch (Exception ex) { Mailer.Log("slots", ex); ctx.Response.StatusCode = 500; ctx.Response.Write("{\"enabled\":false}"); }
        }
    }

    /// <summary>POST /api/book: books a call. Form fields name, email, phone, company, topic, notes, mode, date (yyyy-MM-dd IST), time (HH:mm).</summary>
    public class BookHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (ctx.Request.HttpMethod != "POST") { ctx.Response.StatusCode = 405; return; }
            if (!Guard.SameOrigin(ctx.Request)) { ctx.Response.StatusCode = 403; return; }
            var f = ctx.Request.Unvalidated.Form;
            if (!string.IsNullOrEmpty(f["website"])) { ctx.Response.Write("{\"ok\":true}"); return; } // honeypot

            var ip = Analytics.ClientIp(ctx.Request) ?? "";
            var key = "book:" + ip;
            var count = (ctx.Cache[key] as int?) ?? 0;
            if (count >= 6) { Write(ctx, 429, false, "Too many bookings from this connection. Please call us instead.", null); return; }
            ctx.Cache.Insert(key, count + 1, null, DateTime.UtcNow.AddHours(1), Cache.NoSlidingExpiration);
            if (!Guard.CaptchaPassed(Guard.TokenFrom(f), ctx.Request)) { Write(ctx, 422, false, "Please confirm you are not a robot and try again.", null); return; }

            string error;
            Booking b;
            try
            {
                var vid = ctx.Request.Cookies["yn_vid"] != null ? ctx.Request.Cookies["yn_vid"].Value : null;
                b = Bookings.Book(f["name"], f["email"], f["phone"], f["company"], f["topic"], f["notes"], f["mode"], f["date"], f["time"], vid, ip, out error);
            }
            catch (Exception ex) { Mailer.Log("booking", ex); Write(ctx, 500, false, "We could not save your booking. Please call or WhatsApp us.", null); return; }
            if (b == null) { Write(ctx, 422, false, error, null); return; }
            Write(ctx, 200, true, null, b.WhenText);
        }

        private static void Write(HttpContext ctx, int status, bool ok, string error, string when)
        {
            ctx.Response.StatusCode = status;
            ctx.Response.TrySkipIisCustomErrors = true;
            ctx.Response.Write(new JavaScriptSerializer().Serialize(new { ok, error, when }));
        }
    }

    /// <summary>GET /r/{token}: records that a client opened a review request, then forwards to Google.</summary>
    public class ReviewLinkHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            var token = Convert.ToString(ctx.Request.RequestContext.RouteData.Values["token"]);
            string url;
            try { url = Reviews.Click(token); } catch { url = "/"; }
            ctx.Response.Redirect(url, false);
        }
    }
}
