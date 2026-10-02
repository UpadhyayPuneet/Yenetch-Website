using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>
    /// Blog authors (Admin &gt; Website content &gt; Blog authors). A post's Author field is matched to an author by name or
    /// page address; matched posts show the author's photo, a link to their profile page (/blog/author/{slug}) and an
    /// author box, and tell Google who wrote the article (schema.org Person). This is part of what Google calls E-E-A-T:
    /// showing real, experienced people stand behind the content.
    /// </summary>
    public static class Authors
    {
        public static List<Author> All { get { try { return (SiteContent.Current.Authors ?? new List<Author>()).Where(a => a != null && !string.IsNullOrEmpty(a.Slug)).ToList(); } catch { return new List<Author>(); } } }

        public static Author Find(string nameOrSlug)
        {
            if (string.IsNullOrWhiteSpace(nameOrSlug)) return null;
            var k = nameOrSlug.Trim();
            return All.FirstOrDefault(a => string.Equals(a.Slug, k, StringComparison.OrdinalIgnoreCase) || string.Equals(a.Name, k, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Posts written by an author (matched by name or address), newest first.</summary>
        public static List<BlogPost> Posts(Author a)
        {
            return BlogStore.Repository.Latest(1000).Where(p => Is(a, p.Author)).ToList();
        }

        public static bool Is(Author a, string postAuthor)
        {
            if (a == null || string.IsNullOrWhiteSpace(postAuthor)) return false;
            var k = postAuthor.Trim();
            return string.Equals(a.Name, k, StringComparison.OrdinalIgnoreCase) || string.Equals(a.Slug, k, StringComparison.OrdinalIgnoreCase);
        }

        public static string PhotoHtml(Author a, string cls)
        {
            if (a != null && a.HasPhoto)
                return "<img class=\"" + cls + "\" src=\"" + H(Photos.Src(a.Photo)) + "\" alt=\"" + H(a.Name) + "\" width=\"72\" height=\"72\" loading=\"lazy\" decoding=\"async\">";
            return "<span class=\"" + cls + "\" aria-hidden=\"true\">" + H(a == null ? "Y" : a.Initial) + "</span>";
        }

        /// <summary>The name line under an article's title.</summary>
        public static string Byline(string postAuthor, string fallbackMark)
        {
            var a = Find(postAuthor);
            if (a == null) return "<span class=\"byline__av\">" + fallbackMark + "</span><span><b>" + H(string.IsNullOrEmpty(postAuthor) ? "Team Yenetch" : postAuthor) + "</b>";
            var av = a.IsTeam && !a.HasPhoto ? "<span class=\"byline__av\">" + fallbackMark + "</span>" : "<span class=\"byline__av byline__av--photo\">" + PhotoHtml(a, "byline__photo") + "</span>";
            return av + "<span><b><a href=\"" + H(a.Url) + "\" rel=\"author\">" + H(a.Name) + "</a></b>";
        }

        /// <summary>The "written by" box at the end of an article.</summary>
        public static string Box(string postAuthor, string fallbackMark)
        {
            var a = Find(postAuthor);
            if (a == null)
                return "<div class=\"author\"><span class=\"byline__av\">" + fallbackMark + "</span><div><b>Written by the Yenetch team</b><p>Our articles are written and reviewed by the marketers, engineers and designers who run these projects for clients every day.</p></div></div>";
            var pic = a.IsTeam && !a.HasPhoto ? "<span class=\"byline__av\">" + fallbackMark + "</span>" : PhotoHtml(a, "author-card__photo");
            return "<div class=\"author author-card\">" + pic + "<div><b>Written by <a href=\"" + H(a.Url) + "\" rel=\"author\">" + H(a.Name) + "</a></b>"
                 + (string.IsNullOrEmpty(a.Role) ? "" : "<span class=\"muted\"> · " + H(a.Role) + "</span>") + "<p>" + H(a.Bio) + "</p><a class=\"more\" href=\"" + H(a.Url) + "\">More from " + H(a.Name.Split(' ')[0]) + "</a></div></div>";
        }

        /// <summary>schema.org author for an article: a Person (or the team as an Organization) with a profile link.</summary>
        public static Dictionary<string, object> Ld(string postAuthor)
        {
            var a = Find(postAuthor);
            if (a == null) return new Dictionary<string, object> { { "@type", "Organization" }, { "name", string.IsNullOrEmpty(postAuthor) ? "Yenetch" : postAuthor } };
            return PersonLd(a);
        }

        public static Dictionary<string, object> PersonLd(Author a)
        {
            var d = new Dictionary<string, object> { { "@type", a.IsTeam ? "Organization" : "Person" }, { "name", a.Name }, { "url", Seo.Root + a.Url } };
            if (!a.IsTeam && !string.IsNullOrEmpty(a.Role)) d["jobTitle"] = a.Role;
            if (!string.IsNullOrEmpty(a.Bio)) d["description"] = a.Bio;
            if (a.HasPhoto) d["image"] = Absolute(Photos.Src(a.Photo));
            var same = new[] { a.Linkedin, a.Twitter, a.Website }.Where(u => !string.IsNullOrWhiteSpace(u) && u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)).ToList();
            if (same.Count > 0) d["sameAs"] = same;
            if (a.Expertise != null && a.Expertise.Count > 0) d["knowsAbout"] = a.Expertise;
            if (!a.IsTeam) d["worksFor"] = new Dictionary<string, object> { { "@type", "Organization" }, { "name", "Yenetch" }, { "url", Seo.Root } };
            return d;
        }

        private static string Absolute(string src) { return src.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? src : Seo.Root + src; }
        private static string H(string s) { return HttpUtility.HtmlEncode(s ?? ""); }
    }
}
