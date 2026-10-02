using System;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/applications/{id}. One job application: details, CV, status, rating, team notes and email to the candidate.</summary>
    public partial class ApplicationPage : AdminPage
    {
        public override string Section { get { return "applications"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected JobApplication A;

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            int.TryParse(Convert.ToString(RouteData.Values["id"] ?? Request.QueryString["id"]), out id);
            A = Applications.Get(id);
            if (A == null) { RedirectWith("/admin/applications", "That application no longer exists."); return; }
            Title = A.Name + " · Application";
            if (IsPostBack) return;
            foreach (var s in Applications.Statuses) StatusList.Items.Add(new ListItem(s, s));
            StatusList.SelectedValue = A.Status;
            RatingList.Items.Add(new ListItem("Not rated", "0"));
            for (var i = 1; i <= 5; i++) RatingList.Items.Add(new ListItem(new string('★', i) + " (" + i + ")", i.ToString()));
            RatingList.SelectedValue = A.Rating.ToString();
            Notes.Text = A.AdminNotes;
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            int rating; int.TryParse(RatingList.SelectedValue, out rating);
            Applications.Update(A.Id, StatusList.SelectedValue, rating, Notes.Text);
            RedirectWith(Request.RawUrl, "Saved.");
        }

        protected void SendButton_Click(object sender, EventArgs e)
        {
            var error = Applications.EmailCandidate(A, Subject.Text, Message.Text, Me);
            if (error != null) { ErrorText.Text = Server.HtmlEncode(error); ErrorBox.Visible = true; return; }
            RedirectWith(Request.RawUrl, "Email sent to " + A.Email + ".");
        }

        protected void DeleteButton_Click(object sender, EventArgs e)
        {
            Applications.Delete(A.Id);
            RedirectWith("/admin/applications", "Application deleted.");
        }

        protected string WhatsApp
        {
            get { var d = Util.Digits(A.Phone); if (d.Length == 10) d = "91" + d; return "https://wa.me/" + d; }
        }

        protected string Company { get { var c = Yenetch.Data.SiteContent.Current.Company; return c != null && !string.IsNullOrEmpty(c.Name) ? c.Name : "Yenetch"; } }

        protected static string Js(string s) { return new JavaScriptSerializer().Serialize(s ?? "").Replace("</", "<\\/"); }
    }
}
