using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/subscribers. Newsletter list: search, add, import, unsubscribe, delete and export.</summary>
    public partial class Subscribers : AdminPage
    {
        public override string Section { get { return "subscribers"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }
        protected const int PageSize = 50;

        protected List<Subscriber> Rows;
        protected List<Tally> Sources;
        protected int Total, AllCount, ActiveCount, NewThisMonth;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack && Act()) return;
            Rows = Newsletter.List(Q("q"), Q("status"), (PageNo - 1) * PageSize, PageSize, out Total);
            AllCount = Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers");
            ActiveCount = Newsletter.ActiveCount();
            NewThisMonth = Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers WHERE CreatedOn >= @since", new { since = DateTime.UtcNow.AddDays(-30) });
            Sources = Tally.Rank(Db.Rows("SELECT COALESCE(Source, '(not set)') AS Label, COUNT(*) AS Value FROM NewsSubscribers WHERE Status = 'Active' GROUP BY COALESCE(Source, '(not set)') ORDER BY COUNT(*) DESC" + Db.Page(0, 8)));
        }

        private bool Act()
        {
            int id;
            if (int.TryParse(Request.Form["unsub"], out id)) { Newsletter.Unsubscribe(id); RedirectWith(Request.RawUrl, "Unsubscribed."); return true; }
            if (int.TryParse(Request.Form["resub"], out id)) { Newsletter.Resubscribe(id); RedirectWith(Request.RawUrl, "Subscribed again."); return true; }
            if (int.TryParse(Request.Form["del"], out id)) { Newsletter.Delete(id); RedirectWith(Request.RawUrl, "Subscriber deleted."); return true; }
            return false;
        }

        protected void AddButton_Click(object sender, EventArgs e)
        {
            if (!Newsletter.Subscribe(NewEmail.Text, NewName.Text, "Added by " + Me.Name, null)) { RedirectWith(Request.RawUrl, "That email address does not look right."); return; }
            RedirectWith(Request.RawUrl, NewEmail.Text.Trim() + " added.");
        }

        protected void ImportButton_Click(object sender, EventArgs e)
        {
            var r = Newsletter.Import(ImportText.Text, "Import");
            RedirectWith(Request.RawUrl, r.Item1 + " added" + (r.Item2 > 0 ? ", " + r.Item2 + " already on the list or invalid." : "."));
        }
    }
}
