using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/automations: the automatic email series. Admins and managers.</summary>
    public partial class AutomationListPage : AdminPage
    {
        public override string Section { get { return "automations"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }
        protected List<AutoSeries> Series;

        protected void Page_Load(object sender, EventArgs e) { Series = Automations.All(); }

        public static string Delay(int hours)
        {
            if (hours <= 0) return "Straight away";
            if (hours % 24 == 0) return "After " + hours / 24 + (hours == 24 ? " day" : " days");
            return "After " + hours + (hours == 1 ? " hour" : " hours");
        }
    }
}
