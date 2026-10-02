using System;
using System.Collections.Generic;
using Yenetch.Crm;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web.Admin
{
    /// <summary>
    /// /admin/posts/{id|new}. Write or edit a blog post with the visual editor, set its cover, category and search details,
    /// then save it as a draft, publish it (now or on a date) or hide it. /admin/posts/new?from={slug} starts an edited copy
    /// of a built-in article, which replaces it on the site once saved.
    /// </summary>
    public partial class PostPage : AdminPage
    {
        public override string Section { get { return "posts"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected CmsPost P;
        protected BlogPost BuiltIn, Original;
        protected bool IsNew;
        protected List<string> Categories;
        protected string CoverSrc = "", StatusLine = "", PreviewUrl = "", PublishValue = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            var key = Convert.ToString(RouteData.Values["id"] ?? Request.QueryString["id"] ?? "new");
            IsNew = key == "new";
            int id;
            if (!IsNew)
            {
                P = int.TryParse(key, out id) ? Posts.Get(id) : null;
                if (P == null) { RedirectWith("/admin/posts", "Post not found."); return; }
                BuiltIn = Posts.BuiltIn(P.Slug);
                Title = P.Title;
                PreviewUrl = "/blog/" + P.Slug + "?preview=1";
                StatusLine = P.IsLive ? "Live at <a href=\"/blog/" + Att(P.Slug) + "\" target=\"_blank\" rel=\"noopener\">/blog/" + H(P.Slug) + "</a> · updated " + H(Ago(P.UpdatedOn))
                    : P.IsScheduled ? "Scheduled for " + H(When(P.PublishedOn))
                    : P.Status == "Hidden" ? "Hidden from the site"
                    : "Draft · last saved " + H(Ago(P.UpdatedOn));
            }
            else
            {
                var from = Q("from");
                if (from != "")
                {
                    var existing = Posts.BySlug(from);
                    if (existing != null) { RedirectWith("/admin/posts/" + existing.Id, null); return; }
                    Original = new JsonBlogRepository().BySlug(from);
                    if (Original == null) { RedirectWith("/admin/posts", "Article not found."); return; }
                    BuiltIn = Original;
                    StatusLine = "Built-in article, live on the site. Your saved copy will replace it.";
                }
                else StatusLine = "Starts as a draft. Only people signed in here can see drafts.";
                Title = Original != null ? Original.Title : "New post";
            }
            Categories = Posts.Categories();
            if (BuiltIn != null) Slug.ReadOnly = true;

            if (!IsPostBack)
            {
                if (P != null) Fill(P);
                else if (Original != null) Fill(Original);
                else { Author.Text = Me.Name; StatusList.SelectedValue = "Draft"; }
            }
            else if (!IsNew && Request.Form["delete"] == "1")
            {
                Posts.Delete(P.Id);
                RedirectWith("/admin/posts", BuiltIn != null ? "Your edits were deleted. The original article is back on the site." : "Post deleted.");
                return;
            }
            CoverSrc = Photos.Src(IsPostBack ? Cover.Text.Trim() : Cover.Text);
        }

        private void Fill(CmsPost p)
        {
            PostTitle.Text = p.Title; Body.Text = p.BodyHtml; Excerpt.Text = p.Excerpt; Slug.Text = p.Slug;
            MetaTitle.Text = p.MetaTitle; MetaDesc.Text = p.MetaDescription; Category.Text = p.Category;
            Author.Text = p.Author; Tags.Text = p.Tags; Cover.Text = p.CoverImage;
            StatusList.SelectedValue = p.Status;
            PublishValue = Util.InputDateTime(p.PublishedOn);
        }

        private void Fill(BlogPost b)
        {
            PostTitle.Text = b.Title; Body.Text = b.BodyHtml; Excerpt.Text = b.Excerpt; Slug.Text = b.Slug;
            MetaTitle.Text = b.MetaTitle; MetaDesc.Text = b.MetaDescription; Category.Text = b.Category;
            Author.Text = b.Author; Tags.Text = b.Tags == null ? "" : string.Join(", ", b.Tags); Cover.Text = Photos.Src(b.CoverImage);
            StatusList.SelectedValue = "Published";
            PublishValue = Util.InputDateTime(Util.FromIst(b.PublishedOn));
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            var title = PostTitle.Text.Trim();
            var slug = BuiltIn != null ? BuiltIn.Slug : Posts.MakeSlug(Slug.Text.Trim() == "" ? title : Slug.Text);
            var status = Array.IndexOf(Posts.Statuses, StatusList.SelectedValue) >= 0 ? StatusList.SelectedValue : "Draft";
            var body = Posts.CleanHtml(Body.Text);
            Body.Text = body;
            Slug.Text = slug;

            if (title.Length < 3) { Fail("Write a title."); return; }
            if (status == "Published" && Posts.PlainText(body).Trim().Length < 40) { Fail("The article is too short to publish. Save it as a draft for now."); return; }
            if (Category.Text.Trim() == "") { Fail("Choose a category, or type a new one."); return; }
            if (slug.Length < 3) { Fail("The web address needs at least 3 letters or numbers."); return; }
            if (Posts.SlugTaken(slug, P == null ? 0 : P.Id) || (BuiltIn == null && Posts.BuiltIn(slug) != null)) { Fail("Another post already uses /blog/" + slug + ". Change the web address."); return; }

            var excerpt = Excerpt.Text.Trim();
            if (excerpt == "") { var text = Posts.PlainText(body).Trim(); excerpt = Util.Cut(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " "), 220); }
            DateTime? when = null;
            PublishValue = (Request.Form["publishOn"] ?? "").Trim();
            if (PublishValue != "")
            {
                when = Util.ParseInput(PublishValue);
                if (!when.HasValue) { Fail("Check the publish date."); return; }
            }

            var post = new CmsPost
            {
                Id = P == null ? 0 : P.Id, Slug = slug, Title = title, MetaTitle = MetaTitle.Text, MetaDescription = MetaDesc.Text,
                Category = Category.Text.Trim(), Excerpt = excerpt, BodyHtml = body, CoverImage = Cover.Text.Trim(), Author = Author.Text,
                Tags = Tags.Text, Status = status, PublishedOn = when ?? (P != null ? P.PublishedOn : null)
            };
            var id = Posts.Save(post, Me.Id);
            var saved = Posts.Get(id);
            RedirectWith("/admin/posts/" + id, saved.IsLive ? "Published. It's live on the Insights page." : saved.IsScheduled ? "Scheduled for " + When(saved.PublishedOn) + "." : saved.Status == "Hidden" ? "Saved and hidden from the site." : "Draft saved.");
        }

        private void Fail(string message) { ErrorText.Text = Server.HtmlEncode(message); ErrorBox.Visible = true; }
    }
}
