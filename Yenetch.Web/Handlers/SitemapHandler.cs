using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Yenetch.Data;

namespace Yenetch.Web.Handlers
{
    /// <summary>/sitemap.xml, built from the same content as the pages, so new services, products,
    /// case studies and articles appear automatically. Registered in Web.config under system.webServer/handlers.</summary>
    public class SitemapHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            var root = Seo.Root;
            var data = SiteContent.Current;
            var urls = new List<Tuple<string, string, string>>
            {
                U("/", "1.0"), U("/digital-marketing", "0.9"), U("/software-development", "0.9"), U("/talent-resourcing", "0.9"),
                U("/services", "0.8"), U("/products", "0.8"), U("/case-studies", "0.8"), U("/about", "0.7"),
                U("/careers", "0.6"), U("/blog", "0.7"), U("/contact", "0.7"), U("/solution-finder", "0.6"),
                U("/website-audit", "0.8"), U("/book", "0.6"), U("/pricing", "0.8"),
                U("/privacy", "0.2"), U("/terms", "0.2")
            };
            urls.AddRange(data.Services.Select(s => U(s.Url, "0.8")));
            urls.AddRange(data.Products.Select(p => U(p.Url, "0.7")));
            urls.AddRange(data.CaseStudies.Select(c => U(c.PageUrl, "0.6")));
            urls.AddRange(SiteContent.LandingPages.Select(l => U(l.Url, "0.8")));
            urls.AddRange(Authors.All.Select(a => U(a.Url, "0.5")));
            urls.AddRange(BlogStore.Repository.Latest(1000).Select(b => Tuple.Create(b.Url, "0.6", b.PublishedOn.ToString("yyyy-MM-dd"))));

            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
            foreach (var u in urls)
            {
                sb.Append("  <url><loc>").Append(HttpUtility.HtmlEncode(root + u.Item1)).Append("</loc>");
                if (!string.IsNullOrEmpty(u.Item3)) sb.Append("<lastmod>").Append(u.Item3).Append("</lastmod>");
                sb.Append("<priority>").Append(u.Item2).Append("</priority></url>\n");
            }
            sb.Append("</urlset>");

            ctx.Response.ContentType = "application/xml";
            ctx.Response.Cache.SetCacheability(HttpCacheability.Public);
            ctx.Response.Cache.SetMaxAge(TimeSpan.FromHours(6));
            ctx.Response.Write(sb.ToString());
        }

        private static Tuple<string, string, string> U(string path, string priority) { return Tuple.Create(path, priority, (string)null); }
    }
}
