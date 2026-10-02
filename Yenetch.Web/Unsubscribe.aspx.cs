using System;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web
{
    /// <summary>
    /// /newsletter/unsubscribe?t={token}. Asks before unsubscribing (so link scanners do not unsubscribe people),
    /// and accepts one-click POSTs from mail apps (List-Unsubscribe-Post).
    /// </summary>
    public partial class Unsubscribe : Page
    {
        private Subscriber _sub;
        private string _email;

        protected void Page_Load(object sender, EventArgs e)
        {
            Db.EnsureSchema();
            // ?t= comes from newsletters, ?a= from automatic emails (welcome series, follow-ups, review requests).
            _sub = Newsletter.ByToken(Request.QueryString["t"]);
            _email = _sub != null ? _sub.Email : Automations.EmailForToken(Request.QueryString["a"]);
            if (_sub == null && _email != null) _sub = Newsletter.ByEmail(_email);
            if (Request.HttpMethod == "POST" && Request.Form["List-Unsubscribe"] == "One-Click")
            {
                StopEverything();
                Response.StatusCode = 200; Response.End(); return;
            }
            if (_email == null) { Show(MissingPanel); return; }
            if (!IsPostBack)
            {
                EmailText.Text = Server.HtmlEncode(_email);
                if (_sub != null && _sub.Status != "Active" && Request.QueryString["a"] == null) Show(DonePanel);
            }
        }

        private void StopEverything()
        {
            if (_sub != null) Newsletter.Unsubscribe(_sub.Id);
            if (_email != null) Automations.StopAll(_email);
        }

        protected void ConfirmButton_Click(object sender, EventArgs e) { StopEverything(); Show(DonePanel); }
        protected void ResubscribeButton_Click(object sender, EventArgs e) { if (_sub != null) Newsletter.Resubscribe(_sub.Id); Show(BackPanel); }

        private void Show(Control panel)
        {
            AskPanel.Visible = DonePanel.Visible = BackPanel.Visible = MissingPanel.Visible = false;
            panel.Visible = true;
        }
    }
}
