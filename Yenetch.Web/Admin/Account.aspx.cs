using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Security;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/account. Any signed-in user can change their name and password, and set up two-step sign-in.</summary>
    public partial class Account : AdminPage
    {
        public override string Section { get { return "account"; } }

        protected string TfMethod, TfError, SetupSecret, SetupUri, ProtectedSecret;
        protected List<string> NewCodes;
        protected int BackupLeft;
        protected List<KnownDevice> DeviceList;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) MyName.Text = Me.Name;
            Load2fa();
            var act = Request.Form["tf"];
            if (IsPostBack && !string.IsNullOrEmpty(act)) TwoStep(act);
        }

        private void Load2fa()
        {
            var r = TwoFactor.UserRow(Me.Id);
            TfMethod = r.Str("TwoFactor") == "app" && !string.IsNullOrEmpty(r.Str("TwoFactorSecret")) ? "app" : r.Str("TwoFactor") == "email" ? "email" : null;
            BackupLeft = TwoFactor.BackupCodesLeft(r);
            DeviceList = TwoFactor.Devices(r);
        }

        private void TwoStep(string act)
        {
            switch (act)
            {
                case "start":
                    ShowSetup(TwoFactor.NewSecret());
                    return;
                case "confirm":
                    string secret = null;
                    try { secret = Encoding.UTF8.GetString(MachineKey.Unprotect(Convert.FromBase64String(Request.Form["tf_secret"] ?? ""), "totp-setup", Me.Id.ToString())); } catch { }
                    if (secret == null) { TfError = "The setup expired. Please start again."; return; }
                    if (!TwoFactor.CheckTotp(secret, Request.Form["tf_code"], Me.Id)) { ShowSetup(secret); TfError = "That code is not right. Check the time on your phone is set automatically, then try the newest code."; return; }
                    NewCodes = TwoFactor.EnableApp(Me.Id, secret);
                    Load2fa();
                    Flash("Two-step sign-in is on.");
                    return;
                case "email":
                    if (TfMethod != null) return;
                    TwoFactor.EnableEmail(Me.Id);
                    RedirectWith("/admin/account#two-step", "Two-step sign-in is on. We will email you a code each time you sign in.");
                    return;
                case "forget":
                    TwoFactor.ForgetTrust(Me.Id);
                    RedirectWith("/admin/account#two-step", "Every browser will ask for a code at the next sign-in.");
                    return;
            }
            if (!Auth.CheckPassword(Me.Id, Request.Form["tf_pw"] ?? "")) { TfError = "Enter your current password to make this change."; return; }
            if (act == "codes" && TfMethod == "app") { NewCodes = TwoFactor.RegenerateBackupCodes(Me.Id); Load2fa(); return; }
            if (act == "off")
            {
                if (TwoFactor.RequiredForAll) { TwoFactor.EnableEmail(Me.Id); RedirectWith("/admin/account#two-step", "Your admin requires two-step sign-in, so you now get email codes."); }
                else { TwoFactor.Disable(Me.Id); RedirectWith("/admin/account#two-step", "Two-step sign-in is off."); }
            }
        }

        private void ShowSetup(string secret)
        {
            SetupSecret = secret;
            SetupUri = TwoFactor.SetupUri(secret, Me.Email);
            ProtectedSecret = Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(secret), "totp-setup", Me.Id.ToString()));
        }

        protected static string Grouped(string s)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < s.Length; i++) { if (i > 0 && i % 4 == 0) sb.Append(' '); sb.Append(s[i]); }
            return sb.ToString();
        }

        protected void NameButton_Click(object sender, EventArgs e)
        {
            var name = MyName.Text.Trim();
            if (name.Length == 0) { RedirectWith(Request.RawUrl, "Enter a name."); return; }
            Auth.UpdateUser(Me.Id, name, Me.Role, true);
            RedirectWith(Request.RawUrl, "Name saved.");
        }

        protected void PasswordButton_Click(object sender, EventArgs e)
        {
            if (!Auth.CheckPassword(Me.Id, CurrentPassword.Text)) { RedirectWith(Request.RawUrl, "Your current password is not right."); return; }
            if (NewPassword.Text != ConfirmPassword.Text) { RedirectWith(Request.RawUrl, "The new passwords do not match."); return; }
            var problem = Auth.PasswordProblem(NewPassword.Text);
            if (problem != null) { RedirectWith(Request.RawUrl, problem); return; }
            Auth.SetPassword(Me.Id, NewPassword.Text);
            // A new cookie keeps this browser signed in after the change.
            Auth.IssueCookie(Auth.User(Me.Id), false);
            RedirectWith(Request.RawUrl, "Password changed.");
        }
    }
}
