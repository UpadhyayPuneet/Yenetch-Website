using System;
using System.Collections.Generic;
using System.Globalization;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/scoring: points per lead-scoring signal, Hot/Warm thresholds and automatic priority. Admins and managers.</summary>
    public partial class ScoringPage : AdminPage
    {
        public override string Section { get { return "scoring"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected string Err, Avg;
        protected Dictionary<string, int> W;
        protected int Hot, Warm, Cold;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            W = LeadScoring.Weights;
            const string open = "Status IN ('New','Contacted','Qualified','Proposal','Negotiation')";
            Hot = Db.Scalar<int>("SELECT COUNT(*) FROM CrmLeads WHERE " + open + " AND Score >= @h", new { h = LeadScoring.HotAt });
            Warm = Db.Scalar<int>("SELECT COUNT(*) FROM CrmLeads WHERE " + open + " AND Score >= @w AND Score < @h", new { w = LeadScoring.WarmAt, h = LeadScoring.HotAt });
            Cold = Db.Scalar<int>("SELECT COUNT(*) FROM CrmLeads WHERE " + open + " AND Score < @w", new { w = LeadScoring.WarmAt });
            var avg = Db.Scalar<double?>("SELECT AVG(CAST(Score AS FLOAT)) FROM CrmLeads WHERE " + open + " AND Score IS NOT NULL");
            Avg = avg.HasValue ? Math.Round(avg.Value).ToString(CultureInfo.InvariantCulture) : "–";
        }

        private void Handle()
        {
            var f = Request.Form;
            if (f["act"] == "recalc") { LeadScoring.RecalcOpen(); RedirectWith("/admin/scoring", "Open leads re-scored."); return; }
            if (f["act"] == "defaults") { Settings.Set("scoring.weights", null); LeadScoring.RecalcOpen(); RedirectWith("/admin/scoring", "Default points restored and leads re-scored."); return; }
            var w = new Dictionary<string, int>();
            foreach (var s in LeadScoring.Signals)
            {
                int v;
                if (!int.TryParse(f["w_" + s[0]], NumberStyles.Integer, CultureInfo.InvariantCulture, out v) || v < -50 || v > 100) { Err = "\"" + s[1] + "\" needs a whole number from -50 to 100."; return; }
                w[s[0]] = v;
            }
            int hot, warm;
            if (!int.TryParse(f["hot"], out hot) || !int.TryParse(f["warm"], out warm) || warm < 1 || hot <= warm || hot > 100) { Err = "Warm must be at least 1, and Hot above Warm and at most 100."; return; }
            LeadScoring.SaveWeights(w);
            Settings.Set("scoring.hot", hot.ToString());
            Settings.Set("scoring.warm", warm.ToString());
            Settings.Set("scoring.auto", f["auto"] == "1" ? "1" : "0");
            LeadScoring.RecalcOpen();
            RedirectWith("/admin/scoring", "Scoring saved and open leads re-scored.");
        }
    }
}
