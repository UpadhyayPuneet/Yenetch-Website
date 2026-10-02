using System;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Script.Serialization;
using Yenetch.Crm;

namespace Yenetch.Web.Handlers
{
    /// <summary>POST /api/audit: runs the free website audit. Form fields url, email, name, phone, vid. Returns the report as JSON.</summary>
    public class AuditHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            ctx.Response.TrySkipIisCustomErrors = true;
            if (ctx.Request.HttpMethod != "POST") { ctx.Response.StatusCode = 405; return; }
            if (!Guard.SameOrigin(ctx.Request)) { ctx.Response.StatusCode = 403; return; }
            var f = ctx.Request.Unvalidated.Form;
            if (!string.IsNullOrEmpty(f["website"])) { Fail(ctx, 200, "We could not check that website. Please try again."); return; } // honeypot

            var email = (f["email"] ?? "").Trim();
            if (!Newsletter.IsEmail(email)) { Fail(ctx, 422, "Enter a valid email address so we can send the report."); return; }
            if (string.IsNullOrWhiteSpace(f["url"])) { Fail(ctx, 422, "Enter your website address."); return; }

            var ip = Analytics.ClientIp(ctx.Request) ?? "";
            var key = "audit:" + ip;
            var count = (ctx.Cache[key] as int?) ?? 0;
            if (count >= 8) { Fail(ctx, 429, "You have run several audits in the last hour. Please try again later, or book a call and we will check it for you."); return; }
            ctx.Cache.Insert(key, count + 1, null, DateTime.UtcNow.AddHours(1), Cache.NoSlidingExpiration);
            if (!Guard.CaptchaPassed(Guard.TokenFrom(f), ctx.Request)) { Fail(ctx, 422, "Please confirm you are not a robot and try again."); return; }

            AuditResult r;
            try
            {
                Db.EnsureSchema();
                r = Audits.RunAndSave(f["url"], f["name"], email, f["phone"], f["vid"], ip);
            }
            catch (Exception ex) { Mailer.Log("audit", ex); Fail(ctx, 500, "Something went wrong while checking that website. Please try again."); return; }
            if (!r.Ok) { Fail(ctx, 422, r.Error); return; }

            ctx.Response.Write(new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(new
            {
                ok = true,
                id = r.Id,
                host = r.Host,
                url = r.Url,
                score = r.Score,
                headline = r.Headline,
                categories = r.Categories,
                stats = r.Stats,
                checks = r.Checks.Select(c => new { cat = c.Cat, status = c.Status, title = c.Title, detail = c.Detail, fix = c.Fix })
            }));
        }

        private static void Fail(HttpContext ctx, int status, string error)
        {
            ctx.Response.StatusCode = status;
            ctx.Response.Write(new JavaScriptSerializer().Serialize(new { ok = false, error }));
        }
    }
}
