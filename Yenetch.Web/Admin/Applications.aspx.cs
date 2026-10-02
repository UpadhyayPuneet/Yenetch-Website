using System;
using System.Collections.Generic;
using System.Globalization;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/applications. Job applications from the Careers page: search, filter by status, team, role and experience, sort, quick shortlist.</summary>
    public partial class ApplicationsPage : AdminPage
    {
        public override string Section { get { return "applications"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }
        protected const int PageSize = 50;

        protected List<JobApplication> Rows;
        protected List<string> Categories, Jobs;
        protected Dictionary<string, int> Counts;
        protected int Total, AllCount;
        protected bool Filtered;
        protected string Sort { get { return Q("sort") == "" ? "new" : Q("sort"); } }
        protected static readonly Dictionary<string, string> Sorts = new Dictionary<string, string>
        {
            { "new", "Newest first" }, { "oldest", "Oldest first" }, { "exp", "Most experience" }, { "exp-asc", "Least experience" }, { "rating", "Highest rated" }, { "name", "Name A to Z" }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) Act(); // the page still loads its data: the redirect may be applied after rendering
            decimal minExp;
            decimal.TryParse(Q("exp"), NumberStyles.Number, CultureInfo.InvariantCulture, out minExp);
            Rows = Applications.List(Q("q"), Q("status"), Q("category"), Q("job"), minExp, Sort, (PageNo - 1) * PageSize, PageSize, out Total);
            Counts = Applications.StatusCounts();
            AllCount = 0; foreach (var v in Counts.Values) AllCount += v;
            Categories = Applications.Distinct("Category");
            Jobs = Applications.Distinct("Job");
            Filtered = Q("q") != "" || Q("category") != "" || Q("job") != "" || Q("exp") != "";
        }

        private bool Act()
        {
            var v = Request.Form["status"] ?? "";
            var i = v.IndexOf(':');
            int id;
            if (i > 0 && int.TryParse(v.Substring(i + 1), out id))
            {
                Applications.SetStatus(id, v.Substring(0, i));
                RedirectWith(Request.RawUrl, "Marked as " + v.Substring(0, i).ToLowerInvariant() + ".");
                return true;
            }
            return false;
        }

        protected int Count(string status) { int n; return Counts != null && Counts.TryGetValue(status, out n) ? n : 0; }

        public static string Css(string status)
        {
            switch (status)
            {
                case "New": return "new";
                case "Shortlisted": case "Interview": case "Offered": return "hot";
                case "Hired": return "won";
                case "Rejected": return "lost";
                default: return "open";
            }
        }
    }
}
