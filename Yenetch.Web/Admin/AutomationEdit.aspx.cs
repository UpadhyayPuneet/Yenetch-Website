using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/automations/{key}: edit one email series (up to five emails), see who is in it, send a test.</summary>
    public partial class AutomationEditPage : AdminPage
    {
        public override string Section { get { return "automations"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected static readonly int[] DelayChoices = { 0, 1, 3, 6, 12, 24, 48, 72, 96, 120, 168, 240, 336, 504, 720 };
        protected AutoSeries S;
        protected Dictionary<string, int> C;
        protected List<Row> People;
        protected string Err;

        protected void Page_Load(object sender, EventArgs e)
        {
            S = Automations.Get(Convert.ToString(RouteData.Values["key"]));
            if (S == null) { RedirectWith("/admin/automations", null); return; }
            Title = S.Name;
            int preview;
            if (int.TryParse(Q("preview"), out preview) && preview >= 0 && preview < S.Steps.Count)
            {
                var st = S.Steps[preview];
                Response.ContentType = "text/html";
                Response.Write(Mailer.Wrap(Automations.Fill(st.Subject, null, true), Automations.Fill(st.Body, null, false), Mailer.SiteUrl + "/newsletter/unsubscribe"));
                Response.End();
                return;
            }
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            C = Automations.Counts(S.Key);
            People = Automations.Recent(S.Key, 15);
        }

        private void Handle()
        {
            var f = Request.Unvalidated.Form;
            int stop;
            if (int.TryParse(f["stop"], out stop)) { Automations.Stop(stop); RedirectWith(Request.RawUrl, "Stopped. They will not get the rest of this series."); return; }

            var steps = new List<AutoStep>();
            for (var i = 0; i < Automations.MaxSteps; i++)
            {
                var subject = (f["s" + i] ?? "").Trim();
                var body = (f["b" + i] ?? "").Trim();
                if (f["x" + i] == "1" || (subject == "" && body == "")) continue;
                if (subject == "") { Err = "Email " + (i + 1) + " needs a subject."; Keep(f); return; }
                if (body == "") { Err = "Email " + (i + 1) + " needs some text."; Keep(f); return; }
                int d; int.TryParse(f["d" + i], out d);
                steps.Add(new AutoStep { DelayHours = Math.Max(0, Math.Min(24 * 90, d)), Subject = Util.Cut(subject, 150), Body = Util.Cut(body, 50000) });
            }
            S.Enabled = f["enabled"] == "1";
            if (S.Enabled && steps.Count == 0) { Err = "Add at least one email before turning this on."; Keep(f); return; }
            S.Steps = steps;
            Automations.Save(S);
            if (f["act"] == "test")
            {
                var to = (f["testTo"] ?? "").Trim();
                if (!Newsletter.IsEmail(to)) { Err = "Enter a valid email for the test."; return; }
                try { Automations.SendTest(S, to); }
                catch (Exception ex) { Err = "Saved, but the test could not be sent: " + ex.Message; return; }
                RedirectWith(Request.RawUrl, "Saved. " + steps.Count + " test email" + (steps.Count == 1 ? "" : "s") + " sent to " + to + ".");
                return;
            }
            RedirectWith(Request.RawUrl, S.Enabled ? "Saved. The series is on for new people from now." : "Saved. The series is off.");
        }

        /// <summary>Shows what was typed again after an error.</summary>
        private void Keep(System.Collections.Specialized.NameValueCollection f)
        {
            var steps = new List<AutoStep>();
            for (var i = 0; i < Automations.MaxSteps; i++)
            {
                var subject = (f["s" + i] ?? "").Trim();
                var body = (f["b" + i] ?? "").Trim();
                if (subject == "" && body == "") continue;
                int d; int.TryParse(f["d" + i], out d);
                steps.Add(new AutoStep { DelayHours = d, Subject = subject, Body = body });
            }
            S.Steps = steps;
            S.Enabled = f["enabled"] == "1";
        }
    }
}
