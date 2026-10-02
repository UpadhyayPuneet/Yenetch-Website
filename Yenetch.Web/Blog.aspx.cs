using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/blog, /blog/category/{category} and ?page=n. The first post on page one is featured.</summary>
    public partial class Blog : Page
    {
        private const int PageSize = 9;
        protected string Category;
        protected IList<string> Categories;
        protected BlogPost Featured;
        protected List<BlogPost> Posts;
        protected string PagerHtml = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            var repo = BlogStore.Repository;
            Categories = repo.Categories();
            var slug = Convert.ToString(RouteData.Values["category"]);
            Category = Categories.FirstOrDefault(c => BlogPost.Slugify(c) == slug);
            if (!string.IsNullOrEmpty(slug) && Category == null) { Server.Transfer("~/NotFound.aspx"); return; }

            int page; if (!int.TryParse(Request.QueryString["page"], out page) || page < 1) page = 1;
            int total;
            var list = repo.List(page, PageSize + (page == 1 ? 1 : 0), Category, out total).ToList();
            Featured = page == 1 ? list.FirstOrDefault() : null;
            Posts = page == 1 ? list.Skip(1).ToList() : list;

            if (Category != null)
            {
                Title = Category + " Articles | Yenetch Insights";
                MetaDescription = "Practical " + Category.ToLowerInvariant() + " guides for Indian businesses from the Yenetch team.";
            }
            Master.NavKey = "insights";

            var pages = (int)Math.Ceiling((total - 1) / (double)PageSize);
            if (pages > 1)
            {
                var root = Category == null ? "/blog" : "/blog/category/" + slug;
                var sb = new StringBuilder("<nav class=\"pager\" aria-label=\"Pages\">");
                for (var i = 1; i <= pages; i++)
                    sb.AppendFormat("<a href=\"{0}{1}\"{2}>{3}</a>", root, i == 1 ? "" : "?page=" + i, i == page ? " aria-current=\"page\"" : "", i);
                PagerHtml = sb.Append("</nav>").ToString();
            }
        }
    }
}
