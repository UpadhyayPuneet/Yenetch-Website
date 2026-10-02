using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/{slug}: landing pages managed in Admin &gt; Content &gt; Landing pages. Unknown or switched-off addresses show the 404 page.</summary>
    public partial class Landing : Page
    {
        protected LandingPage Lp;
        protected List<Service> LpServices;
        protected List<CaseStudy> LpCases;
        protected string FaqLd, CrumbLd, PlaceLd;

        protected void Page_Load(object sender, EventArgs e)
        {
            var slug = Convert.ToString(RouteData.Values["slug"]);
            Lp = SiteContent.Landing(slug);
            if (Lp == null) { Server.Transfer("~/NotFound.aspx"); return; }

            var wanted = Lp.Services ?? new List<string>();
            LpServices = wanted.Select(SiteContent.Service).Where(s => s != null).ToList();
            LpCases = SiteContent.Current.CaseStudies.Where(c => c.Services != null && c.Services.Intersect(wanted).Any()).Take(2).ToList();

            Title = string.IsNullOrWhiteSpace(Lp.SeoTitle) ? Lp.Kicker + " | Yenetch" : Lp.SeoTitle;
            MetaDescription = string.IsNullOrWhiteSpace(Lp.SeoDescription) ? Lp.Lead : Lp.SeoDescription;
            if (!string.IsNullOrEmpty(Lp.Photo)) Master.OgImage = Photos.Src(Lp.Photo).StartsWith("http") ? Photos.Src(Lp.Photo) : Master.SiteUrl + Photos.Src(Lp.Photo);
            Master.NavKey = "";

            FaqLd = Seo.FaqPage(Lp.Faqs);
            CrumbLd = Seo.Breadcrumbs("Home", "/", Lp.Kicker ?? Lp.Headline, Lp.Url);
            var office = Yenetch.Data.Site.Offices.FirstOrDefault(o => !string.IsNullOrEmpty(o.City) && string.Equals(o.City, (Lp.City ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            PlaceLd = office != null ? Seo.LocalBusiness(office, Lp.Url, MetaDescription) : "{}";
        }
    }
}
