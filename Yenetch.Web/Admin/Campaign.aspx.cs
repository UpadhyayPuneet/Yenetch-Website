using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/campaigns/{id|new}. Write a newsletter with a live preview, send a test, then send to every active subscriber.</summary>
    public partial class CampaignPage : AdminPage
    {
        public override string Section { get { return "campaigns"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected Campaign C;
        protected bool IsNew, IsLocked;
        protected int Active;
        protected string Shell;
        protected Dictionary<string, int> Counts;
        protected List<Delivery> Problems = new List<Delivery>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var key = Convert.ToString(RouteData.Values["id"] ?? Request.QueryString["id"] ?? "new");
            IsNew = key == "new";
            int id;
            if (!IsNew)
            {
                C = int.TryParse(key, out id) ? Newsletter.Campaign(id) : null;
                if (C == null) { RedirectWith("/admin/campaigns", "Campaign not found."); return; }
                IsLocked = C.Status != "Draft";
                Title = C.Subject;
            }
            else Title = "New campaign";
            Active = Newsletter.ActiveCount();
            // The preview wraps the body in the same email layout the subscribers get.
            Shell = Mailer.Wrap("{{preheader}}", "{{content}}", Mailer.SiteUrl + "/newsletter/unsubscribe");

            if (IsLocked)
            {
                if (IsPostBack && Act()) return;
                Counts = NewsSender.Counts(C.Id);
                var total = Counts.Values.Sum();
                if (total > 0) { C.Recipients = total; C.SentCount = Counts["Sent"]; C.FailedCount = Counts["Failed"]; } // live numbers between saves
                Problems = NewsSender.Problems(C.Id, 100);
                if (C.Status == "Sending" || C.Status == "Queueing") ((AdminMaster)Master).RefreshAttr = " data-refresh=\"10\"";
                return;
            }
            if (!IsPostBack)
            {
                TestTo.Text = Me.Email;
                if (IsNew) Body.Text = Starter;
                else { Subject.Text = C.Subject; Preheader.Text = C.Preheader; Body.Text = C.BodyHtml; }
                return;
            }
            if (!IsNew && (Request.Form["send"] == "1" || Request.Form["send"] == "later"))
            {
                DateTime? at = null;
                if (Request.Form["send"] == "later")
                {
                    at = Util.ParseInput(Request.Form["sendAt"]);
                    if (at == null || at.Value <= DateTime.UtcNow.AddMinutes(1)) { Fail("Pick a date and time in the future to schedule the newsletter."); return; }
                }
                SaveDraft();
                if (ErrorBox.Visible) return;
                if (Active == 0 && at == null) { Fail("There are no active subscribers yet."); return; }
                if (!NewsSender.Start(C.Id, at)) { RedirectWith("/admin/campaigns/" + C.Id, "This campaign was already sent."); return; }
                RedirectWith("/admin/campaigns/" + C.Id, at.HasValue ? "Scheduled for " + Util.When(at) + "." : "Sending to " + Active + " subscribers.");
                return;
            }
            if (!IsNew && Request.Form["delete"] == "1")
            {
                Newsletter.DeleteCampaign(C.Id);
                RedirectWith("/admin/campaigns", "Draft deleted.");
            }
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            var id = SaveDraft();
            if (id > 0) RedirectWith("/admin/campaigns/" + id, "Draft saved.");
        }

        protected void TestButton_Click(object sender, EventArgs e)
        {
            if (!Newsletter.IsEmail(TestTo.Text)) { Fail("Enter a valid email for the test."); return; }
            var id = SaveDraft();
            if (id == 0) return;
            try { Newsletter.SendTest(id, TestTo.Text.Trim()); RedirectWith("/admin/campaigns/" + id, "Test sent to " + TestTo.Text.Trim() + "."); }
            catch (Exception ex) { Fail("The test could not be sent: " + ex.Message + " Check the SMTP settings in Web.config."); }
        }

        private int SaveDraft()
        {
            if (Subject.Text.Trim().Length < 3) { Fail("Write a subject line."); return 0; }
            if (Body.Text.Trim().Length < 10) { Fail("Write the email body."); return 0; }
            var id = Newsletter.SaveCampaign(IsNew ? 0 : C.Id, Subject.Text.Trim(), Preheader.Text.Trim(), Body.Text, Me.Id);
            if (C == null) C = Newsletter.Campaign(id);
            return id;
        }

        /// <summary>Buttons on a scheduled, sending, paused or sent campaign.</summary>
        private bool Act()
        {
            var url = "/admin/campaigns/" + C.Id;
            switch (Request.Form["act"])
            {
                case "now":
                    if (Active == 0) { LockedErrorText.Text = "There are no active subscribers yet."; LockedError.Visible = true; return false; }
                    RedirectWith(url, NewsSender.Start(C.Id, null) ? "Sending to " + Active + " subscribers." : null); return true;
                case "unschedule": NewsSender.Unschedule(C.Id); RedirectWith(url, "Schedule cancelled. The campaign is a draft again."); return true;
                case "pause": NewsSender.Pause(C.Id); RedirectWith(url, "Paused. Resume whenever you are ready."); return true;
                case "resume": NewsSender.Resume(C.Id); RedirectWith(url, "Sending again."); return true;
                case "stop": NewsSender.Stop(C.Id); RedirectWith(url, "Stopped. Nobody else will get this email."); return true;
                case "retry": var n = NewsSender.RetryFailed(C.Id); RedirectWith(url, n > 0 ? "Retrying " + n + " addresses." : null); return true;
            }
            return false;
        }

        /// <summary>One line under the subject: what state the send is in.</summary>
        protected string Summary()
        {
            switch (C.Status)
            {
                case "Scheduled": return "Scheduled for " + H(When(C.ScheduledFor)) + " (IST) to the " + N(Active) + " people subscribed at that time.";
                case "Queueing": return "Preparing the recipient list.";
                case "Sending": return "Sending now. This page refreshes on its own.";
                case "Paused": return "Paused. " + N(Counts["Pending"]) + " people have not been sent this yet.";
                default: return "Sent " + H(When(C.SentOn)) + " to " + N(C.SentCount) + " of " + N(C.Recipients) + " subscribers.";
            }
        }

        private void Fail(string message) { ErrorText.Text = Server.HtmlEncode(message); ErrorBox.Visible = true; }

        private const string Starter =
@"<p style=""margin:0 0 16px"">Hi {{name}},</p>
<h2 style=""font-size:22px;line-height:1.3;margin:0 0 12px"">Your headline goes here</h2>
<p style=""margin:0 0 16px"">Open with the one idea worth your reader's time. Keep paragraphs short and useful.</p>
<p style=""margin:24px 0""><a href=""https://www.yenetch.com/contact"" style=""display:inline-block;background:#0066FF;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:600"">Book a free consultation</a></p>
<p style=""margin:0"">Team Yenetch</p>";
    }
}
