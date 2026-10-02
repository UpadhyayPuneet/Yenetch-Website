using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/security: two-step sign-in policy, sign-in alerts, team sign-in status and backups. Admins only.</summary>
    public partial class SecurityPage : AdminPage
    {
        public override string Section { get { return "security"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected List<Row> People;
        protected List<BackupFile> Files;
        protected string Err;
        protected bool HasCaptchaSecret;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            People = Db.Rows("SELECT Id, Name, Email, Role, IsActive, TwoFactor, TwoFactorSecret, LastLoginOn, LastLoginIp FROM CrmUsers ORDER BY IsActive DESC, Name");
            Files = Backups.List();
            HasCaptchaSecret = !string.IsNullOrEmpty(Yenetch.Data.Settings.Get("captcha.secret"));
            if (Backups.Running) ((AdminMaster)Master).RefreshAttr = " data-refresh=\"5\"";
        }

        private void Handle()
        {
            var f = Request.Form;
            int reset;
            if (int.TryParse(f["reset"], out reset))
            {
                TwoFactor.Disable(reset);
                RedirectWith("/admin/security", "Two-step sign-in was reset. They can set it up again in My account.");
                return;
            }
            if (!string.IsNullOrEmpty(f["del"])) { Backups.Delete(f["del"]); RedirectWith("/admin/security", "Backup deleted."); return; }
            switch (f["act"])
            {
                case "captcha":
                    var provider = f["capProvider"] == "turnstile" || f["capProvider"] == "recaptcha" ? f["capProvider"] : "";
                    var site = (f["capSite"] ?? "").Trim();
                    if (provider != "" && site.Length < 10) { Err = "Paste the site key from your CAPTCHA provider."; return; }
                    if (provider != "" && string.IsNullOrWhiteSpace(f["capSecret"]) && string.IsNullOrEmpty(Yenetch.Data.Settings.Get("captcha.secret"))) { Err = "Paste the secret key too."; return; }
                    Yenetch.Data.Settings.Set("captcha.provider", provider);
                    Yenetch.Data.Settings.Set("captcha.siteKey", site.Length > 0 ? site : null);
                    if (!string.IsNullOrWhiteSpace(f["capSecret"])) Yenetch.Data.Settings.SetSecret("captcha.secret", f["capSecret"].Trim());
                    RedirectWith("/admin/security#captcha", provider == "" ? "Spam protection saved (CAPTCHA off)." : "CAPTCHA switched on. Test a form on the website to check it.");
                    return;
                case "security":
                    TwoFactor.RequiredForAll = f["require2fa"] == "1";
                    TwoFactor.Alerts = f["alerts"] == "1";
                    TwoFactor.AllowTrust = f["trust"] == "1";
                    if (!TwoFactor.AllowTrust) foreach (var r in Db.Rows("SELECT Id FROM CrmUsers")) TwoFactor.ForgetTrust(r.Int("Id"));
                    RedirectWith("/admin/security", "Security settings saved.");
                    return;
                case "backup":
                    int hour, keep;
                    int.TryParse(f["b_hour"], out hour);
                    if (!int.TryParse(f["b_keep"], out keep)) keep = 14;
                    Backups.Save(f["b_on"] == "1", hour, keep, f["b_files"] == "1");
                    RedirectWith("/admin/security", "Backup settings saved.");
                    return;
                case "now":
                    string error;
                    var name = Backups.Run(out error);
                    if (name == null) { Err = "The backup did not finish: " + error; return; }
                    RedirectWith("/admin/security", "Backup ready: " + name + ".");
                    return;
            }
        }

        protected static string Badge(Row u)
        {
            var m = u.Str("TwoFactor");
            if (m == "app" && !string.IsNullOrEmpty(u.Str("TwoFactorSecret"))) return "<span class=\"badge badge--won\">App</span>";
            if (m == "email") return "<span class=\"badge badge--won\">Email codes</span>";
            return TwoFactor.RequiredForAll ? "<span class=\"badge badge--hot\">Email codes (required)</span>" : "<span class=\"badge badge--open\">Off</span>";
        }
    }
}
