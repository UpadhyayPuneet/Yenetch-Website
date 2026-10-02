using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

namespace Yenetch.Data
{
    /// <summary>A script or tag added in /admin/tracking.</summary>
    public class CustomScript
    {
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary>head, body-start or body-end.</summary>
        public string Placement { get; set; }
        /// <summary>necessary (always runs), analytics or marketing (run only after the visitor accepts that kind of cookie).</summary>
        public string Category { get; set; }
        public bool Enabled { get; set; }
        /// <summary>Empty for every page, or page addresses one per line; a line ending in * matches everything under it.</summary>
        public string Pages { get; set; }
        public string Code { get; set; }
    }

    public class TrackingSettings
    {
        public string Ga4 { get; set; }
        public string Gtm { get; set; }
        public string GoogleAds { get; set; }
        public string MetaPixel { get; set; }
        public string LinkedIn { get; set; }
        public string Clarity { get; set; }
        public string GoogleVerify { get; set; }
        public string BingVerify { get; set; }
        public List<CustomScript> Scripts { get; set; }
    }

    /// <summary>
    /// Third-party tags (Google Analytics, Ads, Tag Manager, Meta, LinkedIn, Clarity) and custom scripts, edited in the admin and
    /// rendered by Site.Master. Analytics and marketing tags are wrapped in &lt;template data-consent&gt; and only started by consent.js
    /// after the visitor accepts that kind of cookie, so the site stays within the cookie banner's promise.
    /// </summary>
    public static class Tracking
    {
        private const string Key = "tracking";
        public static readonly string[] Placements = { "head", "body-start", "body-end" };
        public static readonly string[] Categories = { "necessary", "analytics", "marketing" };

        public static TrackingSettings Load()
        {
            TrackingSettings t = null;
            try
            {
                var json = Settings.Get(Key);
                if (!string.IsNullOrEmpty(json)) t = new JavaScriptSerializer().Deserialize<TrackingSettings>(json);
            }
            catch { }
            t = t ?? new TrackingSettings();
            if (t.Scripts == null) t.Scripts = new List<CustomScript>();
            return t;
        }

        public static void Save(TrackingSettings t) { Settings.Set(Key, new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(t)); }

        // Ids are checked against their real formats, so a typo cannot break the page or inject markup.
        public static string Problem(TrackingSettings t)
        {
            if (Bad(t.Ga4, @"^G-[A-Z0-9]{4,20}$")) return "The Google Analytics ID looks like G-XXXXXXXXXX.";
            if (Bad(t.Gtm, @"^GTM-[A-Z0-9]{4,12}$")) return "The Tag Manager ID looks like GTM-XXXXXXX.";
            if (Bad(t.GoogleAds, @"^AW-[0-9]{6,15}$")) return "The Google Ads ID looks like AW-123456789.";
            if (Bad(t.MetaPixel, @"^[0-9]{6,20}$")) return "The Meta Pixel ID is a number, for example 123456789012345.";
            if (Bad(t.LinkedIn, @"^[0-9]{3,12}$")) return "The LinkedIn Partner ID is a number, for example 1234567.";
            if (Bad(t.Clarity, @"^[a-z0-9]{6,16}$")) return "The Microsoft Clarity ID is letters and numbers, for example abcd1234ef.";
            if (Bad(t.GoogleVerify, @"^[A-Za-z0-9_\-]{10,100}$")) return "Paste only the content value of the Google verification tag.";
            if (Bad(t.BingVerify, @"^[A-Za-z0-9]{10,64}$")) return "Paste only the content value of the Bing verification tag.";
            return null;
        }

        private static bool Bad(string v, string pattern) { return !string.IsNullOrEmpty(v) && !Regex.IsMatch(v, pattern); }

        // ---- Rendering ---------------------------------------------------------------------------------------

        private static TrackingSettings Cached
        {
            get
            {
                var t = HttpRuntime.Cache["tracking.parsed"] as TrackingSettings;
                if (t != null) return t;
                t = Load();
                HttpRuntime.Cache.Insert("tracking.parsed", t, null, DateTime.UtcNow.AddMinutes(2), System.Web.Caching.Cache.NoSlidingExpiration);
                return t;
            }
        }

        public static void Invalidate() { HttpRuntime.Cache.Remove("tracking.parsed"); }

        public static string Head(string path) { return Safe(() => Render("head", path)); }
        public static string BodyStart(string path) { return Safe(() => Render("body-start", path)); }
        public static string BodyEnd(string path) { return Safe(() => Render("body-end", path)); }

        private static string Safe(Func<string> f) { try { return f(); } catch { return ""; } }

        private static string Render(string placement, string path)
        {
            var t = Cached;
            var sb = new StringBuilder();
            if (placement == "head")
            {
                if (!string.IsNullOrEmpty(t.GoogleVerify)) sb.Append("<meta name=\"google-site-verification\" content=\"" + t.GoogleVerify + "\" />\n");
                if (!string.IsNullOrEmpty(t.BingVerify)) sb.Append("<meta name=\"msvalidate.01\" content=\"" + t.BingVerify + "\" />\n");

                // gtag.js serves Google Analytics and Google Ads; it starts with whichever kind of consent comes first.
                var gtagId = !string.IsNullOrEmpty(t.Ga4) ? t.Ga4 : t.GoogleAds;
                if (!string.IsNullOrEmpty(gtagId))
                {
                    var cfg = "";
                    if (!string.IsNullOrEmpty(t.Ga4)) cfg += "gtag('config','" + t.Ga4 + "');";
                    if (!string.IsNullOrEmpty(t.GoogleAds)) cfg += "gtag('config','" + t.GoogleAds + "');";
                    var gtag = "<script async src=\"https://www.googletagmanager.com/gtag/js?id=" + gtagId + "\"></script>"
                             + "<script>window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments);}gtag('js',new Date());" + cfg + "</script>";
                    sb.Append(Gate(string.IsNullOrEmpty(t.Ga4) ? "marketing" : "analytics", gtag));
                }
                if (!string.IsNullOrEmpty(t.Gtm))
                    sb.Append(Gate("analytics", "<script>(function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src='https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);})(window,document,'script','dataLayer','" + t.Gtm + "');</script>"));
                if (!string.IsNullOrEmpty(t.Clarity))
                    sb.Append(Gate("analytics", "<script>(function(c,l,a,r,i,t,y){c[a]=c[a]||function(){(c[a].q=c[a].q||[]).push(arguments)};t=l.createElement(r);t.async=1;t.src='https://www.clarity.ms/tag/'+i;y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y);})(window,document,'clarity','script','" + t.Clarity + "');</script>"));
                if (!string.IsNullOrEmpty(t.MetaPixel))
                    sb.Append(Gate("marketing", "<script>!function(f,b,e,v,n,t,s){if(f.fbq)return;n=f.fbq=function(){n.callMethod?n.callMethod.apply(n,arguments):n.queue.push(arguments)};if(!f._fbq)f._fbq=n;n.push=n;n.loaded=!0;n.version='2.0';n.queue=[];t=b.createElement(e);t.async=!0;t.src=v;s=b.getElementsByTagName(e)[0];s.parentNode.insertBefore(t,s)}(window,document,'script','https://connect.facebook.net/en_US/fbevents.js');fbq('init','" + t.MetaPixel + "');fbq('track','PageView');</script>"));
                if (!string.IsNullOrEmpty(t.LinkedIn))
                    sb.Append(Gate("marketing", "<script>window._linkedin_partner_id='" + t.LinkedIn + "';window._linkedin_data_partner_ids=window._linkedin_data_partner_ids||[];window._linkedin_data_partner_ids.push(window._linkedin_partner_id);(function(l){if(!l){window.lintrk=function(a,b){window.lintrk.q.push([a,b])};window.lintrk.q=[]}var s=document.getElementsByTagName('script')[0];var b=document.createElement('script');b.type='text/javascript';b.async=true;b.src='https://snap.licdn.com/li/lms-analytics/insight.min.js';s.parentNode.insertBefore(b,s);})(window.lintrk);</script>"));
            }
            foreach (var s in t.Scripts.Where(x => x.Enabled && x.Placement == placement && !string.IsNullOrWhiteSpace(x.Code) && OnPage(x.Pages, path)))
                sb.Append("<!-- " + Regex.Replace(s.Name ?? "script", "[^A-Za-z0-9 ._-]", "") + " -->\n").Append(s.Category == "necessary" ? s.Code : Gate(s.Category, s.Code)).Append("\n");
            return sb.ToString();
        }

        /// <summary>Wraps markup so consent.js runs it only after the visitor accepts this kind of cookie.</summary>
        private static string Gate(string category, string html) { return "<template data-consent=\"" + category + "\">" + html + "</template>\n"; }

        public static bool OnPage(string pages, string path)
        {
            if (string.IsNullOrWhiteSpace(pages)) return true;
            path = (path ?? "/").ToLowerInvariant().TrimEnd('/');
            if (path == "") path = "/";
            foreach (var raw in pages.Split('\n'))
            {
                var p = raw.Trim().ToLowerInvariant();
                if (p == "") continue;
                if (p.EndsWith("*")) { if (path.StartsWith(p.TrimEnd('*').TrimEnd('/'))) return true; }
                else if (path == (p.TrimEnd('/') == "" ? "/" : p.TrimEnd('/'))) return true;
            }
            return false;
        }
    }
}
