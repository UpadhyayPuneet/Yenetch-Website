using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/leads. Views, filters, sorting, paging, bulk assign and status, and CSV export (Export.ashx).</summary>
    public partial class Leads : AdminPage
    {
        public override string Section { get { return "leads"; } }
        protected const int PageSize = 25;

        protected class ViewDef { public string Key, Label; public int Count; }

        protected List<Lead> Rows;
        protected List<ViewDef> Views;
        protected List<CrmUser> Team;
        protected int Total;
        protected string View { get { var v = Q("view"); return v == "" ? "open" : v; } }
        protected bool Filtered { get { return new[] { "q", "status", "type", "source", "priority", "owner", "created" }.Any(k => Q(k) != ""); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            Team = Auth.ActiveUsers();
            if (IsPostBack && Request.Form["bulk"] == "apply") { Bulk(); return; }

            var views = new List<ViewDef>
            {
                new ViewDef { Key = "open", Label = "Open" }, new ViewDef { Key = "mine", Label = "Mine" }, new ViewDef { Key = "new", Label = "New" },
                new ViewDef { Key = "unassigned", Label = "Unassigned" }, new ViewDef { Key = "overdue", Label = "Overdue" },
                new ViewDef { Key = "won", Label = "Won" }, new ViewDef { Key = "lost", Label = "Lost" }, new ViewDef { Key = "all", Label = "All" }
            };
            foreach (var v in views) { int n; LeadService.List(ForView(v.Key, false), 0, 1, out n); v.Count = n; }
            Views = views;
            Rows = LeadService.List(ForView(View, true), (PageNo - 1) * PageSize, PageSize, out Total);
        }

        /// <summary>The filter for a view, plus the query-string filters when asked.</summary>
        internal LeadFilter ForView(string view, bool withQuery) { return ForView(Request.QueryString, Me, view, withQuery); }

        /// <summary>Shared with the CSV export so it exports exactly what the list shows.</summary>
        internal static LeadFilter ForView(System.Collections.Specialized.NameValueCollection qs, CrmUser me, string view, bool withQuery)
        {
            Func<string, string> Qs = k => (qs[k] ?? "").Trim();
            Func<string, int> Qi = k => { int v; return int.TryParse(Qs(k), out v) ? v : 0; };
            var f = new LeadFilter { RestrictToUser = me.SeesAllLeads ? (int?)null : me.Id, Sort = Qs("sort") == "" ? null : Qs("sort") };
            switch (view)
            {
                case "mine": f.AssignedTo = me.Id; f.OpenOnly = true; break;
                case "new": f.Status = "New"; break;
                case "unassigned": f.Unassigned = true; f.OpenOnly = true; break;
                case "overdue": f.OverdueOnly = true; if (f.Sort == null) f.Sort = "followup"; break;
                case "won": f.Status = "Won"; break;
                case "lost": f.Status = "Lost"; break;
                case "all": break;
                default: f.OpenOnly = true; break;
            }
            if (!withQuery) return f;
            f.Search = Qs("q");
            if (Qs("status") != "") { f.Status = Qs("status"); f.OpenOnly = false; }
            if (Qs("type") != "") f.LeadType = Qs("type");
            if (Qs("source") != "") f.Source = Qs("source");
            if (Qs("priority") != "") f.Priority = Qs("priority");
            if (Qs("owner") == "none") f.Unassigned = true;
            else if (Qs("owner") != "" && me.SeesAllLeads) f.AssignedTo = Qi("owner");
            var today = Util.TodayIst;
            switch (Qs("created"))
            {
                case "today": f.From = Util.FromIst(today); break;
                case "7d": f.From = Util.FromIst(today.AddDays(-6)); break;
                case "30d": f.From = Util.FromIst(today.AddDays(-29)); break;
                case "90d": f.From = Util.FromIst(today.AddDays(-89)); break;
            }
            return f;
        }

        private void Bulk()
        {
            var ids = (Request.Form["ids"] ?? "").Split(',').Select(s => { int i; return int.TryParse(s, out i) ? i : 0; }).Where(i => i > 0).ToList();
            var owner = Request.Form["bulkOwner"] ?? "";
            var status = Request.Form["bulkStatus"] ?? "";
            if (ids.Count == 0 || (owner == "" && status == "")) { RedirectWith(Request.RawUrl, "Select leads and an action first."); return; }
            foreach (var id in ids)
            {
                var lead = LeadService.Get(id);
                if (lead == null || (!Me.SeesAllLeads && lead.AssignedTo.HasValue && lead.AssignedTo != Me.Id)) continue;
                if (owner != "")
                {
                    int? to = owner == "none" ? (int?)null : int.Parse(owner);
                    if (!Me.SeesAllLeads && to != Me.Id) continue;
                    if (lead.AssignedTo != to) LeadService.Assign(id, to, Me.Id);
                }
                if (status != "") LeadService.SetStatus(id, status, Me.Id);
            }
            RedirectWith(Request.RawUrl, ids.Count + (ids.Count == 1 ? " lead updated." : " leads updated."));
        }

        protected string Sort(string key, string label)
        {
            var cur = Q("sort");
            var on = cur.TrimStart('-') == key;
            var next = on && !cur.StartsWith("-") ? "-" + key : key;
            if (!on && (key == "created" || key == "value" || key == "priority")) next = key == "priority" ? key : "-" + key;
            return "<a href=\"" + Att(With("sort", next)) + "\"" + (on ? " class=\"is-sorted\"" : "") + ">" + H(label) + (on ? (cur.StartsWith("-") ? " ↓" : " ↑") : "") + "</a>";
        }

        protected static string Options(IEnumerable<string> values, string selected, string[] labels = null)
        {
            var list = values.ToList();
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < list.Count; i++)
                sb.Append("<option value=\"" + Att(list[i]) + "\"" + (list[i] == selected ? " selected" : "") + ">" + H(labels != null ? labels[i] : list[i]) + "</option>");
            return sb.ToString();
        }
    }
}
