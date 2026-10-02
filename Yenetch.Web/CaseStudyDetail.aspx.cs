using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/case-studies/{slug}</summary>
    public partial class CaseStudyDetail : Page
    {
        protected CaseStudy Case;
        protected List<CaseStudy> More;
        protected string CrumbLd;

        protected void Page_Load(object sender, EventArgs e)
        {
            var slug = Convert.ToString(RouteData.Values["slug"]);
            Case = SiteContent.CaseStudy(slug);
            if (Case == null) { Server.Transfer("~/NotFound.aspx"); return; }

            More = SiteContent.Current.CaseStudies.Where(c => c.Slug != slug).Take(3).ToList();
            Title = Case.Client + " Case Study | Yenetch";
            var summary = Case.Title + ". " + Case.Challenge;
            MetaDescription = summary.Length > 155 ? summary.Substring(0, 152).TrimEnd() + "..." : summary;
            Master.NavKey = "work";
            CrumbLd = Seo.Breadcrumbs("Home", "/", "Case studies", "/case-studies", Case.Client, Case.PageUrl);
        }
    }
}
