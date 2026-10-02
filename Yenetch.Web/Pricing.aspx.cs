using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using Yenetch.Data;

namespace Yenetch.Web
{
    /// <summary>
    /// /pricing: plans for every service (from Admin &gt; Website content &gt; Plans &amp; prices), the custom plan builder and
    /// pricing FAQs. Title and description are set in the @Page directive (tools/build.py). The helpers below also render
    /// the plan cards on service pages.
    /// </summary>
    public partial class Pricing : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Staff preview while prices are hidden: never cache, and keep it out of search results.
            if (Yenetch.Data.Pricing.IsPreview) { Response.Cache.SetCacheability(HttpCacheability.NoCache); Response.AppendHeader("X-Robots-Tag", "noindex"); }
        }

        /// <summary>Service shortcuts at the top of the page.</summary>
        public static string Tabs()
        {
            var sb = new StringBuilder("<nav class=\"ptabs\" aria-label=\"Services\">");
            foreach (var s in Yenetch.Data.Pricing.ServicesWithPlans)
                sb.Append("<a href=\"#plans-").Append(H(s.Slug)).Append("\">").Append(H(Short(s.Name))).Append("</a>");
            return sb.Append("</nav>").ToString();
        }

        /// <summary>One block per service with its plans, grouped in the order of the service areas.</summary>
        public static string Sections()
        {
            var sb = new StringBuilder();
            var services = Yenetch.Data.Pricing.ServicesWithPlans;
            if (services.Count == 0) return "<p class=\"empty\">Plans are being updated. <a href=\"/contact\">Ask us for a quote</a>.</p>";
            foreach (var pillar in SiteContent.Current.Pillars)
            {
                var list = services.Where(s => s.Pillar == pillar.Id).ToList();
                if (list.Count == 0) continue;
                sb.Append("<h2 class=\"ppillar\">").Append(H(pillar.Name)).Append("</h2>");
                foreach (var s in list)
                    sb.Append("<section class=\"psvc\" id=\"plans-").Append(H(s.Slug)).Append("\"><div class=\"psvc__head\"><h3>").Append(H(s.Name)).Append("</h3><p>").Append(H(s.Summary))
                      .Append("</p><a class=\"more\" href=\"").Append(H(s.Url)).Append("\">About this service</a></div>").Append(Yenetch.Data.Pricing.PlanCards(s.Slug)).Append("</section>");
            }
            // Services whose area was removed still show.
            foreach (var s in services.Where(s => SiteContent.Current.Pillars.All(p => p.Id != s.Pillar)))
                sb.Append("<section class=\"psvc\" id=\"plans-").Append(H(s.Slug)).Append("\"><div class=\"psvc__head\"><h3>").Append(H(s.Name)).Append("</h3></div>").Append(Yenetch.Data.Pricing.PlanCards(s.Slug)).Append("</section>");
            return sb.ToString();
        }

        public static string PreviewNote()
        {
            return Yenetch.Data.Pricing.IsPreview
                ? "<p class=\"notice\">Preview for signed-in staff: visitors do not see prices yet. Check them in Admin &gt; Website content &gt; Plans &amp; prices, then switch them on in Admin &gt; Pricing &amp; currency.</p>"
                : "";
        }

        public static string CurrencySwitch()
        {
            return "<div class=\"cur\" data-currency-switch><label for=\"cur-select\">Show prices in</label><select class=\"field field--sm\" id=\"cur-select\" data-currency-select><option value=\"INR\">₹ INR</option></select></div>";
        }

        /// <summary>The Plans block on a service page (empty when the service has no plans).</summary>
        public static string ServicePlans(string slug)
        {
            if (!Yenetch.Data.Pricing.HasPlans(slug)) return "";
            return "<div class=\"svc-plans\"><div class=\"svc-plans__bar\"><h3>Plans</h3>" + (Yenetch.Data.Pricing.ShowPrices ? CurrencySwitch() : "") + "</div>"
                 + Yenetch.Data.Pricing.PlanCards(slug, true)
                 + "<p class=\"svc-plans__more\"><a class=\"more\" href=\"/pricing#builder\">Combine with other services in the plan builder</a></p></div>";
        }

        private static string Short(string name) { var i = (name ?? "").IndexOf(" (", StringComparison.Ordinal); return i > 0 ? name.Substring(0, i) : name; }
        private static string H(string s) { return HttpUtility.HtmlEncode(s ?? ""); }
    }
}
