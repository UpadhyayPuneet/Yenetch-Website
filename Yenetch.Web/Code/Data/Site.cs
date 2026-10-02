using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>
    /// Company details and small null-safe helpers for page markup (phone, email, WhatsApp, social links, offices,
    /// service lists, JSON-LD), read from the CMS database through SiteContent. Generated pages bind to these,
    /// so changes in /admin/content show without a rebuild.
    /// </summary>
    public static class Site
    {
        public static Company Company { get { return SiteContent.Current.Company ?? new Company(); } }

        /// <summary>tel: link with the country code (10-digit Indian numbers get +91).</summary>
        public static string PhoneTel
        {
            get
            {
                var raw = Company.Phone ?? "";
                var digits = new string(raw.Where(char.IsDigit).ToArray());
                if (digits.Length == 10) digits = "91" + digits;
                return "tel:" + (raw.TrimStart().StartsWith("+") || digits.Length > 10 ? "+" : "") + digits;
            }
        }

        /// <summary>WhatsApp chat link with a prefilled message.</summary>
        public static string WhatsApp(string text)
        {
            var url = Company.Whatsapp ?? "";
            if (url.Length > 0 && !url.StartsWith("http")) url = "https://wa.me/" + new string(url.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(text) ? url : url + (url.Contains("?") ? "&" : "?") + "text=" + Uri.EscapeDataString(text);
        }

        public static string MailTo(string subject)
        {
            return "mailto:" + Company.Email + (string.IsNullOrEmpty(subject) ? "" : "?subject=" + Uri.EscapeDataString(subject));
        }

        /// <summary>Replaces {phone}, {email} and {name} in page titles and descriptions (used by Site.Master).</summary>
        public static string Expand(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var c = Company;
            return text.Replace("{phone}", c.Phone ?? "").Replace("{email}", c.Email ?? "").Replace("{name}", c.Name ?? "Yenetch");
        }

        // ------------------------------------------------------------------ lists (never null, safe for Repeater.DataSource)

        public static List<Office> Offices { get { return Company.Offices ?? new List<Office>(); } }
        public static List<SocialLink> Social { get { return Company.Social ?? new List<SocialLink>(); } }
        public static List<CompanyValue> Values { get { return Company.Values ?? new List<CompanyValue>(); } }
        public static List<Stat> Stats { get { return Company.Stats ?? new List<Stat>(); } }
        public static List<Service> Services { get { return SiteContent.Current.Services ?? new List<Service>(); } }
        public static List<Product> Products { get { return SiteContent.Current.Products ?? new List<Product>(); } }
        public static List<Pillar> Pillars { get { return SiteContent.Current.Pillars ?? new List<Pillar>(); } }
        public static List<CaseStudy> CaseStudies { get { return SiteContent.Current.CaseStudies ?? new List<CaseStudy>(); } }
        public static List<string> Clients { get { return SiteContent.Current.Clients ?? new List<string>(); } }
        /// <summary>Logo image for a client, or "" when the client has none (the name is shown instead).</summary>
        public static string ClientLogo(string name)
        {
            var logos = SiteContent.Current.ClientLogos;
            string v;
            return logos != null && name != null && logos.TryGetValue(name, out v) && !string.IsNullOrEmpty(v) ? Photos.Src(v) : "";
        }

        /// <summary>Image address for a testimonial photo (upload, full address or built-in photo name), or "".</summary>
        public static string PhotoUrl(string photo) { return string.IsNullOrWhiteSpace(photo) ? "" : Photos.Src(photo.Trim()); }

        public static List<Testimonial> Testimonials { get { return SiteContent.Current.Testimonials ?? new List<Testimonial>(); } }
        public static List<ProcessStep> Process { get { return SiteContent.Current.Process ?? new List<ProcessStep>(); } }
        public static List<Industry> Industries { get { return SiteContent.Current.Industries ?? new List<Industry>(); } }
        public static List<Squad> Squads { get { var t = SiteContent.Current.Team; return t != null && t.Squads != null ? t.Squads : new List<Squad>(); } }
        public static Careers Careers { get { return SiteContent.Current.Careers ?? new Careers(); } }
        public static List<TitleText> Perks { get { return Careers.Perks ?? new List<TitleText>(); } }
        public static List<Job> Jobs { get { return Careers.Roles ?? new List<Job>(); } }
        public static List<ProcessStep> HiringSteps { get { return Careers.Steps ?? new List<ProcessStep>(); } }

        /// <summary>Talent &amp; Resourcing page copy (pages.json "talent"), never null.</summary>
        public static TalentCopy Talent { get { return (SiteContent.Copy != null ? SiteContent.Copy.Talent : null) ?? new TalentCopy(); } }

        public static List<Service> ServicesFor(string pillarId) { return Services.Where(s => s.Pillar == pillarId).ToList(); }

        /// <summary>Service by slug, or an empty Service (so markup never throws when a slug is removed).</summary>
        public static Service Service(string slug) { return Services.FirstOrDefault(s => s.Slug == slug) ?? new Service { Slug = slug, Includes = new List<string>() }; }

        /// <summary>Pillar by id, or an empty Pillar.</summary>
        public static Pillar Pillar(string id) { return Pillars.FirstOrDefault(p => p.Id == id) ?? new Pillar { Id = id }; }

        /// <summary>The first n items of a list (null-safe).</summary>
        public static List<string> Top(IEnumerable<string> items, int n) { return (items ?? Enumerable.Empty<string>()).Take(n).ToList(); }

        /// <summary>Text up to the first full stop (short price notes on product cards).</summary>
        public static string FirstSentence(string text) { return (text ?? "").Split('.')[0]; }

        public static string Join(IEnumerable<string> items, string separator) { return string.Join(separator, items ?? Enumerable.Empty<string>()); }

        /// <summary>Distinct case-study industries, sorted (filter chips on /case-studies).</summary>
        public static List<string> CaseIndustries
        {
            get { return CaseStudies.Select(c => c.Industry).Where(i => !string.IsNullOrEmpty(i)).Distinct().OrderBy(i => i, StringComparer.Ordinal).ToList(); }
        }

        /// <summary>Options for the contact form's "I'm interested in" list: every service, every product, then "Something else".</summary>
        public static List<string> ContactTopics
        {
            get { return Services.Select(s => s.Name).Concat(Products.Select(p => p.Name)).Concat(new[] { "Something else" }).Distinct().ToList(); }
        }

        /// <summary>Contact topic for /contact?service={slug}, or null when the slug is unknown.</summary>
        public static string ContactTopic(string serviceSlug)
        {
            var s = Services.FirstOrDefault(x => x.Slug == serviceSlug);
            return s == null ? null : s.Name;
        }

        /// <summary>Footer label for a service: drops the bracketed part and long suffixes.</summary>
        public static string ShortName(string name)
        {
            var n = name ?? "";
            var i = n.IndexOf(" (", StringComparison.Ordinal);
            if (i >= 0) n = n.Substring(0, i);
            return n.Replace(" & Paid Media", "").Replace(" & Community", "").Replace("Website & Web Application Development", "Web Development");
        }

        /// <summary>A company stat value whose label contains the keyword (e.g. "served", "satisfaction"), or the fallback.</summary>
        public static string StatValue(string keyword, string fallback)
        {
            var s = Stats.FirstOrDefault(x => x.Label != null && x.Label.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            return s == null || string.IsNullOrEmpty(s.Value) ? fallback : s.Value;
        }

        /// <summary>Office cities joined for copy, e.g. "Gurugram &amp; Jaipur".</summary>
        public static string OfficeCities
        {
            get
            {
                var c = Offices.Select(o => o.City).Where(x => !string.IsNullOrEmpty(x)).ToList();
                return c.Count <= 1 ? string.Join("", c) : string.Join(", ", c.Take(c.Count - 1)) + " & " + c[c.Count - 1];
            }
        }

        private static readonly string[] Ones = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve",
            "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        private static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

        /// <summary>Number in words with a capital first letter ("Twenty-one"); digits from 100 up.</summary>
        public static string CountWord(int n)
        {
            string w;
            if (n < 0 || n >= 100) w = n.ToString();
            else if (n < 20) w = Ones[n];
            else w = Tens[n / 10] + (n % 10 == 0 ? "" : "-" + Ones[n % 10]);
            return w.Substring(0, 1).ToUpperInvariant() + w.Substring(1);
        }

        // ------------------------------------------------------------------ social icons

        /// <summary>Generic link glyph for social networks without a brand icon.</summary>
        public const string GenericIconPath = "M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z";

        /// <summary>SVG path (24x24 viewBox) for a social network id such as "linkedin"; a link glyph for unknown ids.</summary>
        public static string SocialIconPath(string id)
        {
            string d;
            return id != null && SocialIcons.Paths.TryGetValue(id.Trim().ToLowerInvariant(), out d) ? d : GenericIconPath;
        }

        // ------------------------------------------------------------------ structured data

        /// <summary>schema.org Organization JSON-LD for Site.Master, built from the company record (safe inside &lt;script&gt;).</summary>
        public static string OrganizationLd
        {
            get
            {
                var c = Company;
                var digits = PhoneTel.Substring(4);
                var tel = digits.StartsWith("+91") && digits.Length == 13 ? "+91-" + digits.Substring(3) : digits;
                var ld = new Dictionary<string, object> {
                    { "@context", "https://schema.org" }, { "@type", "Organization" }, { "name", string.IsNullOrEmpty(c.Name) ? "Yenetch" : c.Name },
                    { "url", Seo.Root }, { "logo", Seo.Root + "/assets/img/logo.png" },
                    { "foundingDate", c.Founded > 0 ? c.Founded.ToString() : "2019" }, { "telephone", tel },
                    { "sameAs", Social.Where(s => s.Id != "whatsapp" && !string.IsNullOrEmpty(s.Url)).Select(s => s.Url).ToList() },
                    { "address", Offices.Select(o => new Dictionary<string, object> {
                        { "@type", "PostalAddress" }, { "streetAddress", o.Address ?? "" }, { "addressLocality", o.City ?? "" }, { "addressCountry", "IN" } }).ToList() }
                };
                return new JavaScriptSerializer().Serialize(ld).Replace("</", "<\\/");
            }
        }
    }
}
