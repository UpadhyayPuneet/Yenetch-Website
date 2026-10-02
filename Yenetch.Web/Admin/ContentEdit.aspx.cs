using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>
    /// /admin/content/{collection}/{id|new}. The form is drawn by assets/admin/content.js from the collection's fields
    /// (ContentSchema) and posted back as one JSON value.
    /// </summary>
    public partial class ContentEditPage : AdminPage
    {
        public override string Section { get { return Convert.ToString(RouteData.Values["collection"]) == "landing" ? "landing" : "content"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected ContentCollection C;
        protected ContentItem Item;
        protected string ValueJson = "{}", Heading, ViewUrl;
        protected bool Active = true;
        protected Dictionary<string, List<KeyValuePair<string, string>>> Choices = new Dictionary<string, List<KeyValuePair<string, string>>>();

        private static readonly JavaScriptSerializer Js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };
        private bool IsJson { get { return C.Fields.Count == 1 && C.Fields[0].Type == "json"; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            C = ContentSchema.Get(Convert.ToString(RouteData.Values["collection"]));
            if (C == null) { RedirectWith("/admin/content", "Section not found."); return; }
            ContentStore.EnsureSeeded();
            var key = Convert.ToString(RouteData.Values["id"] ?? "new");
            int id;
            if (key == "new")
            {
                if (C.Fixed) { RedirectWith("/admin/content/" + C.Key, null); return; }
                if (C.IsSingle) { var existing = ContentStore.Items(C.Key).FirstOrDefault(); if (existing != null) { RedirectWith("/admin/content/" + C.Key + "/" + existing.Id, null); return; } }
                Item = new ContentItem { IsActive = true, Data = "{}" };
            }
            else
            {
                Item = int.TryParse(key, out id) ? ContentStore.Get(id) : null;
                if (Item == null || Item.Collection != C.Key) { RedirectWith("/admin/content/" + C.Key, "Item not found."); return; }
            }
            Heading = C.IsSingle ? C.Title : Item.Id == 0 ? "Add to " + C.Title.ToLowerInvariant() : Item.Title;
            Title = Heading;
            Active = Item.IsActive;
            ViewUrl = PublicUrl(C.Key, Item.ItemKey);
            foreach (var src in Sources(C.Fields).Distinct()) Choices[src] = ContentStore.Choices(src.TrimStart('@'));

            if (!IsPostBack) { ValueJson = ToFormValue(Item.Data); return; }
            if (Request.Form["delete"] == "1" && Item.Id > 0 && !C.Fixed && !C.IsSingle)
            {
                ContentStore.Delete(Item.Id);
                RedirectWith("/admin/content/" + C.Key, "Deleted.");
            }
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            var posted = Request.Form["data"] ?? "{}";
            ValueJson = posted;
            Active = C.IsSingle || Request.Form["active"] == "1";
            Dictionary<string, object> data;
            try { data = Js.DeserializeObject(posted) as Dictionary<string, object>; }
            catch { data = null; }
            if (data == null) { Fail("The form could not be read. Reload the page and try again."); return; }
            if (IsJson)
            {
                try { data = Js.DeserializeObject(Convert.ToString(data.ContainsKey("_json") ? data["_json"] : "")) as Dictionary<string, object>; }
                catch (Exception ex) { Fail("The JSON is not valid: " + ex.Message); return; }
                if (data == null) { Fail("The JSON must be an object in { }."); return; }
            }
            if (C.Fixed) data.Remove("_key");
            Item.IsActive = Active;
            int newId;
            var error = ContentStore.Save(C, Item, data, Me.Id, out newId);
            if (error != null) { Fail(error); return; }
            RedirectWith("/admin/content/" + C.Key + (C.IsSingle ? "/" + newId : ""), "Saved. The website is updated.");
        }

        /// <summary>The stored JSON as the form expects it (map key and raw JSON fields added).</summary>
        private string ToFormValue(string stored)
        {
            if (IsJson)
            {
                var pretty = PrettyJson(stored ?? "{}");
                return Js.Serialize(new Dictionary<string, object> { { "_json", pretty } });
            }
            var d = Js.DeserializeObject(stored ?? "{}") as Dictionary<string, object> ?? new Dictionary<string, object>();
            if (C.IsMap && !C.Fixed) d["_key"] = Item.ItemKey;
            return Js.Serialize(d);
        }

        private static IEnumerable<string> Sources(IEnumerable<ContentField> fields)
        {
            foreach (var f in fields)
            {
                if (!string.IsNullOrEmpty(f.Source)) yield return f.Source;
                if (f.Fields != null) foreach (var s in Sources(f.Fields)) yield return s;
            }
        }

        private static string PublicUrl(string collection, string key)
        {
            switch (collection)
            {
                case "services": case "serviceCopy": return string.IsNullOrEmpty(key) ? null : "/services/" + key;
                case "products": case "productCopy": return string.IsNullOrEmpty(key) ? null : "/products/" + key;
                case "caseStudies": return string.IsNullOrEmpty(key) ? null : "/case-studies/" + key;
                case "legal": return key == "terms" ? "/terms" : "/privacy";
                case "seo": return key;
                case "team": case "squads": case "company": return "/about";
                case "jobs": case "perks": case "hiringSteps": return "/careers";
                case "talent": return "/talent-resourcing";
                case "finder": return "/solution-finder";
                case "landing": return string.IsNullOrEmpty(key) ? null : "/" + key;
                default: return "/";
            }
        }

        private static string PrettyJson(string json)
        {
            var sb = new System.Text.StringBuilder();
            int indent = 0; bool quoted = false;
            for (var i = 0; i < json.Length; i++)
            {
                var ch = json[i];
                if (ch == '"' && (i == 0 || json[i - 1] != '\\')) quoted = !quoted;
                if (quoted) { sb.Append(ch); continue; }
                switch (ch)
                {
                    case '{': case '[': sb.Append(ch).Append('\n').Append(new string(' ', ++indent * 2)); break;
                    case '}': case ']': sb.Append('\n').Append(new string(' ', --indent * 2)).Append(ch); break;
                    case ',': sb.Append(ch).Append('\n').Append(new string(' ', indent * 2)); break;
                    case ':': sb.Append(": "); break;
                    default: if (!char.IsWhiteSpace(ch)) sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        private void Fail(string message) { ErrorText.Text = Server.HtmlEncode(message); ErrorBox.Visible = true; }
    }
}
