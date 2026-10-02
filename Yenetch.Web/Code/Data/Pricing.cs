using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>One line of a quote: a plan or an extra, with its quantity and price in the quote's currency.</summary>
    public class QuoteLine
    {
        public string Kind { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Service { get; set; }
        public string ServiceName { get; set; }
        public string Billing { get; set; }
        public string Unit { get; set; }
        public bool IsFrom { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal Setup { get; set; }
        public decimal UnitInr { get; set; }
        public decimal SetupInr { get; set; }
    }

    /// <summary>A priced quote. All figures are in Currency; the *Inr figures are the same quote in rupees for the CRM.</summary>
    public class QuoteResult
    {
        public string Currency { get; set; }
        public decimal Rate { get; set; }
        public List<QuoteLine> Lines { get; set; }
        public decimal OneTime { get; set; }
        public decimal Monthly { get; set; }
        public decimal Yearly { get; set; }
        public decimal DiscountOneTime { get; set; }
        public decimal DiscountMonthly { get; set; }
        public decimal DiscountYearly { get; set; }
        public decimal OneTimeInr { get; set; }
        public decimal MonthlyInr { get; set; }
        public decimal DiscountInr { get; set; }
        public string OfferId { get; set; }
        public string OfferTitle { get; set; }
        public string OfferNote { get; set; }
        public string CodeError { get; set; }
        public bool HasFrom { get; set; }
        public decimal Discount { get { return DiscountOneTime + DiscountMonthly + DiscountYearly; } }
        /// <summary>First payment: one-time items, the first month and the first year, after any discount.</summary>
        public decimal DueNow { get { return OneTime + Monthly + Yearly - Discount; } }
        public decimal MonthlyAfter { get { return Monthly; } }
        public string Money(decimal v) { return Fx.Format(v, Currency); }
    }

    /// <summary>
    /// Plans, extras and quotes. Prices are stored in INR in Admin &gt; Website content &gt; Plans &amp; prices and
    /// Plan builder extras; the switches (show prices, tax line) are in Admin &gt; Pricing &amp; currency.
    /// Quotes are always recalculated here on the server, never trusted from the browser.
    /// </summary>
    public static class Pricing
    {
        // ---- Settings -----------------------------------------------------------------------------------------

        /// <summary>Prices are public only after an admin switches them on (the shipped prices are examples).</summary>
        public static bool PublicPrices { get { try { return Settings.Get("pricing.show") == "1"; } catch { return false; } } }

        /// <summary>Visitors see prices when they are public; signed-in admins and managers always do (to preview).</summary>
        public static bool ShowPrices
        {
            get
            {
                if (PublicPrices) return true;
                try { var u = Auth.Current; return u != null && u.CanUseMarketing; } catch { return false; }
            }
        }

        public static bool IsPreview { get { return !PublicPrices && ShowPrices; } }

        public static string TaxName { get { return Get("pricing.taxName", "GST"); } }
        public static decimal TaxPct { get { decimal d; return decimal.TryParse(Get("pricing.taxPct", "18"), NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 18m; } }
        public static string Note { get { return Get("pricing.note", "Prices are estimates before GST. Your final quote is confirmed after a short call about your goals and scope."); } }
        public static string Heading { get { return Get("pricing.heading", "Simple plans. Or build your own."); } }
        public static string Intro { get { return Get("pricing.intro", "Pick a plan for any service, or combine services and extras into one custom plan. You get an instant estimate, and a specialist confirms it with you."); } }

        private static string Get(string k, string fallback) { try { var v = Settings.Get(k); return string.IsNullOrWhiteSpace(v) ? fallback : v; } catch { return fallback; } }

        // ---- Catalogue ----------------------------------------------------------------------------------------

        public static List<Plan> Plans { get { return (SiteContent.Current.Plans ?? new List<Plan>()).Where(p => p != null && !string.IsNullOrEmpty(p.Id)).ToList(); } }
        public static List<Addon> Addons { get { return (SiteContent.Current.Addons ?? new List<Addon>()).Where(a => a != null && !string.IsNullOrEmpty(a.Id)).ToList(); } }

        public static List<Plan> PlansFor(string service) { return Plans.Where(p => string.Equals(p.Service, service, StringComparison.OrdinalIgnoreCase)).ToList(); }
        public static bool HasPlans(string service) { try { return PlansFor(service).Count > 0; } catch { return false; } }
        public static Plan Plan(string id) { return Plans.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)); }
        public static Addon Addon(string id) { return Addons.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)); }

        /// <summary>Services that have plans, in the order of the service list.</summary>
        /// <summary>The "What do you need?" choices, keeping only services that have plans (and choices left with at least one).</summary>
        public static List<Goal> Goals
        {
            get
            {
                var with = new HashSet<string>(ServicesWithPlans.Select(s => s.Slug), StringComparer.OrdinalIgnoreCase);
                return (SiteContent.Current.Goals ?? new List<Goal>()).Where(g => g != null && !string.IsNullOrWhiteSpace(g.Name))
                    .Select(g => new Goal { Id = string.IsNullOrEmpty(g.Id) ? BlogPost.Slugify(g.Name) : g.Id, Name = g.Name, Hint = g.Hint, Services = (g.Services ?? new List<string>()).Where(with.Contains).Distinct().ToList() })
                    .Where(g => g.Services.Count > 0).ToList();
            }
        }

        private static Dictionary<string, List<string>> _defaultPairs;
        /// <summary>Services suggested next when a service is in a plan: its "Suggest with" list, else the defaults in App_Data/seed/pairs.json.</summary>
        public static List<string> PairsFor(Service s)
        {
            if (s == null) return new List<string>();
            if (s.PairsWith != null && s.PairsWith.Count > 0) return s.PairsWith.Where(x => x != s.Slug).ToList();
            if (_defaultPairs == null)
            {
                try { _defaultPairs = new JavaScriptSerializer().Deserialize<Dictionary<string, List<string>>>(System.IO.File.ReadAllText(Util.AppPath("App_Data/seed/pairs.json"))); }
                catch { _defaultPairs = new Dictionary<string, List<string>>(); }
            }
            List<string> list;
            return _defaultPairs.TryGetValue(s.Slug ?? "", out list) && list != null ? list : new List<string>();
        }

        /// <summary>"3 plans · from ₹15,000/month" for a collapsed service on /pricing.</summary>
        public static string Teaser(string service)
        {
            var plans = PlansFor(service);
            if (plans.Count == 0) return "";
            var label = plans.Count == 1 ? "1 plan" : plans.Count + " plans";
            if (!ShowPrices) return label;
            var low = plans.OrderBy(p => p.Price).First();
            return label + " · from <b data-inr=\"" + low.Price.ToString(CultureInfo.InvariantCulture) + "\">" + Fx.Format(low.Price, Fx.Base) + "</b>" + BillingLabel(low.Billing);
        }

        public static List<Service> ServicesWithPlans
        {
            get { var withPlans = new HashSet<string>(Plans.Select(p => p.Service ?? ""), StringComparer.OrdinalIgnoreCase); return SiteContent.Current.Services.Where(s => withPlans.Contains(s.Slug)).ToList(); }
        }

        public static string BillingLabel(string billing)
        {
            switch (billing) { case "monthly": return "/month"; case "yearly": return "/year"; case "hourly": return "/hour"; default: return ""; }
        }

        // ---- Quotes -------------------------------------------------------------------------------------------

        public class Pick { public string Kind { get; set; } public string Id { get; set; } public int Qty { get; set; } }

        /// <summary>Prices a selection of plans and extras in a currency, with the best running offer (or the coupon typed).</summary>
        public static QuoteResult Calc(IEnumerable<Pick> picks, string currency, string code)
        {
            currency = string.IsNullOrEmpty(currency) || !Fx.Enabled.Contains(currency.ToUpperInvariant()) ? Fx.Base : currency.ToUpperInvariant();
            // No exchange rate yet (or the feed is down and none was set by hand): quote in rupees rather than guess.
            if (currency != Fx.Base && !Fx.Rates.ContainsKey(currency)) currency = Fx.Base;
            var q = new QuoteResult { Currency = currency, Rate = Fx.Rate(currency), Lines = new List<QuoteLine>() };
            var services = SiteContent.Current.Services;
            foreach (var p in (picks ?? Enumerable.Empty<Pick>()).Take(40))
            {
                if (p == null || string.IsNullOrEmpty(p.Id)) continue;
                QuoteLine line = null;
                if (p.Kind == "addon")
                {
                    var a = Addon(p.Id);
                    if (a == null) continue;
                    var qty = Math.Max(Math.Max(1, a.Min), p.Qty);
                    if (a.Max > 0) qty = Math.Min(qty, a.Max);
                    line = new QuoteLine { Kind = "addon", Id = a.Id, Name = a.Name, Service = a.Service, Billing = a.IsMonthly ? "monthly" : "one-time", Unit = a.Unit, Qty = qty, UnitInr = a.Price };
                }
                else
                {
                    var pl = Plan(p.Id);
                    if (pl == null) continue;
                    var qty = pl.Billing == "hourly" ? Math.Min(2000, Math.Max(1, p.Qty)) : 1;
                    line = new QuoteLine { Kind = "plan", Id = pl.Id, Name = pl.Name, Service = pl.Service, Billing = pl.Billing ?? "one-time", Unit = pl.Billing == "hourly" ? "hour" : null, IsFrom = pl.IsFrom, Qty = qty, UnitInr = pl.Price, SetupInr = pl.SetupFee };
                }
                if (q.Lines.Any(x => x.Kind == line.Kind && x.Id == line.Id)) continue;
                var svc = services.FirstOrDefault(s => s.Slug == line.Service);
                line.ServiceName = svc == null ? null : svc.Name;
                line.UnitPrice = Fx.Convert(line.UnitInr, currency);
                line.Setup = Fx.Convert(line.SetupInr, currency);
                line.Amount = line.UnitPrice * line.Qty;
                q.Lines.Add(line);
                if (line.IsFrom) q.HasFrom = true;
            }
            Func<QuoteLine, bool> monthly = l => l.Billing == "monthly";
            Func<QuoteLine, bool> yearly = l => l.Billing == "yearly";
            q.OneTime = q.Lines.Where(l => !monthly(l) && !yearly(l)).Sum(l => l.Amount) + q.Lines.Sum(l => l.Setup);
            q.Monthly = q.Lines.Where(monthly).Sum(l => l.Amount);
            q.Yearly = q.Lines.Where(yearly).Sum(l => l.Amount);
            q.OneTimeInr = q.Lines.Where(l => !monthly(l) && !yearly(l)).Sum(l => l.UnitInr * l.Qty) + q.Lines.Sum(l => l.SetupInr) + q.Lines.Where(yearly).Sum(l => l.UnitInr * l.Qty);
            q.MonthlyInr = q.Lines.Where(monthly).Sum(l => l.UnitInr * l.Qty);

            // Offers: the coupon typed, else the automatic offer that saves the most.
            var candidates = new List<Offer>();
            if (!string.IsNullOrWhiteSpace(code))
            {
                var o = Offers.ByCode(code);
                if (o == null) q.CodeError = "That code is not valid or has ended.";
                else candidates.Add(o);
            }
            if (candidates.Count == 0) candidates.AddRange(Offers.Active.Where(o => string.IsNullOrWhiteSpace(o.Code)));
            QuoteResult best = null;
            foreach (var o in candidates.Where(o => o.DiscountType == "percent" || o.DiscountType == "amount").Where(o => o.DiscountValue > 0))
            {
                var trial = ApplyOffer(q, o, currency);
                if (trial != null && (best == null || trial.Discount > best.Discount)) best = trial;
                else if (trial == null && !string.IsNullOrWhiteSpace(code) && o.MinOrder > 0) q.CodeError = "This code needs an order of at least " + Fx.Format(Fx.Convert(o.MinOrder, currency), currency) + " before discount.";
            }
            if (best != null) q = best;
            if (q.Discount > 0 && q.Rate > 0) q.DiscountInr = Math.Round(q.Discount / (currency == Fx.Base ? 1m : q.Rate), 0);
            return q;
        }

        private static QuoteResult ApplyOffer(QuoteResult q, Offer o, string currency)
        {
            var due = q.OneTime + q.Monthly + q.Yearly;
            if (o.MinOrder > 0 && due < Fx.Convert(o.MinOrder, currency) && currency != Fx.Base) return null;
            if (o.MinOrder > 0 && currency == Fx.Base && due < o.MinOrder) return null;
            var eligible = q.Lines.Where(l => Offers.ForService(o, l.Service)).ToList();
            if (eligible.Count == 0) return null;
            var r = Copy(q);
            if (o.DiscountType == "percent")
            {
                var pct = Math.Min(100m, o.DiscountValue) / 100m;
                r.DiscountOneTime = Math.Round(eligible.Where(l => l.Billing != "monthly" && l.Billing != "yearly").Sum(l => l.Amount + l.Setup) * pct, currency == Fx.Base ? 0 : 2);
                r.DiscountMonthly = Math.Round(eligible.Where(l => l.Billing == "monthly").Sum(l => l.Amount + 0) * pct, currency == Fx.Base ? 0 : 2);
                r.DiscountYearly = Math.Round(eligible.Where(l => l.Billing == "yearly").Sum(l => l.Amount) * pct, currency == Fx.Base ? 0 : 2);
                r.DiscountOneTime += Math.Round(eligible.Where(l => l.Billing == "monthly" || l.Billing == "yearly").Sum(l => l.Setup) * pct, currency == Fx.Base ? 0 : 2);
                r.OfferNote = o.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture) + "% off " + (o.Services != null && o.Services.Count > 0 ? "eligible services in " : "") + "your first payment";
            }
            else
            {
                var amount = currency == Fx.Base ? o.DiscountValue : Math.Round(o.DiscountValue * q.Rate, 2);
                var pool = eligible.Where(l => l.Billing != "monthly" && l.Billing != "yearly").Sum(l => l.Amount) + eligible.Sum(l => l.Setup);
                if (pool > 0) r.DiscountOneTime = Math.Min(amount, pool);
                else r.DiscountMonthly = Math.Min(amount, eligible.Where(l => l.Billing == "monthly").Sum(l => l.Amount));
                if (r.DiscountOneTime + r.DiscountMonthly <= 0) r.DiscountYearly = Math.Min(amount, eligible.Where(l => l.Billing == "yearly").Sum(l => l.Amount));
                r.OfferNote = Fx.Format(amount, currency) + " off" + (r.DiscountMonthly > 0 ? " your first month" : "");
            }
            if (r.Discount <= 0) return null;
            r.OfferId = o.Id; r.OfferTitle = o.Title;
            return r;
        }

        private static QuoteResult Copy(QuoteResult q)
        {
            return new QuoteResult
            {
                Currency = q.Currency, Rate = q.Rate, Lines = q.Lines, OneTime = q.OneTime, Monthly = q.Monthly, Yearly = q.Yearly,
                OneTimeInr = q.OneTimeInr, MonthlyInr = q.MonthlyInr, HasFrom = q.HasFrom, CodeError = q.CodeError
            };
        }

        /// <summary>Plain-text summary of a quote, for the CRM and emails.</summary>
        public static string Summary(QuoteResult q)
        {
            var sb = new StringBuilder();
            foreach (var l in q.Lines)
            {
                sb.Append("• ").Append(l.ServiceName != null && l.Kind == "plan" ? l.ServiceName + ": " : "").Append(l.Name);
                if (l.Qty > 1 || l.Unit != null) sb.Append(" × ").Append(l.Qty).Append(l.Unit != null ? " " + l.Unit + (l.Qty == 1 ? "" : "s") : "");
                sb.Append(" — ").Append(l.IsFrom ? "from " : "").Append(q.Money(l.Amount)).Append(BillingLabel(l.Billing == "hourly" ? "" : l.Billing));
                if (l.Setup > 0) sb.Append(" + ").Append(q.Money(l.Setup)).Append(" set-up");
                sb.Append("\n");
            }
            if (q.Discount > 0) sb.Append("Offer: ").Append(q.OfferTitle).Append(" (−").Append(q.Money(q.Discount)).Append(")\n");
            sb.Append("First payment: ").Append(q.Money(q.DueNow));
            if (q.Monthly > 0) sb.Append(", then ").Append(q.Money(q.Monthly)).Append("/month");
            sb.Append(" (before ").Append(TaxName).Append(")");
            return sb.ToString();
        }

        // ---- Markup -------------------------------------------------------------------------------------------

        /// <summary>A price with the data the browser needs to show it in the visitor's currency.</summary>
        public static string PriceHtml(decimal inr, string billing, bool isFrom, string cls = "price")
        {
            if (!ShowPrices) return "<span class=\"" + cls + " " + cls + "--ask\">Price on request</span>";
            return "<span class=\"" + cls + "\">" + (isFrom ? "<small>From</small> " : "") + "<b data-inr=\"" + inr.ToString(CultureInfo.InvariantCulture) + "\">" + Fx.Format(inr, Fx.Base) + "</b>"
                 + (BillingLabel(billing).Length > 0 ? "<small>" + BillingLabel(billing) + "</small>" : "") + "</span>";
        }

        /// <summary>How many plan features show before "more included".</summary>
        public const int CardFeatures = 4;

        /// <summary>Plan cards for one service (service pages and /pricing).</summary>
        public static string PlanCards(string service, bool onServicePage = false)
        {
            List<Plan> plans;
            try { plans = PlansFor(service); } catch { return ""; }
            if (plans.Count == 0) return "";
            var sb = new StringBuilder("<div class=\"plans\" data-plans>");
            foreach (var p in plans)
            {
                sb.Append("<article class=\"plan").Append(p.Popular ? " plan--popular" : "").Append("\" data-plan=\"").Append(H(p.Id)).Append("\">");
                if (p.Popular) sb.Append("<span class=\"plan__flag\">Most popular</span>");
                sb.Append("<h3>").Append(H(p.Name)).Append("</h3>");
                if (!string.IsNullOrEmpty(p.Tagline)) sb.Append("<p class=\"plan__tag\">").Append(H(p.Tagline)).Append("</p>");
                sb.Append(PriceHtml(p.Price, p.Billing, p.IsFrom, "plan__price"));
                if (ShowPrices && p.SetupFee > 0) sb.Append("<p class=\"plan__setup\">+ <span data-inr=\"").Append(p.SetupFee.ToString(CultureInfo.InvariantCulture)).Append("\">").Append(Fx.Format(p.SetupFee, Fx.Base)).Append("</span> one-time set-up</p>");
                if (p.MinMonths > 1 && p.IsMonthly) sb.Append("<p class=\"plan__setup\">Minimum ").Append(p.MinMonths).Append(" months</p>");
                if (p.Features != null && p.Features.Count > 0)
                {
                    // The first few points keep cards short; the rest open on request.
                    var feats = p.Features.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
                    sb.Append("<ul class=\"plan__list\">");
                    foreach (var f in feats.Take(CardFeatures)) sb.Append("<li>").Append(H(f)).Append("</li>");
                    sb.Append("</ul>");
                    if (feats.Count > CardFeatures)
                    {
                        sb.Append("<details class=\"plan__more\"><summary>").Append(feats.Count - CardFeatures).Append(" more included</summary><ul class=\"plan__list\">");
                        foreach (var f in feats.Skip(CardFeatures)) sb.Append("<li>").Append(H(f)).Append("</li>");
                        sb.Append("</ul></details>");
                    }
                }
                if (!string.IsNullOrEmpty(p.Timeline)) sb.Append("<p class=\"plan__time\">").Append(H(p.Timeline)).Append("</p>");
                sb.Append("<div class=\"plan__cta\">");
                if (onServicePage) sb.Append("<a class=\"btn ").Append(p.Popular ? "btn--blue" : "btn--line").Append(" btn--sm\" href=\"/pricing?add=").Append(HttpUtility.UrlEncode(p.Id)).Append("#builder\">Choose ").Append(H(p.Name)).Append("</a>");
                else sb.Append("<button type=\"button\" class=\"btn ").Append(p.Popular ? "btn--blue" : "btn--line").Append(" btn--sm\" data-add-plan=\"").Append(H(p.Id)).Append("\" aria-pressed=\"false\">Add to my plan</button>");
                sb.Append("</div></article>");
            }
            return sb.Append("</div>").ToString();
        }

        /// <summary>Plans and extras for the plan builder script (only what it needs; the server prices the quote again).</summary>
        public static string BuilderJson()
        {
            var show = ShowPrices;
            var services = SiteContent.Current.Services;
            var data = new
            {
                show,
                tax = new { name = TaxName, pct = TaxPct },
                goals = Goals.Select(g => new { id = g.Id, name = g.Name, services = g.Services }),
                services = ServicesWithPlans.Select(s => new { slug = s.Slug, name = s.Name, pairs = PairsFor(s) }),
                plans = Plans.Select(p => new { id = p.Id, service = p.Service, serviceName = (services.FirstOrDefault(s => s.Slug == p.Service) ?? new Service()).Name, name = p.Name, price = show ? p.Price : 0, billing = p.Billing ?? "one-time", setup = show ? p.SetupFee : 0, from = p.IsFrom, popular = p.Popular }),
                addons = Addons.Select(a => new { id = a.Id, service = a.Service, group = string.IsNullOrEmpty(a.Group) ? "Extras" : a.Group, name = a.Name, description = a.Description, price = show ? a.Price : 0, billing = a.IsMonthly ? "monthly" : "one-time", unit = a.Unit, min = Math.Max(1, a.Min), max = a.Max })
            };
            return new JavaScriptSerializer().Serialize(data).Replace("</", "<\\/");
        }

        /// <summary>schema.org OfferCatalog for /pricing, so Google can show the services and starting prices (only when prices are public).</summary>
        public static string CatalogLd()
        {
            if (!PublicPrices) return "";
            var site = Mailer.SiteUrl;
            var items = ServicesWithPlans.Select(s => new Dictionary<string, object>
            {
                { "@type", "Offer" },
                { "itemOffered", new Dictionary<string, object> { { "@type", "Service" }, { "name", s.Name }, { "url", site + s.Url }, { "provider", new Dictionary<string, object> { { "@type", "Organization" }, { "name", "Yenetch" }, { "url", site } } } } },
                { "priceSpecification", new Dictionary<string, object> { { "@type", "PriceSpecification" }, { "priceCurrency", "INR" }, { "minPrice", PlansFor(s.Slug).Min(p => p.Price) } } }
            }).ToList();
            var ld = new Dictionary<string, object> { { "@context", "https://schema.org" }, { "@type", "OfferCatalog" }, { "name", "Yenetch plans and pricing" }, { "url", site + "/pricing" }, { "itemListElement", items } };
            return "<script type=\"application/ld+json\">" + new JavaScriptSerializer().Serialize(ld).Replace("</", "<\\/") + "</script>";
        }

        private static string H(string s) { return HttpUtility.HtmlEncode(s ?? ""); }
    }
}
