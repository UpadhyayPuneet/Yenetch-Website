using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/setup. Creates the first Admin account. Disabled once any user exists; needs appSettings AdminSetupKey.</summary>
    public partial class Setup : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            Db.EnsureSchema();
            if (Auth.AnyUsers()) { Response.Redirect("~/admin/login", false); return; }
        }

        protected void CreateButton_Click(object sender, EventArgs e)
        {
            var key = ConfigurationManager.AppSettings["AdminSetupKey"] ?? "";
            if (key.Length < 12 || !Same(key, SetupKey.Text.Trim())) { Fail("The setup key does not match AdminSetupKey in Web.config (it must be at least 12 characters)."); return; }
            if (FullName.Text.Trim().Length < 2) { Fail("Enter your name."); return; }
            if (!Newsletter.IsEmail(Email.Text)) { Fail("Enter a valid email address."); return; }
            var problem = Auth.PasswordProblem(Password.Text);
            if (problem != null) { Fail(problem); return; }
            if (Auth.AnyUsers()) { Response.Redirect("~/admin/login", false); return; }
            var id = Auth.CreateUser(Email.Text, FullName.Text, "Admin", Password.Text);
            Auth.IssueCookie(Auth.User(id), false);
            Response.Redirect("~/admin", false);
        }

        private void Fail(string message) { ErrorText.Text = Server.HtmlEncode(message); ErrorBox.Visible = true; }

        private static bool Same(string a, string b)
        {
            using (var sha = SHA256.Create())
            {
                var x = sha.ComputeHash(Encoding.UTF8.GetBytes(a)); var y = sha.ComputeHash(Encoding.UTF8.GetBytes(b));
                var diff = 0; for (var i = 0; i < x.Length; i++) diff |= x[i] ^ y[i];
                return diff == 0;
            }
        }
    }
}
