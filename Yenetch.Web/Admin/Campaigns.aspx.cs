using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/campaigns. Newsletter drafts and sent campaigns.</summary>
    public partial class Campaigns : AdminPage
    {
        public override string Section { get { return "campaigns"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected List<Campaign> Rows;
        protected int Active;

        protected void Page_Load(object sender, EventArgs e)
        {
            Rows = Newsletter.Campaigns();
            Active = Newsletter.ActiveCount();
        }
    }
}
