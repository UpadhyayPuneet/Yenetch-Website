using System;
using System.Configuration;
using System.Web.UI;

namespace Yenetch.Web
{
    public partial class SiteMaster : MasterPage
    {
        /// <summary>Shortcut to Page.Title (pages usually set it in the @Page directive).</summary>
        public string PageTitle { get { return Page.Title; } set { Page.Title = value; } }

        /// <summary>Shortcut to Page.MetaDescription, rendered by ASP.NET as &lt;meta name="description"&gt;.</summary>
        public string MetaDescription { get { return Page.MetaDescription; } set { Page.MetaDescription = value; } }

        /// <summary>Highlights a main navigation item: marketing, development, talent, products, work, company, insights.
        /// Inferred from the URL when a page does not set it.</summary>
        public string NavKey { get; set; }

        /// <summary>Absolute URL of the social sharing image.</summary>
        public string OgImage { get; set; }

        public string SiteUrl { get { return (ConfigurationManager.AppSettings["SiteUrl"] ?? "").TrimEnd('/'); } }

        public string CanonicalUrl { get { return SiteUrl + Request.Url.AbsolutePath.ToLowerInvariant(); } }

        public string Current(string key)
        {
            return key == NavKey ? "aria-current=\"page\"" : "";
        }

        private static string InferNavKey(string path)
        {
            path = (path ?? "").ToLowerInvariant();
            if (path.StartsWith("/digital-marketing")) return "marketing";
            if (path.StartsWith("/software-development")) return "development";
            if (path.StartsWith("/talent-resourcing")) return "talent";
            if (path.StartsWith("/products")) return "products";
            if (path.StartsWith("/case-studies")) return "work";
            if (path.StartsWith("/about") || path.StartsWith("/careers")) return "company";
            if (path.StartsWith("/blog")) return "insights";
            return "";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Page.Title)) Page.Title = "Yenetch | Digital Marketing, Software Development & IT Talent";
            if (string.IsNullOrEmpty(Page.MetaDescription))
                Page.MetaDescription = "Yenetch is a Gurugram and Jaipur based digital marketing, software development and IT resourcing company. 100+ clients since 2019.";
            // Google title and description set in /admin/content (Page titles for Google) win over the page's own.
            var seo = Yenetch.Data.SiteContent.SeoFor(Request.Url.AbsolutePath);
            if (seo != null && !string.IsNullOrWhiteSpace(seo.Title)) Page.Title = seo.Title;
            if (seo != null && !string.IsNullOrWhiteSpace(seo.Description)) Page.MetaDescription = seo.Description;
            // Titles and descriptions may carry {phone} / {email} tokens (tools/build.py writes them): fill in the current details.
            Page.Title = Yenetch.Data.Site.Expand(Page.Title);
            Page.MetaDescription = Yenetch.Data.Site.Expand(Page.MetaDescription);
            if (NavKey == null) NavKey = InferNavKey(Request.Url.AbsolutePath);
            if (string.IsNullOrEmpty(OgImage)) OgImage = SiteUrl + "/assets/img/og-image.png";

            // Every page exposes its data as fields set in its own Page_Load (which runs before this one);
            // binding once here resolves all <%# %> expressions in the page, including Repeaters.
            Page.DataBind();
        }
    }
}
