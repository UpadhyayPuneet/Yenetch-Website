using System;
using System.Web;
using System.Web.Routing;

namespace Yenetch.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RegisterRoutes(RouteTable.Routes);
            Yenetch.Crm.Housekeeping.Start();
            Yenetch.Crm.NewsSender.StartTimer();
        }

        /// <summary>HTTPS sites tell browsers to always use HTTPS (HSTS).</summary>
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.IsSecureConnection) Response.AppendHeader("Strict-Transport-Security", "max-age=31536000");
        }

        /// <summary>Do not tell the world which server software runs the site.</summary>
        protected void Application_PreSendRequestHeaders(object sender, EventArgs e)
        {
            try { Response.Headers.Remove("Server"); Response.Headers.Remove("X-AspNet-Version"); Response.Headers.Remove("X-AspNetMvc-Version"); }
            catch (PlatformNotSupportedException) { /* classic pipeline */ }
        }

        /// <summary>Clean, SEO-friendly URLs: /digital-marketing, /services/seo, /products/ecomm, /case-studies/cabyaari-crm, /blog/{slug}. /sitemap.xml is served by Handlers/SitemapHandler (see Web.config).</summary>
        public static void RegisterRoutes(RouteCollection r)
        {
            // Extensionless endpoints for chat, tracking and job applications. Some hosts (Plesk) block POSTs to .ashx files.
            r.Add("api-lead", new Route("api/lead", new HandlerRoute<Yenetch.Web.Handlers.LeadHandler>()));
            r.Add("api-pulse", new Route("api/pulse", new HandlerRoute<Yenetch.Web.Handlers.PulseHandler>()));
            r.Add("api-apply", new Route("api/apply", new HandlerRoute<Yenetch.Web.Handlers.ApplyHandler>()));
            r.Add("api-slots", new Route("api/slots", new HandlerRoute<Yenetch.Web.Handlers.SlotsHandler>()));
            r.Add("api-book", new Route("api/book", new HandlerRoute<Yenetch.Web.Handlers.BookHandler>()));
            r.Add("api-audit", new Route("api/audit", new HandlerRoute<Yenetch.Web.Handlers.AuditHandler>()));
            r.Add("api-site-data", new Route("api/site-data", new HandlerRoute<Yenetch.Web.Handlers.SiteDataHandler>()));
            r.Add("api-geo", new Route("api/geo", new HandlerRoute<Yenetch.Web.Handlers.GeoHandler>()));
            r.Add("api-quote", new Route("api/quote", new HandlerRoute<Yenetch.Web.Handlers.QuoteHandler>()));
            r.Add("api-chat", new Route("api/chat", new HandlerRoute<Yenetch.Web.Handlers.ChatHandler>()));
            r.Add("api-proposal", new Route("api/proposal", new HandlerRoute<Yenetch.Web.Handlers.ProposalHandler>()));
            r.Add("api-razorpay", new Route("api/razorpay", new HandlerRoute<Yenetch.Web.Handlers.RazorpayWebhookHandler>()));
            r.Add("review-link", new Route("r/{token}", new HandlerRoute<Yenetch.Web.Handlers.ReviewLinkHandler>()));
            r.MapPageRoute("home", "", "~/Default.aspx");
            r.MapPageRoute("marketing", "digital-marketing", "~/DigitalMarketing.aspx");
            r.MapPageRoute("development", "software-development", "~/SoftwareDevelopment.aspx");
            r.MapPageRoute("talent", "talent-resourcing", "~/TalentResourcing.aspx");
            r.MapPageRoute("services", "services", "~/Services.aspx");
            r.MapPageRoute("service", "services/{slug}", "~/ServiceDetail.aspx");
            r.MapPageRoute("finder", "solution-finder", "~/SolutionFinder.aspx");
            r.MapPageRoute("products", "products", "~/Products.aspx");
            r.MapPageRoute("product", "products/{slug}", "~/ProductDetail.aspx");
            r.MapPageRoute("cases", "case-studies", "~/CaseStudies.aspx");
            r.MapPageRoute("case", "case-studies/{slug}", "~/CaseStudyDetail.aspx");
            r.MapPageRoute("about", "about", "~/About.aspx");
            r.MapPageRoute("careers", "careers", "~/Careers.aspx");
            r.MapPageRoute("blog", "blog", "~/Blog.aspx");
            r.MapPageRoute("blog-category", "blog/category/{category}", "~/Blog.aspx");
            r.MapPageRoute("post", "blog/{slug}", "~/BlogArticle.aspx");
            r.MapPageRoute("contact", "contact", "~/Contact.aspx");
            r.MapPageRoute("privacy", "privacy", "~/Privacy.aspx");
            r.MapPageRoute("terms", "terms", "~/Terms.aspx");
            r.MapPageRoute("unsubscribe", "newsletter/unsubscribe", "~/Unsubscribe.aspx");
            r.MapPageRoute("book", "book", "~/Book.aspx");
            r.MapPageRoute("book-cancel", "book/cancel", "~/BookCancel.aspx");
            r.MapPageRoute("audit", "website-audit", "~/WebsiteAudit.aspx");
            r.MapPageRoute("pricing", "pricing", "~/Pricing.aspx");
            r.MapPageRoute("blog-author", "blog/author/{author}", "~/Author.aspx");
            r.MapPageRoute("proposal", "proposal/{token}", "~/Proposal.aspx");

            // Admin: CRM, analytics and newsletter (sign-in required, see Web.config).
            r.MapPageRoute("admin", "admin", "~/Admin/Default.aspx");
            r.MapPageRoute("admin-login", "admin/login", "~/Admin/Login.aspx");
            r.MapPageRoute("admin-setup", "admin/setup", "~/Admin/Setup.aspx");
            r.MapPageRoute("admin-leads", "admin/leads", "~/Admin/Leads.aspx");
            r.MapPageRoute("admin-lead", "admin/leads/{id}", "~/Admin/Lead.aspx");
            r.MapPageRoute("admin-followups", "admin/follow-ups", "~/Admin/FollowUps.aspx");
            r.MapPageRoute("admin-reports", "admin/reports", "~/Admin/Reports.aspx");
            r.MapPageRoute("admin-analytics", "admin/analytics", "~/Admin/Analytics.aspx");
            r.MapPageRoute("admin-visitors", "admin/visitors", "~/Admin/Visitors.aspx");
            r.MapPageRoute("admin-visitor", "admin/visitors/{id}", "~/Admin/Visitor.aspx");
            r.MapPageRoute("admin-subscribers", "admin/subscribers", "~/Admin/Subscribers.aspx");
            r.MapPageRoute("admin-campaigns", "admin/campaigns", "~/Admin/Campaigns.aspx");
            r.MapPageRoute("admin-campaign", "admin/campaigns/{id}", "~/Admin/Campaign.aspx");
            r.MapPageRoute("admin-content", "admin/content", "~/Admin/Content.aspx");
            r.MapPageRoute("admin-content-list", "admin/content/{collection}", "~/Admin/ContentList.aspx");
            r.MapPageRoute("admin-content-edit", "admin/content/{collection}/{id}", "~/Admin/ContentEdit.aspx");
            r.MapPageRoute("admin-applications", "admin/applications", "~/Admin/Applications.aspx");
            r.MapPageRoute("admin-application", "admin/applications/{id}", "~/Admin/Application.aspx");
            r.MapPageRoute("admin-email", "admin/email", "~/Admin/Email.aspx");
            r.MapPageRoute("admin-posts", "admin/posts", "~/Admin/Posts.aspx");
            r.MapPageRoute("admin-post", "admin/posts/{id}", "~/Admin/Post.aspx");
            r.MapPageRoute("admin-team", "admin/team", "~/Admin/Team.aspx");
            r.MapPageRoute("admin-account", "admin/account", "~/Admin/Account.aspx");
            r.MapPageRoute("admin-verify", "admin/verify", "~/Admin/Verify.aspx");
            r.MapPageRoute("admin-bookings", "admin/bookings", "~/Admin/Bookings.aspx");
            r.MapPageRoute("admin-booking", "admin/bookings/{id}", "~/Admin/BookingDetail.aspx");
            r.MapPageRoute("admin-availability", "admin/availability", "~/Admin/Availability.aspx");
            r.MapPageRoute("admin-audits", "admin/audits", "~/Admin/Audits.aspx");
            r.MapPageRoute("admin-audit", "admin/audits/{id}", "~/Admin/AuditDetail.aspx");
            r.MapPageRoute("admin-automations", "admin/automations", "~/Admin/AutomationList.aspx");
            r.MapPageRoute("admin-automation", "admin/automations/{key}", "~/Admin/AutomationEdit.aspx");
            r.MapPageRoute("admin-reviews", "admin/reviews", "~/Admin/ReviewsAdmin.aspx");
            r.MapPageRoute("admin-tracking", "admin/tracking", "~/Admin/Tracking.aspx");
            r.MapPageRoute("admin-robots", "admin/robots", "~/Admin/Robots.aspx");
            r.MapPageRoute("admin-security", "admin/security", "~/Admin/Security.aspx");
            r.MapPageRoute("admin-ai", "admin/ai", "~/Admin/AiAdmin.aspx");
            r.MapPageRoute("admin-pricing", "admin/pricing", "~/Admin/PricingAdmin.aspx");
            r.MapPageRoute("admin-integrations", "admin/integrations", "~/Admin/Integrations.aspx");
            r.MapPageRoute("admin-scoring", "admin/scoring", "~/Admin/Scoring.aspx");
            r.MapPageRoute("admin-proposals", "admin/proposals", "~/Admin/ProposalList.aspx");
            r.MapPageRoute("admin-proposal", "admin/proposals/{id}", "~/Admin/ProposalEdit.aspx");
            r.Add("admin-backup", new Route("admin/backup", new HandlerRoute<Yenetch.Web.Admin.BackupDownload>()));

            // Landing pages managed in Admin > Content > Landing pages. Must stay last: it matches any single-segment path.
            // The constraint keeps out anything with a dot, so /sitemap.xml, /robots.txt and files still reach their handlers.
            r.MapPageRoute("landing", "{slug}", "~/Landing.aspx", false, null, new RouteValueDictionary { { "slug", "^[a-z0-9]+(-[a-z0-9]+)*$" } });
        }
    }

    /// <summary>Serves an IHttpHandler from a route (used for the /api/* endpoints).</summary>
    public class HandlerRoute<T> : IRouteHandler where T : IHttpHandler, new()
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext) { return new T(); }
    }
}
