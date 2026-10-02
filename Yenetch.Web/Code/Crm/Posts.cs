using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Crm
{
    /// <summary>A blog post written in the admin (dbo.CmsPosts).</summary>
    public class CmsPost
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Category { get; set; }
        public string Excerpt { get; set; }
        public string BodyHtml { get; set; }
        public string CoverImage { get; set; }
        public string Author { get; set; }
        public string Tags { get; set; }
        public string Status { get; set; }
        public DateTime? PublishedOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string UpdatedByName { get; set; }

        public bool IsLive { get { return Status == "Published" && PublishedOn.HasValue && PublishedOn.Value <= DateTime.UtcNow; } }
        public bool IsScheduled { get { return Status == "Published" && PublishedOn.HasValue && PublishedOn.Value > DateTime.UtcNow; } }
        public string StatusLabel { get { return IsScheduled ? "Scheduled" : Status; } }

        internal static CmsPost From(Row r)
        {
            return new CmsPost
            {
                Id = r.Int("Id"), Slug = r.Str("Slug"), Title = r.Str("Title"), MetaTitle = r.Str("MetaTitle"), MetaDescription = r.Str("MetaDescription"),
                Category = r.Str("Category"), Excerpt = r.Str("Excerpt"), BodyHtml = r.Str("BodyHtml") ?? "", CoverImage = r.Str("CoverImage"),
                Author = r.Str("Author"), Tags = r.Str("Tags"), Status = r.Str("Status"), PublishedOn = r.DateN("PublishedOn"),
                CreatedOn = r.Date("CreatedOn"), UpdatedOn = r.Date("UpdatedOn"), UpdatedByName = r.Str("UpdatedByName")
            };
        }

        /// <summary>The post in the shape the public blog pages use.</summary>
        public BlogPost ToBlogPost()
        {
            var words = Regex.Matches(Posts.PlainText(BodyHtml), @"\w+").Count;
            return new BlogPost
            {
                Id = Id, Slug = Slug, Title = Title, MetaTitle = MetaTitle, MetaDescription = MetaDescription, Category = Category,
                Excerpt = Excerpt, BodyHtml = BodyHtml, CoverImage = CoverImage, Author = string.IsNullOrWhiteSpace(Author) ? "Team Yenetch" : Author,
                ReadMinutes = Math.Max(1, (int)Math.Round(words / 220.0)),
                Date = Util.Ist(PublishedOn ?? UpdatedOn).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Tags = (Tags ?? "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList()
            };
        }
    }

    /// <summary>One line of the admin post list: a post from the database, a built-in article, or both (an edited built-in).</summary>
    public class PostListItem
    {
        public CmsPost Row { get; set; }
        public BlogPost BuiltIn { get; set; }
        public string Slug { get { return Row != null ? Row.Slug : BuiltIn.Slug; } }
        public string Title { get { return Row != null ? Row.Title : BuiltIn.Title; } }
        public string Category { get { return Row != null ? Row.Category : BuiltIn.Category; } }
        public string Status { get { return Row != null ? Row.StatusLabel : "Published"; } }
        public DateTime? Date { get { return Row != null ? (Row.PublishedOn ?? Row.UpdatedOn) : (DateTime?)Util.FromIst(BuiltIn.PublishedOn); } }
        public string EditUrl { get { return Row != null ? "/admin/posts/" + Row.Id : "/admin/posts/new?from=" + Uri.EscapeDataString(BuiltIn.Slug); } }
    }

    /// <summary>Reads and writes admin blog posts, and cleans the HTML the editor sends.</summary>
    public static class Posts
    {
        public static readonly string[] Statuses = { "Draft", "Published", "Hidden" };

        private const string Select = "SELECT p.*, u.Name AS UpdatedByName FROM CmsPosts p LEFT JOIN CrmUsers u ON u.Id = p.UpdatedBy";

        public static CmsPost Get(int id) { var r = Db.First(Select + " WHERE p.Id = @id", new { id }); return r == null ? null : CmsPost.From(r); }
        public static CmsPost BySlug(string slug) { var r = Db.First(Select + " WHERE p.Slug = @slug", new { slug }); return r == null ? null : CmsPost.From(r); }
        public static List<CmsPost> All() { return Db.Query(Select + " ORDER BY p.UpdatedOn DESC", CmsPost.From); }

        /// <summary>Database posts plus the built-in articles that have no database copy, newest first.</summary>
        public static List<PostListItem> ListForAdmin()
        {
            var rows = All();
            var bySlug = rows.ToDictionary(r => r.Slug, StringComparer.OrdinalIgnoreCase);
            var list = rows.Select(r => new PostListItem { Row = r, BuiltIn = BuiltIn(r.Slug) }).ToList();
            list.AddRange(SiteContent.Current.Blog.Where(b => !bySlug.ContainsKey(b.Slug)).Select(b => new PostListItem { BuiltIn = b }));
            return list.OrderByDescending(i => i.Row != null && i.Row.Status == "Draft").ThenByDescending(i => i.Date).ToList();
        }

        public static BlogPost BuiltIn(string slug)
        {
            return SiteContent.Current.Blog.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> Categories()
        {
            var cats = SiteContent.Current.Blog.Select(b => b.Category).ToList();
            try { cats.AddRange(Db.Query("SELECT DISTINCT Category FROM CmsPosts", r => r.Str("Category"))); } catch { }
            return cats.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList();
        }

        public static bool SlugTaken(string slug, int exceptId)
        {
            return Db.Scalar<int>("SELECT COUNT(*) FROM CmsPosts WHERE Slug = @slug AND Id <> @id", new { slug, id = exceptId }) > 0;
        }

        public static int Save(CmsPost p, int userId)
        {
            var now = DateTime.UtcNow;
            if (p.Status == "Published" && !p.PublishedOn.HasValue) p.PublishedOn = now;
            p.BodyHtml = CleanHtml(p.BodyHtml);
            var args = new Dictionary<string, object>
            {
                { "slug", p.Slug }, { "title", p.Title }, { "mt", Null(p.MetaTitle) }, { "md", Null(p.MetaDescription) }, { "cat", p.Category },
                { "ex", p.Excerpt }, { "body", p.BodyHtml }, { "cover", Null(p.CoverImage) }, { "author", Null(p.Author) }, { "tags", Null(p.Tags) },
                { "status", p.Status }, { "pub", p.PublishedOn }, { "me", userId }, { "now", now }, { "id", p.Id }
            };
            int id;
            if (p.Id == 0)
                id = Db.Insert(@"INSERT INTO CmsPosts (Slug, Title, MetaTitle, MetaDescription, Category, Excerpt, BodyHtml, CoverImage, Author, Tags, Status, PublishedOn, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
                                 VALUES (@slug, @title, @mt, @md, @cat, @ex, @body, @cover, @author, @tags, @status, @pub, @me, @now, @me, @now)", args);
            else
            {
                Db.Exec(@"UPDATE CmsPosts SET Slug = @slug, Title = @title, MetaTitle = @mt, MetaDescription = @md, Category = @cat, Excerpt = @ex, BodyHtml = @body,
                          CoverImage = @cover, Author = @author, Tags = @tags, Status = @status, PublishedOn = @pub, UpdatedBy = @me, UpdatedOn = @now WHERE Id = @id", args);
                id = p.Id;
            }
            BlogStore.Invalidate();
            return id;
        }

        public static void Delete(int id)
        {
            Db.Exec("DELETE FROM CmsPosts WHERE Id = @id", new { id });
            BlogStore.Invalidate();
        }

        private static object Null(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }

        // ---- HTML -------------------------------------------------------------------------------------------

        private static readonly Regex Dangerous = new Regex(@"<\s*(script|style|iframe|object|embed|form|input|button|textarea|select|meta|link|base)\b[^>]*>(.*?<\s*/\s*\1\s*>)?", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex EventAttr = new Regex(@"\s+on\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase);
        private static readonly Regex JsUrl = new Regex(@"(href|src)\s*=\s*([""'])\s*(javascript|vbscript|data):[^""']*\2", RegexOptions.IgnoreCase);
        private static readonly Regex StyleAttr = new Regex(@"\s+(style|dir|data-[\w-]+)\s*=\s*(""[^""]*""|'[^']*')", RegexOptions.IgnoreCase);
        private static readonly Regex ClassAttr = new Regex(@"\s+class\s*=\s*([""'])(.*?)\1", RegexOptions.IgnoreCase);
        private static readonly string[] KnownClasses = { "callout", "table-wrap" };
        private static readonly Regex Spans = new Regex(@"</?(span|font)\b[^>]*>", RegexOptions.IgnoreCase);
        private static readonly Regex H2 = new Regex(@"<h2(\s[^>]*)?>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex EmptyP = new Regex(@"<p>(\s|&nbsp;|<br\s*/?>)*</p>", RegexOptions.IgnoreCase);
        private static readonly Regex Tag = new Regex("<[^>]+>");

        public static string PlainText(string html) { return WebUtility.HtmlDecode(Tag.Replace(html ?? "", " ")); }

        /// <summary>
        /// Keeps the article HTML simple and safe: removes scripts, event handlers, pasted inline styles and empty paragraphs,
        /// opens outside links in a new tab, lazy-loads images, and gives every h2 an id for the table of contents.
        /// </summary>
        public static string CleanHtml(string html)
        {
            html = (html ?? "").Trim();
            html = Dangerous.Replace(html, "");
            html = EventAttr.Replace(html, "");
            html = JsUrl.Replace(html, "$1=$2#$2");
            html = StyleAttr.Replace(html, "");
            html = Spans.Replace(html, "");
            // Only the article styles the site knows keep their class (pasted Word or Docs classes go).
            html = ClassAttr.Replace(html, m => Array.IndexOf(KnownClasses, m.Groups[2].Value.Trim()) >= 0 ? " class=\"" + m.Groups[2].Value.Trim() + "\"" : "");
            html = Regex.Replace(html, @"<(/?)b>", "<$1strong>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<(/?)i>", "<$1em>", RegexOptions.IgnoreCase);
            // Browsers wrap typed lines in plain divs; those become paragraphs (classed wrappers such as table-wrap stay).
            html = Regex.Replace(html, @"<div>(.*?)</div>", "<p>$1</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            // Lists, headings and tables never sit inside a paragraph.
            html = Regex.Replace(html, @"<p>\s*(<(ul|ol|h2|h3|table|div|aside|blockquote|figure)\b)", "$1", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"(</(ul|ol|h2|h3|table|div|aside|blockquote|figure)>)\s*</p>", "$1", RegexOptions.IgnoreCase);
            html = EmptyP.Replace(html, "");
            html = Regex.Replace(html, @"<img(?![^>]*\sloading=)", "<img loading=\"lazy\"", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<a\s+href=""(https?://(?!(www\.)?yenetch\.com)[^""]+)""(?![^>]*target=)", "<a href=\"$1\" target=\"_blank\" rel=\"noopener\"", RegexOptions.IgnoreCase);

            var used = new HashSet<string>();
            html = H2.Replace(html, m =>
            {
                var inner = m.Groups[2].Value;
                var existing = Regex.Match(m.Groups[1].Value, @"\sid=""([^""]+)""");
                var id = existing.Success ? existing.Groups[1].Value : BlogPost.Slugify(PlainText(inner));
                if (id.Length > 60) id = id.Substring(0, 60).TrimEnd('-');
                if (id.Length == 0) id = "section";
                var baseId = id; var n = 2;
                while (!used.Add(id)) id = baseId + "-" + n++;
                return "<h2 id=\"" + id + "\">" + inner.Trim() + "</h2>";
            });
            // One block per line keeps the HTML view readable.
            html = Regex.Replace(html, @"\s*(</(p|h2|h3|ul|ol|li|blockquote|figure)>)\s*", "$1\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"\n{2,}", "\n");
            return html.Trim();
        }

        /// <summary>A URL-safe slug, at most 90 characters.</summary>
        public static string MakeSlug(string s)
        {
            var slug = BlogPost.Slugify(s);
            if (slug.Length > 90) slug = slug.Substring(0, 90).TrimEnd('-');
            return slug;
        }
    }
}
