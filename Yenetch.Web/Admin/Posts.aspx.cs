using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/posts. Every blog post: written here, built in with the site, or a built-in article edited here.</summary>
    public partial class PostsPage : AdminPage
    {
        public override string Section { get { return "posts"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected List<PostListItem> Rows;
        protected string Filter;
        protected int Live, Drafts;

        protected void Page_Load(object sender, EventArgs e)
        {
            var all = Posts.ListForAdmin();
            Live = all.Count(p => p.Status == "Published");
            Drafts = all.Count(p => p.Status == "Draft");
            Filter = Q("show");
            switch (Filter)
            {
                case "draft": Rows = all.Where(p => p.Status == "Draft" || p.Status == "Scheduled").ToList(); break;
                case "live": Rows = all.Where(p => p.Status == "Published").ToList(); break;
                case "builtin": Rows = all.Where(p => p.BuiltIn != null).ToList(); break;
                default: Filter = ""; Rows = all; break;
            }
        }

        protected static string Badge(string status)
        {
            switch (status)
            {
                case "Published": return "badge--won";
                case "Scheduled": return "badge--new";
                case "Hidden": return "badge--lost";
                default: return "badge--open";
            }
        }
    }
}
