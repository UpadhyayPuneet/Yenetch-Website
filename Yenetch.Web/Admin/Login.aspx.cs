using System;
using System.Web;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/login. Also signs out with ?signout=1. Sends first-time installs to /admin/setup.</summary>
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            Db.EnsureSchema();
            if (Request.QueryString["signout"] == "1") { Auth.SignOut(); Response.Redirect("~/admin/login", false); return; }
            if (!Auth.AnyUsers()) { Response.Redirect("~/admin/setup", false); return; }
            if (!IsPostBack && Auth.Current != null) Response.Redirect(Target, false);
            if (!IsPostBack) Email.Focus();
        }

        protected void SignInButton_Click(object sender, EventArgs e)
        {
            bool needsCode;
            var error = Auth.SignIn(Email.Text, Password.Text, Remember.Checked, out needsCode);
            if (error != null) { ErrorText.Text = Server.HtmlEncode(error); ErrorBox.Visible = true; Password.Focus(); return; }
            if (needsCode) { Response.Redirect("~/admin/verify?ReturnUrl=" + HttpUtility.UrlEncode(Target.TrimStart('~')), false); return; }
            Response.Redirect(Target, false);
        }

        /// <summary>Only returns to pages inside the admin area.</summary>
        private string Target
        {
            get
            {
                var r = Request.QueryString["ReturnUrl"] ?? "";
                return r.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) && !r.StartsWith("//") && !r.Contains("\\") && !r.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase) ? r : "~/admin";
            }
        }
    }
}
