using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

namespace Yenetch.Crm
{
    public class AuditCheck
    {
        public string Cat { get; set; }
        /// <summary>pass, warn or fail.</summary>
        public string Status { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
        public string Fix { get; set; }
        internal int Weight;
    }

    public class AuditResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string Host { get; set; }
        public string Url { get; set; }
        public int Score { get; set; }
        public string Headline { get; set; }
        public List<Dictionary<string, object>> Categories { get; set; }
        public List<AuditCheck> Checks { get; set; }
        public Dictionary<string, string> Stats { get; set; }
        public int Id { get; set; }
    }

    /// <summary>
    /// The free website audit (/website-audit). Fetches the visitor's home page the way a search engine does, runs 30+ checks across
    /// SEO, speed, mobile, security and social sharing, scores it out of 100, saves it as a lead and emails the report.
    /// Only public web addresses are fetched: every hop is resolved and checked, so the tool cannot be pointed at the server's own
    /// network (localhost, private ranges, cloud metadata).
    /// </summary>
    public static class Audits
    {
        private const int MaxBytes = 3 * 1024 * 1024;
        private const string Agent = "Mozilla/5.0 (compatible; YenetchAudit/1.0; +https://www.yenetch.com/website-audit)";
        private static readonly string[] CatOrder = { "seo", "speed", "mobile", "security", "social" };
        private static readonly Dictionary<string, string> CatNames = new Dictionary<string, string> { { "seo", "SEO" }, { "speed", "Speed" }, { "mobile", "Mobile" }, { "security", "Security" }, { "social", "Social sharing" } };
        private static readonly Dictionary<string, int> CatWeight = new Dictionary<string, int> { { "seo", 35 }, { "speed", 20 }, { "mobile", 15 }, { "security", 20 }, { "social", 10 } };

        // ---- Safe fetching -----------------------------------------------------------------------------------

        private class Page
        {
            public Uri FinalUri;
            public int Status;
            public string Html;
            public long Bytes;
            public long Ms;
            public WebHeaderCollection Headers;
            public string ContentEncoding;
        }

        /// <summary>Turns what the visitor typed into an http(s) address, or null.</summary>
        public static Uri Normalize(string input)
        {
            var s = (input ?? "").Trim();
            if (s.Length == 0 || s.Length > 300) return null;
            if (!Regex.IsMatch(s, "^https?://", RegexOptions.IgnoreCase)) s = "https://" + s;
            Uri u;
            if (!Uri.TryCreate(s, UriKind.Absolute, out u) || (u.Scheme != "http" && u.Scheme != "https")) return null;
            if (!u.IsDefaultPort || u.UserInfo != "" || u.HostNameType != UriHostNameType.Dns || !u.Host.Contains(".")) return null;
            return u;
        }

        private static bool IsPublic(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.Broadcast)) return false;
            var b = ip.GetAddressBytes();
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                if (b[0] == 10 || b[0] == 127 || b[0] == 0 || b[0] >= 224) return false;
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;
                if (b[0] == 192 && b[1] == 168) return false;
                if (b[0] == 169 && b[1] == 254) return false;          // link-local, cloud metadata
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false; // carrier-grade NAT
                if (b[0] == 192 && b[1] == 0 && b[2] == 0) return false;
                if (b[0] == 198 && (b[1] == 18 || b[1] == 19)) return false;
                return true;
            }
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return false;
                if ((b[0] & 0xFE) == 0xFC) return false; // unique local fc00::/7
                return true;
            }
            return false;
        }

        private static void CheckHost(Uri u)
        {
            if (u.Scheme != "http" && u.Scheme != "https") throw new AuditException("That link goes somewhere we cannot check.");
            if (!u.IsDefaultPort) throw new AuditException("Only websites on the standard web ports can be checked.");
            IPAddress literal;
            if (IPAddress.TryParse(u.Host, out literal)) throw new AuditException("Enter the website's name, not an IP address.");
            IPAddress[] ips;
            try { ips = Dns.GetHostAddresses(u.Host); }
            catch { throw new AuditException("We could not find " + u.Host + ". Check the address."); }
            if (ips.Length == 0 || ips.Any(ip => !IsPublic(ip))) throw new AuditException("That address is not a public website.");
        }

        private static Page Fetch(Uri start, int maxBytes, bool follow = true)
        {
            var u = start;
            var sw = Stopwatch.StartNew();
            for (var hop = 0; hop < 6; hop++)
            {
                CheckHost(u);
                var req = (HttpWebRequest)WebRequest.Create(u);
                req.AllowAutoRedirect = false;
                req.Timeout = 15000; req.ReadWriteTimeout = 15000;
                req.UserAgent = Agent;
                req.Accept = "text/html,application/xhtml+xml,*/*;q=0.8";
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.Headers["Accept-Language"] = "en-IN,en;q=0.9";
                HttpWebResponse res;
                try { res = (HttpWebResponse)req.GetResponse(); }
                catch (WebException ex) when (ex.Response != null) { res = (HttpWebResponse)ex.Response; }
                catch (WebException ex) { throw new AuditException(ex.Status == WebExceptionStatus.Timeout ? "The website took too long to answer (over 15 seconds)." : ex.Status == WebExceptionStatus.TrustFailure || ex.Status == WebExceptionStatus.SecureChannelFailure ? "The website's security certificate is not valid, so browsers would warn visitors too." : "We could not connect to the website."); }
                using (res)
                {
                    var code = (int)res.StatusCode;
                    if (follow && code >= 300 && code < 400 && !string.IsNullOrEmpty(res.Headers["Location"]))
                    {
                        Uri next;
                        if (!Uri.TryCreate(u, res.Headers["Location"], out next)) throw new AuditException("The website redirects to an invalid address.");
                        u = next;
                        continue;
                    }
                    var p = new Page { FinalUri = u, Status = code, Headers = res.Headers, ContentEncoding = res.ContentEncoding };
                    using (var stream = res.GetResponseStream())
                    using (var ms = new MemoryStream())
                    {
                        var buf = new byte[16384];
                        int n;
                        while ((n = stream.Read(buf, 0, buf.Length)) > 0 && ms.Length < maxBytes) ms.Write(buf, 0, n);
                        p.Bytes = ms.Length;
                        p.Ms = sw.ElapsedMilliseconds;
                        var enc = Encoding.UTF8;
                        try { if (!string.IsNullOrEmpty(res.CharacterSet)) enc = Encoding.GetEncoding(res.CharacterSet.Replace("\"", "")); } catch { }
                        p.Html = enc.GetString(ms.ToArray());
                    }
                    return p;
                }
            }
            throw new AuditException("The website redirects too many times.");
        }

        private static Page TryFetch(Uri u, int maxBytes, bool follow = true) { try { return Fetch(u, maxBytes, follow); } catch { return null; } }

        public class AuditException : Exception { public AuditException(string m) : base(m) { } }

        // ---- Checks ------------------------------------------------------------------------------------------

        public static AuditResult Run(string input)
        {
            var start = Normalize(input);
            if (start == null) return new AuditResult { Error = "Enter your website address, for example yourbusiness.com." };
            Page page;
            try { page = Fetch(start, MaxBytes); }
            catch (AuditException ex)
            {
                // Many sites still run on http only: try that before giving up.
                if (start.Scheme == "https" && !Regex.IsMatch(input ?? "", "^https://", RegexOptions.IgnoreCase))
                {
                    try { page = Fetch(new Uri("http://" + start.Host + start.PathAndQuery), MaxBytes); }
                    catch (AuditException) { return new AuditResult { Error = ex.Message }; }
                }
                else return new AuditResult { Error = ex.Message };
            }
            if (page.Status >= 400) return new AuditResult { Error = "The website answered with an error (" + page.Status + "). Check the address, or try again later." };

            var html = page.Html ?? "";
            var head = Section(html, "head");
            var body = Section(html, "body") ?? html;
            var checks = new List<AuditCheck>();
            Action<string, bool, int, string, string, string> add = (cat, ok, weight, t, detail, fix) =>
                checks.Add(new AuditCheck { Cat = cat, Status = ok ? "pass" : weight >= 6 ? "fail" : "warn", Title = t, Detail = ok ? null : detail, Fix = fix, Weight = weight });
            Action<string, string, int, string, string, string> addS = (cat, status, weight, t, detail, fix) =>
                checks.Add(new AuditCheck { Cat = cat, Status = status, Title = t, Detail = status == "pass" ? null : detail, Fix = fix, Weight = weight });
            var https = page.FinalUri.Scheme == "https";

            // --- SEO
            var title = Text(Match(head ?? html, @"<title[^>]*>(.*?)</title>"));
            if (string.IsNullOrEmpty(title)) add("seo", false, 10, "No page title", "The title is the blue headline in Google results and the text on the browser tab.", "Add a <title> of 50 to 60 characters with your main service and city.");
            else addS("seo", title.Length >= 15 && title.Length <= 65 ? "pass" : "warn", 3, title.Length >= 15 && title.Length <= 65 ? "Page title is a good length (" + title.Length + " characters)" : "Page title is " + (title.Length < 15 ? "too short" : "too long") + " (" + title.Length + " characters)",
                "Google shows about 60 characters. \"" + Util.Cut(title, 80) + "\"", "Aim for 50 to 60 characters: main service, city and brand.");
            var desc = Text(Meta(head ?? html, "name", "description"));
            if (string.IsNullOrEmpty(desc)) add("seo", false, 8, "No meta description", "Google shows a random snippet of your page instead of a summary you chose.", "Write a 140 to 160 character description that says what you offer, where, and why to choose you.");
            else addS("seo", desc.Length >= 70 && desc.Length <= 170 ? "pass" : "warn", 3, desc.Length >= 70 && desc.Length <= 170 ? "Meta description is a good length" : "Meta description is " + (desc.Length < 70 ? "short" : "long") + " (" + desc.Length + " characters)",
                "Descriptions of 140 to 160 characters get the most clicks.", "Rewrite it to 140 to 160 characters with a clear reason to click.");
            var h1s = Regex.Matches(body, @"<h1[\s>]", RegexOptions.IgnoreCase).Count;
            if (h1s == 0) add("seo", false, 6, "No main heading (H1)", "Every page needs one main heading that tells people and Google what it is about.", "Add one <h1> near the top with your main service.");
            else addS("seo", h1s == 1 ? "pass" : "warn", 3, h1s == 1 ? "Has one main heading (H1)" : "Has " + h1s + " main headings (H1)", "More than one H1 dilutes what the page is about.", "Keep one H1 and turn the others into H2 subheadings.");
            add("seo", Regex.IsMatch(body, @"<h2[\s>]", RegexOptions.IgnoreCase), 2, "Uses subheadings (H2)", "Subheadings help people scan and help Google understand sections.", "Break the page into sections with H2 headings.");
            var imgs = Regex.Matches(body, @"<img\b[^>]*>", RegexOptions.IgnoreCase).Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToList();
            var noAlt = imgs.Count(i => !Regex.IsMatch(i, @"\balt\s*=", RegexOptions.IgnoreCase));
            add("seo", imgs.Count == 0 || noAlt * 5 <= imgs.Count, 4, noAlt == 0 ? "Images have alt text" : noAlt + " of " + imgs.Count + " images have no alt text",
                "Alt text describes images to Google Images and to people using screen readers.", "Add a short description in the alt attribute of each meaningful image.");
            add("seo", Regex.IsMatch(head ?? html, @"<link[^>]+rel\s*=\s*[""']?canonical", RegexOptions.IgnoreCase), 3, "Has a canonical address", "Without it, Google may treat www, non-www and tracking links as duplicate pages.", "Add <link rel=\"canonical\" href=\"https://yoursite.com/\">.");
            var robotsMeta = (Meta(head ?? html, "name", "robots") ?? "") + " " + (page.Headers["X-Robots-Tag"] ?? "");
            add("seo", robotsMeta.IndexOf("noindex", StringComparison.OrdinalIgnoreCase) < 0, 10, "Page can appear in Google", "The page tells search engines not to index it (noindex), so it cannot show in Google.", "Remove noindex from the robots meta tag or the X-Robots-Tag header.");
            add("seo", Regex.IsMatch(html, @"<html[^>]+\blang\s*=", RegexOptions.IgnoreCase), 2, "Declares its language", "The language tag helps Google show the page to the right audience.", "Add lang=\"en\" (or your language) to the <html> tag.");
            add("seo", html.IndexOf("application/ld+json", StringComparison.OrdinalIgnoreCase) >= 0 || html.IndexOf("itemtype=", StringComparison.OrdinalIgnoreCase) >= 0, 3, "Has structured data", "Structured data (schema.org) helps Google show rich results such as ratings, FAQs and your business details.", "Add Organization or LocalBusiness JSON-LD with your name, address and phone.");
            var words = Regex.Matches(Text(Regex.Replace(body, @"<(script|style|noscript)[^>]*>.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase)) ?? "", @"[\p{L}\p{N}]{2,}").Count;
            add("seo", words >= 250, 3, words >= 250 ? "Enough text for Google (" + words + " words)" : "Thin content (" + words + " words)", "Pages with very little text rarely rank.", "Add at least 300 words that answer what customers ask: services, prices, areas served, FAQs.");
            add("seo", Regex.IsMatch(head ?? html, @"<link[^>]+rel\s*=\s*[""']?[^""'>]*icon", RegexOptions.IgnoreCase), 1, "Has a favicon", "The small icon shows in browser tabs and in Google's mobile results.", "Add a favicon (a square PNG or SVG of your logo).");
            var origin = new Uri(page.FinalUri.GetLeftPart(UriPartial.Authority));
            var robots = TryFetch(new Uri(origin, "/robots.txt"), 200000);
            add("seo", robots != null && robots.Status == 200 && !Regex.IsMatch(robots.Html ?? "", "<html", RegexOptions.IgnoreCase), 2, "Has a robots.txt file", "robots.txt tells search engines what to crawl and where your sitemap is.", "Add /robots.txt with a Sitemap line.");
            var robotsBlocksAll = robots != null && robots.Status == 200 && Regex.IsMatch(robots.Html ?? "", @"User-agent:\s*\*\s*[\r\n]+(?:(?!User-agent)[^\n]*\n)*?\s*Disallow:\s*/\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (robotsBlocksAll) add("seo", false, 10, "robots.txt blocks search engines", "Your robots.txt tells every search engine to stay away from the whole site.", "Remove \"Disallow: /\" for User-agent: *.");
            var sitemapUrl = robots != null ? Match(robots.Html ?? "", @"(?im)^\s*Sitemap:\s*(\S+)") : null;
            Uri sm;
            var sitemap = TryFetch(sitemapUrl != null && Uri.TryCreate(sitemapUrl, UriKind.Absolute, out sm) ? sm : new Uri(origin, "/sitemap.xml"), 500000);
            add("seo", sitemap != null && sitemap.Status == 200 && Regex.IsMatch(sitemap.Html ?? "", "<(urlset|sitemapindex)", RegexOptions.IgnoreCase), 3, "Has an XML sitemap", "A sitemap helps Google find every page, especially new ones.", "Create /sitemap.xml and submit it in Google Search Console.");

            // --- Speed
            var ms = page.Ms;
            addS("speed", ms < 800 ? "pass" : ms < 1800 ? "warn" : "fail", 8, "Server responds in " + (ms / 1000.0).ToString("0.0") + " s", "Slow first responses make every visit slower, and Google uses speed in rankings.", "Use good hosting, turn on caching and use a CDN.");
            var kb = page.Bytes / 1024;
            add("speed", kb <= 150, 3, "HTML size " + kb + " KB", "Large HTML takes longer to download, especially on mobile data.", "Remove inline code and unused page builders; keep HTML under 150 KB.");
            add("speed", !string.IsNullOrEmpty(page.ContentEncoding) && Regex.IsMatch(page.ContentEncoding, "gzip|br|deflate", RegexOptions.IgnoreCase), 4, "Text is compressed", "Compression (gzip or Brotli) makes pages about 70% smaller to download.", "Turn on gzip or Brotli compression on the server.");
            var scripts = Regex.Matches(html, @"<script\b[^>]*\bsrc\s*=", RegexOptions.IgnoreCase).Count;
            var styles = Regex.Matches(html, @"<link\b[^>]*rel\s*=\s*[""']?stylesheet", RegexOptions.IgnoreCase).Count;
            add("speed", scripts <= 15, 3, scripts + " script files", "Each script file is another download that can slow the page.", "Remove unused plugins and combine scripts.");
            add("speed", styles <= 6, 2, styles + " stylesheet files", "Many stylesheets delay the first paint.", "Combine stylesheets and remove unused ones.");
            var blocking = Regex.Matches(head ?? "", @"<script\b[^>]*\bsrc\s*=[^>]*>", RegexOptions.IgnoreCase).Cast<System.Text.RegularExpressions.Match>().Count(m => !Regex.IsMatch(m.Value, @"\b(async|defer|type\s*=\s*[""']?module)", RegexOptions.IgnoreCase));
            add("speed", blocking <= 2, 3, blocking == 0 ? "No render-blocking scripts" : blocking + " render-blocking scripts in the head", "Scripts in the head without async or defer stop the page from showing until they load.", "Add defer to scripts, or move them to the end of the page.");
            var lazy = imgs.Count(i => Regex.IsMatch(i, @"loading\s*=\s*[""']?lazy", RegexOptions.IgnoreCase));
            add("speed", imgs.Count <= 6 || lazy * 2 >= imgs.Count - 3, 2, "Images load lazily", "Loading every image up front slows the first view.", "Add loading=\"lazy\" to images below the fold.");
            var noSize = imgs.Count(i => !Regex.IsMatch(i, @"\bwidth\s*=", RegexOptions.IgnoreCase) || !Regex.IsMatch(i, @"\bheight\s*=", RegexOptions.IgnoreCase));
            add("speed", imgs.Count == 0 || noSize * 2 <= imgs.Count, 2, "Images have set sizes", "Images without width and height make the page jump while loading (layout shift).", "Add width and height attributes to images.");

            // --- Mobile
            var viewport = Meta(head ?? html, "name", "viewport");
            add("mobile", !string.IsNullOrEmpty(viewport), 10, "Set up for mobile screens", "Without a viewport tag, phones show a tiny desktop page. Most visitors are on mobile.", "Add <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">.");
            if (!string.IsNullOrEmpty(viewport))
                add("mobile", !Regex.IsMatch(viewport, @"user-scalable\s*=\s*(no|0)|maximum-scale\s*=\s*1(\.0)?\b", RegexOptions.IgnoreCase), 2, "Visitors can zoom", "Blocking zoom makes text hard to read for many people.", "Remove user-scalable=no and maximum-scale=1 from the viewport tag.");
            add("mobile", Regex.IsMatch(head ?? html, @"apple-touch-icon", RegexOptions.IgnoreCase), 1, "Has a home screen icon", "Shown when someone saves your site to their phone's home screen.", "Add an apple-touch-icon (180 x 180 PNG).");
            add("mobile", Regex.IsMatch(body, @"href\s*=\s*[""']?(tel:|https?://(wa\.me|api\.whatsapp\.com))", RegexOptions.IgnoreCase), 2, "Tap to call or WhatsApp", "Mobile visitors expect to call or message you with one tap.", "Link your phone number with tel: and add a WhatsApp button.");

            // --- Security
            add("security", https, 10, https ? "Served over HTTPS" : "Not secure (no HTTPS)", "Browsers mark the site as Not secure and Google ranks it lower.", "Install a free SSL certificate (Let's Encrypt) and redirect all traffic to https.");
            if (https)
            {
                var plain = TryFetch(new Uri("http://" + page.FinalUri.Host + "/"), 50000, false);
                add("security", plain == null || (plain.Status >= 300 && plain.Status < 400 && (plain.Headers["Location"] ?? "").StartsWith("https", StringComparison.OrdinalIgnoreCase)), 4, "http redirects to https", "People who type your address without https stay on the insecure version.", "Redirect every http request to https.");
                add("security", !string.IsNullOrEmpty(page.Headers["Strict-Transport-Security"]), 2, "Uses HSTS", "HSTS tells browsers to always use the secure version.", "Add the Strict-Transport-Security header.");
                var mixed = Regex.Matches(html, @"(src|href)\s*=\s*[""']http://[^""']+\.(js|css|png|jpe?g|gif|webp|svg)", RegexOptions.IgnoreCase).Count;
                add("security", mixed == 0, 4, mixed == 0 ? "No insecure files on a secure page" : mixed + " files load over insecure http", "Browsers block or warn about insecure files on a secure page.", "Change those links to https.");
            }
            add("security", !string.IsNullOrEmpty(page.Headers["X-Content-Type-Options"]) || !string.IsNullOrEmpty(page.Headers["Content-Security-Policy"]), 2, "Has basic security headers", "Security headers protect visitors from some common attacks.", "Add X-Content-Type-Options: nosniff and X-Frame-Options: SAMEORIGIN.");
            var gen = Meta(head ?? html, "name", "generator") ?? "";
            add("security", !Regex.IsMatch(gen, @"\d+\.\d+"), 1, "Does not reveal software versions", "Showing the exact version (" + gen + ") helps attackers find known holes.", "Hide the generator tag and keep the software updated.");

            // --- Social
            add("social", Meta(head ?? html, "property", "og:title") != null, 3, "Has a sharing title (og:title)", "Controls the headline when your link is shared on WhatsApp, LinkedIn and Facebook.", "Add an og:title tag.");
            add("social", Meta(head ?? html, "property", "og:description") != null, 2, "Has a sharing description", "Shown under the headline in shared links.", "Add an og:description tag.");
            add("social", Meta(head ?? html, "property", "og:image") != null, 4, "Has a sharing image (og:image)", "Links shared without an image get far fewer clicks.", "Add an og:image tag with a 1200 x 630 image.");
            add("social", Meta(head ?? html, "name", "twitter:card") != null, 2, "Has a Twitter/X card", "Controls how the link looks on X.", "Add <meta name=\"twitter:card\" content=\"summary_large_image\">.");

            // --- Score
            var cats = new List<Dictionary<string, object>>();
            double total = 0;
            foreach (var c in CatOrder)
            {
                var list = checks.Where(x => x.Cat == c).ToList();
                var max = list.Sum(x => x.Weight);
                var lost = list.Sum(x => x.Status == "fail" ? x.Weight : x.Status == "warn" ? x.Weight * 0.6 : 0);
                var score = max == 0 ? 100 : (int)Math.Round(100 * (1 - lost / max));
                cats.Add(new Dictionary<string, object> { { "key", c }, { "name", CatNames[c] }, { "score", score } });
                total += score * CatWeight[c];
            }
            var overall = (int)Math.Round(total / CatWeight.Values.Sum());
            var order = new Dictionary<string, int> { { "fail", 0 }, { "warn", 1 }, { "pass", 2 } };
            checks = checks.OrderBy(x => order[x.Status]).ThenByDescending(x => x.Weight).ToList();
            var imgsTotal = imgs.Count;
            return new AuditResult
            {
                Ok = true, Host = page.FinalUri.Host, Url = page.FinalUri.ToString(), Score = overall, Categories = cats, Checks = checks,
                Headline = overall >= 90 ? "Excellent. Your website is in great shape." : overall >= 75 ? "Good, with a few quick wins left." : overall >= 55 ? "Good start, with a few things holding you back." : "Your website is losing visitors and rankings. Let's fix that.",
                Stats = new Dictionary<string, string> { { "time", (ms / 1000.0).ToString("0.0") + " s" }, { "size", kb + " KB HTML" }, { "scripts", scripts.ToString() }, { "images", imgsTotal.ToString() } }
            };
        }

        // ---- HTML helpers ------------------------------------------------------------------------------------

        private static string Section(string html, string tag)
        {
            var m = Regex.Match(html, "<" + tag + @"\b[^>]*>(.*?)</" + tag + ">", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Match(string s, string pattern)
        {
            var m = Regex.Match(s ?? "", pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>content of &lt;meta {attr}="{name}" content="..."&gt;, in either attribute order.</summary>
        private static string Meta(string html, string attr, string name)
        {
            foreach (System.Text.RegularExpressions.Match m in Regex.Matches(html ?? "", @"<meta\b[^>]*>", RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(m.Value, attr + @"\s*=\s*[""']?" + Regex.Escape(name) + @"[""'\s/>]", RegexOptions.IgnoreCase)) continue;
                var c = Regex.Match(m.Value, @"content\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))", RegexOptions.IgnoreCase);
                if (c.Success) return HttpUtility.HtmlDecode(c.Groups[1].Success ? c.Groups[1].Value : c.Groups[2].Success ? c.Groups[2].Value : c.Groups[3].Value).Trim();
                return "";
            }
            return null;
        }

        private static string Text(string html)
        {
            if (html == null) return null;
            return Regex.Replace(HttpUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), @"\s+", " ").Trim();
        }

        // ---- Saving, lead and emails -------------------------------------------------------------------------

        public static AuditResult RunAndSave(string url, string name, string email, string phone, string visitorId, string ip)
        {
            var r = Run(url);
            if (!r.Ok) return r;
            var json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(r);
            var token = Util.NewId();
            r.Id = Db.Insert("INSERT INTO WebAudits (Url, FinalUrl, Name, Email, Phone, Score, ResultJson, Token, VisitorId, Ip, CreatedOn) VALUES (@url, @final, @name, @email, @phone, @score, @json, @token, @vid, @ip, @now)",
                new { url = Util.Cut(url, 400), final = Util.Cut(r.Url, 400), name = Blank(name, 120), email = email.Trim().ToLowerInvariant(), phone = Blank(phone, 40), score = r.Score, json, token, vid = Blank(visitorId, 40), ip = Blank(ip, 64), now = DateTime.UtcNow });
            var top = r.Checks.Where(c => c.Status == "fail").Take(5).Select(c => "- " + c.Title).ToList();
            try
            {
                var lead = new Lead
                {
                    Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim(), Email = email.Trim(), Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                    Interest = "Website audit / SEO", LeadType = "Marketing", Source = "Website audit", Priority = r.Score < 70 ? "Hot" : "Warm", VisitorId = visitorId, Page = "/website-audit",
                    Need = "Ran the free website audit for " + r.Host + ": score " + r.Score + "/100." + (top.Count > 0 ? "\nTop issues:\n" + string.Join("\n", top) : "")
                };
                var leadId = LeadService.CreateFromTool(lead, "Ran the free website audit for " + r.Host + " (score " + r.Score + "/100).");
                Db.Exec("UPDATE WebAudits SET LeadId = @leadId WHERE Id = @id", new { leadId, id = r.Id });
                Automations.Enroll("lead", lead.Email, lead.Name, leadId);
                NotifyTeam(r, lead, leadId);
            }
            catch (Exception ex) { Mailer.Log("audit lead " + r.Id, ex); }
            EmailReport(r, name, email);
            return r;
        }

        private static object Blank(string s, int max) { return string.IsNullOrWhiteSpace(s) ? null : Util.Cut(s.Trim(), max); }

        private static string ReportHtml(AuditResult r)
        {
            Func<int, string> color = s => s >= 85 ? "#1A9F5B" : s >= 60 ? "#B26A00" : "#D93025";
            var sb = new StringBuilder();
            sb.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;margin:0 0 20px\"><tr><td style=\"width:110px;vertical-align:middle\"><div style=\"width:96px;height:96px;border-radius:50%;border:6px solid " + color(r.Score) + ";text-align:center;line-height:84px;font-size:34px;font-weight:700;color:#1d1d1f\">" + r.Score + "</div></td>");
            sb.Append("<td style=\"vertical-align:middle;font-size:15px;color:#48484e\"><b style=\"color:#1d1d1f;font-size:17px\">" + Util.H(r.Headline) + "</b><br>Score out of 100 for " + Util.H(r.Host) + "</td></tr></table>");
            sb.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;margin:0 0 20px\">");
            foreach (var c in r.Categories)
            {
                var s = Convert.ToInt32(c["score"]);
                sb.Append("<tr><td style=\"padding:6px 12px 6px 0;font-size:14px;width:120px\">" + Util.H(Convert.ToString(c["name"])) + "</td><td style=\"padding:6px 0\"><div style=\"background:#f0f0f2;border-radius:99px;height:8px\"><div style=\"width:" + s + "%;background:" + color(s) + ";height:8px;border-radius:99px\"></div></div></td><td style=\"padding:6px 0 6px 12px;font-size:14px;font-weight:600;width:36px;text-align:right\">" + s + "</td></tr>");
            }
            sb.Append("</table>");
            var issues = r.Checks.Where(c => c.Status != "pass").Take(10).ToList();
            if (issues.Count > 0)
            {
                sb.Append("<h2 style=\"font-size:18px;margin:0 0 10px\">What to fix</h2>");
                foreach (var c in issues)
                    sb.Append("<div style=\"padding:12px 14px;margin:0 0 8px;border-radius:12px;background:" + (c.Status == "fail" ? "#fdecea" : "#fff6e5") + "\"><b style=\"font-size:15px\">" + Util.H(c.Title) + "</b>"
                              + (string.IsNullOrEmpty(c.Detail) ? "" : "<div style=\"font-size:14px;color:#48484e;margin-top:4px\">" + Util.H(c.Detail) + "</div>")
                              + (string.IsNullOrEmpty(c.Fix) ? "" : "<div style=\"font-size:14px;margin-top:4px\"><b>Fix:</b> " + Util.H(c.Fix) + "</div>") + "</div>");
            }
            sb.Append("<p style=\"margin:16px 0 0;font-size:14px;color:#48484e\">" + r.Checks.Count(c => c.Status == "pass") + " checks passed.</p>");
            return sb.ToString();
        }

        private static void EmailReport(AuditResult r, string name, string email)
        {
            try
            {
                var first = string.IsNullOrWhiteSpace(name) ? "" : ", " + Util.H(name.Trim().Split(' ')[0]);
                var body = Mailer.Heading("Your website audit" + first) + "<p style=\"margin:0 0 20px\">Here is the free audit for <b>" + Util.H(r.Host) + "</b>.</p>" + ReportHtml(r)
                         + "<h2 style=\"font-size:18px;margin:24px 0 8px\">Want these fixed?</h2><p style=\"margin:0 0 8px\">Book a free 30-minute call. A specialist will walk you through the report and the fixes that will move your rankings and leads most.</p>"
                         + Mailer.Button("Book a free call", Mailer.SiteUrl + "/book?topic=SEO");
                Mailer.Send(email.Trim(), "Your website audit: " + r.Score + "/100 for " + r.Host, Mailer.Wrap("Your website scored " + r.Score + " out of 100.", body, null));
            }
            catch (Exception ex) { Mailer.Log("audit report " + r.Id, ex); }
        }

        private static void NotifyTeam(AuditResult r, Lead l, int leadId)
        {
            var inbox = Mailer.LeadInbox;
            if (string.IsNullOrEmpty(inbox)) return;
            try
            {
                var body = Mailer.Heading("Website audit: " + Util.H(r.Host) + " scored " + r.Score) + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%;margin:0 0 16px\">"
                         + Mailer.Row("Name", l.Name) + Mailer.Row("Email", l.Email) + Mailer.Row("Phone", l.Phone) + Mailer.Row("Website", r.Url) + "</table>" + ReportHtml(r)
                         + Mailer.Button("Open lead", Mailer.SiteUrl + "/admin/leads/" + leadId);
                Mailer.Send(inbox, "Website audit lead: " + r.Host + " (" + r.Score + "/100)", Mailer.Wrap("New website audit lead", body, null), null,
                    m => { if (Newsletter.IsEmail(l.Email)) { m.ReplyToList.Clear(); m.ReplyToList.Add(new System.Net.Mail.MailAddress(l.Email, l.Name)); } });
            }
            catch (Exception ex) { Mailer.Log("audit team " + r.Id, ex); }
        }

        public static List<Row> List(string q, int offset, int count, out int total)
        {
            var where = string.IsNullOrWhiteSpace(q) ? "" : " WHERE Url LIKE @q OR Email LIKE @q OR Name LIKE @q";
            var args = new { q = "%" + (q ?? "").Trim() + "%" };
            total = Db.Scalar<int>("SELECT COUNT(*) FROM WebAudits" + where, args);
            return Db.Rows("SELECT Id, Url, FinalUrl, Name, Email, Phone, Score, LeadId, CreatedOn FROM WebAudits" + where + " ORDER BY Id DESC" + Db.Page(offset, count), args);
        }

        public static AuditResult Get(int id, out Row row)
        {
            row = Db.First("SELECT * FROM WebAudits WHERE Id = @id", new { id });
            if (row == null) return null;
            try { var r = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<AuditResult>(row.Str("ResultJson")); r.Id = id; return r; } catch { return null; }
        }
    }
}
