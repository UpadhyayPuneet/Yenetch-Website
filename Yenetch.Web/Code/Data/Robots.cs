using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Yenetch.Data
{
    public class RobotsGroup
    {
        /// <summary>Crawler names, one per line (* for all).</summary>
        public string Agents { get; set; }
        public string Allow { get; set; }
        public string Disallow { get; set; }
        public int CrawlDelay { get; set; }
    }

    public class RobotsSettings
    {
        /// <summary>rules (built from the groups below) or custom (the text as typed).</summary>
        public string Mode { get; set; }
        public List<RobotsGroup> Groups { get; set; }
        public bool BlockAi { get; set; }
        public bool BlockAll { get; set; }
        /// <summary>Keeps test copies (any address other than SiteUrl) out of search results.</summary>
        public bool HideTestSites { get; set; }
        public bool Sitemap { get; set; }
        public string ExtraSitemaps { get; set; }
        public string Custom { get; set; }
    }

    /// <summary>robots.txt, edited in /admin/robots and served by Handlers/RobotsHandler.</summary>
    public static class Robots
    {
        /// <summary>Crawlers that collect pages for training AI models (search engines are not affected).</summary>
        public static readonly string[] AiBots = { "GPTBot", "ChatGPT-User", "CCBot", "Google-Extended", "anthropic-ai", "ClaudeBot", "Claude-Web", "PerplexityBot", "Bytespider", "Amazonbot", "Applebot-Extended", "meta-externalagent", "cohere-ai", "Diffbot", "Omgilibot" };

        public static RobotsSettings Default()
        {
            return new RobotsSettings
            {
                Mode = "rules", HideTestSites = true, Sitemap = true,
                Groups = new List<RobotsGroup> { new RobotsGroup { Agents = "*", Allow = "/", Disallow = "/admin\n/Admin/\n/Handlers/\n/App_Data/\n/api/\n/newsletter/\n/book/cancel" } }
            };
        }

        public static RobotsSettings Load()
        {
            try
            {
                var json = Settings.Get("robots");
                var r = string.IsNullOrEmpty(json) ? null : new JavaScriptSerializer().Deserialize<RobotsSettings>(json);
                if (r != null) { if (r.Groups == null) r.Groups = new List<RobotsGroup>(); return r; }
            }
            catch { }
            return Default();
        }

        public static void Save(RobotsSettings r) { Settings.Set("robots", new JavaScriptSerializer().Serialize(r)); }

        /// <summary>The file for a request to this host. isTestHost: the host is not the SiteUrl host.</summary>
        public static string Render(RobotsSettings r, bool isTestHost)
        {
            var root = Seo.Root;
            if (r.BlockAll || (r.HideTestSites && isTestHost))
                return "# This copy of the site is not meant for search engines.\nUser-agent: *\nDisallow: /\n";
            var sb = new StringBuilder();
            if (r.Mode == "custom" && !string.IsNullOrWhiteSpace(r.Custom)) sb.Append(r.Custom.Replace("\r\n", "\n").TrimEnd()).Append("\n");
            else
            {
                foreach (var g in r.Groups)
                {
                    var agents = Lines(g.Agents);
                    if (agents.Count == 0) agents.Add("*");
                    foreach (var a in agents) sb.Append("User-agent: ").Append(a).Append("\n");
                    foreach (var p in Lines(g.Allow)) sb.Append("Allow: ").Append(p).Append("\n");
                    foreach (var p in Lines(g.Disallow)) sb.Append("Disallow: ").Append(p).Append("\n");
                    if (g.CrawlDelay > 0) sb.Append("Crawl-delay: ").Append(g.CrawlDelay).Append("\n");
                    sb.Append("\n");
                }
                if (r.BlockAi)
                {
                    sb.Append("# AI training crawlers\n");
                    foreach (var b in AiBots) sb.Append("User-agent: ").Append(b).Append("\n");
                    sb.Append("Disallow: /\n\n");
                }
            }
            if (r.Sitemap) sb.Append("Sitemap: ").Append(root).Append("/sitemap.xml\n");
            foreach (var s in Lines(r.ExtraSitemaps)) sb.Append("Sitemap: ").Append(s).Append("\n");
            return sb.ToString().TrimEnd() + "\n";
        }

        private static List<string> Lines(string s)
        {
            return (s ?? "").Split('\n').Select(x => new string(x.Where(ch => ch >= ' ').ToArray()).Trim()).Where(x => x.Length > 0 && x.Length < 300).ToList();
        }

        /// <summary>Paths must start with / (or be a full URL for sitemaps); anything else is a typo.</summary>
        public static string Problem(RobotsSettings r)
        {
            foreach (var g in r.Groups)
                foreach (var p in (g.Allow ?? "").Split('\n').Concat((g.Disallow ?? "").Split('\n')).Select(x => x.Trim()).Where(x => x != ""))
                    if (!p.StartsWith("/") && !p.StartsWith("*")) return "Each Allow or Disallow line must start with /, for example /admin. Check \"" + p + "\".";
            foreach (var s in (r.ExtraSitemaps ?? "").Split('\n').Select(x => x.Trim()).Where(x => x != ""))
                if (!s.StartsWith("https://") && !s.StartsWith("http://")) return "Extra sitemaps must be full addresses starting with https://.";
            return null;
        }
    }
}
