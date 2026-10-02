using System;
using System.Collections.Generic;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/integrations: Razorpay keys, client portal address and outgoing webhooks. Admins only.</summary>
    public partial class IntegrationsPage : AdminPage
    {
        public override string Section { get { return "integrations"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected string Err;
        protected bool HasRzSecret, HasRzHook, HasHookSecret;
        protected List<Row> Log;
        protected string SiteUrl { get { return Mailer.SiteUrl; } }
        protected static string Settings_(string k) { return Settings.Get(k); }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            HasRzSecret = !string.IsNullOrEmpty(Settings.Get("razorpay.keySecret"));
            HasRzHook = !string.IsNullOrEmpty(Settings.Get("razorpay.webhookSecret"));
            HasHookSecret = !string.IsNullOrEmpty(Settings.Get("hooks.secret"));
            Log = Webhooks.Log(30);
        }

        private void Handle()
        {
            var f = Request.Form;
            var rzId = (f["rzId"] ?? "").Trim();
            if (rzId.Length > 0 && !(rzId.StartsWith("rzp_test_") || rzId.StartsWith("rzp_live_"))) { Err = "The Razorpay Key ID starts with rzp_test_ or rzp_live_."; return; }
            var portal = (f["portal"] ?? "").Trim();
            if (portal.Length > 0 && !portal.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { Err = "The portal address must start with https://"; return; }
            var hook = (f["hookUrl"] ?? "").Trim();
            if (hook.Length > 0 && !hook.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !hook.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase)) { Err = "The webhook address must start with https://"; return; }

            Settings.Set("razorpay.keyId", rzId.Length > 0 ? rzId : null);
            if (!string.IsNullOrWhiteSpace(f["rzSecret"])) Settings.SetSecret("razorpay.keySecret", f["rzSecret"].Trim());
            if (!string.IsNullOrWhiteSpace(f["rzHook"])) Settings.SetSecret("razorpay.webhookSecret", f["rzHook"].Trim());
            Settings.Set("integrations.portalUrl", portal.Length > 0 ? portal : null);
            Settings.Set("hooks.url", hook.Length > 0 ? hook : null);
            if (!string.IsNullOrWhiteSpace(f["hookSecret"])) Settings.SetSecret("hooks.secret", f["hookSecret"].Trim());

            if (f["act"] == "rzTest")
            {
                var error = Razorpay.Test();
                RedirectWith("/admin/integrations#razorpay", error == null ? "Razorpay accepted the keys." : error);
                return;
            }
            if (f["act"] == "hookTest")
            {
                if (!Webhooks.Enabled) { RedirectWith("/admin/integrations#hooks", "Saved. Add a webhook address to send a test."); return; }
                var body = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new { @event = "test", id = Util.NewId(), createdAt = DateTime.UtcNow.ToString("o"), site = Mailer.SiteUrl, data = new { message = "Test from the Yenetch website" } });
                var result = Webhooks.Send("test", Webhooks.Url, Settings.GetSecret("hooks.secret") ?? "", body);
                RedirectWith("/admin/integrations#log", "Test sent. " + result);
                return;
            }
            RedirectWith("/admin/integrations", "Integrations saved.");
        }
    }
}
