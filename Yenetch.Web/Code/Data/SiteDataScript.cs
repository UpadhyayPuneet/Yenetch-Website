using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;

namespace Yenetch.Data
{
    /// <summary>
    /// /api/site-data: the website content the scripts need (chatbot, solution finder, plan builder, offers, currencies),
    /// as one cacheable JavaScript file. Pages link it with a version number, so browsers download it once and reuse it
    /// on every page until something changes (it used to be repeated inside every page). Contact details in it are
    /// encoded so address-harvesting bots cannot read them; the site's scripts decode them.
    /// </summary>
    public static class SiteDataScript
    {
        private const string CacheKey = "sitedata.js";

        public class Built { public string Script; public string Version; }

        public static void Invalidate() { HttpRuntime.Cache.Remove(CacheKey); }

        public static string Version { get { try { return Current.Version; } catch { return "0"; } } }

        public static Built Current
        {
            get
            {
                var b = HttpRuntime.Cache[CacheKey] as Built;
                if (b != null) return b;
                b = Build();
                // Rebuilt at least every 10 minutes so offers start and end on time.
                HttpRuntime.Cache.Insert(CacheKey, b, null, DateTime.UtcNow.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);
                return b;
            }
        }

        private static Built Build()
        {
            var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };
            var data = js.DeserializeObject(SiteContent.RawJson.Replace("<\\/", "</")) as Dictionary<string, object> ?? new Dictionary<string, object>();

            // Only running offers, without coupon codes.
            data["offers"] = Offers.ClientList();
            // Plans and extras carry prices only when prices are public.
            if (!Pricing.PublicPrices)
            {
                foreach (var key in new[] { "plans", "addons" })
                {
                    object list;
                    if (data.TryGetValue(key, out list) && list is IEnumerable)
                        foreach (var item in (IEnumerable)list) { var d = item as Dictionary<string, object>; if (d != null) { d["price"] = 0; d["setupFee"] = 0; } }
                }
            }
            data["showPrices"] = Pricing.PublicPrices;
            data.Remove("blog");
            Protect.EncodeCompany(data);

            var config = new Dictionary<string, object>
            {
                { "lead", "/api/lead" }, { "pulse", "/api/pulse" }, { "chat", Ai.Enabled ? "/api/chat" : null },
                { "fx", Fx.ClientConfig() }, { "captcha", Guard.ClientConfig() }, { "popups", Popups.ClientConfig() }
            };
            var script = "window.YENETCH_DATA=" + js.Serialize(data).Replace("</", "<\\/") + ";\n"
                       + "window.YENETCH_CONFIG=" + js.Serialize(config).Replace("</", "<\\/") + ";\n"
                       + "window.YENBOT_ENDPOINT=window.YENETCH_CONFIG.lead;window.YENETCH_PULSE=window.YENETCH_CONFIG.pulse;\n";
            string version;
            using (var md5 = MD5.Create()) version = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(script))).Replace("-", "").Substring(0, 12).ToLowerInvariant();
            return new Built { Script = script, Version = version };
        }
    }
}
