using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/audits: website audits run by visitors. Admins and managers.</summary>
    public partial class AuditsPage : AdminPage
    {
        public override string Section { get { return "audits"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected const int PageSize = 30;
        protected List<Row> Rows;
        protected int Total, Last30, Weak30;
        protected string Avg30;

        protected void Page_Load(object sender, EventArgs e)
        {
            Rows = Audits.List(Q("q"), (PageNo - 1) * PageSize, PageSize, out Total);
            var since = new { since = DateTime.UtcNow.AddDays(-30) };
            Last30 = Db.Scalar<int>("SELECT COUNT(*) FROM WebAudits WHERE CreatedOn >= @since", since);
            Weak30 = Db.Scalar<int>("SELECT COUNT(*) FROM WebAudits WHERE CreatedOn >= @since AND Score < 60", since);
            var avg = Db.Scalar<double?>("SELECT AVG(Score * 1.0) FROM WebAudits WHERE CreatedOn >= @since", since);
            Avg30 = avg.HasValue ? Math.Round(avg.Value).ToString() : "–";
        }

        public static string Host(string url) { Uri u; return Uri.TryCreate(url ?? "", UriKind.Absolute, out u) ? u.Host.Replace("www.", "") : url; }
        public static string Grade(int score) { return score >= 85 ? "good" : score >= 60 ? "ok" : "bad"; }
    }
}
