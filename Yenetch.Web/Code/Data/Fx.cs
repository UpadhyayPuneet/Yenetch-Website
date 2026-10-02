using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;

namespace Yenetch.Data
{
    /// <summary>
    /// Currencies for visitors abroad. Prices are kept in INR; rates come once a day from the free open.er-api.com
    /// service (no key needed, with frankfurter.app as a backup), and any rate can be fixed by hand in
    /// Admin &gt; Pricing &amp; currency. A visitor's currency is guessed from their country and can be changed on the page.
    /// </summary>
    public static class Fx
    {
        public const string Base = "INR";

        /// <summary>Currencies a visitor can pick, with symbol and name. Admins choose which are switched on.</summary>
        public static readonly Dictionary<string, string[]> Known = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "INR", new[] { "₹", "Indian rupee" } }, { "USD", new[] { "$", "US dollar" } }, { "EUR", new[] { "€", "Euro" } },
            { "GBP", new[] { "£", "British pound" } }, { "AED", new[] { "AED ", "UAE dirham" } }, { "SAR", new[] { "SAR ", "Saudi riyal" } },
            { "QAR", new[] { "QAR ", "Qatari riyal" } }, { "SGD", new[] { "S$", "Singapore dollar" } }, { "AUD", new[] { "A$", "Australian dollar" } },
            { "CAD", new[] { "C$", "Canadian dollar" } }, { "NZD", new[] { "NZ$", "New Zealand dollar" } }, { "JPY", new[] { "¥", "Japanese yen" } },
            { "CHF", new[] { "CHF ", "Swiss franc" } }, { "ZAR", new[] { "R ", "South African rand" } }, { "MYR", new[] { "RM ", "Malaysian ringgit" } },
            { "NPR", new[] { "NPR ", "Nepalese rupee" } }, { "BDT", new[] { "৳", "Bangladeshi taka" } }, { "LKR", new[] { "LKR ", "Sri Lankan rupee" } },
            { "KWD", new[] { "KWD ", "Kuwaiti dinar" } }, { "OMR", new[] { "OMR ", "Omani rial" } }, { "BHD", new[] { "BHD ", "Bahraini dinar" } }
        };

        /// <summary>Country (ISO 3166 alpha-2) to currency, for the countries most visitors come from. Others use the default foreign currency.</summary>
        private static readonly Dictionary<string, string> CountryCurrency = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "IN", "INR" }, { "US", "USD" }, { "GB", "GBP" }, { "AE", "AED" }, { "SA", "SAR" }, { "QA", "QAR" }, { "SG", "SGD" }, { "AU", "AUD" },
            { "CA", "CAD" }, { "NZ", "NZD" }, { "JP", "JPY" }, { "CH", "CHF" }, { "ZA", "ZAR" }, { "MY", "MYR" }, { "NP", "NPR" }, { "BD", "BDT" },
            { "LK", "LKR" }, { "KW", "KWD" }, { "OM", "OMR" }, { "BH", "BHD" },
            { "DE", "EUR" }, { "FR", "EUR" }, { "IT", "EUR" }, { "ES", "EUR" }, { "NL", "EUR" }, { "BE", "EUR" }, { "IE", "EUR" }, { "AT", "EUR" },
            { "PT", "EUR" }, { "FI", "EUR" }, { "GR", "EUR" }, { "LU", "EUR" }, { "SK", "EUR" }, { "SI", "EUR" }, { "EE", "EUR" }, { "LV", "EUR" },
            { "LT", "EUR" }, { "MT", "EUR" }, { "CY", "EUR" }, { "HR", "EUR" }
        };

        public static string Symbol(string code) { string[] v; return Known.TryGetValue(code ?? "", out v) ? v[0] : (code + " "); }

        /// <summary>Currencies switched on in the admin (INR always first).</summary>
        public static List<string> Enabled
        {
            get
            {
                var raw = Safe(() => Settings.Get("fx.currencies")) ?? "INR,USD,EUR,GBP,AED,SGD,AUD,CAD";
                var list = raw.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToUpperInvariant()).Where(Known.ContainsKey).Distinct().ToList();
                list.Remove(Base);
                list.Insert(0, Base);
                return list;
            }
        }

        /// <summary>Currency shown to visitors from countries without their own enabled currency (USD by default).</summary>
        public static string ForeignDefault { get { var v = Safe(() => Settings.Get("fx.foreign")); return string.IsNullOrEmpty(v) ? "USD" : v; } }

        /// <summary>Prices abroad are rounded so they read naturally (for example $199 instead of $197.43). Off: exact conversion.</summary>
        public static bool Round { get { return Safe(() => Settings.Get("fx.round")) != "0"; } }

        public static string ForCountry(string country)
        {
            if (string.IsNullOrEmpty(country)) return null;
            string c;
            var enabled = Enabled;
            if (CountryCurrency.TryGetValue(country.Trim(), out c) && enabled.Contains(c)) return c;
            return country.Equals("IN", StringComparison.OrdinalIgnoreCase) ? Base : (enabled.Contains(ForeignDefault) ? ForeignDefault : Base);
        }

        // ---- Rates ------------------------------------------------------------------------------------------

        private static readonly object Lock = new object();
        private static volatile bool _refreshing;

        /// <summary>Units of each currency per 1 INR. Manual rates from the admin win over the daily feed.</summary>
        public static Dictionary<string, decimal> Rates
        {
            get
            {
                var cached = HttpRuntime.Cache["fx.rates"] as Dictionary<string, decimal>;
                if (cached != null) return cached;
                var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { { Base, 1m } };
                var feed = Safe(() => Settings.Get("fx.feed"));
                if (!string.IsNullOrEmpty(feed))
                {
                    try
                    {
                        foreach (var kv in new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(feed))
                        {
                            decimal d;
                            if (decimal.TryParse(System.Convert.ToString(kv.Value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d > 0) rates[kv.Key] = d;
                        }
                    }
                    catch { }
                }
                foreach (var code in Known.Keys)
                {
                    decimal m;
                    var manual = Safe(() => Settings.Get("fx.manual." + code));
                    // Manual rates are typed the human way: rupees for one unit (1 USD = 83.5 INR).
                    if (!string.IsNullOrEmpty(manual) && decimal.TryParse(manual, NumberStyles.Float, CultureInfo.InvariantCulture, out m) && m > 0) rates[code] = 1m / m;
                }
                rates[Base] = 1m;
                HttpRuntime.Cache.Insert("fx.rates", rates, null, DateTime.UtcNow.AddMinutes(30), System.Web.Caching.Cache.NoSlidingExpiration);
                RefreshIfStale();
                return rates;
            }
        }

        public static DateTime? UpdatedOn
        {
            get { DateTime d; var v = Safe(() => Settings.Get("fx.updated")); return DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out d) ? (DateTime?)d : null; }
        }

        public static bool IsManual(string code) { return !string.IsNullOrEmpty(Safe(() => Settings.Get("fx.manual." + code))); }

        /// <summary>Fetches fresh rates in the background when the stored ones are more than 12 hours old.</summary>
        public static void RefreshIfStale()
        {
            var u = UpdatedOn;
            if (_refreshing || (u.HasValue && u.Value > DateTime.UtcNow.AddHours(-12))) return;
            if (HttpRuntime.Cache["fx.tried"] != null) return;
            HttpRuntime.Cache.Insert("fx.tried", true, null, DateTime.UtcNow.AddMinutes(30), System.Web.Caching.Cache.NoSlidingExpiration);
            _refreshing = true;
            System.Threading.ThreadPool.QueueUserWorkItem(_ => { try { Refresh(); } catch (Exception ex) { Mailer.Log("currency rates", ex); } finally { _refreshing = false; } });
        }

        /// <summary>Downloads today's rates. Returns an error message, or null.</summary>
        public static string Refresh()
        {
            lock (Lock)
            {
                Dictionary<string, decimal> got = null;
                string error = null;
                foreach (var src in new[] { "https://open.er-api.com/v6/latest/INR", "https://api.frankfurter.app/latest?from=INR" })
                {
                    try { got = Download(src); if (got != null && got.Count > 5) break; }
                    catch (Exception ex) { error = ex.Message; got = null; }
                }
                if (got == null || got.Count == 0) return "Could not reach the exchange rate service" + (error != null ? ": " + error : ".");
                var keep = got.Where(kv => Known.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value.ToString(CultureInfo.InvariantCulture));
                Settings.Set("fx.feed", new JavaScriptSerializer().Serialize(keep));
                Settings.Set("fx.updated", DateTime.UtcNow.ToString("o"));
                HttpRuntime.Cache.Remove("fx.rates");
                return null;
            }
        }

        private static Dictionary<string, decimal> Download(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 15000; req.ReadWriteTimeout = 15000;
            req.UserAgent = "Yenetch-Website/11";
            string json;
            using (var res = (HttpWebResponse)req.GetResponse())
            using (var sr = new System.IO.StreamReader(res.GetResponseStream())) json = sr.ReadToEnd();
            var d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            object rates;
            if (!d.TryGetValue("rates", out rates) || !(rates is Dictionary<string, object>)) return null;
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in (Dictionary<string, object>)rates)
            {
                decimal v;
                if (decimal.TryParse(System.Convert.ToString(kv.Value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v > 0) result[kv.Key] = v;
            }
            return result;
        }

        // ---- Conversion and formatting ------------------------------------------------------------------------

        public static decimal Rate(string code)
        {
            decimal r;
            return Rates.TryGetValue(code ?? Base, out r) ? r : 1m;
        }

        /// <summary>Converts an INR amount, rounding foreign prices to a natural-looking figure when rounding is on.</summary>
        public static decimal Convert(decimal inr, string code)
        {
            if (string.IsNullOrEmpty(code) || code.Equals(Base, StringComparison.OrdinalIgnoreCase) || !Rates.ContainsKey(code)) return inr;
            var v = inr * Rate(code);
            return Round ? Nice(v) : Math.Round(v, 2);
        }

        /// <summary>Rounds up to a tidy price: 7.3 → 8, 47 → 49, 197 → 199, 1234 → 1,249, 12,345 → 12,499.</summary>
        public static decimal Nice(decimal v)
        {
            if (v <= 0) return 0;
            if (v < 10) return Math.Ceiling(v);
            if (v < 100) { var n = Math.Ceiling(v / 5m) * 5m; return n - 1; }
            if (v < 1000) { var n = Math.Ceiling(v / 10m) * 10m; return n - 1; }
            if (v < 10000) { var n = Math.Ceiling(v / 50m) * 50m; return n - 1; }
            var step = v < 100000 ? 500m : 5000m;
            return Math.Ceiling(v / step) * step - 1;
        }

        public static string Format(decimal amount, string code)
        {
            code = string.IsNullOrEmpty(code) ? Base : code.ToUpperInvariant();
            if (code == Base) return "₹" + amount.ToString("#,##0", Util.India);
            var fmt = amount == Math.Floor(amount) ? "#,##0" : "#,##0.00";
            return Symbol(code) + amount.ToString(fmt, CultureInfo.InvariantCulture);
        }

        /// <summary>Best guess of the visitor's country: Cloudflare's header, the analytics location cache, or null.</summary>
        public static string Country(HttpRequest req)
        {
            var cf = req.Headers["CF-IPCountry"];
            if (!string.IsNullOrEmpty(cf) && cf.Length == 2 && cf != "XX" && cf != "T1") return cf.ToUpperInvariant();
            try
            {
                var ip = Analytics.ClientIp(req);
                var name = Db.Scalar<string>("SELECT Country FROM GeoCache WHERE Ip = @ip", new { ip });
                if (!string.IsNullOrEmpty(name)) return CountryCode(name);
            }
            catch { }
            return null;
        }

        private static string CountryCode(string nameOrCode)
        {
            if (nameOrCode.Length == 2) return nameOrCode.ToUpperInvariant();
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try { var r = new RegionInfo(ci.Name); if (r.EnglishName.Equals(nameOrCode, StringComparison.OrdinalIgnoreCase)) return r.TwoLetterISORegionName; }
                catch { }
            }
            return null;
        }

        /// <summary>Everything the browser needs to convert prices: enabled currencies, rates and symbols.</summary>
        public static object ClientConfig()
        {
            var rates = Rates;
            return new
            {
                @base = Base,
                foreign = ForeignDefault,
                round = Round,
                currencies = Enabled.Select(c => new { code = c, symbol = Symbol(c), name = Known[c][1], rate = rates.ContainsKey(c) ? rates[c] : 0m }).Where(c => c.rate > 0).ToList(),
                countries = CountryCurrency
            };
        }

        private static T Safe<T>(Func<T> f) where T : class { try { return f(); } catch { return null; } }
    }
}
