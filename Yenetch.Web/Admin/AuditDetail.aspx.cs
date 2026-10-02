using System;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/audits/{id}: the full report a visitor received.</summary>
    public partial class AuditDetailPage : AdminPage
    {
        public override string Section { get { return "audits"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected Row Row;
        protected AuditResult R;

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            int.TryParse(Convert.ToString(RouteData.Values["id"]), out id);
            R = Audits.Get(id, out Row);
            if (Row == null) { RedirectWith("/admin/audits", "That audit was not found."); return; }
            Title = AuditsPage.Host(Row.Str("FinalUrl") ?? Row.Str("Url")) + " · Website audit";
        }

        protected static string StatName(string key)
        {
            switch (key) { case "time": return "Response time"; case "size": return "Page size"; case "scripts": return "Scripts"; case "images": return "Images"; default: return key; }
        }
    }
}
