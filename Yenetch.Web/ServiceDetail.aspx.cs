using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/services/{slug}: one template for all 21 services. Copy comes from assets/data/pages.json.</summary>
    public partial class ServiceDetail : Page
    {
        protected Service Svc;
        protected ServiceCopy Copy;
        protected Pillar Pillar;
        protected List<Service> Related;
        protected List<CaseStudy> Cases;
        protected string FaqLd, ServiceLd, CrumbLd;

        protected void Page_Load(object sender, EventArgs e)
        {
            var slug = Convert.ToString(RouteData.Values["slug"]);
            Svc = SiteContent.Service(slug);
            if (Svc == null) { Server.Transfer("~/NotFound.aspx"); return; }

            Copy = SiteContent.ServiceCopy(slug);
            Pillar = SiteContent.Pillar(Svc.Pillar);
            Related = SiteContent.ServicesFor(Svc.Pillar).Where(s => s.Slug != slug).ToList();
            Cases = SiteContent.CasesForService(slug);

            Title = string.IsNullOrEmpty(Copy.SeoTitle) ? Svc.Name + " | Yenetch" : Copy.SeoTitle;
            MetaDescription = string.IsNullOrEmpty(Copy.SeoDescription) ? Svc.Summary : Copy.SeoDescription;
            Master.NavKey = Svc.Pillar == "marketing" ? "marketing" : Svc.Pillar == "talent" ? "talent" : "development";

            FaqLd = Seo.FaqPage(Copy.Faqs);
            ServiceLd = Seo.Service(Svc, MetaDescription);
            CrumbLd = Seo.Breadcrumbs("Home", "/", Pillar.Name, Pillar.Url, Svc.Name, Svc.Url);
        }
    }
}
