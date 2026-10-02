using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    /// <summary>
    /// Spam and abuse protection for the public forms and APIs: per-visitor rate limits, a hidden honeypot field, a
    /// minimum time to fill a form, and an optional CAPTCHA (Cloudflare Turnstile or Google reCAPTCHA v3, set up in
    /// Admin &gt; Security &amp; backups). Turnstile is recommended: free, private and usually invisible to people.
    /// </summary>
    public static class Guard
    {
        /// <summary>True while the caller is within max requests per window for this bucket (counted per IP).</summary>
        public static bool Allow(HttpRequest req, string bucket, int max, TimeSpan window)
        {
            var key = "guard|" + bucket + "|" + Analytics.ClientIp(req);
            var counter = HttpRuntime.Cache[key] as int[];
            if (counter == null)
            {
                counter = new[] { 0 };
                // Add returns the entry another request stored first, if any.
                var existing = HttpRuntime.Cache.Add(key, counter, null, DateTime.UtcNow.Add(window), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null) as int[];
                if (existing != null) counter = existing;
            }
            lock (counter) { counter[0]++; return counter[0] <= max; }
        }

        /// <summary>Bots fill every field; people never see this one.</summary>
        public static bool IsHoneypotFilled(string value) { return !string.IsNullOrWhiteSpace(value); }

        /// <summary>Forms carry the time the page was shown (milliseconds, from the script). Under 2 seconds is a bot.</summary>
        public static bool TooFast(string startedMs)
        {
            long t;
            if (!long.TryParse(startedMs, out t) || t <= 0) return false;
            var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - t;
            return elapsed >= 0 && elapsed < 2000;
        }

        // ---- CAPTCHA -------------------------------------------------------------------------------------------

        /// <summary>turnstile, recaptcha or empty (off).</summary>
        public static string CaptchaProvider { get { try { return Settings.Get("captcha.provider") ?? ""; } catch { return ""; } } }
        public static string CaptchaSiteKey { get { try { return Settings.Get("captcha.siteKey") ?? ""; } catch { return ""; } } }
        public static bool CaptchaOn { get { return (CaptchaProvider == "turnstile" || CaptchaProvider == "recaptcha") && CaptchaSiteKey.Length > 0 && !string.IsNullOrEmpty(SafeSecret()); } }

        private static string SafeSecret() { try { return Settings.GetSecret("captcha.secret"); } catch { return null; } }

        /// <summary>What the browser needs to show the CAPTCHA (nothing when it is off).</summary>
        public static object ClientConfig() { return CaptchaOn ? new { provider = CaptchaProvider, siteKey = CaptchaSiteKey } : null; }

        /// <summary>Checks a CAPTCHA token with the provider. Passes when the CAPTCHA is off. If the provider cannot be
        /// reached the request is let through (and logged), so a provider outage never blocks real enquiries.</summary>
        public static bool CaptchaPassed(string token, HttpRequest req)
        {
            if (!CaptchaOn) return true;
            if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return false;
            var url = CaptchaProvider == "turnstile" ? "https://challenges.cloudflare.com/turnstile/v0/siteverify" : "https://www.google.com/recaptcha/api/siteverify";
            try
            {
                var body = "secret=" + HttpUtility.UrlEncode(SafeSecret()) + "&response=" + HttpUtility.UrlEncode(token) + "&remoteip=" + HttpUtility.UrlEncode(Analytics.ClientIp(req));
                var http = (HttpWebRequest)WebRequest.Create(url);
                http.Method = "POST"; http.ContentType = "application/x-www-form-urlencoded"; http.Timeout = 8000;
                var bytes = Encoding.UTF8.GetBytes(body);
                using (var s = http.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                string json;
                using (var res = (HttpWebResponse)http.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream())) json = sr.ReadToEnd();
                var d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                object ok, score;
                var success = d.TryGetValue("success", out ok) && ok is bool && (bool)ok;
                // reCAPTCHA v3 also scores how human the visit looked (0 = bot, 1 = human).
                if (success && CaptchaProvider == "recaptcha" && d.TryGetValue("score", out score))
                    success = Convert.ToDouble(score, CultureInfo.InvariantCulture) >= 0.4;
                return success;
            }
            catch (Exception ex) { Mailer.Log("captcha check", ex); return true; }
        }

        /// <summary>All the checks a public form runs. Returns null when the request looks human, else a message.</summary>
        public static string Check(HttpRequest req, string bucket, int perHour, string honeypot, string startedMs, string captchaToken)
        {
            if (!Allow(req, bucket, perHour, TimeSpan.FromHours(1))) return "Too many requests. Please try again in a little while, or call us.";
            if (IsHoneypotFilled(honeypot) || TooFast(startedMs)) return "bot";
            if (!CaptchaPassed(captchaToken, req)) return "Please confirm you are not a robot and try again.";
            return null;
        }

        /// <summary>The CAPTCHA answer sent with a form: Turnstile's own field, or the "captcha" field the site script fills.</summary>
        public static string TokenFrom(System.Collections.Specialized.NameValueCollection f)
        {
            return f["captcha"] ?? f["cf-turnstile-response"] ?? f["g-recaptcha-response"] ?? "";
        }

        /// <summary>Requests from another website (cross-site) are refused by the JSON APIs that change data.</summary>
        public static bool SameOrigin(HttpRequest req)
        {
            var origin = req.Headers["Origin"];
            if (string.IsNullOrEmpty(origin)) return true;
            Uri u;
            return Uri.TryCreate(origin, UriKind.Absolute, out u) && string.Equals(u.Host, req.Url.Host, StringComparison.OrdinalIgnoreCase);
        }
    }
}
