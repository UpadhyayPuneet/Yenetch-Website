using System;
using System.Globalization;
using System.Linq;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/pricing: show or hide prices, tax line, currencies and rates, the audit popup and proposal defaults. Admins and managers.</summary>
    public partial class PricingAdminPage : AdminPage
    {
        public override string Section { get { return "pricing"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected string Err;
        protected Popups.AuditPopup Audit;
        protected int Quotes30;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            Audit = Popups.Audit;
            Quotes30 = Db.Scalar<int>("SELECT COUNT(*) FROM Quotes WHERE CreatedOn >= @since", new { since = DateTime.UtcNow.AddDays(-30) });
        }

        private void Handle()
        {
            var f = Request.Form;
            if (f["act"] == "rates")
            {
                var error = Fx.Refresh();
                RedirectWith("/admin/pricing", error ?? "Today's exchange rates downloaded.");
                return;
            }
            decimal tax;
            if (!decimal.TryParse(f["taxPct"], NumberStyles.Float, CultureInfo.InvariantCulture, out tax) || tax < 0 || tax > 50) { Err = "The tax rate must be a number from 0 to 50."; return; }
            foreach (var code in Fx.Known.Keys.Where(k => k != "INR"))
            {
                var v = (f["manual_" + code] ?? "").Trim();
                decimal m;
                if (v.Length > 0 && (!decimal.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out m) || m <= 0)) { Err = "The fixed rate for " + code + " must be a number, like 83.5."; return; }
            }
            int delay, scroll, days, offerDays, valid;
            if (!int.TryParse(f["popDelay"], out delay) || delay < 3 || delay > 600) { Err = "The popup delay must be between 3 and 600 seconds."; return; }
            if (!int.TryParse(f["popScroll"], out scroll) || scroll < 10 || scroll > 100) { Err = "The scroll point must be between 10 and 100%."; return; }
            if (!int.TryParse(f["popDays"], out days) || days < 1 || days > 365) { Err = "Show again after must be 1 to 365 days."; return; }
            if (!int.TryParse(f["offerDays"], out offerDays) || offerDays < 1 || offerDays > 365) { Err = "Offer popups: show again after must be 1 to 365 days."; return; }
            if (!int.TryParse(f["validDays"], out valid) || valid < 1 || valid > 365) { Err = "Proposals must be valid for 1 to 365 days."; return; }

            Settings.Set("pricing.show", f["show"] == "1" ? "1" : "0");
            Settings.Set("pricing.heading", Util.Cut((f["heading"] ?? "").Trim(), 120));
            Settings.Set("pricing.intro", Util.Cut((f["intro"] ?? "").Trim(), 400));
            Settings.Set("pricing.note", Util.Cut((f["note"] ?? "").Trim(), 400));
            Settings.Set("pricing.taxName", Util.Cut((f["taxName"] ?? "").Trim(), 20));
            Settings.Set("pricing.taxPct", tax.ToString(CultureInfo.InvariantCulture));
            var chosen = (f.GetValues("cur") ?? new string[0]).Where(Fx.Known.ContainsKey).ToList();
            chosen.Insert(0, "INR");
            Settings.Set("fx.currencies", string.Join(",", chosen.Distinct()));
            foreach (var code in Fx.Known.Keys.Where(k => k != "INR"))
            {
                var v = (f["manual_" + code] ?? "").Trim();
                if (v.Length > 0 || Settings.Get("fx.manual." + code) != null) Settings.Set("fx.manual." + code, v.Length > 0 ? v : null);
            }
            Settings.Set("fx.foreign", new[] { "USD", "EUR", "GBP" }.Contains(f["foreign"]) ? f["foreign"] : "USD");
            Settings.Set("fx.round", f["round"] == "1" ? "1" : "0");

            Settings.Set("popup.audit.enabled", f["pop"] == "1" ? "1" : "0");
            Settings.Set("popup.audit.trigger", new[] { "exit", "delay", "scroll" }.Contains(f["popTrigger"]) ? f["popTrigger"] : "exit");
            Settings.Set("popup.audit.delay", delay.ToString());
            Settings.Set("popup.audit.scroll", scroll.ToString());
            Settings.Set("popup.audit.days", days.ToString());
            Settings.Set("popup.audit.title", Util.Cut((f["popTitle"] ?? "").Trim(), 120));
            Settings.Set("popup.audit.text", Util.Cut((f["popText"] ?? "").Trim(), 300));
            Settings.Set("popup.audit.button", Util.Cut((f["popButton"] ?? "").Trim(), 60));
            Settings.Set("popup.audit.exclude", Util.Cut((f["popExclude"] ?? "").Replace("\r", ""), 2000));
            Settings.Set("popup.offer.days", offerDays.ToString());
            Settings.Set("proposal.validDays", valid.ToString());
            Settings.Set("proposal.terms", Util.Cut((f["terms"] ?? "").Replace("\r", "").Trim(), 6000));
            RedirectWith("/admin/pricing", "Saved." + (f["show"] == "1" ? " Prices are visible on the website." : ""));
        }
    }
}
