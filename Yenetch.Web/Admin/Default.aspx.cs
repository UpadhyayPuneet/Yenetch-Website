using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin. KPIs, the traffic chart, newest leads, the signed-in user's follow-ups and top channels and pages (last 30 days).</summary>
    public partial class Dashboard : AdminPage
    {
        public override string Section { get { return "dashboard"; } }

        protected Overview O;
        protected string ChartJson;
        protected List<Lead> Newest;
        protected List<Activity> Tasks;
        protected List<Tally> Channels, TopPages;
        protected int OpenLeads, Unassigned, OverdueCount, DueToday;

        protected string Greeting
        {
            get { var h = Util.Ist(DateTime.UtcNow).Hour; return h < 12 ? "Good morning" : h < 17 ? "Good afternoon" : "Good evening"; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int total;
            var mine = Me.SeesAllLeads ? (int?)null : Me.Id;
            LeadService.List(new LeadFilter { OpenOnly = true, AssignedTo = mine }, 0, 1, out OpenLeads);
            LeadService.List(new LeadFilter { OpenOnly = true, Unassigned = true }, 0, 1, out Unassigned);
            Newest = LeadService.List(new LeadFilter { RestrictToUser = mine }, 0, 8, out total);

            var due = LeadService.FollowUps(Me.Id, Util.FromIst(Util.TodayIst.AddDays(1)), 50);
            OverdueCount = LeadService.FollowUps(mine, DateTime.UtcNow, 500).Count;
            DueToday = due.Count(t => t.DueOn >= DateTime.UtcNow);
            Tasks = due.Take(8).ToList();

            if (Me.CanUseMarketing)
            {
                O = Stats.Overview(From, To);
                var days = Stats.Daily(From, To);
                ChartJson = Json(new
                {
                    type = "area",
                    labels = days.Select(d => d.Day),
                    series = new[]
                    {
                        new { name = "Visitors", values = days.Select(d => d.Visitors) },
                        new { name = "Pageviews", values = days.Select(d => d.Pageviews) },
                        new { name = "Leads", values = days.Select(d => d.Leads) }
                    }
                });
                Channels = Stats.By("Channel", From, To, 6);
                TopPages = Stats.Pages(From, To, 6);
            }
        }
    }
}
