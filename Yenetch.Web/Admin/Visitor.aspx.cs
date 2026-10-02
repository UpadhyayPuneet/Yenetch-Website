using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/visitors/{id}. One visitor's profile and every visit with its pages, time on page, scroll depth and actions.</summary>
    public partial class VisitorPage : AdminPage
    {
        public override string Section { get { return "visitors"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected Stats.VisitorRow V;
        protected List<Stats.SessionView> Sessions;
        protected Stats.SessionView Latest, First;
        protected double TotalSeconds;
        protected List<Tally> TopPages;

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "cta_click", "Clicked" }, { "call", "Tapped to call" }, { "whatsapp", "Opened WhatsApp" }, { "email", "Clicked email" }, { "outbound", "Left for" },
            { "chat_open", "Opened the chatbot" }, { "finder_done", "Finished the solution finder" }, { "lead", "Sent an enquiry" }, { "newsletter", "Subscribed to the newsletter" }
        };

        protected static string EventText(string name) { string n; return name != null && Names.TryGetValue(name, out n) ? n : name; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = Convert.ToString(RouteData.Values["id"] ?? Request.QueryString["id"] ?? "");
            V = id.Length == 32 ? Stats.Visitor(id) : null;
            if (V == null) { RedirectWith("/admin/visitors", "That visitor was not found. Old analytics are deleted after the retention period."); return; }
            Title = V.LeadName ?? "Visitor " + V.Id.Substring(0, 6);
            Sessions = Stats.Journey(V.Id, 50);
            Latest = Sessions.FirstOrDefault();
            First = Sessions.LastOrDefault();
            TotalSeconds = Sessions.Sum(s => (s.LastOn - s.StartedOn).TotalSeconds);
            var pages = Sessions.SelectMany(s => s.Steps).Where(s => s.Kind == "page").GroupBy(s => s.Path)
                                .Select(g => { var r = new Row(); r["Label"] = g.Key; r["Value"] = g.Count(); return r; })
                                .OrderByDescending(r => r.Dbl("Value")).Take(8).ToList();
            TopPages = Tally.Rank(pages);
        }
    }
}
