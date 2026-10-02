using System.Collections.Generic;
using System.Linq;

namespace Yenetch.Data
{
    /// <summary>One editable field. Types: text, textarea, html, image, url, email, number, bool, select, multi, lines,
    /// paragraphs, items (a repeating group of sub-fields), object (a nested group) and json (advanced).</summary>
    public class ContentField
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public string Help { get; set; }
        public bool Required { get; set; }
        /// <summary>Choices for select and multi: fixed values, or "@collection" for the keys of another collection.</summary>
        public List<string> Options { get; set; }
        public string Source { get; set; }
        public List<ContentField> Fields { get; set; }
        public string ItemLabel { get; set; }
    }

    /// <summary>
    /// A content collection shown in /admin/content. Kind Single = one item (company details), List = ordered items
    /// (services, team), Map = items addressed by key (page copy per service). Target says where the items go in the
    /// JSON the site reads: "site:path" (yenetch.json shape), "copy:path" (pages.json shape), or "legal" / "seo".
    /// </summary>
    public class ContentCollection
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string Kind { get; set; }
        public string Target { get; set; }
        public string KeyField { get; set; }
        public string TitleField { get; set; }
        /// <summary>Set for lists of plain strings (clients): the field whose value becomes the string.</summary>
        public string StringField { get; set; }
        /// <summary>Items are fixed (legal pages): no adding or deleting.</summary>
        public bool Fixed { get; set; }
        public List<ContentField> Fields { get; set; }

        public bool IsSingle { get { return Kind == "Single"; } }
        public bool IsMap { get { return Kind == "Map"; } }
    }

    public static class ContentSchema
    {
        // ---- field builders ----
        private static ContentField F(string name, string label, string type = "text", string help = null, bool required = false)
        { return new ContentField { Name = name, Label = label, Type = type, Help = help, Required = required }; }
        private static ContentField Sel(string name, string label, string help, params string[] options)
        { return new ContentField { Name = name, Label = label, Type = "select", Help = help, Options = options.ToList() }; }
        private static ContentField Ref(string name, string label, string type, string source, string help = null)
        { return new ContentField { Name = name, Label = label, Type = type, Source = source, Help = help }; }
        private static ContentField Items(string name, string label, string itemLabel, params ContentField[] fields)
        { return new ContentField { Name = name, Label = label, Type = "items", ItemLabel = itemLabel, Fields = fields.ToList() }; }
        private static ContentField Obj(string name, string label, params ContentField[] fields)
        { return new ContentField { Name = name, Label = label, Type = "object", Fields = fields.ToList() }; }
        private static ContentField[] TitleText() { return new[] { F("title", "Title"), F("text", "Text", "textarea") }; }
        private static ContentField[] NameText() { return new[] { F("name", "Name"), F("text", "Text", "textarea") }; }
        private static ContentField Faqs() { return Items("faqs", "FAQs", "Question", F("q", "Question"), F("a", "Answer", "textarea")); }
        private static ContentField Seo() { return Obj("_seo", "Search engines", F("seoTitle", "Google title", help: "About 60 characters. Leave empty to use the headline."), F("seoDescription", "Google description", "textarea", "About 155 characters.")); }

        private static ContentCollection C(string key, string title, string group, string kind, string target, string description, params ContentField[] fields)
        { return new ContentCollection { Key = key, Title = title, Group = group, Kind = kind, Target = target, Description = description, Fields = fields.ToList() }; }

        public static readonly List<ContentCollection> All = new List<ContentCollection>
        {
            // ---------------- Company
            C("company", "Company & contact", "Company", "Single", "site:company", "Name, phone, email, WhatsApp, offices and addresses, social media links, and the numbers shown on the About page.",
                F("name", "Company name", required: true), F("tagline", "Tagline"),
                F("phone", "Phone", help: "Shown on the site and used by every Call button.", required: true),
                F("email", "Email", "email", "Shown on the site and used by every Email link.", true),
                F("whatsapp", "WhatsApp link", "url", "For example https://wa.me/919587365247"),
                F("founded", "Founded (year)", "number"), F("team", "Team size text", help: "For example 30+ specialists"),
                F("recognition", "Recognition line", help: "Shown in the footer, for example Recognised by Startup India."),
                Items("offices", "Offices and addresses", "Office", F("city", "City"), F("label", "Label", help: "For example Head office"), F("address", "Address", "textarea"), F("map", "Google Maps link", "url"), F("photo", "Photo", "image")),
                Items("social", "Social media", "Link", Sel("id", "Network", "Picks the icon.", "linkedin", "instagram", "facebook", "youtube", "x", "whatsapp"), F("name", "Name"), F("url", "Link", "url")),
                Items("stats", "Numbers", "Number", F("value", "Value", help: "For example 100+"), F("label", "Label")),
                Items("values", "Values", "Value", NameText())),
            C("legal", "Privacy policy & terms", "Company", "Map", "legal", "The legal pages. Use headings (H2) for each section.",
                F("title", "Page title", required: true), F("html", "Page text", "html")),
            C("seo", "Page titles for Google", "Company", "List", "seo", "Override the Google title and description of any page by its address, for example /contact.",
                F("path", "Page address", help: "For example /contact or /services/seo", required: true), F("title", "Google title", help: "About 60 characters."), F("description", "Google description", "textarea", "About 155 characters.")),

            // ---------------- Services and products
            C("pillars", "Service areas", "Services & products", "List", "site:pillars", "The main areas: Marketing, Development and Talent.",
                F("name", "Name", required: true), F("id", "Id", help: "Short code used by services, for example marketing.", required: true), F("short", "Short label"),
                F("summary", "Summary", "textarea"), F("url", "Page address"), F("photo", "Photo", "image")),
            C("services", "Services", "Services & products", "List", "site:services", "Every service: its card, page address, what is included, timeline and pricing (also used by the chatbot).",
                F("name", "Name", required: true), F("slug", "Page address", help: "Lower case with dashes. The page is /services/this-address.", required: true),
                Ref("pillar", "Area", "select", "@pillars"), F("summary", "Summary", "textarea", required: true), F("photo", "Photo", "image"),
                Sel("visual", "Illustration", "The animated illustration on the service page.", "dashboard", "serp", "social", "reviews", "crm", "dev", "store", "design", "ai", "cloud", "talent"),
                F("includes", "What is included", "lines", "One per line."), F("timeline", "Timeline", "textarea", "Used by the chatbot for How long does it take."),
                F("pricing", "Pricing", "textarea", "Used by the chatbot for What does it cost."), F("keywords", "Chatbot keywords", "lines", "Words people might type, one per line.")),
            C("serviceCopy", "Service pages", "Services & products", "Map", "copy:services", "The long text on each service page. Add one for a new service to give it a full page.",
                Ref("_key", "Service", "select", "@services"), F("headline", "Headline"), F("intro", "Introduction", "paragraphs", "Separate paragraphs with an empty line."),
                Items("outcomes", "Outcomes", "Outcome", TitleText()), Items("deliverables", "What you get", "Item", TitleText()),
                Items("process", "Process", "Step", NameText()), F("tools", "Tools", "lines"), Items("engagement", "Ways to work together", "Option", NameText()),
                F("idealFor", "Ideal for", "lines"), Faqs(), Seo()),
            C("products", "Products", "Services & products", "List", "site:products", "Ready-to-use products with price and status.",
                F("name", "Name", required: true), F("slug", "Page address", help: "The page is /products/this-address.", required: true), F("category", "Category"),
                Sel("status", "Status", null, "Live", "Early access", "Coming soon"), F("summary", "Summary", "textarea", required: true), F("photo", "Photo", "image"),
                F("features", "Features", "lines"), F("price", "Price", help: "For example From ₹1,499/month"), F("priceNote", "Price note"),
                F("timeline", "Set-up time", "textarea"), F("keywords", "Chatbot keywords", "lines")),
            C("productCopy", "Product pages", "Services & products", "Map", "copy:products", "The long text on each product page.",
                Ref("_key", "Product", "select", "@products"), F("headline", "Headline"), F("intro", "Introduction", "paragraphs"),
                Items("highlights", "Highlights", "Highlight", TitleText()), Items("howItWorks", "How it works", "Step", NameText()), F("whoFor", "Who it is for", "lines"), Faqs(), Seo()),
            C("talent", "Talent page", "Services & products", "Single", "copy:talent", "Text on the Talent & resourcing page.",
                F("headline", "Headline"), F("intro", "Introduction", "paragraphs"), Items("roles", "Roles we provide", "Role", F("name", "Name"), F("skills", "Skills", "lines")),
                Items("models", "Engagement models", "Model", F("name", "Name"), F("text", "Text", "textarea"), F("bestFor", "Best for"), F("timeline", "Timeline")),
                Items("steps", "Steps", "Step", NameText()), Items("guarantees", "Guarantees", "Guarantee", TitleText()), Faqs()),

            // ---------------- Work
            C("caseStudies", "Case studies", "Work & clients", "List", "site:caseStudies", "Projects with results. Each gets a page at /case-studies/address.",
                F("title", "Title", required: true), F("slug", "Page address", required: true), F("client", "Client", required: true), F("industry", "Industry"),
                Ref("services", "Services used", "multi", "@services"), F("photo", "Photo", "image"), F("challenge", "Challenge", "textarea"), F("solution", "Solution", "textarea"),
                Items("metrics", "Results", "Result", F("value", "Value", help: "For example 3x"), F("label", "Label")),
                Obj("quote", "Client quote", F("text", "Quote", "textarea"), F("name", "Name"), F("role", "Role")), F("url", "Live website", "url")),
            C("clients", "Clients", "Work & clients", "List", "site:clients", "Client names and logos shown in the logo strip. Without a logo the name is shown.", F("name", "Client name", required: true),
                F("logo", "Logo", "image", help: "Transparent PNG or SVG works best. Every logo is fitted to the same size.")),
            C("testimonials", "Testimonials", "Work & clients", "List", "site:testimonials", "Client quotes.",
                F("name", "Name", required: true), F("company", "Company"), F("photo", "Photo", "image", help: "A square head-and-shoulders photo. Without one, the first letter of the name is shown."),
                F("text", "Quote", "textarea", required: true)),
            C("industries", "Industries", "Work & clients", "List", "site:industries", "Industries with example clients.",
                F("name", "Name", required: true), F("photo", "Photo", "image"), F("clients", "Clients", "lines")),

            // ---------------- People
            C("team", "Team", "People", "List", "site:team.leadership", "Leadership shown on the About page. Leave the name empty to show only the role.",
                F("name", "Name"), F("role", "Role", required: true), F("remit", "What they do", "textarea"), F("photo", "Photo", "image"), F("linkedin", "LinkedIn", "url")),
            C("squads", "Teams", "People", "List", "site:team.squads", "Groups of specialists.", F("name", "Name", required: true), F("text", "Text", "textarea"), F("roles", "Roles", "lines")),
            C("jobs", "Job openings", "People", "List", "site:careers.roles", "Open roles on the Careers page. Turn one off when it is filled.",
                F("title", "Job title", required: true), F("team", "Team"), F("location", "Location", help: "A city with an office, for example Jaipur or Gurugram."), F("type", "Type", help: "For example Full-time · 4+ years"),
                F("description", "Job description", "textarea", "Needed for Google Jobs: what the person will do, skills and experience. Roles with a description appear in Google's job search."),
                Sel("employmentType", "Employment type", "For Google Jobs.", "FULL_TIME", "PART_TIME", "CONTRACTOR", "INTERN", "TEMPORARY"),
                F("posted", "Date posted", help: "For example 2026-10-01. Leave empty to use today."), F("validThrough", "Apply by", help: "Optional, for example 2026-12-31."),
                F("salaryMin", "Salary from (₹)", "number", "Optional. Google shows jobs with pay more often."), F("salaryMax", "Salary up to (₹)", "number"),
                Sel("salaryUnit", "Salary per", null, "MONTH", "YEAR"), F("remote", "Remote job", "bool")),
            C("perks", "Why work here", "People", "List", "site:careers.perks", "Perks on the Careers page.", TitleText()),
            C("hiringSteps", "Hiring steps", "People", "List", "site:careers.steps", "How hiring works.", NameText()),

            // ---------------- Growth
            C("landing", "Landing pages", "Growth", "List", "landing", "Pages for ads and Google searches such as \"digital marketing agency in Jaipur\". Each one lives at yenetch.com/its-address and is added to the sitemap. Turn a page off to hide it.",
                F("slug", "Page address", help: "Lower case with dashes, for example seo-company-in-jaipur. Use the words people search for.", required: true),
                F("seoTitle", "Google title", help: "About 60 characters. Put the main search phrase first, for example SEO Company in Jaipur | Yenetch.", required: true),
                F("seoDescription", "Google description", "textarea", "About 155 characters. Say what you offer, where, and why to click.", true),
                F("kicker", "Small heading", help: "Usually the search phrase, for example SEO company in Jaipur.", required: true),
                F("headline", "Headline (H1)", help: "The promise, in one sentence.", required: true),
                F("lead", "Introduction line", "textarea", required: true),
                F("photo", "Photo", "image"),
                F("city", "City", help: "Used for Google local business data. Leave empty for all of India."),
                F("industry", "Industry", help: "Optional, for example Healthcare."),
                F("topic", "Topic", help: "Preselects the topic on the booking form and the chat, for example SEO."),
                F("intro", "Main text", "paragraphs", "Separate paragraphs with an empty line. Use the search phrase naturally once or twice."),
                F("highlights", "Key points", "lines", "One per line, shown as ticks."),
                Items("stats", "Numbers", "Number", F("value", "Value", help: "For example 100+"), F("label", "Label")),
                Items("benefits", "Benefits", "Benefit", TitleText()),
                Ref("services", "Services to show", "multi", "@services"),
                Items("process", "Steps", "Step", NameText()),
                Faqs(),
                F("ctaTitle", "Closing heading"), F("ctaText", "Closing text", "textarea")),

            // ---------------- Pricing & offers
            C("plans", "Plans & prices", "Pricing & offers", "List", "site:plans", "Price plans for each service, shown on /pricing, on the service page, in the plan builder and by the chatbot. Enter prices in rupees (INR); visitors abroad see them converted to their currency.",
                F("id", "Plan code", help: "Short and unique, for example seo-starter. Used in links and quotes.", required: true),
                Ref("service", "Service", "select", "@services", "Which service this plan is for."),
                F("name", "Plan name", help: "For example Starter, Growth or Scale.", required: true), F("tagline", "One-line summary", help: "For example For new businesses getting found locally."),
                Sel("priceType", "Price shown as", "Fixed shows the price as it is. From shows \"From ₹…\" for work that is quoted after scoping.", "Fixed", "From"),
                F("price", "Price (₹)", "number", "In rupees, before GST.", true),
                Sel("billing", "Billed", null, "one-time", "monthly", "yearly", "hourly"),
                F("setupFee", "One-time set-up fee (₹)", "number", "Optional, for monthly plans. 0 for none."),
                F("minMonths", "Minimum months", "number", "Optional, for monthly plans, for example 3."),
                F("features", "What is included", "lines", "One per line. The first 6 show on the cards."),
                F("timeline", "Timeline", help: "For example Live in 2 weeks."),
                F("popular", "Mark as most popular", "bool")),
            C("addons", "Plan builder extras", "Pricing & offers", "List", "site:addons", "Extras people can add to a custom plan with a quantity, such as extra pages, blog posts or developer hours.",
                F("id", "Code", help: "Short and unique, for example extra-page.", required: true), F("name", "Name", required: true),
                Ref("service", "Service", "select", "@services", "Optional. Shown with this service in the builder."), F("group", "Group", help: "Heading in the builder, for example Website extras."),
                F("description", "Description", "textarea"), F("price", "Price per unit (₹)", "number", required: true),
                Sel("billing", "Billed", null, "one-time", "monthly"), F("unit", "Unit", help: "What one unit is: page, post, hour, user, language…"),
                F("min", "Minimum quantity", "number"), F("max", "Maximum quantity", "number", "0 for no limit.")),
            C("offers", "Offers", "Pricing & offers", "List", "site:offers", "Discounts and promotions. Choose the dates, the pages and services they show on, and whether they appear as a banner, a popup and in the chatbot. An offer outside its dates is hidden automatically.",
                F("id", "Offer code", help: "Short and unique, for example diwali-2026.", required: true),
                F("title", "Headline", help: "For example 20% off SEO plans this Diwali.", required: true), F("text", "Details", "textarea"),
                F("badge", "Badge", help: "Short label, for example 20% OFF."),
                Sel("discountType", "Discount", "none: an offer without a price change, such as a free audit.", "percent", "amount", "none"),
                F("discountValue", "Discount value", "number", "Percent (for example 20) or rupees off (for example 5000)."),
                F("code", "Coupon code", help: "Optional. Leave empty to apply the discount automatically in the plan builder."),
                F("minOrder", "Minimum order (₹)", "number", "Optional. The discount applies above this amount."),
                F("starts", "Starts on", help: "yyyy-mm-dd, for example 2026-10-20. Empty: starts now."),
                F("ends", "Ends on", help: "yyyy-mm-dd, the last day of the offer. Empty: no end date."),
                F("pages", "Show on pages", "lines", "One address per line: / for home, /pricing, /services/* for every service page, * for every page."),
                Ref("services", "Applies to services", "multi", "@services", "Empty: every service. Service pages of these services show the offer too."),
                F("showBanner", "Show as a banner at the top of those pages", "bool"),
                F("showPopup", "Show as a popup on those pages (once per visitor)", "bool"),
                F("showInChat", "Let the chatbot mention it", "bool"),
                F("ctaText", "Button text", help: "For example Claim offer."), F("ctaUrl", "Button link", help: "For example /pricing or /book.")),

            // ---------------- Blog
            C("authors", "Blog authors", "Blog", "List", "site:authors", "People who write for the blog. Each gets a profile page at /blog/author/address that lists their articles, and Google sees who wrote what (this builds trust, called E-E-A-T). Choose the author on each post.",
                F("name", "Name", required: true), F("slug", "Page address", help: "Lower case with dashes, for example riya-sharma.", required: true),
                F("role", "Job title", help: "For example Head of Digital Marketing."), F("bio", "Short bio", "textarea", "One or two sentences, shown under every article.", true),
                F("about", "Full profile", "paragraphs", "Experience, qualifications and what they work on. Separate paragraphs with an empty line."),
                F("photo", "Photo", "image", "A square head-and-shoulders photo."), F("expertise", "Topics", "lines", "One per line, for example SEO."),
                F("linkedin", "LinkedIn", "url"), F("twitter", "X (Twitter)", "url"), F("website", "Website", "url"),
                F("isTeam", "This is a team account, not a person", "bool")),

            // ---------------- General
            C("process", "How we work", "General", "List", "site:process", "The process steps on the home page.", NameText()),
            C("faqs", "FAQs", "General", "List", "site:faqs", "General questions, also answered by the chatbot.",
                F("q", "Question", required: true), F("a", "Answer", "textarea", required: true), F("keywords", "Chatbot keywords", "lines")),
            C("finder", "Solution finder", "General", "Single", "site:finder", "Questions and rules of the solution finder. Advanced: edit carefully.",
                F("_json", "Settings (JSON)", "json")),
        };

        static ContentSchema()
        {
            foreach (var c in All)
            {
                var names = c.Fields.Select(f => f.Name).ToList();
                c.KeyField = c.IsMap ? "_key" : names.Contains("slug") ? "slug" : names.Contains("id") ? "id" : c.Key == "seo" ? "path" : null;
                c.TitleField = new[] { "title", "name", "q", "role", "path", "headline" }.FirstOrDefault(names.Contains);
                if (c.Key == "clients") c.StringField = "name";
                if (c.Key == "legal") c.Fixed = true;
            }
        }

        public static ContentCollection Get(string key) { return All.FirstOrDefault(c => c.Key == key); }
    }
}
