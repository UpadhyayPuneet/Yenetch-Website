using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>schema.org JSON-LD builders. Pages render the result inside &lt;script type="application/ld+json"&gt;.</summary>
    public static class Seo
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static string Root
        {
            get { return (System.Configuration.ConfigurationManager.AppSettings["SiteUrl"] ?? "https://www.yenetch.com").TrimEnd('/'); }
        }

        private static string Out(object o) { return Json.Serialize(o).Replace("</", "<\\/"); }

        public static string FaqPage(IEnumerable<Faq> faqs)
        {
            var list = (faqs ?? Enumerable.Empty<Faq>()).ToList();
            if (list.Count == 0) return "{}";
            return Out(new Dictionary<string, object> {
                { "@context", "https://schema.org" }, { "@type", "FAQPage" },
                { "mainEntity", list.Select(f => new Dictionary<string, object> {
                    { "@type", "Question" }, { "name", f.Q },
                    { "acceptedAnswer", new Dictionary<string, object> { { "@type", "Answer" }, { "text", f.A } } } }).ToList() } });
        }

        public static string Breadcrumbs(params string[] nameUrlPairs)
        {
            var items = new List<object>();
            for (int i = 0; i + 1 < nameUrlPairs.Length; i += 2)
                items.Add(new Dictionary<string, object> { { "@type", "ListItem" }, { "position", i / 2 + 1 }, { "name", nameUrlPairs[i] }, { "item", Root + nameUrlPairs[i + 1] } });
            return Out(new Dictionary<string, object> { { "@context", "https://schema.org" }, { "@type", "BreadcrumbList" }, { "itemListElement", items } });
        }

        public static string Service(Service s, string description)
        {
            return Out(new Dictionary<string, object> {
                { "@context", "https://schema.org" }, { "@type", "Service" }, { "name", s.Name }, { "description", description },
                { "serviceType", s.Name }, { "url", Root + s.Url }, { "areaServed", "IN" },
                { "provider", new Dictionary<string, object> { { "@type", "Organization" }, { "name", "Yenetch" }, { "url", Root } } } });
        }

        public static string Product(Product p, string description)
        {
            return Out(new Dictionary<string, object> {
                { "@context", "https://schema.org" }, { "@type", "SoftwareApplication" }, { "name", p.Name }, { "description", description },
                { "applicationCategory", "BusinessApplication" }, { "operatingSystem", "Web" }, { "url", Root + p.Url },
                { "publisher", new Dictionary<string, object> { { "@type", "Organization" }, { "name", "Yenetch" } } } });
        }

        public static string Article(BlogPost p)
        {
            return Out(new Dictionary<string, object> {
                { "@context", "https://schema.org" }, { "@type", "BlogPosting" }, { "headline", p.Title }, { "description", p.SeoDescription },
                { "datePublished", p.Date }, { "dateModified", p.Date }, { "mainEntityOfPage", Root + p.Url },
                { "image", Root + Photos.Src(p.CoverImage) },
                { "author", new Dictionary<string, object> { { "@type", "Organization" }, { "name", p.Author ?? "Yenetch" } } },
                { "publisher", new Dictionary<string, object> { { "@type", "Organization" }, { "name", "Yenetch" },
                    { "logo", new Dictionary<string, object> { { "@type", "ImageObject" }, { "url", Root + "/assets/img/logo.png" } } } } } });
        }

        /// <summary>Local business data for one office (Google Maps and local results). url: the page it describes.</summary>
        public static Dictionary<string, object> LocalBusinessData(Office o, string url, string description)
        {
            var c = Site.Company;
            var name = (string.IsNullOrEmpty(c.Name) ? "Yenetch" : c.Name) + (o == null || string.IsNullOrEmpty(o.City) ? "" : " " + o.City);
            var d = new Dictionary<string, object> {
                { "@context", "https://schema.org" }, { "@type", "ProfessionalService" }, { "name", name }, { "url", Root + (url ?? "") },
                { "image", Root + "/assets/img/og-image.png" }, { "logo", Root + "/assets/img/logo.png" }, { "telephone", Site.PhoneTel.Replace("tel:", "") },
                { "email", c.Email ?? "" }, { "priceRange", "₹₹" }, { "parentOrganization", new Dictionary<string, object> { { "@type", "Organization" }, { "name", string.IsNullOrEmpty(c.Name) ? "Yenetch" : c.Name }, { "url", Root } } },
                { "openingHoursSpecification", new[] { new Dictionary<string, object> { { "@type", "OpeningHoursSpecification" }, { "dayOfWeek", new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" } }, { "opens", "10:00" }, { "closes", "19:00" } } } }
            };
            if (!string.IsNullOrEmpty(description)) d["description"] = description;
            if (o != null)
            {
                d["address"] = new Dictionary<string, object> { { "@type", "PostalAddress" }, { "streetAddress", o.Address ?? "" }, { "addressLocality", o.City ?? "" }, { "addressCountry", "IN" } };
                d["areaServed"] = new Dictionary<string, object> { { "@type", "City" }, { "name", o.City ?? "" } };
                if (!string.IsNullOrEmpty(o.Map)) d["hasMap"] = o.Map;
            }
            var same = Site.Social.Where(x => x.Id != "whatsapp" && !string.IsNullOrEmpty(x.Url)).Select(x => x.Url).ToList();
            if (same.Count > 0) d["sameAs"] = same;
            return d;
        }

        public static string LocalBusiness(Office o, string url, string description) { return Out(LocalBusinessData(o, url, description)); }

        /// <summary>All offices as local businesses (Contact page).</summary>
        public static string LocalBusinesses()
        {
            var list = Site.Offices.Select(o => (object)LocalBusinessData(o, "/contact", null)).ToList();
            return list.Count == 0 ? "{}" : Out(list);
        }

        /// <summary>The Careers page's JobPosting script tag, or nothing when no role has a description yet.</summary>
        public static string JobPostingsTag()
        {
            var json = JobPostings(Site.Jobs);
            return string.IsNullOrEmpty(json) ? "" : "<script type=\"application/ld+json\">" + json + "</script>";
        }

        /// <summary>Google for Jobs: one JobPosting per open role that has a description.</summary>
        public static string JobPostings(IEnumerable<Job> roles)
        {
            var c = Site.Company;
            var org = string.IsNullOrEmpty(c.Name) ? "Yenetch" : c.Name;
            var today = System.DateTime.UtcNow.AddHours(5.5).ToString("yyyy-MM-dd");
            var list = new List<object>();
            foreach (var j in (roles ?? Enumerable.Empty<Job>()).Where(j => !string.IsNullOrWhiteSpace(j.Description) && !string.IsNullOrWhiteSpace(j.Title)))
            {
                var city = (j.Location ?? "").Split(new[] { ',', '/', '·', '&' })[0].Trim();
                var office = Site.Offices.FirstOrDefault(o => !string.IsNullOrEmpty(o.City) && (j.Location ?? "").IndexOf(o.City, System.StringComparison.OrdinalIgnoreCase) >= 0);
                var desc = j.Description.Contains("<") ? j.Description : "<p>" + System.Web.HttpUtility.HtmlEncode(j.Description).Replace("\r\n", "\n").Replace("\n\n", "</p><p>").Replace("\n", "<br>") + "</p>";
                var d = new Dictionary<string, object> {
                    { "@context", "https://schema.org" }, { "@type", "JobPosting" }, { "title", j.Title }, { "description", desc },
                    { "datePosted", string.IsNullOrWhiteSpace(j.Posted) ? today : j.Posted.Trim() },
                    { "employmentType", string.IsNullOrWhiteSpace(j.EmploymentType) ? "FULL_TIME" : j.EmploymentType },
                    { "hiringOrganization", new Dictionary<string, object> { { "@type", "Organization" }, { "name", org }, { "sameAs", Root }, { "logo", Root + "/assets/img/logo.png" } } },
                    { "directApply", true }, { "url", Root + "/careers" }
                };
                if (!string.IsNullOrWhiteSpace(j.ValidThrough)) d["validThrough"] = j.ValidThrough.Trim();
                if (j.Remote)
                {
                    d["jobLocationType"] = "TELECOMMUTE";
                    d["applicantLocationRequirements"] = new Dictionary<string, object> { { "@type", "Country" }, { "name", "India" } };
                }
                else
                    d["jobLocation"] = new Dictionary<string, object> { { "@type", "Place" }, { "address", new Dictionary<string, object> {
                        { "@type", "PostalAddress" }, { "streetAddress", office != null ? office.Address ?? "" : "" }, { "addressLocality", office != null ? office.City : city },
                        { "addressRegion", (office != null ? office.City : city) == "Jaipur" ? "Rajasthan" : (office != null ? office.City : city) == "Gurugram" ? "Haryana" : "" }, { "addressCountry", "IN" } } } };
                decimal min, max;
                var hasMin = decimal.TryParse(j.SalaryMin, out min) && min > 0;
                var hasMax = decimal.TryParse(j.SalaryMax, out max) && max > 0;
                if (hasMin || hasMax)
                {
                    var v = new Dictionary<string, object> { { "@type", "QuantitativeValue" }, { "unitText", string.IsNullOrEmpty(j.SalaryUnit) ? "MONTH" : j.SalaryUnit } };
                    if (hasMin && hasMax) { v["minValue"] = min; v["maxValue"] = max; } else v["value"] = hasMin ? min : max;
                    d["baseSalary"] = new Dictionary<string, object> { { "@type", "MonetaryAmount" }, { "currency", "INR" }, { "value", v } };
                }
                list.Add(d);
            }
            return list.Count == 0 ? "" : Out(list);
        }
    }
}
