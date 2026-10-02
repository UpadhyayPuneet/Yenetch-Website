using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Data
{
    /// <summary>
    /// The free website audit popup, set up in Admin &gt; Popups &amp; offers. It opens when a visitor is about to leave
    /// (desktop), after a delay, or after scrolling part of the page, at most once every few days per visitor, and never
    /// on the pages listed as excluded. Offer popups (Admin &gt; Website content &gt; Offers) use the same rules.
    /// </summary>
    public static class Popups
    {
        public class AuditPopup
        {
            public bool Enabled;
            public string Trigger = "exit";
            public int Delay = 25;
            public int Scroll = 60;
            public int Days = 7;
            public string Title = "Is your website losing you customers?";
            public string Text = "Get a free SEO, speed and security check of your website in 60 seconds, with plain-English fixes.";
            public string Button = "Check my website free";
            public List<string> Exclude = new List<string> { "/website-audit", "/contact", "/book", "/pricing" };
        }

        public static AuditPopup Audit
        {
            get
            {
                var p = new AuditPopup();
                try
                {
                    p.Enabled = Settings.Get("popup.audit.enabled") != "0";
                    p.Trigger = Get("popup.audit.trigger", p.Trigger);
                    p.Delay = Int("popup.audit.delay", p.Delay);
                    p.Scroll = Int("popup.audit.scroll", p.Scroll);
                    p.Days = Int("popup.audit.days", p.Days);
                    p.Title = Get("popup.audit.title", p.Title);
                    p.Text = Get("popup.audit.text", p.Text);
                    p.Button = Get("popup.audit.button", p.Button);
                    var ex = Settings.Get("popup.audit.exclude");
                    if (ex != null) p.Exclude = ex.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                }
                catch { }
                return p;
            }
        }

        /// <summary>Popup settings for the browser (null when no popup is switched on).</summary>
        public static object ClientConfig()
        {
            var a = Audit;
            return new
            {
                audit = a.Enabled ? new { trigger = a.Trigger, delay = a.Delay, scroll = a.Scroll, days = a.Days, title = a.Title, text = a.Text, button = a.Button, exclude = a.Exclude } : null,
                offerDays = Int("popup.offer.days", 3)
            };
        }

        private static string Get(string k, string fallback) { var v = Settings.Get(k); return string.IsNullOrWhiteSpace(v) ? fallback : v; }
        private static int Int(string k, int fallback) { int v; try { return int.TryParse(Settings.Get(k), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && v >= 0 ? v : fallback; } catch { return fallback; } }
    }
}
