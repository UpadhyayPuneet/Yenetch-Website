using System;
using System.Collections.Generic;
using System.Web.UI;
using Yenetch.Data;

namespace Yenetch.Web
{
    /// <summary>/contact. The enquiry form posts back and is saved to the CRM with any attached file, then emailed to the team (see /admin/email).</summary>
    public partial class Contact : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            // Pre-select a topic from links such as /contact?service=seo
            var svc = SiteContent.Service(Request.QueryString["service"] ?? "");
            if (svc != null && Interest.Items.FindByText(svc.Name) != null) Interest.SelectedValue = svc.Name;
        }

        protected void SendButton_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            try
            {
                Yenetch.Crm.Db.EnsureSchema();
                Yenetch.Crm.LeadService.CreateFromWebsite(Name.Text, ContactInfo.Text, Need.Text, Interest.SelectedValue, "contact-form",
                    Request.Url.AbsolutePath, Yenetch.Crm.Analytics.CookieId(Request, Yenetch.Crm.Analytics.VisitorCookie), null, Uploads());
            }
            catch (Exception ex)
            {
                // Database unavailable: keep the enquiry in App_Data/leads.jsonl.
                LeadStore.Save(new Dictionary<string, object> {
                    { "name", Name.Text.Trim() }, { "contact", ContactInfo.Text.Trim() }, { "need", Need.Text.Trim() },
                    { "topic", Interest.SelectedValue }, { "source", "contact-form" }, { "page", Request.Url.AbsolutePath }, { "error", ex.Message }
                });
            }
            FormFields.Visible = false;
            ThankYou.Visible = true;
        }

        /// <summary>Files sent with the form (the optional "Attach a brief" field).</summary>
        private IEnumerable<System.Web.HttpPostedFile> Uploads()
        {
            for (var i = 0; i < Request.Files.Count; i++) yield return Request.Files[i];
        }
    }
}
