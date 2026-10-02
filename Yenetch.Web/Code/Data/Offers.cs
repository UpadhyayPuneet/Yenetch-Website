using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Yenetch.Crm;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>
    /// Offers from Admin &gt; Website content &gt; Offers. An offer runs between its start and end dates (India time, both days
    /// included) and shows on the pages and services it targets: as a banner, a once-per-visitor popup, in the plan builder
    /// and in the chatbot.
    /// </summary>
    public static class Offers
    {
        public static List<Offer> All { get { return (SiteContent.Current.Offers ?? new List<Offer>()).Where(o => o != null && !string.IsNullOrWhiteSpace(o.Title)).ToList(); } }

        public static bool IsRunning(Offer o, DateTime? nowUtc = null)
        {
            var today = Util.Ist(nowUtc ?? DateTime.UtcNow).Date;
            DateTime d;
            if (!string.IsNullOrWhiteSpace(o.Starts) && DateTime.TryParseExact(o.Starts.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) && today < d.Date) return false;
            if (!string.IsNullOrWhiteSpace(o.Ends) && DateTime.TryParseExact(o.Ends.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) && today > d.Date) return false;
            return true;
        }

        public static List<Offer> Active { get { return All.Where(o => IsRunning(o)).ToList(); } }

        /// <summary>Does a page address match the offer's page list (exact, prefix with *, or * for all)? Service pages also match the offer's services.</summary>
        public static bool OnPage(Offer o, string path)
        {
            path = ContentStore.Norm(path);
            foreach (var raw in o.Pages ?? new List<string>())
            {
                var p = (raw ?? "").Trim();
                if (p == "") continue;
                if (p == "*") return true;
                if (p.EndsWith("*")) { if (path.StartsWith(ContentStore.Norm(p.TrimEnd('*')).TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase) || path == ContentStore.Norm(p.TrimEnd('*'))) return true; continue; }
                if (ContentStore.Norm(p) == path) return true;
            }
            if (o.Services != null && o.Services.Count > 0 && path.StartsWith("/services/"))
                return o.Services.Contains(path.Substring("/services/".Length), StringComparer.OrdinalIgnoreCase);
            return false;
        }

        public static bool ForService(Offer o, string slug)
        {
            return o.Services == null || o.Services.Count == 0 || (slug != null && o.Services.Contains(slug, StringComparer.OrdinalIgnoreCase));
        }

        public static List<Offer> ForPage(string path) { return Active.Where(o => OnPage(o, path)).ToList(); }

        public static Offer ByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            return Active.FirstOrDefault(o => !string.IsNullOrEmpty(o.Code) && o.Code.Trim().Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Banner(s) at the top of the page for offers that target it.</summary>
        public static string BannerHtml(string path)
        {
            List<Offer> list;
            try { list = ForPage(path).Where(o => o.ShowBanner).Take(2).ToList(); }
            catch { return ""; }
            if (list.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var o in list)
            {
                sb.Append("<aside class=\"offer-bar\" data-offer=\"").Append(H(o.Id)).Append("\" role=\"region\" aria-label=\"Offer\"><div class=\"wrap offer-bar__in\">");
                if (!string.IsNullOrEmpty(o.Badge)) sb.Append("<span class=\"offer-bar__badge\">").Append(H(o.Badge)).Append("</span>");
                sb.Append("<p><b>").Append(H(o.Title)).Append("</b>");
                if (!string.IsNullOrEmpty(o.Text)) sb.Append(" <span>").Append(H(o.Text)).Append("</span>");
                if (!string.IsNullOrEmpty(o.Ends)) sb.Append(" <small>Ends ").Append(H(EndLabel(o))).Append("</small>");
                sb.Append("</p>");
                if (!string.IsNullOrEmpty(o.CtaUrl)) sb.Append("<a class=\"offer-bar__cta\" href=\"").Append(HttpUtility.HtmlAttributeEncode(SafeUrl(o.CtaUrl))).Append("\">").Append(H(string.IsNullOrEmpty(o.CtaText) ? "See offer" : o.CtaText)).Append("</a>");
                sb.Append("<button type=\"button\" class=\"offer-bar__x\" aria-label=\"Hide this offer\" data-offer-close>×</button></div></aside>");
            }
            return sb.ToString();
        }

        public static string EndLabel(Offer o)
        {
            DateTime d;
            return DateTime.TryParseExact((o.Ends ?? "").Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d.ToString("d MMM yyyy", Util.India) : "";
        }

        /// <summary>The fields of running offers that the browser needs (banner pages, popup, builder, chatbot). Codes are not sent:
        /// a coupon is checked on the server when the visitor types it.</summary>
        public static List<object> ClientList()
        {
            return Active.Select(o => (object)new
            {
                id = o.Id, title = o.Title, text = o.Text, badge = o.Badge, discountType = o.DiscountType ?? "none", discountValue = o.DiscountValue,
                needsCode = !string.IsNullOrWhiteSpace(o.Code), minOrder = o.MinOrder, ends = o.Ends, endsLabel = EndLabel(o),
                pages = o.Pages ?? new List<string>(), services = o.Services ?? new List<string>(),
                popup = o.ShowPopup, chat = o.ShowInChat, ctaText = o.CtaText, ctaUrl = SafeUrl(o.CtaUrl)
            }).ToList();
        }

        public static string Describe(Offer o)
        {
            var d = o.DiscountType == "percent" && o.DiscountValue > 0 ? o.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture) + "% off"
                  : o.DiscountType == "amount" && o.DiscountValue > 0 ? Fx.Format(o.DiscountValue, "INR") + " off" : null;
            return o.Title + (d != null && o.Title.IndexOf(d, StringComparison.OrdinalIgnoreCase) < 0 ? " (" + d + ")" : "");
        }

        /// <summary>Only site addresses and https links are allowed in offer buttons.</summary>
        public static string SafeUrl(string url)
        {
            url = (url ?? "").Trim();
            if (url.StartsWith("/") && !url.StartsWith("//")) return url;
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? url : "";
        }

        private static string H(string s) { return HttpUtility.HtmlEncode(s ?? ""); }
    }
}
