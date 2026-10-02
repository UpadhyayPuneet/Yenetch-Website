using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/proposals: every proposal with its status. Sales users see proposals they created or for their leads.</summary>
    public partial class ProposalListPage : AdminPage
    {
        public override string Section { get { return "proposals"; } }

        protected List<Proposal> Rows;
        protected int Total, Open, Accepted90, Sent90, Paid90, Viewed;
        protected decimal OpenValue;

        protected void Page_Load(object sender, EventArgs e)
        {
            var status = Q("status");
            if (Array.IndexOf(Proposals.Statuses, status) < 0) status = null;
            int? restrict = Me.SeesAllLeads ? (int?)null : Me.Id;
            Rows = Proposals.List(status, null, restrict, (PageNo - 1) * 50, 50, out Total);
            var scope = restrict.HasValue ? " AND (CreatedBy = @me OR LeadId IN (SELECT Id FROM CrmLeads WHERE AssignedTo = @me))" : "";
            var a = new Dictionary<string, object> { { "me", Me.Id }, { "since", DateTime.UtcNow.AddDays(-90) } };
            Open = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE Status IN ('Draft','Sent','Viewed')" + scope, a);
            OpenValue = Db.Scalar<decimal?>("SELECT SUM(Total) FROM Proposals WHERE Status IN ('Sent','Viewed') AND Currency = 'INR'" + scope, a) ?? 0;
            Sent90 = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE SentOn >= @since" + scope, a);
            Accepted90 = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE AcceptedOn >= @since" + scope, a);
            Paid90 = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE PaidOn >= @since" + scope, a);
            Viewed = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE Status = 'Viewed'" + scope, a);
        }

        public static string StatusBadgeFor(Proposal p)
        {
            var s = p.IsExpired && p.Status != "Paid" ? "Expired" : p.Status;
            var css = s == "Accepted" || s == "Paid" ? "won" : s == "Declined" || s == "Expired" ? "lost" : s == "Viewed" ? "hot" : s == "Draft" ? "open" : "new";
            return "<span class=\"badge badge--" + css + "\">" + H(s) + "</span>" + (p.PayStatus == "paid" && s != "Paid" ? " <span class=\"badge badge--won\">Paid</span>" : "");
        }
    }
}
