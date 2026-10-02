using System;
using System.Globalization;
using System.Web;
using System.Web.Caching;
using Yenetch.Crm;

namespace Yenetch.Web.Handlers
{
    /// <summary>Receives job applications from the Careers popup (multipart form with an optional CV) and saves them to CrmApplications.</summary>
    public class ApplyHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (ctx.Request.HttpMethod != "POST") { ctx.Response.StatusCode = 405; return; }
            var f = ctx.Request.Unvalidated.Form; // free text such as a cover note may contain "<"; every value is HTML-encoded on output

            // Honeypot: people never fill "website"; bots do.
            if (!string.IsNullOrEmpty(f["website"])) { ctx.Response.Write("{\"ok\":true}"); return; }

            // At most 5 applications per address per hour.
            var ip = Analytics.ClientIp(ctx.Request) ?? "";
            var key = "apply:" + ip;
            var count = (ctx.Cache[key] as int?) ?? 0;
            if (count >= 5) { Fail(ctx, 429, "Too many applications from this connection. Please try again later."); return; }
            ctx.Cache.Insert(key, count + 1, null, DateTime.UtcNow.AddHours(1), Cache.NoSlidingExpiration);

            var a = new JobApplication
            {
                Name = Get(f, "name", 120), Email = Get(f, "email", 160), Phone = Get(f, "phone", 40), Job = Get(f, "job", 160),
                Category = Get(f, "team", 80), City = Get(f, "city", 80), Notice = Get(f, "notice", 40), Skills = Get(f, "skills", 400),
                ResumeUrl = Url(Get(f, "resumeUrl", 400)), Portfolio = Url(Get(f, "portfolio", 400)), Note = Get(f, "note", 2000), Ip = ip
            };
            decimal exp;
            if (!decimal.TryParse(Get(f, "experience", 10), NumberStyles.Number, CultureInfo.InvariantCulture, out exp) || exp < 0 || exp > 50) { Fail(ctx, 422, "Enter your total experience in years."); return; }
            a.Experience = Math.Round(exp * 2) / 2;
            if (a.Job == "") a.Job = "General application";
            if (a.Category == "") a.Category = a.Job == "General application" ? "General" : null;
            if (a.Name.Length < 2) { Fail(ctx, 422, "Enter your full name."); return; }
            if (!Newsletter.IsEmail(a.Email)) { Fail(ctx, 422, "Enter a valid email address."); return; }
            if (Util.Digits(a.Phone).Length < 8) { Fail(ctx, 422, "Enter a phone number we can call."); return; }

            var cv = ctx.Request.Files["cv"];
            if (cv != null && cv.ContentLength == 0) cv = null;
            if (cv == null && string.IsNullOrEmpty(a.ResumeUrl)) { Fail(ctx, 422, "Attach your CV or add a link to it."); return; }
            if (cv != null) { var p = Applications.FileProblem(cv); if (p != null) { Fail(ctx, 422, p); return; } }

            try { Applications.Create(a, cv); }
            catch (Exception ex) { Mailer.Log("application save", ex); Fail(ctx, 500, "We could not save your application. Please email your CV to us instead."); return; }
            ctx.Response.Write("{\"ok\":true}");
        }

        private static string Get(System.Collections.Specialized.NameValueCollection f, string key, int max)
        {
            var v = (f[key] ?? "").Trim();
            return v.Length > max ? v.Substring(0, max) : v;
        }

        /// <summary>Keeps only http(s) links; adds https:// to bare addresses.</summary>
        private static string Url(string v)
        {
            if (v == "") return null;
            if (!v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !v.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) v = "https://" + v;
            Uri u;
            return Uri.TryCreate(v, UriKind.Absolute, out u) && (u.Scheme == "http" || u.Scheme == "https") ? u.ToString() : null;
        }

        private static void Fail(HttpContext ctx, int status, string message)
        {
            ctx.Response.StatusCode = status;
            ctx.Response.TrySkipIisCustomErrors = true;
            ctx.Response.Write("{\"ok\":false,\"error\":" + new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(message) + "}");
        }
    }
}
