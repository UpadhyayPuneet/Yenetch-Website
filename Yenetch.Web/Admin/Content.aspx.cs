using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/content. Every editable section of the website, grouped.</summary>
    public partial class ContentPage : AdminPage
    {
        public override string Section { get { return "content"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected List<IGrouping<string, ContentCollection>> Groups;
        private Dictionary<string, int> _counts;

        protected void Page_Load(object sender, EventArgs e)
        {
            ContentStore.EnsureSeeded();
            _counts = ContentStore.Counts();
            Groups = ContentSchema.All.GroupBy(c => c.Group).ToList();
        }

        protected int Count(string key) { int n; return _counts.TryGetValue(key, out n) ? n : 0; }
    }
}
