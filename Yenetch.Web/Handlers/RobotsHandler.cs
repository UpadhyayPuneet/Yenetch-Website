using System;
using System.Web;
using Yenetch.Data;

namespace Yenetch.Web.Handlers
{
    /// <summary>/robots.txt from the rules in /admin/robots. Registered in Web.config under system.webServer/handlers.</summary>
    public class RobotsHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            string text;
            try
            {
                var site = new Uri(Seo.Root).Host.Replace("www.", "");
                var host = ctx.Request.Url.Host.Replace("www.", "");
                var isTest = !string.Equals(site, host, StringComparison.OrdinalIgnoreCase) && host != "localhost";
                text = Robots.Render(Robots.Load(), isTest);
            }
            catch { text = "User-agent: *\nDisallow: /admin\n\nSitemap: " + Seo.Root + "/sitemap.xml\n"; }
            ctx.Response.ContentType = "text/plain";
            ctx.Response.ContentEncoding = System.Text.Encoding.UTF8;
            ctx.Response.Cache.SetCacheability(HttpCacheability.Public);
            ctx.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(10));
            ctx.Response.Write(text);
        }
    }
}
