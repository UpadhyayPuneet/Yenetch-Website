using System;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/email. Mail server, sender and alert addresses (CmsSettings), and a test send. Admins only.</summary>
    public partial class EmailPage : AdminPage
    {
        public override string Section { get { return "email"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected bool Configured, HasPassword;

        protected void Page_Load(object sender, EventArgs e)
        {
            var cfg = MailConfig.Load();
            Configured = cfg.HasServer;
            HasPassword = !string.IsNullOrEmpty(cfg.Password);
            if (IsPostBack)
            {
                if (Request.Form["digestTest"] == "1" && Save())
                {
                    var err = DailyDigest.SendTest(Me.Email, null);
                    if (err != null) Fail("The summary could not be sent: " + err);
                    else RedirectWith("/admin/email#daily", "Settings saved. Today's summary was sent to " + Me.Email + ".");
                }
                return;
            }
            Host.Text = cfg.Host; Port.Text = cfg.HasServer ? cfg.Port.ToString() : "587"; Ssl.SelectedValue = cfg.Ssl ? "1" : "0"; SmtpUser.Text = cfg.User;
            FromAddr.Text = string.IsNullOrWhiteSpace(cfg.From) ? "hello@yenetch.com" : cfg.From; FromName.Text = cfg.FromName;
            LeadsTo.Text = cfg.LeadsTo; CareersTo.Text = Settings.Get("mail.careersTo"); ReplyTo.Text = cfg.ReplyTo; AutoReply.Checked = cfg.AutoReply; TestTo.Text = Me.Email;
            PerMinute.Text = NewsSender.PerMinute.ToString(); PerHour.Text = NewsSender.PerHour.ToString();
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            if (Save()) RedirectWith("/admin/email", "Email settings saved.");
        }

        protected void TestButton_Click(object sender, EventArgs e)
        {
            if (!Save()) return;
            if (!Newsletter.IsEmail(TestTo.Text)) { Fail("Enter the address to send the test to."); return; }
            var cfg = MailConfig.Load();
            Configured = cfg.HasServer; HasPassword = !string.IsNullOrEmpty(cfg.Password);
            var error = Mailer.SendTest(TestTo.Text.Trim());
            if (error != null) { Fail("The test could not be sent. The mail server said: " + error); return; }
            OkText.Text = Server.HtmlEncode(cfg.HasServer ? "Test sent to " + TestTo.Text.Trim() + ". Check the inbox (and the spam folder the first time)." : "Saved to App_Data/mail because no mail server is set yet.");
            OkBox.Visible = true;
        }

        private bool Save()
        {
            int port;
            if (Host.Text.Trim() != "" && (!int.TryParse(Port.Text.Trim(), out port) || port < 1 || port > 65535)) { Fail("Enter a port number, usually 587."); return false; }
            foreach (var addr in new[] { FromAddr.Text, ReplyTo.Text })
                if (addr.Trim() != "" && !Newsletter.IsEmail(addr)) { Fail("Check the email address \"" + addr.Trim() + "\"."); return false; }
            foreach (var addr in LeadsTo.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                if (!Newsletter.IsEmail(addr)) { Fail("Check the lead alert address \"" + addr.Trim() + "\"."); return false; }
            foreach (var addr in CareersTo.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                if (!Newsletter.IsEmail(addr)) { Fail("Check the job application address \"" + addr.Trim() + "\"."); return false; }
            int perMinute, perHour;
            if (!int.TryParse(PerMinute.Text.Trim(), out perMinute) || perMinute < 1 || perMinute > 600) { Fail("Emails a minute must be between 1 and 600."); return false; }
            if (!int.TryParse(PerHour.Text.Trim(), out perHour) || perHour < 10 || perHour > 100000) { Fail("Emails an hour must be between 10 and 100,000."); return false; }
            Settings.Set("mail.host", Host.Text.Trim());
            Settings.Set("mail.port", Port.Text.Trim());
            Settings.Set("mail.ssl", Ssl.SelectedValue);
            Settings.Set("mail.user", SmtpUser.Text.Trim());
            if (Password.Text != "") Settings.SetSecret("mail.password", Password.Text);
            Settings.Set("mail.from", FromAddr.Text.Trim());
            Settings.Set("mail.fromName", FromName.Text.Trim());
            Settings.Set("mail.leadsTo", LeadsTo.Text.Trim());
            Settings.Set("mail.careersTo", CareersTo.Text.Trim());
            Settings.Set("mail.replyTo", ReplyTo.Text.Trim());
            Settings.Set("mail.autoReply", AutoReply.Checked ? "1" : "0");
            Settings.Set("news.perMinute", perMinute.ToString());
            Settings.Set("news.perHour", perHour.ToString());
            int dgHour;
            if (!int.TryParse(Request.Form["dg_hour"], out dgHour)) dgHour = 9;
            var extra = Request.Form["dg_extra"] ?? "";
            foreach (var addr in extra.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (!Newsletter.IsEmail(addr)) { Fail("Check the daily summary address \"" + addr.Trim() + "\"."); return false; }
            DailyDigest.Save(Request.Form["dg_on"] == "1", dgHour, Request.Form["dg_sales"] == "1", extra);
            return true;
        }

        private void Fail(string message) { ErrorText.Text = Server.HtmlEncode(message); ErrorBox.Visible = true; }
    }
}
