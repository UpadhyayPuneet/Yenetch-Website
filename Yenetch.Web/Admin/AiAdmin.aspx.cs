using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/ai: API keys for the AI assistant (order, limits, status), its settings, and recent conversations. Admins only.</summary>
    public partial class AiAdminPage : AdminPage
    {
        public override string Section { get { return "ai"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected List<AiKey> Keys;
        protected AiKey Editing;
        protected List<Row> Chats;
        protected int Chats30, ChatLeads30;
        protected string Err;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            Keys = Ai.Keys();
            var edit = QInt("edit");
            Editing = edit > 0 ? Keys.FirstOrDefault(k => k.Id == edit) : null;
            Chats = Db.Rows("SELECT * FROM AiChats ORDER BY LastOn DESC" + Db.Page(0, 30));
            var since = new { since = DateTime.UtcNow.AddDays(-30) };
            Chats30 = Db.Scalar<int>("SELECT COUNT(*) FROM AiChats WHERE StartedOn >= @since", since);
            ChatLeads30 = Db.Scalar<int>("SELECT COUNT(*) FROM AiChats WHERE StartedOn >= @since AND LeadId IS NOT NULL", since);
        }

        private void Handle()
        {
            var f = Request.Form;
            int id;
            if (int.TryParse(f["test"], out id))
            {
                var error = Ai.Test(id);
                RedirectWith("/admin/ai", error == null ? "The key works. The assistant answered." : "The test failed: " + error);
                return;
            }
            if (int.TryParse(f["wake"], out id)) { Ai.Wake(id); RedirectWith("/admin/ai", "The key is back in use."); return; }
            if (int.TryParse(f["up"], out id)) { Ai.Move(id, -1); RedirectWith("/admin/ai", null); return; }
            if (int.TryParse(f["del"], out id)) { Ai.DeleteKey(id); RedirectWith("/admin/ai", "Key deleted."); return; }

            if (f["act"] == "key")
            {
                int.TryParse(f["keyId"], out id);
                int limit; int.TryParse(f["limit"], out limit);
                var label = (f["label"] ?? "").Trim();
                if (label.Length == 0) label = "Key " + (Ai.Keys().Count + 1);
                var key = (f["apikey"] ?? "").Trim();
                if (id == 0)
                {
                    if (key.Length < 20) { Err = "Paste the full API key (it starts with sk-ant-)."; return; }
                    var newId = Ai.AddKey(label, f["model"], key, limit);
                    var error = Ai.Test(newId);
                    RedirectWith("/admin/ai", error == null ? "Key added and tested: the assistant is answering with AI." : "Key added, but the test failed: " + error);
                }
                else
                {
                    if (key.Length > 0 && key.Length < 20) { Err = "Paste the full API key."; return; }
                    Ai.UpdateKey(id, label, f["model"], key, limit, f["active"] == "1");
                    RedirectWith("/admin/ai", "Key saved.");
                }
                return;
            }

            // Settings
            int perVisitor, cap;
            if (!int.TryParse(f["perVisitor"], out perVisitor) || perVisitor < 1 || perVisitor > 1000) { Err = "Questions per visitor per hour must be between 1 and 1000."; return; }
            if (!int.TryParse(f["dailyCap"], out cap) || cap < 1 || cap > 1000000) { Err = "Answers per day must be a number above 0."; return; }
            Settings.Set("ai.enabled", f["enabled"] == "1" ? "1" : "0");
            Settings.Set("ai.perVisitor", perVisitor.ToString());
            Settings.Set("ai.dailyCap", cap.ToString());
            Settings.Set("ai.extra", Util.Cut((f["extra"] ?? "").Trim(), 3000));
            HttpRuntimeRemove();
            RedirectWith("/admin/ai", "AI settings saved.");
        }

        private static void HttpRuntimeRemove() { System.Web.HttpRuntime.Cache.Remove("ai.has"); SiteDataScript.Invalidate(); }

        protected static string ModelName(string model) { var m = Ai.Models.FirstOrDefault(x => x[0] == model); return m == null ? model : m[1].Split('(')[0].Trim(); }

        protected static string StatusOf(AiKey k)
        {
            if (!k.IsActive) return "<span class=\"badge badge--lost\">Off</span>";
            if (k.OverLimit) return "<span class=\"badge badge--open\">Daily limit reached</span>";
            if (k.Resting) return "<span class=\"badge badge--hot\">" + H(k.Status ?? "Resting") + "</span><span class=\"cell-sub\">until " + H(Util.Time(k.RestUntil)) + "</span>";
            return "<span class=\"badge badge--won\">Ready</span>";
        }

        private static List<Dictionary<string, object>> Parse(string json)
        {
            try { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<List<Dictionary<string, object>>>(json ?? "[]") ?? new List<Dictionary<string, object>>(); }
            catch { return new List<Dictionary<string, object>>(); }
        }

        protected static string FirstQuestion(string json)
        {
            var first = Parse(json).FirstOrDefault(m => Convert.ToString(m["r"]) == "user");
            return first == null ? "(empty)" : Convert.ToString(first["t"]);
        }

        protected static string Transcript(string json)
        {
            var sb = new StringBuilder();
            foreach (var m in Parse(json))
                sb.Append("<p class=\"chatlog__").Append(Convert.ToString(m["r"]) == "user" ? "q" : "a").Append("\"><b>").Append(Convert.ToString(m["r"]) == "user" ? "Visitor" : "Assistant")
                  .Append(":</b> ").Append(H(Convert.ToString(m["t"]))).Append("</p>");
            return sb.ToString();
        }
    }
}
