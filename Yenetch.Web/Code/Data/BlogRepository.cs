using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>Blog data access. Pages only talk to this interface, so moving to SQL Server is a config change.</summary>
    public interface IBlogRepository
    {
        IList<BlogPost> List(int page, int pageSize, string category, out int total);
        IList<BlogPost> Latest(int count);
        BlogPost BySlug(string slug);
        IList<string> Categories();
    }

    public static class BlogStore
    {
        /// <summary>
        /// appSettings "BlogSource":
        /// Auto (default) = the built-in articles in assets/data plus posts written in the admin (dbo.CmsPosts in CrmDb);
        /// Json = built-in articles only; Sql = the legacy dbo.BlogPosts table via the YenetchDb connection string.
        /// </summary>
        public static IBlogRepository Repository
        {
            get
            {
                var source = ConfigurationManager.AppSettings["BlogSource"] ?? "Auto";
                if (source.Equals("Sql", StringComparison.OrdinalIgnoreCase)) return new SqlBlogRepository();
                if (source.Equals("Json", StringComparison.OrdinalIgnoreCase) || !Yenetch.Crm.Db.IsConfigured) return new JsonBlogRepository();
                return new CmsBlogRepository();
            }
        }

        /// <summary>Called after a post is saved in the admin so the site shows it straight away.</summary>
        public static void Invalidate() { System.Web.HttpRuntime.Cache.Remove(CmsBlogRepository.CacheKey); }
    }

    /// <summary>
    /// Built-in articles merged with admin posts. An admin post with the same slug replaces the built-in one;
    /// status Hidden removes it. Published posts with a future date stay off the site until that time.
    /// The merged list is cached for a minute. If the database cannot be reached the built-in articles are still served.
    /// </summary>
    public class CmsBlogRepository : IBlogRepository
    {
        internal const string CacheKey = "blog.cms";

        private static List<BlogPost> All
        {
            get
            {
                var cache = System.Web.HttpRuntime.Cache;
                var list = cache[CacheKey] as List<BlogPost>;
                if (list != null) return list;
                var bySlug = SiteContent.Current.Blog.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);
                var expires = DateTime.UtcNow.AddMinutes(1);
                try
                {
                    Yenetch.Crm.Db.EnsureSchema();
                    foreach (var row in Yenetch.Crm.Posts.All().Where(r => r.Status != "Draft"))
                    {
                        if (row.Status == "Hidden") { if (!row.IsScheduled) bySlug.Remove(row.Slug); continue; }
                        if (row.IsLive) bySlug[row.Slug] = row.ToBlogPost();
                        else if (row.PublishedOn.HasValue && row.PublishedOn.Value < expires) expires = row.PublishedOn.Value;
                    }
                }
                catch (Exception ex)
                {
                    Yenetch.Crm.Mailer.Log("Blog posts could not be read from the database", ex);
                    expires = DateTime.UtcNow.AddSeconds(20);
                }
                list = bySlug.Values.OrderByDescending(p => p.PublishedOn).ThenBy(p => p.Title).ToList();
                cache.Insert(CacheKey, list, null, expires, System.Web.Caching.Cache.NoSlidingExpiration);
                return list;
            }
        }

        public IList<BlogPost> List(int page, int pageSize, string category, out int total)
        {
            IEnumerable<BlogPost> q = All;
            if (!string.IsNullOrEmpty(category)) q = q.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            total = q.Count();
            return q.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList();
        }

        public IList<BlogPost> Latest(int count) { return All.Take(count).ToList(); }

        public BlogPost BySlug(string slug)
        {
            var post = All.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return post != null && string.IsNullOrEmpty(post.BodyHtml) ? new JsonBlogRepository().BySlug(post.Slug) : post;
        }

        public IList<string> Categories() { return All.Select(p => p.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList(); }

        /// <summary>Any post by slug whatever its status, for the signed-in preview (/blog/{slug}?preview=1).</summary>
        public static BlogPost Preview(string slug)
        {
            var row = Yenetch.Crm.Posts.BySlug(slug);
            return row != null ? row.ToBlogPost() : new JsonBlogRepository().BySlug(slug);
        }
    }

    public class JsonBlogRepository : IBlogRepository
    {
        private static IEnumerable<BlogPost> All { get { return SiteContent.Current.Blog.OrderByDescending(p => p.PublishedOn); } }

        public IList<BlogPost> List(int page, int pageSize, string category, out int total)
        {
            var q = All;
            if (!string.IsNullOrEmpty(category)) q = q.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            total = q.Count();
            return q.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList();
        }

        public IList<BlogPost> Latest(int count) { return All.Take(count).ToList(); }
        public BlogPost BySlug(string slug)
        {
            var post = All.FirstOrDefault(p => p.Slug == slug);
            if (post != null && string.IsNullOrEmpty(post.BodyHtml))
            {
                // Article bodies live next to the JSON as assets/data/blog/{slug}.html until posts move to SQL.
                var file = System.Web.HttpContext.Current.Server.MapPath("~/assets/data/blog/" + slug + ".html");
                if (System.IO.File.Exists(file)) post.BodyHtml = System.IO.File.ReadAllText(file);
            }
            return post;
        }
        public IList<string> Categories() { return All.Select(p => p.Category).Distinct().OrderBy(c => c).ToList(); }
    }

    /// <summary>SQL Server implementation against dbo.BlogPosts (see App_Data/sql/schema.sql).</summary>
    public class SqlBlogRepository : IBlogRepository
    {
        private const string Columns = "Id, Slug, Title, MetaTitle, MetaDescription, Category, Excerpt, BodyHtml, Author, CoverImage, ReadMinutes, PublishedOn";

        private static SqlConnection Open()
        {
            var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["YenetchDb"].ConnectionString);
            cn.Open();
            return cn;
        }

        private static BlogPost Map(SqlDataReader r)
        {
            return new BlogPost
            {
                Id = r.GetInt32(0),
                Slug = r.GetString(1),
                Title = r.GetString(2),
                MetaTitle = r.IsDBNull(3) ? null : r.GetString(3),
                MetaDescription = r.IsDBNull(4) ? null : r.GetString(4),
                Category = r.GetString(5),
                Excerpt = r.GetString(6),
                BodyHtml = r.IsDBNull(7) ? "" : r.GetString(7),
                Author = r.IsDBNull(8) ? "Team Yenetch" : r.GetString(8),
                CoverImage = r.IsDBNull(9) ? null : r.GetString(9),
                ReadMinutes = r.GetInt32(10),
                Date = r.GetDateTime(11).ToString("yyyy-MM-dd")
            };
        }

        public IList<BlogPost> List(int page, int pageSize, string category, out int total)
        {
            var list = new List<BlogPost>();
            using (var cn = Open())
            {
                var where = "WHERE IsPublished = 1" + (string.IsNullOrEmpty(category) ? "" : " AND Category = @cat");
                using (var count = new SqlCommand("SELECT COUNT(*) FROM dbo.BlogPosts " + where, cn))
                {
                    if (!string.IsNullOrEmpty(category)) count.Parameters.AddWithValue("@cat", category);
                    total = (int)count.ExecuteScalar();
                }
                var sql = "SELECT " + Columns + " FROM dbo.BlogPosts " + where +
                          " ORDER BY PublishedOn DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    if (!string.IsNullOrEmpty(category)) cmd.Parameters.AddWithValue("@cat", category);
                    cmd.Parameters.AddWithValue("@skip", (Math.Max(page, 1) - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@take", pageSize);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(Map(r));
                }
            }
            return list;
        }

        public IList<BlogPost> Latest(int count)
        {
            int total;
            return List(1, count, null, out total);
        }

        public BlogPost BySlug(string slug)
        {
            using (var cn = Open())
            using (var cmd = new SqlCommand("SELECT " + Columns + " FROM dbo.BlogPosts WHERE Slug = @slug AND IsPublished = 1", cn))
            {
                cmd.Parameters.AddWithValue("@slug", slug);
                using (var r = cmd.ExecuteReader()) return r.Read() ? Map(r) : null;
            }
        }

        public IList<string> Categories()
        {
            var list = new List<string>();
            using (var cn = Open())
            using (var cmd = new SqlCommand("SELECT DISTINCT Category FROM dbo.BlogPosts WHERE IsPublished = 1 ORDER BY Category", cn))
            using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(r.GetString(0));
            return list;
        }
    }
}
