using System;
using System.Collections.Generic;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/robots. Rule builder for robots.txt (see Yenetch.Data.Robots). Admins only.</summary>
    public partial class RobotsPage : AdminPage
    {
        public override string Section { get { return "robots"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected RobotsSettings R;
        protected string Err, Preview;

        protected void Page_Load(object sender, EventArgs e)
        {
            R = Robots.Load();
            if (IsPostBack && Request.Form["reset"] == "1") { Robots.Save(Robots.Default()); RedirectWith("/admin/robots", "Recommended rules restored."); return; }
            if (IsPostBack && Request.Form["save"] == "1")
            {
                var f = Request.Unvalidated.Form;
                Func<string, string> v = k => (f[k] ?? "").Replace("\r", "").Trim();
                var r = new RobotsSettings
                {
                    Mode = v("mode") == "custom" ? "custom" : "rules", HideTestSites = v("test") == "1", BlockAi = v("ai") == "1", Sitemap = v("sitemap") == "1",
                    BlockAll = v("all") == "1", ExtraSitemaps = Util.Cut(v("extra"), 2000), Custom = Util.Cut(v("custom"), 20000), Groups = new List<RobotsGroup>()
                };
                int count; int.TryParse(f["count"], out count);
                for (var i = 0; i < Math.Min(count, 50); i++)
                {
                    var p = "g" + i + "_";
                    if (v(p + "del") == "1") continue;
                    var g = new RobotsGroup { Agents = Util.Cut(v(p + "agents"), 1000), Allow = Util.Cut(v(p + "allow"), 4000), Disallow = Util.Cut(v(p + "disallow"), 4000) };
                    int d; if (int.TryParse(v(p + "delay"), out d) && d > 0 && d <= 120) g.CrawlDelay = d;
                    if (g.Agents == "" && g.Allow == "" && g.Disallow == "") continue;
                    r.Groups.Add(g);
                }
                if (r.Mode == "custom" && r.Custom == "") Err = "Write your robots.txt, or switch back to the rule builder.";
                else Err = Robots.Problem(r);
                if (Err != null) R = r;
                else { Robots.Save(r); RedirectWith("/admin/robots", "robots.txt saved."); return; }
            }
            Preview = Robots.Render(R, false);
        }
    }
}
