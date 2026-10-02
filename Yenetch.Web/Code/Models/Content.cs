using System;
using System.Collections.Generic;

namespace Yenetch.Models
{
    // Shapes mirror assets/data/yenetch.json (which also feeds the chatbot and solution finder)
    // and assets/data/pages.json (long-form copy for service, product and talent pages).
    // JSON keys are camelCase; JavaScriptSerializer maps them to these PascalCase properties.

    public class SiteData
    {
        public Company Company { get; set; }
        public List<Pillar> Pillars { get; set; }
        public List<Service> Services { get; set; }
        public List<Product> Products { get; set; }
        public List<CaseStudy> CaseStudies { get; set; }
        public List<string> Clients { get; set; }
        /// <summary>Client name to logo image (only clients that have one).</summary>
        public Dictionary<string, string> ClientLogos { get; set; }
        public List<Testimonial> Testimonials { get; set; }
        public List<ProcessStep> Process { get; set; }
        public List<Faq> Faqs { get; set; }
        public List<BlogPost> Blog { get; set; }
        public Team Team { get; set; }
        public List<Industry> Industries { get; set; }
        public Careers Careers { get; set; }
    }

    public class Company
    {
        public string Name { get; set; }
        public string Tagline { get; set; }
        public int Founded { get; set; }
        public string Team { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Whatsapp { get; set; }
        public string Recognition { get; set; }
        public List<Office> Offices { get; set; }
        public List<Stat> Stats { get; set; }
        public List<CompanyValue> Values { get; set; }
        public List<SocialLink> Social { get; set; }
    }

    public class Office
    {
        public string City { get; set; }
        public string Label { get; set; }
        public string Address { get; set; }
        public string Photo { get; set; }
        public string Map { get; set; }
    }

    public class SocialLink { public string Id { get; set; } public string Name { get; set; } public string Url { get; set; } }
    public class Stat { public string Value { get; set; } public string Label { get; set; } }
    public class CompanyValue { public string Name { get; set; } public string Text { get; set; } }
    public class TitleText { public string Title { get; set; } public string Text { get; set; } }

    public class Pillar
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Short { get; set; }
        public string Summary { get; set; }
        public string Url { get; set; }
        public string Photo { get; set; }
    }

    public class Service
    {
        public string Slug { get; set; }
        public string Pillar { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Photo { get; set; }
        /// <summary>Which built-in illustration the service page shows: dashboard, serp, social, crm, reviews, dev, store, design, ai, cloud, talent.</summary>
        public string Visual { get; set; }
        public List<string> Includes { get; set; }
        public List<string> Keywords { get; set; }
        public string Url { get { return "/services/" + Slug; } }
    }

    public class Product
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Summary { get; set; }
        public string Photo { get; set; }
        public List<string> Features { get; set; }
        public string Price { get; set; }
        public string PriceNote { get; set; }
        public List<string> Keywords { get; set; }
        public string Url { get { return "/products/" + Slug; } }
        public string ShortName { get { return (Name ?? "").Replace("Yenetch ", ""); } }

        public string StatusCss
        {
            get
            {
                switch (Status)
                {
                    case "Live": return "pill--live";
                    case "Early access": return "pill--new";
                    default: return "pill--soon";
                }
            }
        }
    }

    public class Metric { public string Value { get; set; } public string Label { get; set; } }
    public class Quote { public string Text { get; set; } public string Name { get; set; } public string Role { get; set; } }

    public class CaseStudy
    {
        public string Slug { get; set; }
        public string Client { get; set; }
        public string Industry { get; set; }
        public string Photo { get; set; }
        public List<string> Services { get; set; }
        public string Title { get; set; }
        public string Challenge { get; set; }
        public string Solution { get; set; }
        public List<Metric> Metrics { get; set; }
        public Quote Quote { get; set; }
        public string Url { get; set; }
        public string PageUrl { get { return "/case-studies/" + Slug; } }
        public string UrlLabel { get { return string.IsNullOrEmpty(Url) ? "" : Url.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/'); } }
        public Metric Headline { get { return Metrics != null && Metrics.Count > 0 ? Metrics[0] : new Metric(); } }
        public bool HasQuote { get { return Quote != null && !string.IsNullOrEmpty(Quote.Text); } }
    }

    public class Testimonial
    {
        public string Text { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Photo { get; set; }
        public string Initial { get { return string.IsNullOrEmpty(Name) ? "" : Name.Substring(0, 1); } }
    }

    public class ProcessStep { public string Name { get; set; } public string Text { get; set; } }
    public class Faq { public string Q { get; set; } public string A { get; set; } public List<string> Keywords { get; set; } }

    public class Team
    {
        public List<Leader> Leadership { get; set; }
        public List<Squad> Squads { get; set; }
    }

    /// <summary>Leadership card. Name, photo and LinkedIn are optional: the card shows the role until they are filled in.</summary>
    public class Leader
    {
        public string Role { get; set; }
        public string Remit { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public string Linkedin { get; set; }
        public string Title { get { return string.IsNullOrEmpty(Name) ? Role : Name; } }
        public string Subtitle { get { return string.IsNullOrEmpty(Name) ? Remit : Role; } }
        public bool HasPhoto { get { return !string.IsNullOrEmpty(Photo); } }
        public bool HasName { get { return !string.IsNullOrEmpty(Name); } }
    }

    public class Squad { public string Name { get; set; } public string Text { get; set; } public List<string> Roles { get; set; } }
    public class Industry { public string Name { get; set; } public string Photo { get; set; } public List<string> Clients { get; set; } }

    public class Careers
    {
        public List<TitleText> Perks { get; set; }
        public List<Job> Roles { get; set; }
        public List<ProcessStep> Steps { get; set; }
    }

    public class Job
    {
        public string Title { get; set; }
        public string Team { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        /// <summary>Google for Jobs: the full job description (plain text or simple HTML). Without it the role is not sent to Google.</summary>
        public string Description { get; set; }
        /// <summary>yyyy-MM-dd.</summary>
        public string Posted { get; set; }
        public string ValidThrough { get; set; }
        /// <summary>FULL_TIME, PART_TIME, CONTRACTOR, INTERN or TEMPORARY.</summary>
        public string EmploymentType { get; set; }
        public string SalaryMin { get; set; }
        public string SalaryMax { get; set; }
        /// <summary>MONTH or YEAR.</summary>
        public string SalaryUnit { get; set; }
        public bool Remote { get; set; }
    }

    // ---- long-form page copy (assets/data/pages.json) ----

    public class PageCopy
    {
        public Dictionary<string, ServiceCopy> Services { get; set; }
        public Dictionary<string, ProductCopy> Products { get; set; }
        public TalentCopy Talent { get; set; }
    }

    public class ServiceCopy
    {
        public string Headline { get; set; }
        public string SeoTitle { get; set; }
        public string SeoDescription { get; set; }
        public List<string> Intro { get; set; }
        public List<TitleText> Outcomes { get; set; }
        public List<TitleText> Deliverables { get; set; }
        public List<ProcessStep> Process { get; set; }
        public List<string> Tools { get; set; }
        public List<ProcessStep> Engagement { get; set; }
        public List<string> IdealFor { get; set; }
        public List<Faq> Faqs { get; set; }
    }

    public class ProductCopy
    {
        public string Headline { get; set; }
        public string SeoTitle { get; set; }
        public string SeoDescription { get; set; }
        public List<string> Intro { get; set; }
        public List<TitleText> Highlights { get; set; }
        public List<ProcessStep> HowItWorks { get; set; }
        public List<string> WhoFor { get; set; }
        public List<Faq> Faqs { get; set; }
    }

    public class TalentCopy
    {
        public string Headline { get; set; }
        public List<string> Intro { get; set; }
        public List<RoleFamily> Roles { get; set; }
        public List<EngagementModel> Models { get; set; }
        public List<ProcessStep> Steps { get; set; }
        public List<TitleText> Guarantees { get; set; }
        public List<Faq> Faqs { get; set; }
    }

    public class RoleFamily { public string Name { get; set; } public List<string> Skills { get; set; } }
    public class EngagementModel { public string Name { get; set; } public string Text { get; set; } public string BestFor { get; set; } public string Timeline { get; set; } }

    // ---- blog ----

    // Blog post. Today it is loaded from yenetch.json (body from assets/data/blog/{slug}.html);
    // the same shape maps 1:1 to the dbo.BlogPosts table (App_Data/sql/schema.sql).
    public class BlogPost
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Category { get; set; }
        public string Excerpt { get; set; }
        public string BodyHtml { get; set; }
        public string Author { get; set; }
        public string CoverImage { get; set; }
        public int ReadMinutes { get; set; }
        public string Date { get; set; }
        public List<string> Tags { get; set; }

        public DateTime PublishedOn { get { DateTime d; return DateTime.TryParse(Date, out d) ? d : DateTime.MinValue; } }
        public string DisplayDate { get { return PublishedOn.ToString("d MMM yyyy"); } }
        public string Url { get { return "/blog/" + Slug; } }
        public string CategoryUrl { get { return "/blog/category/" + Slugify(Category); } }
        public string SeoTitle { get { return string.IsNullOrEmpty(MetaTitle) ? Title : MetaTitle; } }
        public string SeoDescription { get { return string.IsNullOrEmpty(MetaDescription) ? Excerpt : MetaDescription; } }

        public static string Slugify(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var chars = new System.Text.StringBuilder();
            foreach (var ch in s.ToLowerInvariant())
                chars.Append(char.IsLetterOrDigit(ch) ? ch : '-');
            var slug = chars.ToString();
            while (slug.Contains("--")) slug = slug.Replace("--", "-");
            return slug.Trim('-');
        }
    }

    public class TocItem { public string Id { get; set; } public string Text { get; set; } }

    /// <summary>A landing page at /{slug}, managed in Admin &gt; Content &gt; Landing pages (for example /digital-marketing-agency-in-jaipur).</summary>
    public class LandingPage
    {
        public string Slug { get; set; }
        public string SeoTitle { get; set; }
        public string SeoDescription { get; set; }
        public string Kicker { get; set; }
        public string Headline { get; set; }
        public string Lead { get; set; }
        public string Photo { get; set; }
        public string City { get; set; }
        public string Industry { get; set; }
        public List<string> Intro { get; set; }
        public List<string> Highlights { get; set; }
        public List<Stat> Stats { get; set; }
        public List<TitleText> Benefits { get; set; }
        public List<string> Services { get; set; }
        public List<ProcessStep> Process { get; set; }
        public List<Faq> Faqs { get; set; }
        public string CtaTitle { get; set; }
        public string CtaText { get; set; }
        public string Topic { get; set; }
        public string Url { get { return "/" + Slug; } }
    }
}
