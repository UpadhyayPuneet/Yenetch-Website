using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/tracking. Tag IDs, site verification and custom scripts (see Yenetch.Data.Tracking). Admins only.</summary>
    public partial class TrackingPage : AdminPage
    {
        public override string Section { get { return "tracking"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected TrackingSettings T;
        protected string Err;
        protected static readonly string[][] PlaceNames = { new[] { "head", "Inside head" }, new[] { "body-start", "Start of body" }, new[] { "body-end", "End of body" } };

        protected void Page_Load(object sender, EventArgs e)
        {
            T = Tracking.Load();
            if (!IsPostBack || Request.Form["save"] != "1") return;
            var f = Request.Unvalidated.Form;
            Func<string, string> v = k => (f[k] ?? "").Trim();
            var t = new TrackingSettings
            {
                Ga4 = v("ga4").ToUpperInvariant(), Gtm = v("gtm").ToUpperInvariant(), GoogleAds = v("ads").ToUpperInvariant(), MetaPixel = v("meta"),
                LinkedIn = v("li"), Clarity = v("clarity").ToLowerInvariant(), GoogleVerify = v("gv"), BingVerify = v("bv"), Scripts = new List<CustomScript>()
            };
            int count; int.TryParse(f["count"], out count);
            for (var i = 0; i < Math.Min(count, 100); i++)
            {
                var p = "s" + i + "_";
                if (v(p + "del") == "1") continue;
                var code = (f[p + "code"] ?? "").Trim();
                var name = v(p + "name");
                if (code == "" && name == "") continue;
                if (code == "") { Fail("Paste the code for \"" + name + "\".", t); return; }
                t.Scripts.Add(new CustomScript
                {
                    Id = Guid.NewGuid().ToString("N").Substring(0, 8), Name = name == "" ? "Script " + (t.Scripts.Count + 1) : Util.Cut(name, 80),
                    Placement = Tracking.Placements.Contains(v(p + "place")) ? v(p + "place") : "body-end",
                    Category = Tracking.Categories.Contains(v(p + "cat")) ? v(p + "cat") : "marketing",
                    Pages = Util.Cut(v(p + "pages"), 1000), Code = Util.Cut(code, 20000), Enabled = v(p + "on") == "1"
                });
            }
            var problem = Tracking.Problem(t);
            if (problem != null) { Fail(problem, t); return; }
            Tracking.Save(t);
            Tracking.Invalidate();
            RedirectWith("/admin/tracking", "Tracking saved. It shows on the website within a minute or two.");
        }

        private void Fail(string message, TrackingSettings typed) { Err = message; T = typed; }
    }
}
