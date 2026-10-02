using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Script.Serialization;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>
    /// Site content. With a CrmDb connection (the default) everything comes from the CMS database through
    /// ContentStore and is edited in /admin/content. appSettings ContentSource=Files, or a database that cannot be
    /// reached, falls back to the files in assets/data (yenetch.json and pages.json) so the site keeps working.
    /// </summary>
    public static class SiteContent
    {
        private static bool UseDatabase
        {
            get { return Yenetch.Crm.Db.IsConfigured && !string.Equals(System.Configuration.ConfigurationManager.AppSettings["ContentSource"], "Files", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>The database snapshot, or null when running from files (or the database is down).</summary>
        private static ContentStore.Snapshot Db
        {
            get
            {
                if (!UseDatabase || HttpRuntime.Cache["content.down"] != null) return null;
                try { return ContentStore.Current; }
                catch (Exception ex)
                {
                    Yenetch.Crm.Mailer.Log("Website content could not be read from the database; showing the built-in files", ex);
                    HttpRuntime.Cache.Insert("content.down", true, null, DateTime.UtcNow.AddSeconds(30), Cache.NoSlidingExpiration);
                    return null;
                }
            }
        }

        public static SiteData Current { get { var d = Db; return d != null ? d.Site : Load<SiteData>("~/assets/data/yenetch.json").Item1; } }

        /// <summary>Raw JSON, inlined into Site.Master as window.YENETCH_DATA for the chatbot and finder.</summary>
        public static string RawJson { get { var d = Db; return d != null ? d.SiteJson.Replace("</", "<\\/") : Load<SiteData>("~/assets/data/yenetch.json").Item2; } }

        public static PageCopy Copy { get { var d = Db; return d != null ? d.Copy : Load<PageCopy>("~/assets/data/pages.json").Item1; } }

        /// <summary>HTML of a legal page ("privacy" or "terms") from the admin, or the text that shipped with the site.</summary>
        public static string LegalHtml(string key)
        {
            var d = Db;
            LegalPage p;
            if (d != null && d.Legal.TryGetValue(key, out p) && !string.IsNullOrWhiteSpace(p.Html)) return p.Html;
            var file = Yenetch.Crm.Util.AppPath("App_Data/seed/" + key + ".html");
            return File.Exists(file) ? File.ReadAllText(file) : "";
        }

        public static string LegalTitle(string key, string fallback)
        {
            var d = Db;
            LegalPage p;
            return d != null && d.Legal.TryGetValue(key, out p) && !string.IsNullOrWhiteSpace(p.Title) ? p.Title : fallback;
        }

        /// <summary>Google title and description set in the admin for a page address, or null.</summary>
        public static SeoOverride SeoFor(string path)
        {
            var d = Db;
            if (d == null) return null;
            var p = ContentStore.Norm(path);
            return d.Seo.FirstOrDefault(s => s.Path == p);
        }

        internal static Tuple<T, string> Load<T>(string virtualPath)
        {
            var key = "Yenetch.Json." + virtualPath;
            var cached = HttpRuntime.Cache[key] as Tuple<T, string>;
            if (cached != null) return cached;

            var path = HttpContext.Current.Server.MapPath(virtualPath);
            var json = File.ReadAllText(path);
            var data = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<T>(json);

            // Escape "</" so the JSON can never close an inline <script> block.
            cached = Tuple.Create(data, json.Replace("</", "<\\/"));
            HttpRuntime.Cache.Insert(key, cached, new CacheDependency(path));
            return cached;
        }

        /// <summary>A published landing page by address, or null.</summary>
        public static LandingPage Landing(string slug)
        {
            var d = Db;
            LandingPage p;
            return d != null && slug != null && d.Landing.TryGetValue(slug, out p) ? p : null;
        }

        public static IEnumerable<LandingPage> LandingPages { get { var d = Db; return d != null ? d.Landing.Values : Enumerable.Empty<LandingPage>(); } }

        public static IEnumerable<Service> ServicesFor(string pillarId) { return Current.Services.Where(s => s.Pillar == pillarId); }
        public static Service Service(string slug) { return Current.Services.FirstOrDefault(s => s.Slug == slug); }
        public static Product Product(string slug) { return Current.Products.FirstOrDefault(p => p.Slug == slug); }
        public static CaseStudy CaseStudy(string slug) { return Current.CaseStudies.FirstOrDefault(c => c.Slug == slug); }
        public static Pillar Pillar(string id) { return Current.Pillars.FirstOrDefault(p => p.Id == id); }

        public static ServiceCopy ServiceCopy(string slug)
        {
            ServiceCopy c;
            return Copy.Services != null && Copy.Services.TryGetValue(slug, out c) ? c : new ServiceCopy();
        }

        public static ProductCopy ProductCopy(string slug)
        {
            ProductCopy c;
            return Copy.Products != null && Copy.Products.TryGetValue(slug, out c) ? c : new ProductCopy();
        }

        public static string ServiceName(string slug)
        {
            var s = Service(slug);
            return s == null ? slug : s.Name;
        }

        public static List<CaseStudy> CasesForService(string slug)
        {
            return Current.CaseStudies.Where(c => c.Services != null && c.Services.Contains(slug)).ToList();
        }

        public static List<CaseStudy> Cases(params string[] slugs)
        {
            return slugs.Select(CaseStudy).Where(c => c != null).ToList();
        }

        public static List<Service> ServiceList(IEnumerable<string> slugs)
        {
            return (slugs ?? Enumerable.Empty<string>()).Select(Service).Where(s => s != null).ToList();
        }
    }

    /// <summary>Stock photography (assets/img/stock/photos.json). Local 400px files always work;
    /// with appSettings StockCdn=true the browser also gets full-resolution sizes from the Unsplash CDN.</summary>
    public static class Photos
    {
        private static Dictionary<string, Dictionary<string, object>> All
        {
            get { return SiteContent.Load<Dictionary<string, Dictionary<string, object>>>("~/assets/img/stock/photos.json").Item1; }
        }

        private static Dictionary<string, object> Find(string key)
        {
            Dictionary<string, object> p;
            return key != null && All.TryGetValue(key, out p) ? p : null;
        }

        private static string Field(string key, string name)
        {
            var p = Find(key);
            object v;
            return p != null && p.TryGetValue(name, out v) ? Convert.ToString(v) : "";
        }

        /// <summary>Accepts a stock key ("office-team") or a full URL / site path, so database rows can hold either.</summary>
        public static string Src(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (key.Contains("/")) return key;
            var file = Field(key, "file");
            return string.IsNullOrEmpty(file) ? "" : "/assets/img/stock/" + file;
        }

        public static string Alt(string key) { return Field(key, "alt"); }

        /// <summary>Every built-in photo name with its address (for the admin image picker).</summary>
        public static Dictionary<string, string> Map() { return All.ToDictionary(kv => kv.Key, kv => Src(kv.Key)); }
        public static string Credit(string key) { return Field(key, "credit"); }

        public static string SrcSet(string key)
        {
            var src = Src(key);
            // Uploaded images and full URLs have one size only.
            if (string.IsNullOrEmpty(src) || key.Contains("/")) return "";
            var photo = Field(key, "photo");
            var cdn = string.Equals(System.Configuration.ConfigurationManager.AppSettings["StockCdn"], "true", StringComparison.OrdinalIgnoreCase);
            if (!cdn || string.IsNullOrEmpty(photo)) return src + " 400w";
            var root = "https://images.unsplash.com/" + photo + "?auto=format&fit=crop&q=78&w=";
            return src + " 400w, " + root + "800 800w, " + root + "1200 1200w, " + root + "1800 1800w";
        }
    }
}
