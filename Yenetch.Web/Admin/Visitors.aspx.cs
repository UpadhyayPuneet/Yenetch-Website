using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/visitors. Consenting visitors with location, device, source and activity; filters for live and converted.</summary>
    public partial class Visitors : AdminPage
    {
        public override string Section { get { return "visitors"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }
        protected const int PageSize = 30;

        protected List<Stats.VisitorRow> Rows;
        protected int Total, AllCount, LiveCount, LeadCount;

        protected void Page_Load(object sender, EventArgs e)
        {
            Rows = Stats.Visitors(Q("q"), Q("leads") == "1", Q("live") == "1", (PageNo - 1) * PageSize, PageSize, out Total);
            Stats.Visitors(null, false, false, 0, 1, out AllCount);
            Stats.Visitors(null, false, true, 0, 1, out LiveCount);
            Stats.Visitors(null, true, false, 0, 1, out LeadCount);
        }
    }
}
