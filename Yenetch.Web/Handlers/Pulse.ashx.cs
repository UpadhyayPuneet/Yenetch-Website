using System.IO;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Web.Handlers
{
    /// <summary>First-party analytics endpoint. assets/js/consent.js sends pageviews, time on page and events here (navigator.sendBeacon).</summary>
    public class PulseHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.StatusCode = 204;
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (ctx.Request.HttpMethod != "POST" || ctx.Request.ContentLength > 4000) return;
            // Only accept hits sent by pages of this site.
            var origin = ctx.Request.Headers["Origin"];
            if (!string.IsNullOrEmpty(origin) && !origin.EndsWith("//" + ctx.Request.Url.Authority, System.StringComparison.OrdinalIgnoreCase)) return;
            string body;
            using (var sr = new StreamReader(ctx.Request.InputStream)) body = sr.ReadToEnd();
            try { Db.EnsureSchema(); Analytics.Record(ctx, body); }
            catch (System.Exception ex) { Mailer.Log("pulse", ex); }
        }
    }
}
