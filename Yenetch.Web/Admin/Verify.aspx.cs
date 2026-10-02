using System;
using System.Web;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/verify: the second step after the password (authenticator code, backup code or email code). See Yenetch.Crm.TwoFactor.</summary>
    public partial class VerifyPage : Page
    {
        protected string MethodName, MaskedEmail, Err, Info;
        protected bool AllowTrust;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Db.EnsureSchema();
            var p = TwoFactor.Current();
            if (p == null) { Response.Redirect("~/admin/login", false); Context.ApplicationInstance.CompleteRequest(); return; }
            var r = TwoFactor.UserRow(p.UserId);
            if (r == null) { TwoFactor.Clear(); Response.Redirect("~/admin/login", false); Context.ApplicationInstance.CompleteRequest(); return; }
            MethodName = TwoFactor.Method(r) ?? "email";
            MaskedEmail = Mask(r.Str("Email"));
            AllowTrust = TwoFactor.AllowTrust;

            if (Request.HttpMethod != "POST") return;
            if (Request.Form["act"] == "email")
            {
                Err = TwoFactor.SendEmailCode(r);
                if (Err == null) { Info = "We emailed a code to " + MaskedEmail + "."; MethodName = "email-sent"; }
                return;
            }
            bool usedBackup;
            Err = TwoFactor.Verify(p, Request.Form["code"], Request.Form["trust"] == "1", out usedBackup);
            if (Err != null)
            {
                if (TwoFactor.Current() == null && Err.StartsWith("Too many")) { Response.Redirect("~/admin/login", false); Context.ApplicationInstance.CompleteRequest(); }
                return;
            }
            if (usedBackup) Response.Cookies.Add(new HttpCookie("yn_flash", HttpUtility.UrlEncode("You used a backup code. " + TwoFactor.BackupCodesLeft(TwoFactor.UserRow(p.UserId)) + " are left. Create new ones in My account if you are running low.")) { HttpOnly = true, Path = "/admin" });
            Response.Redirect(Target, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (Response.IsRequestBeingRedirected) return;
            base.Render(writer);
        }

        private static string Mask(string email)
        {
            var at = (email ?? "").IndexOf('@');
            if (at < 1) return email;
            return email.Substring(0, Math.Min(2, at)) + new string('•', Math.Max(1, at - 2)) + email.Substring(at);
        }

        private string Target
        {
            get
            {
                var r = Request.QueryString["ReturnUrl"] ?? "";
                return r.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) && !r.StartsWith("//") && !r.Contains("\\") && !r.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase) && !r.StartsWith("/admin/verify", StringComparison.OrdinalIgnoreCase) ? r : "~/admin";
            }
        }
    }
}
