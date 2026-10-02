using System;
using System.Collections.Generic;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/content/{collection}. Items of one section: show or hide, reorder, open to edit, add.</summary>
    public partial class ContentListPage : AdminPage
    {
        public override string Section { get { return Convert.ToString(RouteData.Values["collection"]) == "landing" ? "landing" : "content"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected ContentCollection C;
        protected List<ContentItem> Rows;

        protected void Page_Load(object sender, EventArgs e)
        {
            C = ContentSchema.Get(Convert.ToString(RouteData.Values["collection"]));
            if (C == null) { RedirectWith("/admin/content", "Section not found."); return; }
            ContentStore.EnsureSeeded();
            Title = C.Title;
            Rows = ContentStore.Items(C.Key);
            if (C.IsSingle) { RedirectWith("/admin/content/" + C.Key + "/" + (Rows.Count > 0 ? Rows[0].Id.ToString() : "new"), null); return; }

            if (IsPostBack)
            {
                var op = (Request.Form["op"] ?? "").Split(':');
                int id;
                if (op.Length != 2 || !int.TryParse(op[1], out id) || !Rows.Exists(r => r.Id == id)) return;
                var row = Rows.Find(r => r.Id == id);
                switch (op[0])
                {
                    case "up": ContentStore.Move(id, -1); break;
                    case "down": ContentStore.Move(id, 1); break;
                    case "toggle": ContentStore.SetActive(id, !row.IsActive); break;
                }
                RedirectWith(Request.RawUrl, op[0] == "toggle" ? (row.IsActive ? "Hidden from the site." : "Shown on the site.") : null);
            }
        }
    }
}
