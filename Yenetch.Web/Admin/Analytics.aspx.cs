using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/analytics. Traffic over time, acquisition, pages, geography, technology, actions and busy hours.</summary>
    public partial class AnalyticsPage : AdminPage
    {
        public override string Section { get { return "analytics"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected Overview O;
        protected string DailyJson, HoursJson;
        protected List<Tally> Channels, Sources, Campaigns, Pages, Landing, Exits, Countries, Cities, Languages, Devices, Browsers, Oses, EventRows, TopCtas;

        private static readonly Dictionary<string, string> EventNames = new Dictionary<string, string>
        {
            { "cta_click", "Clicked a call to action" }, { "call", "Tapped to call" }, { "whatsapp", "Opened WhatsApp" }, { "email", "Clicked an email address" },
            { "outbound", "Left for another site" }, { "chat_open", "Opened the chatbot" }, { "finder_done", "Finished the solution finder" },
            { "lead", "Sent an enquiry" }, { "newsletter", "Subscribed to the newsletter" }
        };

        protected static string EventName(Tally t) { string n; return EventNames.TryGetValue(t.Label, out n) ? n : t.Label; }

        protected void Page_Load(object sender, EventArgs e)
        {
            O = Stats.Overview(From, To);
            var days = Stats.Daily(From, To);
            DailyJson = Json(new
            {
                type = days.Count > 60 ? "bar" : "area",
                labels = days.Select(d => d.Day),
                series = new[]
                {
                    new { name = "Visitors", values = days.Select(d => d.Visitors) }, new { name = "Visits", values = days.Select(d => d.Sessions) },
                    new { name = "Pageviews", values = days.Select(d => d.Pageviews) }, new { name = "Leads", values = days.Select(d => d.Leads) }
                }
            });
            var hours = Stats.Hours(From, To);
            HoursJson = Json(new { type = "bar", labels = hours.Select(h => h.Label + ":00"), series = new[] { new { name = "Pageviews", values = hours.Select(h => h.Value) } } });

            Channels = Stats.By("Channel", From, To, 8);
            Sources = Stats.By("Source", From, To, 8);
            Campaigns = Stats.By("Campaign", From, To, 8);
            Pages = Stats.Pages(From, To, 15);
            Landing = Stats.By("Landing", From, To, 8);
            Exits = Stats.By("ExitPage", From, To, 8);
            Countries = Stats.By("Country", From, To, 8);
            Cities = Stats.By("City", From, To, 8);
            Languages = Stats.By("Lang", From, To, 6);
            Devices = Stats.By("Device", From, To, 4);
            Browsers = Stats.By("Browser", From, To, 6);
            Oses = Stats.By("Os", From, To, 6);
            EventRows = Stats.Events(From, To, 10);
            TopCtas = Stats.EventLabels("cta_click", From, To, 8);
        }
    }
}
