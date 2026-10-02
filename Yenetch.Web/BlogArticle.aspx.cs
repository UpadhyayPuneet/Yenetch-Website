using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/blog/{slug}. Reads through IBlogRepository, so it works the same with JSON or SQL Server.</summary>
    public partial class BlogArticle : Page
    {
        protected BlogPost Post;
        protected List<TocItem> Toc;
        protected List<BlogPost> Related;
        protected string ShareUrl, ShareText, ArticleLd, FaqLd, CrumbLd;

        protected void Page_Load(object sender, EventArgs e)
        {
            var repo = BlogStore.Repository;
            var slug = Convert.ToString(RouteData.Values["slug"]);
            Post = repo.BySlug(slug);
            // Admin preview of drafts and scheduled posts: /blog/{slug}?preview=1 while signed in to /admin.
            if (Request.QueryString["preview"] == "1" && Yenetch.Crm.Db.IsConfigured)
            {
                var me = Yenetch.Crm.Auth.Current;
                if (me != null && me.CanUseMarketing)
                {
                    Post = CmsBlogRepository.Preview(slug) ?? Post;
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
                }
            }
            if (Post == null) { Server.Transfer("~/NotFound.aspx"); return; }

            Toc = BlogHtml.Toc(Post.BodyHtml);
            Related = repo.Latest(12).Where(p => p.Slug != Post.Slug)
                .OrderByDescending(p => p.Category == Post.Category).Take(3).ToList();

            Title = Post.SeoTitle + " | Yenetch";
            MetaDescription = Post.SeoDescription;
            Master.NavKey = "insights";
            Master.OgImage = Seo.Root + Photos.Src(Post.CoverImage);

            ShareUrl = HttpUtility.UrlEncode(Seo.Root + Post.Url);
            ShareText = HttpUtility.UrlEncode(Post.Title);
            ArticleLd = Seo.Article(Post);
            FaqLd = Seo.FaqPage(BlogHtml.Faqs(Post.BodyHtml));
            CrumbLd = Seo.Breadcrumbs("Home", "/", "Insights", "/blog", Post.Title, Post.Url);
        }
    }
}
