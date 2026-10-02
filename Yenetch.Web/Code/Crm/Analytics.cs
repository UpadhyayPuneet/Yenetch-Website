using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

namespace Yenetch.Crm
{
    /// <summary>
    /// First-party analytics. Browsers send hits to Handlers/Pulse.ashx (assets/js/consent.js).
    /// With analytics consent a visitor cookie (yn_vid) and session cookie (yn_sid) tie pages into sessions and visitors, and the IP is stored.
    /// Without consent, pageviews are counted anonymously: no cookie, no IP, one anonymous session per entry from another site.
    /// </summary>
    public static class Analytics
    {
        public const string VisitorCookie = "yn_vid";
        public const string SessionCookie = "yn_sid";
        public const string ConsentCookie = "yn_consent";

        private static readonly Regex Bot = new Regex(@"bot|crawl|spider|slurp|facebookexternalhit|headless|lighthouse|pingdom|uptime|monitor|curl|wget|python|java/|go-http|preview|scan", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex IdRx = new Regex("^[a-f0-9]{32}$", RegexOptions.Compiled);

        public class Hit
        {
            public string k { get; set; }   // pv | end | ev
            public int c { get; set; }      // 1 = analytics consent
            public string id { get; set; }  // pageview id
            public string p { get; set; }   // path
            public string q { get; set; }   // query string (UTM parameters)
            public string t { get; set; }   // title
            public string r { get; set; }   // referrer
            public int sw { get; set; }
            public int sh { get; set; }
            public string l { get; set; }   // language
            public string tz { get; set; }  // time zone
            public int d { get; set; }      // seconds on page
            public int sc { get; set; }     // max scroll %
            public string n { get; set; }   // event name
            public string lb { get; set; }  // event label
        }

        public static bool IsBot(string ua) { return string.IsNullOrEmpty(ua) || Bot.IsMatch(ua); }

        public static string CookieId(HttpRequest req, string name)
        {
            var c = req.Cookies[name];
            return c != null && IdRx.IsMatch(c.Value ?? "") ? c.Value : null;
        }

        public static bool HasConsent(HttpRequest req)
        {
            var c = req.Cookies[ConsentCookie];
            return c != null && (c.Value ?? "").Contains("a1");
        }

        /// <summary>Handles one hit from the browser. Never throws to the caller.</summary>
        public static void Record(HttpContext ctx, string json)
        {
            var req = ctx.Request;
            if (IsBot(req.UserAgent)) return;
            Hit h;
            try { h = new JavaScriptSerializer().Deserialize<Hit>(json); } catch { return; }
            if (h == null || string.IsNullOrEmpty(h.k)) return;

            var consent = h.c == 1 && HasConsent(req);
            var vid = consent ? CookieId(req, VisitorCookie) : null;
            var sid = consent ? CookieId(req, SessionCookie) : null;
            if (vid == null || sid == null) { consent = false; vid = null; sid = null; }
            var now = DateTime.UtcNow;
            var path = Util.Cut(CleanPath(h.p), 300);
            var pvid = h.id != null && IdRx.IsMatch(h.id) ? h.id : null;

            switch (h.k)
            {
                case "pv":
                    if (path == null || pvid == null) return;
                    if (consent) TouchSession(ctx, h, vid, sid, path, now);
                    else if (IsEntry(req, h.r)) CreateSession(ctx, h, null, Util.NewId(), path, now, false);
                    // A visitor who accepts cookies on the first page re-sends that page: attach the anonymous view to the new session.
                    if (consent && Db.Exec("UPDATE WebPageviews SET SessionId = @sid, VisitorId = @vid WHERE Id = @id", new { sid, vid, id = pvid }) > 0) break;
                    try
                    {
                        Db.Exec("INSERT INTO WebPageviews (Id, SessionId, VisitorId, ViewedOn, Path, Title) VALUES (@id, @sid, @vid, @now, @path, @title)",
                            new { id = pvid, sid, vid, now, path, title = Util.Cut(h.t, 200) });
                    }
                    catch { /* repeated beacon */ }
                    break;

                case "end":
                    if (pvid == null) return;
                    Db.Exec("UPDATE WebPageviews SET DurationSec = @d, ScrollPct = @sc WHERE Id = @id",
                        new { d = Math.Max(0, Math.Min(h.d, 3600)), sc = Math.Max(0, Math.Min(h.sc, 100)), id = pvid });
                    if (consent) Db.Exec("UPDATE WebSessions SET LastOn = @now WHERE Id = @sid", new { now, sid });
                    break;

                case "ev":
                    if (string.IsNullOrEmpty(h.n)) return;
                    Db.Exec("INSERT INTO WebEvents (SessionId, VisitorId, OccurredOn, Name, Label, Path) VALUES (@sid, @vid, @now, @name, @label, @path)",
                        new { sid, vid, now, name = Util.Cut(h.n, 60), label = Util.Cut(h.lb, 200), path });
                    if (consent) Db.Exec("UPDATE WebSessions SET LastOn = @now WHERE Id = @sid", new { now, sid });
                    break;
            }
        }

        /// <summary>Events raised on the server (a lead was submitted, a newsletter signup).</summary>
        public static void RecordServerEvent(string visitorId, string name, string label, string path)
        {
            try
            {
                Db.Exec("INSERT INTO WebEvents (SessionId, VisitorId, OccurredOn, Name, Label, Path) VALUES (NULL, @vid, @now, @name, @label, @path)",
                    new { vid = visitorId, now = DateTime.UtcNow, name, label = Util.Cut(label, 200), path = Util.Cut(path, 300) });
            }
            catch { /* analytics must never break a lead */ }
        }

        private static void TouchSession(HttpContext ctx, Hit h, string vid, string sid, string path, DateTime now)
        {
            var updated = Db.Exec("UPDATE WebSessions SET LastOn = @now, Pageviews = Pageviews + 1, ExitPage = @path WHERE Id = @sid", new { now, path, sid });
            var isNew = updated == 0;
            if (isNew) CreateSession(ctx, h, vid, sid, path, now, true);

            var s = Db.First("SELECT Country, Region, City, Device, Browser, Os, Channel, Source, Landing, Ip FROM WebSessions WHERE Id = @sid", new { sid });
            if (s == null) return;
            var changed = Db.Exec(@"UPDATE WebVisitors SET LastSeen = @now, Pageviews = Pageviews + 1, Sessions = Sessions + @inc, LastIp = @ip,
                                    Country = COALESCE(@country, Country), Region = COALESCE(@region, Region), City = COALESCE(@city, City),
                                    Device = @device, Browser = @browser, Os = @os WHERE Id = @vid",
                new { now, inc = isNew ? 1 : 0, ip = s.Str("Ip"), country = s.Str("Country"), region = s.Str("Region"), city = s.Str("City"),
                      device = s.Str("Device"), browser = s.Str("Browser"), os = s.Str("Os"), vid });
            if (changed == 0)
            {
                try
                {
                    Db.Exec(@"INSERT INTO WebVisitors (Id, FirstSeen, LastSeen, Sessions, Pageviews, Country, Region, City, Device, Browser, Os, FirstChannel, FirstSource, FirstLanding, LastIp)
                              VALUES (@vid, @now, @now, 1, 1, @country, @region, @city, @device, @browser, @os, @channel, @source, @landing, @ip)",
                        new { vid, now, country = s.Str("Country"), region = s.Str("Region"), city = s.Str("City"), device = s.Str("Device"), browser = s.Str("Browser"),
                              os = s.Str("Os"), channel = s.Str("Channel"), source = s.Str("Source"), landing = s.Str("Landing"), ip = s.Str("Ip") });
                }
                catch { /* a parallel hit created the visitor first */ }
            }
        }

        private static void CreateSession(HttpContext ctx, Hit h, string vid, string sid, string path, DateTime now, bool consent)
        {
            var req = ctx.Request;
            var src = Attribution(h.q, h.r, req.Url.Host);
            var ua = UserAgent.Parse(req.UserAgent);
            var ip = ClientIp(req);
            var geo = Geo.Lookup(ip, req, h.tz);
            try
            {
                Db.Exec(@"INSERT INTO WebSessions (Id, VisitorId, StartedOn, LastOn, Pageviews, Landing, ExitPage, Referrer, Channel, Source, Medium, Campaign,
                          Device, Browser, Os, Screen, Lang, TimeZone, Country, Region, City, Ip, Consent)
                          VALUES (@sid, @vid, @now, @now, 1, @path, @path, @ref, @channel, @source, @medium, @campaign, @device, @browser, @os, @screen, @lang, @tz,
                          @country, @region, @city, @ip, @consent)",
                    new
                    {
                        sid, vid, now, path, @ref = Util.Cut(h.r, 400), channel = src.Channel, source = Util.Cut(src.Source, 120), medium = Util.Cut(src.Medium, 60),
                        campaign = Util.Cut(src.Campaign, 120), device = ua.Device, browser = ua.Browser, os = ua.Os,
                        screen = h.sw > 0 ? h.sw + "×" + h.sh : null, lang = Util.Cut(h.l, 20), tz = Util.Cut(h.tz, 60),
                        country = geo.Country, region = geo.Region, city = geo.City, ip = consent ? ip : null, consent
                    });
            }
            catch { /* duplicate session id from a parallel hit */ }
        }

        // ---- Attribution -------------------------------------------------------------------------------------

        public class Attrib { public string Channel, Source, Medium, Campaign; }

        /// <summary>Channel and source from UTM parameters, click ids or the referrer.</summary>
        public static Attrib Attribution(string query, string referrer, string ownHost)
        {
            var q = HttpUtility.ParseQueryString((query ?? "").TrimStart('?'));
            var s = new Attrib { Source = q["utm_source"], Medium = q["utm_medium"], Campaign = q["utm_campaign"] };
            var refHost = "";
            Uri u;
            if (!string.IsNullOrEmpty(referrer) && Uri.TryCreate(referrer, UriKind.Absolute, out u)) refHost = u.Host.ToLowerInvariant().Replace("www.", "");
            if (refHost == (ownHost ?? "").ToLowerInvariant().Replace("www.", "")) refHost = "";
            var medium = (s.Medium ?? "").ToLowerInvariant();

            if (!string.IsNullOrEmpty(q["gclid"]) || !string.IsNullOrEmpty(q["fbclid"]) && medium.Contains("paid") || Regex.IsMatch(medium, "cpc|ppc|paid|display|ads"))
                s.Channel = "Paid";
            else if (medium == "email" || medium == "newsletter") s.Channel = "Email";
            else if (!string.IsNullOrEmpty(s.Source) && Regex.IsMatch(s.Source, "facebook|instagram|linkedin|twitter|x.com|youtube|whatsapp", RegexOptions.IgnoreCase)) s.Channel = "Social";
            else if (!string.IsNullOrEmpty(s.Source)) s.Channel = "Campaign";
            else if (refHost == "") s.Channel = "Direct";
            else if (Regex.IsMatch(refHost, @"(^|\.)(google|bing|yahoo|duckduckgo|yandex|baidu|ecosia)\.")) s.Channel = "Organic search";
            else if (Regex.IsMatch(refHost, @"facebook|instagram|linkedin|lnkd|t\.co$|twitter|x\.com|youtube|youtu\.be|whatsapp|pinterest|reddit|quora")) s.Channel = "Social";
            else if (Regex.IsMatch(refHost, @"chatgpt|openai|perplexity|claude\.ai|gemini|copilot")) s.Channel = "AI assistants";
            else s.Channel = "Referral";

            if (string.IsNullOrEmpty(s.Source)) s.Source = refHost == "" ? "(direct)" : refHost;
            if (!string.IsNullOrEmpty(q["fbclid"]) && s.Channel == "Referral") s.Channel = "Social";
            return s;
        }

        private static bool IsEntry(HttpRequest req, string referrer)
        {
            Uri u;
            if (string.IsNullOrEmpty(referrer) || !Uri.TryCreate(referrer, UriKind.Absolute, out u)) return true;
            return !u.Host.Equals(req.Url.Host, StringComparison.OrdinalIgnoreCase);
        }

        private static string CleanPath(string p)
        {
            if (string.IsNullOrEmpty(p)) return null;
            p = p.Split('?', '#')[0];
            if (!p.StartsWith("/", StringComparison.Ordinal)) p = "/" + p;
            if (p.Length > 1) p = p.TrimEnd('/');
            return p.ToLowerInvariant();
        }

        public static string ClientIp(HttpRequest req)
        {
            var cf = req.Headers["CF-Connecting-IP"];
            if (!string.IsNullOrEmpty(cf)) return cf.Trim();
            if (ConfigurationManager.AppSettings["TrustForwardedFor"] == "true")
            {
                var xff = req.Headers["X-Forwarded-For"];
                if (!string.IsNullOrEmpty(xff)) return xff.Split(',')[0].Trim();
            }
            return req.UserHostAddress;
        }
    }

    /// <summary>Device, browser and operating system from the user agent string.</summary>
    public class UserAgent
    {
        public string Device, Browser, Os;

        public static UserAgent Parse(string ua)
        {
            ua = ua ?? "";
            var r = new UserAgent();
            r.Os = Has(ua, "Windows") ? "Windows" : Has(ua, "iPhone") || Has(ua, "iPad") || Has(ua, "iPod") ? "iOS" : Has(ua, "Android") ? "Android"
                 : Has(ua, "CrOS") ? "ChromeOS" : Has(ua, "Mac OS X") || Has(ua, "Macintosh") ? "macOS" : Has(ua, "Linux") ? "Linux" : "Other";
            r.Browser = Has(ua, "Edg/") ? "Edge" : Has(ua, "OPR/") || Has(ua, "Opera") ? "Opera" : Has(ua, "SamsungBrowser") ? "Samsung Internet"
                 : Has(ua, "UCBrowser") ? "UC Browser" : Has(ua, "Firefox/") || Has(ua, "FxiOS") ? "Firefox" : Has(ua, "CriOS") || Has(ua, "Chrome/") ? "Chrome"
                 : Has(ua, "Safari/") ? "Safari" : "Other";
            r.Device = Has(ua, "iPad") || Has(ua, "Tablet") || (Has(ua, "Android") && !Has(ua, "Mobile")) ? "Tablet"
                 : Has(ua, "Mobi") || Has(ua, "iPhone") ? "Mobile" : "Desktop";
            return r;
        }

        private static bool Has(string ua, string s) { return ua.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0; }
    }

    /// <summary>
    /// Location from the IP address. Order: Cloudflare headers (enable "Add visitor location headers"), the GeoCache table,
    /// ipinfo.io when appSettings GeoIpToken is set, then a country guess from the browser time zone.
    /// </summary>
    public static class Geo
    {
        public class Place { public string Country, Region, City; }

        private static readonly Dictionary<string, string> Countries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "IN", "India" }, { "US", "United States" }, { "GB", "United Kingdom" }, { "AE", "United Arab Emirates" }, { "SG", "Singapore" }, { "AU", "Australia" },
            { "CA", "Canada" }, { "DE", "Germany" }, { "FR", "France" }, { "NL", "Netherlands" }, { "SA", "Saudi Arabia" }, { "QA", "Qatar" }, { "NP", "Nepal" },
            { "BD", "Bangladesh" }, { "LK", "Sri Lanka" }, { "IE", "Ireland" }, { "NZ", "New Zealand" }, { "JP", "Japan" }, { "OM", "Oman" }, { "KW", "Kuwait" }
        };

        private static readonly Dictionary<string, string> Zones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Asia/Kolkata", "India" }, { "Asia/Calcutta", "India" }, { "Asia/Dubai", "United Arab Emirates" }, { "Asia/Singapore", "Singapore" },
            { "Europe/London", "United Kingdom" }, { "Asia/Kathmandu", "Nepal" }, { "Asia/Dhaka", "Bangladesh" }, { "Asia/Colombo", "Sri Lanka" },
            { "Asia/Riyadh", "Saudi Arabia" }, { "Asia/Qatar", "Qatar" }, { "Australia/Sydney", "Australia" }, { "Australia/Melbourne", "Australia" },
            { "America/New_York", "United States" }, { "America/Chicago", "United States" }, { "America/Los_Angeles", "United States" }, { "America/Toronto", "Canada" }
        };

        public static string CountryName(string code)
        {
            string n;
            return string.IsNullOrEmpty(code) || code == "XX" || code == "T1" ? null : Countries.TryGetValue(code, out n) ? n : code.ToUpperInvariant();
        }

        public static Place Lookup(string ip, HttpRequest req, string timeZone)
        {
            var p = new Place();
            var cf = req.Headers["CF-IPCountry"];
            if (!string.IsNullOrEmpty(cf))
            {
                p.Country = CountryName(cf);
                p.City = req.Headers["CF-IPCity"];
                p.Region = req.Headers["CF-Region"];
                if (!string.IsNullOrEmpty(p.City)) return p;
            }
            if (!string.IsNullOrEmpty(ip) && !IsPrivate(ip))
            {
                var cached = Db.First("SELECT Country, Region, City FROM GeoCache WHERE Ip = @ip", new { ip });
                if (cached != null) return new Place { Country = cached.Str("Country") ?? p.Country, Region = cached.Str("Region"), City = cached.Str("City") };
                var token = ConfigurationManager.AppSettings["GeoIpToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    var found = IpInfo(ip, token);
                    if (found != null)
                    {
                        try { Db.Exec("INSERT INTO GeoCache (Ip, Country, Region, City, CachedOn) VALUES (@ip, @c, @r, @city, @now)", new { ip, c = found.Country, r = found.Region, city = found.City, now = DateTime.UtcNow }); }
                        catch { }
                        return found;
                    }
                }
            }
            if (p.Country == null && !string.IsNullOrEmpty(timeZone)) { string c; if (Zones.TryGetValue(timeZone, out c)) p.Country = c; }
            return p;
        }

        private static Place IpInfo(string ip, string token)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("https://ipinfo.io/" + Uri.EscapeDataString(ip) + "/json?token=" + Uri.EscapeDataString(token));
                req.Timeout = 2500;
                using (var res = req.GetResponse())
                using (var sr = new System.IO.StreamReader(res.GetResponseStream()))
                {
                    var d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(sr.ReadToEnd());
                    Func<string, string> g = k => d.ContainsKey(k) && d[k] != null ? Convert.ToString(d[k]) : null;
                    return new Place { Country = CountryName(g("country")), Region = g("region"), City = g("city") };
                }
            }
            catch { return null; }
        }

        private static bool IsPrivate(string ip)
        {
            IPAddress a;
            if (!IPAddress.TryParse(ip, out a)) return true;
            if (IPAddress.IsLoopback(a)) return true;
            var b = a.GetAddressBytes();
            if (b.Length == 4) return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
            return a.IsIPv6LinkLocal || a.IsIPv6SiteLocal || (b[0] & 0xFE) == 0xFC;
        }
    }
}
