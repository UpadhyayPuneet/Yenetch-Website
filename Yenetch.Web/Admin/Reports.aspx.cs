using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/reports. Lead volume, pipeline, sources, types, channels, requested services and per-owner performance.</summary>
    public partial class Reports : AdminPage
    {
        public override string Section { get { return "reports"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected double Total, PrevTotal, Won, Lost, Open;
        protected decimal WonValue, OpenValue;
        protected string FirstResponse, MonthlyJson;
        protected List<Tally> ByStatus, BySource, ByType, ByChannel, ByInterest;
        protected List<Stats.OwnerRow> Owners;

        protected void Page_Load(object sender, EventArgs e)
        {
            ByStatus = Stats.LeadsBy("Status", From, To);
            ByStatus = ByStatus.OrderBy(t => Array.IndexOf(Lists.Statuses, t.Label)).ToList();
            BySource = Stats.LeadsBy("Source", From, To);
            ByType = Stats.LeadsBy("LeadType", From, To);
            ByChannel = Stats.LeadsBy("Channel", From, To);
            ByInterest = Stats.LeadsBy("Interest", From, To).Where(t => t.Label != "(not set)").Take(8).ToList();
            Owners = Stats.ByOwner(From, To);

            Total = ByStatus.Sum(t => t.Value);
            Won = ByStatus.Where(t => t.Label == "Won").Sum(t => t.Value);
            Lost = ByStatus.Where(t => t.Label == "Lost").Sum(t => t.Value);
            Open = ByStatus.Where(t => Lists.OpenStatuses.Contains(t.Label)).Sum(t => t.Value);
            WonValue = (decimal)ByStatus.Where(t => t.Label == "Won").Sum(t => t.Extra2);
            OpenValue = Db.Scalar<decimal>("SELECT COALESCE(SUM(EstValue), 0) FROM CrmLeads WHERE CreatedOn >= @from AND CreatedOn < @to AND Status IN ('" + string.Join("','", Lists.OpenStatuses) + "')", new { from = From, to = To });
            PrevTotal = Db.Scalar<double>("SELECT COUNT(*) FROM CrmLeads WHERE CreatedOn >= @from AND CreatedOn < @to", new { from = From - (To - From), to = From });

            var h = Stats.MedianFirstResponseHours(From, To);
            FirstResponse = !h.HasValue ? "—" : h.Value < 1 ? Math.Max(1, Math.Round(h.Value * 60)) + " min" : h.Value < 48 ? Math.Round(h.Value, 1) + " h" : Math.Round(h.Value / 24, 1) + " days";

            var months = Stats.LeadsByMonth(12);
            MonthlyJson = Json(new { type = "bar", labels = months.Select(m => m.Label), series = new[] { new { name = "Leads", values = months.Select(m => m.Value) } } });
        }
    }
}
