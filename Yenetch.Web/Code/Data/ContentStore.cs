using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>A content item as stored in dbo.CmsItems.</summary>
    public class ContentItem
    {
        public int Id { get; set; }
        public string Collection { get; set; }
        public string ItemKey { get; set; }
        public string Title { get; set; }
        public int Sort { get; set; }
        public bool IsActive { get; set; }
        public string Data { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string UpdatedByName { get; set; }

        internal static ContentItem From(Row r)
        {
            return new ContentItem
            {
                Id = r.Int("Id"), Collection = r.Str("Collection"), ItemKey = r.Str("ItemKey"), Title = r.Str("Title"), Sort = r.Int("Sort"),
                IsActive = r.Bool("IsActive"), Data = r.Str("Data"), UpdatedOn = r.Date("UpdatedOn"), UpdatedByName = r.Str("UpdatedByName")
            };
        }
    }

    public class LegalPage { public string Title { get; set; } public string Html { get; set; } }
    public class SeoOverride { public string Path { get; set; } public string Title { get; set; } public string Description { get; set; } }

    /// <summary>
    /// The website content database (dbo.CmsItems). Items are assembled into the same JSON shapes the site has always
    /// used (yenetch.json and pages.json), so every page, the chatbot and the solution finder read live data.
    /// On first run the tables are filled from assets/data and App_Data/seed; after that only the database is read.
    /// The assembled content is cached until something is saved in the admin.
    /// </summary>
    public static class ContentStore
    {
        public class Snapshot
        {
            public SiteData Site;
            public PageCopy Copy;
            public string SiteJson;
            public Dictionary<string, LegalPage> Legal = new Dictionary<string, LegalPage>(StringComparer.OrdinalIgnoreCase);
            public List<SeoOverride> Seo = new List<SeoOverride>();
            public Dictionary<string, LandingPage> Landing = new Dictionary<string, LandingPage>(StringComparer.OrdinalIgnoreCase);
        }

        private const string CacheKey = "content.snapshot";
        private static readonly object Lock = new object();
        private static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 }; }

        public static Snapshot Current
        {
            get
            {
                var s = HttpRuntime.Cache[CacheKey] as Snapshot;
                if (s != null) return s;
                lock (Lock)
                {
                    s = HttpRuntime.Cache[CacheKey] as Snapshot;
                    if (s != null) return s;
                    s = Build(null);
                    HttpRuntime.Cache.Insert(CacheKey, s, null, DateTime.UtcNow.AddHours(6), System.Web.Caching.Cache.NoSlidingExpiration);
                    return s;
                }
            }
        }

        public static void Invalidate() { HttpRuntime.Cache.Remove(CacheKey); BlogStore.Invalidate(); SiteDataScript.Invalidate(); }

        // ---- Assembly -----------------------------------------------------------------------------------------

        /// <summary>Builds the site content from the database. override replaces one item's data (used to test a save).</summary>
        private static Snapshot Build(ContentItem replace)
        {
            Db.EnsureSchema();
            EnsureSeeded();
            var js = Json();
            var site = new Dictionary<string, object>();
            var copy = new Dictionary<string, object>();
            var snap = new Snapshot();

            // Every collection exists even when empty, so pages never see null lists.
            foreach (var c in ContentSchema.All)
            {
                if (c.Target.StartsWith("site:")) Slot(site, c.Target.Substring(5), c);
                else if (c.Target.StartsWith("copy:")) Slot(copy, c.Target.Substring(5), c);
            }
            site["blog"] = new ArrayList();

            var rows = Db.Query("SELECT Id, Collection, ItemKey, Data FROM CmsItems WHERE IsActive = 1 ORDER BY Collection, Sort, Id", r => new ContentItem { Id = r.Int("Id"), Collection = r.Str("Collection"), ItemKey = r.Str("ItemKey"), Data = r.Str("Data") });
            if (replace != null)
            {
                rows.RemoveAll(r => r.Id == replace.Id);
                if (replace.IsActive) rows.Add(replace);
            }
            foreach (var row in rows)
            {
                var c = ContentSchema.Get(row.Collection);
                if (c == null) continue;
                var data = js.DeserializeObject(row.Data ?? "{}") as Dictionary<string, object> ?? new Dictionary<string, object>();
                if (c.Target == "legal") { snap.Legal[row.ItemKey ?? ""] = new LegalPage { Title = S(data, "title"), Html = S(data, "html") }; continue; }
                if (c.Target == "landing")
                {
                    // One bad page must not take the rest of the site content down with it.
                    try { var lp = js.Deserialize<LandingPage>(js.Serialize(data)); lp.Slug = row.ItemKey; if (!string.IsNullOrEmpty(lp.Slug)) snap.Landing[lp.Slug] = lp; }
                    catch (Exception ex) { Yenetch.Crm.Mailer.Log("landing page " + row.ItemKey, ex); }
                    continue;
                }
                if (c.Target == "seo") { snap.Seo.Add(new SeoOverride { Path = Norm(S(data, "path")), Title = S(data, "title"), Description = S(data, "description") }); continue; }
                var root = c.Target.StartsWith("site:") ? site : copy;
                Place(root, c.Target.Substring(5), c, data, row.ItemKey);
            }

            snap.SiteJson = js.Serialize(site);
            snap.Site = js.Deserialize<SiteData>(snap.SiteJson);
            snap.Copy = js.Deserialize<PageCopy>(js.Serialize(copy));
            return snap;
        }

        private static Dictionary<string, object> Parent(Dictionary<string, object> root, string path, out string last)
        {
            var parts = path.Split('.');
            var node = root;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                object next;
                if (!node.TryGetValue(parts[i], out next) || !(next is Dictionary<string, object>)) node[parts[i]] = next = new Dictionary<string, object>();
                node = (Dictionary<string, object>)next;
            }
            last = parts[parts.Length - 1];
            return node;
        }

        private static void Slot(Dictionary<string, object> root, string path, ContentCollection c)
        {
            string last;
            var node = Parent(root, path, out last);
            if (!node.ContainsKey(last)) node[last] = c.IsSingle || c.IsMap ? (object)new Dictionary<string, object>() : new ArrayList();
        }

        private static void Place(Dictionary<string, object> root, string path, ContentCollection c, Dictionary<string, object> data, string key)
        {
            string last;
            var node = Parent(root, path, out last);
            if (c.IsSingle) node[last] = data;
            else if (c.IsMap) ((Dictionary<string, object>)node[last])[key ?? ""] = data;
            else ((ArrayList)node[last]).Add(c.StringField != null ? (object)S(data, c.StringField) : data);
            // Client logos ride alongside the plain name list: site.clientLogos = { "Name": "/uploads/..." }.
            if (c.Key == "clients" && !string.IsNullOrWhiteSpace(S(data, "logo")))
            {
                object logos;
                if (!node.TryGetValue("clientLogos", out logos) || !(logos is Dictionary<string, object>)) node["clientLogos"] = logos = new Dictionary<string, object>();
                ((Dictionary<string, object>)logos)[S(data, c.StringField) ?? ""] = S(data, "logo").Trim();
            }
        }

        private static string S(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null ? Convert.ToString(v) : null; }

        public static string Norm(string path)
        {
            path = (path ?? "").Trim().ToLowerInvariant();
            if (path.StartsWith("http")) { Uri u; if (Uri.TryCreate(path, UriKind.Absolute, out u)) path = u.AbsolutePath; }
            if (!path.StartsWith("/")) path = "/" + path;
            return path.Length > 1 ? path.TrimEnd('/') : path;
        }

        // ---- Admin reads and writes ---------------------------------------------------------------------------

        private const string Select = "SELECT i.*, u.Name AS UpdatedByName FROM CmsItems i LEFT JOIN CrmUsers u ON u.Id = i.UpdatedBy";

        public static List<ContentItem> Items(string collection)
        {
            return Db.Query(Select + " WHERE i.Collection = @c ORDER BY i.Sort, i.Id", ContentItem.From, new { c = collection });
        }

        public static ContentItem Get(int id) { var r = Db.First(Select + " WHERE i.Id = @id", new { id }); return r == null ? null : ContentItem.From(r); }

        public static Dictionary<string, int> Counts()
        {
            return Db.Rows("SELECT Collection, COUNT(*) AS N FROM CmsItems GROUP BY Collection").ToDictionary(r => r.Str("Collection"), r => r.Int("N"));
        }

        /// <summary>Keys (slugs or ids) of a collection, for select fields in the editor.</summary>
        public static List<KeyValuePair<string, string>> Choices(string collection)
        {
            return Items(collection).Where(i => !string.IsNullOrEmpty(i.ItemKey)).Select(i => new KeyValuePair<string, string>(i.ItemKey, i.Title)).ToList();
        }

        /// <summary>
        /// Validates and saves an item. Returns an error message, or null when saved. The whole site content is rebuilt
        /// with the change first, so a save can never leave the website unable to load.
        /// </summary>
        public static string Save(ContentCollection c, ContentItem item, Dictionary<string, object> data, int userId, out int id)
        {
            id = item.Id;
            var js = Json();
            if (c.IsMap && !c.Fixed) { item.ItemKey = S(data, "_key"); data.Remove("_key"); }
            else if (!c.IsMap && c.KeyField != null) item.ItemKey = c.KeyField == "path" ? Norm(S(data, "path")) : (S(data, c.KeyField) ?? "").Trim();
            if (c.KeyField == "slug" || c.KeyField == "id")
            {
                var k = BlogPost.Slugify(item.ItemKey);
                if (c.KeyField == "slug") data["slug"] = k; else data["id"] = k;
                item.ItemKey = k;
            }
            if (c.KeyField == "path" && data.ContainsKey("path")) data["path"] = item.ItemKey;

            if (c.Key == "landing" && ReservedSlugs.Contains(item.ItemKey ?? "")) return "\"" + item.ItemKey + "\" is already used by another page of the website. Choose a different address.";
            foreach (var f in c.Fields.Where(f => f.Required))
                if (string.IsNullOrWhiteSpace(S(data, f.Name))) return f.Label + " is required.";
            if (c.KeyField != null && !c.IsSingle && !c.Fixed)
            {
                if (string.IsNullOrEmpty(item.ItemKey)) return (c.IsMap ? "Choose which one this is for." : "Fill in the " + c.Fields.First(f => f.Name == c.KeyField).Label.ToLowerInvariant() + ".");
                if (Db.Scalar<int>("SELECT COUNT(*) FROM CmsItems WHERE Collection = @c AND ItemKey = @k AND Id <> @id", new { c = c.Key, k = item.ItemKey, id = item.Id }) > 0)
                    return "Another item already uses \"" + item.ItemKey + "\".";
            }

            // Rich text (legal pages and other html fields) is cleaned of scripts before it is stored.
            foreach (var f in c.Fields.Where(f => f.Type == "html"))
            {
                object v;
                if (data.TryGetValue(f.Name, out v) && v is string) data[f.Name] = HtmlSanitizer.Clean((string)v);
            }
            item.Collection = c.Key;
            item.Data = js.Serialize(data);
            item.Title = Util.Cut(TitleOf(c, data, item.ItemKey), 300);
            try { Build(item); }
            catch (Exception ex) { return "This change could not be applied: " + ex.Message; }

            var now = DateTime.UtcNow;
            if (item.Id == 0)
            {
                var sort = Db.Scalar<int?>("SELECT MAX(Sort) FROM CmsItems WHERE Collection = @c", new { c = c.Key }) ?? 0;
                id = Db.Insert("INSERT INTO CmsItems (Collection, ItemKey, Title, Sort, IsActive, Data, UpdatedBy, UpdatedOn) VALUES (@c, @k, @t, @s, @a, @d, @u, @now)",
                    new { c = c.Key, k = item.ItemKey, t = item.Title, s = sort + 10, a = item.IsActive, d = item.Data, u = userId, now });
            }
            else
                Db.Exec("UPDATE CmsItems SET ItemKey = @k, Title = @t, IsActive = @a, Data = @d, UpdatedBy = @u, UpdatedOn = @now WHERE Id = @id",
                    new { k = item.ItemKey, t = item.Title, a = item.IsActive, d = item.Data, u = userId, now, id = item.Id });
            Invalidate();
            return null;
        }

        /// <summary>First-level addresses the website already uses, which a landing page cannot take.</summary>
        public static readonly HashSet<string> ReservedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "admin", "api", "assets", "uploads", "handlers", "app_data", "bin", "r", "digital-marketing", "software-development", "talent-resourcing", "services", "solution-finder",
            "products", "case-studies", "about", "careers", "blog", "contact", "privacy", "terms", "newsletter", "book", "website-audit", "sitemap", "robots", "404", "default", "notfound",
            "pricing", "proposal", "quote", "data"
        };

        public static void Delete(int id) { Db.Exec("DELETE FROM CmsItems WHERE Id = @id", new { id }); Invalidate(); }

        public static void SetActive(int id, bool active) { Db.Exec("UPDATE CmsItems SET IsActive = @a WHERE Id = @id", new { a = active, id }); Invalidate(); }

        /// <summary>Moves an item one place up (-1) or down (+1) and renumbers the collection.</summary>
        public static void Move(int id, int dir)
        {
            var item = Get(id);
            if (item == null) return;
            var list = Items(item.Collection);
            var i = list.FindIndex(x => x.Id == id);
            var j = i + dir;
            if (j < 0 || j >= list.Count) return;
            list[i] = list[j]; list[j] = item;
            for (var n = 0; n < list.Count; n++) Db.Exec("UPDATE CmsItems SET Sort = @s WHERE Id = @id", new { s = (n + 1) * 10, id = list[n].Id });
            Invalidate();
        }

        public static string TitleOf(ContentCollection c, Dictionary<string, object> data, string key)
        {
            // Plans of different services share names (Starter, Growth), so the list shows the service too.
            if (c.Key == "plans" && !string.IsNullOrWhiteSpace(S(data, "name")))
            {
                var svc = S(data, "service");
                var svcName = string.IsNullOrEmpty(svc) ? null : Db.Scalar<string>("SELECT Title FROM CmsItems WHERE Collection = 'services' AND ItemKey = @k", new { k = svc });
                return (svcName ?? svc ?? "All services") + " · " + S(data, "name").Trim();
            }
            foreach (var f in new[] { c.TitleField, "title", "name", "q", "role", "path", "headline" })
            {
                var v = f == null ? null : S(data, f);
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            if (c.IsMap && !string.IsNullOrEmpty(key)) return key;
            return c.Title;
        }

        // ---- First-run seeding --------------------------------------------------------------------------------

        private static readonly object SeedLock = new object();
        private static bool _seeded;

        /// <summary>Fills the content tables from the files that ship with the site, once.</summary>
        public static void EnsureSeeded()
        {
            if (_seeded) return;
            lock (SeedLock)
            {
                if (_seeded) return;
                if (Settings.Get("content.seeded") == null) Seed();
                if (Settings.Get("landing.seeded") == null) SeedLanding();
                // v11: example plans, builder extras, an offer and the editorial author, added once to every site.
                foreach (var k in new[] { "plans", "addons", "offers", "authors" })
                    if (Settings.Get(k + ".seeded") == null) SeedFile(k);
                _seeded = true;
            }
        }

        private static void Seed()
        {
            var js = Json();
            var site = js.DeserializeObject(File.ReadAllText(Util.AppPath("assets/data/yenetch.json"))) as Dictionary<string, object>;
            var copyFile = Util.AppPath("assets/data/pages.json");
            var copy = File.Exists(copyFile) ? js.DeserializeObject(File.ReadAllText(copyFile)) as Dictionary<string, object> : new Dictionary<string, object>();
            var now = DateTime.UtcNow;

            foreach (var c in ContentSchema.All)
            {
                if (Db.Scalar<int>("SELECT COUNT(*) FROM CmsItems WHERE Collection = @c", new { c = c.Key }) > 0) continue;
                var items = new List<Tuple<string, Dictionary<string, object>>>();
                if (c.Target == "legal")
                {
                    foreach (var k in new[] { "privacy", "terms" })
                    {
                        var file = Util.AppPath("App_Data/seed/" + k + ".html");
                        var html = File.Exists(file) ? File.ReadAllText(file) : "<p>Add the text of this page in the admin.</p>";
                        items.Add(Tuple.Create(k, new Dictionary<string, object> { { "title", k == "privacy" ? "Privacy policy" : "Terms of use" }, { "html", html } }));
                    }
                }
                else if (c.Target.StartsWith("site:") || c.Target.StartsWith("copy:"))
                {
                    var src = Find(c.Target.StartsWith("site:") ? site : copy, c.Target.Substring(5));
                    if (src is Dictionary<string, object> && c.IsSingle) items.Add(Tuple.Create((string)null, (Dictionary<string, object>)src));
                    else if (src is Dictionary<string, object> && c.IsMap)
                        foreach (var kv in (Dictionary<string, object>)src) items.Add(Tuple.Create(kv.Key, kv.Value as Dictionary<string, object> ?? new Dictionary<string, object>()));
                    else if (src is IEnumerable && !(src is string))
                        foreach (var x in (IEnumerable)src)
                        {
                            var d = c.StringField != null ? new Dictionary<string, object> { { c.StringField, Convert.ToString(x) } } : x as Dictionary<string, object>;
                            if (d != null) items.Add(Tuple.Create(c.KeyField != null ? S(d, c.KeyField) : null, d));
                        }
                }
                var sort = 0;
                foreach (var it in items)
                {
                    sort += 10;
                    Db.Exec("INSERT INTO CmsItems (Collection, ItemKey, Title, Sort, IsActive, Data, UpdatedOn) VALUES (@c, @k, @t, @s, @a, @d, @now)",
                        new { c = c.Key, k = it.Item1, t = Util.Cut(TitleOf(c, it.Item2, it.Item1), 300), s = sort, a = true, d = js.Serialize(it.Item2), now });
                }
            }
            SeedBlog(site);
            Settings.Set("content.seeded", now.ToString("o"));
        }

        /// <summary>Adds the example landing pages that ship with the site (App_Data/seed/landing.json), once. Sites set up before v10 get them too.</summary>
        private static void SeedLanding()
        {
            var file = Util.AppPath("App_Data/seed/landing.json");
            if (File.Exists(file) && Db.Scalar<int>("SELECT COUNT(*) FROM CmsItems WHERE Collection = 'landing'") == 0)
            {
                var js = Json();
                var c = ContentSchema.Get("landing");
                var sort = 0;
                foreach (var x in (js.DeserializeObject(File.ReadAllText(file)) as IEnumerable) ?? new object[0])
                {
                    var d = x as Dictionary<string, object>;
                    if (d == null || string.IsNullOrEmpty(S(d, "slug"))) continue;
                    sort += 10;
                    Db.Exec("INSERT INTO CmsItems (Collection, ItemKey, Title, Sort, IsActive, Data, UpdatedOn) VALUES (@c, @k, @t, @s, @a, @d, @now)",
                        new { c = c.Key, k = S(d, "slug"), t = Util.Cut(TitleOf(c, d, S(d, "slug")), 300), s = sort, a = true, d = js.Serialize(d), now = DateTime.UtcNow });
                }
            }
            Settings.Set("landing.seeded", DateTime.UtcNow.ToString("o"));
        }

        /// <summary>Adds the items in App_Data/seed/{collection}.json to an empty collection, once.</summary>
        private static void SeedFile(string collection)
        {
            var file = Util.AppPath("App_Data/seed/" + collection + ".json");
            var c = ContentSchema.Get(collection);
            if (c != null && File.Exists(file) && Db.Scalar<int>("SELECT COUNT(*) FROM CmsItems WHERE Collection = @c", new { c = collection }) == 0)
            {
                var js = Json();
                var sort = 0;
                foreach (var x in (js.DeserializeObject(File.ReadAllText(file)) as IEnumerable) ?? new object[0])
                {
                    var d = x as Dictionary<string, object>;
                    if (d == null) continue;
                    var key = c.KeyField == null ? null : S(d, c.KeyField);
                    sort += 10;
                    Db.Exec("INSERT INTO CmsItems (Collection, ItemKey, Title, Sort, IsActive, Data, UpdatedOn) VALUES (@c, @k, @t, @s, @a, @d, @now)",
                        new { c = c.Key, k = key, t = Util.Cut(TitleOf(c, d, key), 300), s = sort, a = true, d = js.Serialize(d), now = DateTime.UtcNow });
                }
            }
            Settings.Set(collection + ".seeded", DateTime.UtcNow.ToString("o"));
        }

        /// <summary>Copies the articles that ship with the site into the blog table (skips any already there).</summary>
        private static void SeedBlog(Dictionary<string, object> site)
        {
            var list = Find(site, "blog") as IEnumerable;
            if (list == null) return;
            foreach (var x in list)
            {
                var d = x as Dictionary<string, object>;
                var slug = S(d, "slug");
                if (string.IsNullOrEmpty(slug) || Posts.BySlug(slug) != null) continue;
                var file = Util.AppPath("assets/data/blog/" + slug + ".html");
                DateTime date;
                var published = DateTime.TryParse(S(d, "date"), out date) ? Util.FromIst(date.AddHours(10)) : DateTime.UtcNow;
                Posts.Save(new CmsPost
                {
                    Slug = slug, Title = S(d, "title"), MetaTitle = S(d, "metaTitle"), MetaDescription = S(d, "metaDescription"), Category = S(d, "category") ?? "Insights",
                    Excerpt = S(d, "excerpt") ?? "", BodyHtml = File.Exists(file) ? File.ReadAllText(file) : "", CoverImage = S(d, "coverImage"),
                    Author = S(d, "author"), Status = "Published", PublishedOn = published
                }, 0);
            }
        }

        private static object Find(Dictionary<string, object> root, string path)
        {
            object node = root;
            foreach (var p in path.Split('.'))
            {
                var d = node as Dictionary<string, object>;
                if (d == null || !d.TryGetValue(p, out node)) return null;
            }
            return node;
        }
    }

    /// <summary>Name/value settings in dbo.CmsSettings. Protected values are encrypted with the machine key.</summary>
    public static class Settings
    {
        public static string Get(string name)
        {
            var cache = HttpRuntime.Cache["settings"] as Dictionary<string, string>;
            if (cache == null)
            {
                cache = Db.Rows("SELECT Name, Value FROM CmsSettings").ToDictionary(r => r.Str("Name"), r => r.Str("Value"), StringComparer.OrdinalIgnoreCase);
                HttpRuntime.Cache.Insert("settings", cache, null, DateTime.UtcNow.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);
            }
            string v;
            return cache.TryGetValue(name, out v) ? v : null;
        }

        public static void Set(string name, string value)
        {
            if (Db.Scalar<int>("SELECT COUNT(*) FROM CmsSettings WHERE Name = @n", new { n = name }) > 0)
                Db.Exec("UPDATE CmsSettings SET Value = @v, UpdatedOn = @now WHERE Name = @n", new { n = name, v = value, now = DateTime.UtcNow });
            else
                Db.Exec("INSERT INTO CmsSettings (Name, Value, UpdatedOn) VALUES (@n, @v, @now)", new { n = name, v = value, now = DateTime.UtcNow });
            HttpRuntime.Cache.Remove("settings");
            HttpRuntime.Cache.Remove("fx.rates");
            SiteDataScript.Invalidate();
        }

        public static string GetSecret(string name)
        {
            var v = Get(name);
            if (string.IsNullOrEmpty(v)) return v;
            try { return System.Text.Encoding.UTF8.GetString(System.Web.Security.MachineKey.Unprotect(Convert.FromBase64String(v), "settings", name)); }
            catch { return null; }
        }

        public static void SetSecret(string name, string value)
        {
            Set(name, string.IsNullOrEmpty(value) ? null : Convert.ToBase64String(System.Web.Security.MachineKey.Protect(System.Text.Encoding.UTF8.GetBytes(value), "settings", name)));
        }
    }
}
