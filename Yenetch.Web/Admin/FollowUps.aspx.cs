using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/follow-ups. Open follow-up tasks grouped as overdue, today and upcoming; mark done or move by a day.</summary>
    public partial class FollowUps : AdminPage
    {
        public override string Section { get { return "followups"; } }

        protected List<Activity> Tasks;
        protected List<Tuple<string, string, int>> Tabs;
        protected string Who { get { return Me.SeesAllLeads && Q("who") == "all" ? "all" : "me"; } }
        protected string Tab { get { var t = Q("tab"); return t == "" ? "overdue" : t; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Act(); return; }
            int? user = Who == "all" ? (int?)null : Me.Id;
            var all = LeadService.FollowUps(user, null, 1000);
            var now = DateTime.UtcNow;
            var endToday = Util.FromIst(Util.TodayIst.AddDays(1));
            var overdue = all.Where(a => a.DueOn < now).ToList();
            var today = all.Where(a => a.DueOn >= now && a.DueOn < endToday).ToList();
            var upcoming = all.Where(a => a.DueOn >= endToday).ToList();
            Tabs = new List<Tuple<string, string, int>>
            {
                Tuple.Create("overdue", "Overdue", overdue.Count), Tuple.Create("today", "Later today", today.Count), Tuple.Create("upcoming", "Upcoming", upcoming.Count)
            };
            Tasks = Tab == "today" ? today : Tab == "upcoming" ? upcoming : overdue;
        }

        private void Act()
        {
            int id;
            if (int.TryParse(Request.Form["done"], out id) && Allowed(id)) { LeadService.CompleteFollowUp(id, Me.Id); RedirectWith(Request.RawUrl, "Marked done."); return; }
            if (int.TryParse(Request.Form["snooze"], out id) && Allowed(id)) { LeadService.Snooze(id, 1); RedirectWith(Request.RawUrl, "Moved to tomorrow."); return; }
            RedirectWith(Request.RawUrl, null);
        }

        private bool Allowed(int activityId)
        {
            if (Me.SeesAllLeads) return true;
            var owner = Db.First("SELECT l.AssignedTo FROM CrmActivities a JOIN CrmLeads l ON l.Id = a.LeadId WHERE a.Id = @id", new { id = activityId });
            return owner != null && (owner.IntN("AssignedTo") == null || owner.IntN("AssignedTo") == Me.Id);
        }
    }
}
