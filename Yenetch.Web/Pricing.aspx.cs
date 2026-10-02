using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

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

        /// <summary>
        /// "What do you need?" at the top of the page: goal choices and a search box. The script uses them to show only the
        /// services that fit; without the script every service is listed, folded.
        /// </summary>
        public static string Finder()
        {
            var goals = Yenetch.Data.Pricing.Goals;
            var sb = new StringBuilder("<div class=\"pfind\" data-finder hidden><div class=\"pfind__head\"><h2>What do you need help with?</h2><p>Pick one or more. We show only the plans that fit.</p></div>");
            if (goals.Count > 0)
            {
                sb.Append("<div class=\"pfind__chips\" role=\"group\" aria-label=\"Your goals\">");
                foreach (var g in goals)
                    sb.Append("<button type=\"button\" class=\"gchip\" aria-pressed=\"false\" data-goal=\"").Append(H(g.Id)).Append("\" data-services=\"").Append(H(string.Join(" ", g.Services))).Append("\"><b>")
                      .Append(H(g.Name)).Append("</b>").Append(string.IsNullOrWhiteSpace(g.Hint) ? "" : "<small>" + H(g.Hint) + "</small>").Append("</button>");
                sb.Append("</div>");
            }
            sb.Append("<div class=\"pfind__bar\"><label class=\"sr-only\" for=\"psearch\">Search services</label><input class=\"field field--sm pfind__search\" id=\"psearch\" type=\"search\" placeholder=\"Or search: SEO, app, CRM, hosting…\" autocomplete=\"off\" data-psearch>")
              .Append("<p class=\"pfind__count\" data-pcount aria-live=\"polite\"></p><button type=\"button\" class=\"pfind__clear\" data-pclear hidden>Clear</button></div></div>");
            return sb.ToString();
        }

        /// <summary>
        /// Every service with plans as a folded row ("3 plans · from ₹15,000/month"), grouped by service area. A row opens
        /// to show its plan cards, so the page stays short and crawlers still see every plan.
        /// </summary>
        public static string Sections()
        {
            var sb = new StringBuilder();
            var services = Yenetch.Data.Pricing.ServicesWithPlans;
            if (services.Count == 0) return "<p class=\"empty\">Plans are being updated. <a href=\"/contact\">Ask us for a quote</a>.</p>";
            var pillars = SiteContent.Current.Pillars;
            foreach (var pillar in pillars)
            {
                var list = services.Where(s => s.Pillar == pillar.Id).ToList();
                if (list.Count == 0) continue;
                sb.Append("<div class=\"pgroup\" data-pgroup><h2 class=\"ppillar\">").Append(H(pillar.Name)).Append("</h2>");
                foreach (var s in list) Row(sb, s);
                sb.Append("</div>");
            }
            // Services whose area was removed still show.
            var orphans = services.Where(s => pillars.All(p => p.Id != s.Pillar)).ToList();
            if (orphans.Count > 0)
            {
                sb.Append("<div class=\"pgroup\" data-pgroup><h2 class=\"ppillar\">More services</h2>");
                foreach (var s in orphans) Row(sb, s);
                sb.Append("</div>");
            }
            sb.Append("<p class=\"pmore\" data-pmore hidden><button type=\"button\" class=\"btn btn--line btn--sm\" data-pshowall></button></p>");
            return sb.ToString();
        }

        private static void Row(StringBuilder sb, Service s)
        {
            var words = string.Join(" ", new[] { s.Name, s.Summary }.Concat(s.Keywords ?? new System.Collections.Generic.List<string>())).ToLowerInvariant();
            sb.Append("<details class=\"psvc\" id=\"plans-").Append(H(s.Slug)).Append("\" data-svc=\"").Append(H(s.Slug)).Append("\" data-find=\"").Append(H(words)).Append("\">")
              .Append("<summary class=\"psvc__sum\"><span class=\"psvc__name\"><h3>").Append(H(s.Name)).Append("</h3><span class=\"psvc__picked\" data-picked hidden>In your plan</span></span>")
              .Append("<span class=\"psvc__meta\">").Append(Yenetch.Data.Pricing.Teaser(s.Slug)).Append("</span><span class=\"psvc__chev\" aria-hidden=\"true\"></span></summary>")
              .Append("<div class=\"psvc__body\"><div class=\"psvc__head\"><p>").Append(H(s.Summary)).Append("</p><a class=\"more\" href=\"").Append(H(s.Url)).Append("\">About this service</a></div>")
              .Append(Yenetch.Data.Pricing.PlanCards(s.Slug)).Append("</div></details>");
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
