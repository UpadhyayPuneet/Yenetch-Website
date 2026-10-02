using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/leads/{id} and /admin/leads/new. Details, pipeline stage, activity log with follow-ups, and the visitor's website journey.</summary>
    public partial class LeadPage : AdminPage
    {
        public override string Section { get { return "leads"; } }

        protected Lead L;
        protected bool IsNew;
        protected List<Activity> Timeline = new List<Activity>();
        protected List<LeadFile> Files = new List<LeadFile>();
        protected Stats.VisitorRow Visitor;
        protected List<Stats.JourneyStep> PagesBefore = new List<Stats.JourneyStep>();
        protected string ContextText;
        protected List<ScorePart> ScoreParts = new List<ScorePart>();
        protected List<SavedQuote> LeadQuotes = new List<SavedQuote>();
        protected List<Proposal> LeadProposals = new List<Proposal>();

        protected static string ScorePill(int? score)
        {
            if (!score.HasValue) return "";
            return "<span class=\"score score--" + LeadScoring.Band(score) + "\" title=\"Lead score\">" + score.Value + "</span>";
        }
        protected static readonly string[] Stages = { "New", "Contacted", "Qualified", "Proposal", "Negotiation", "Won", "Lost" };

        protected void Page_Load(object sender, EventArgs e)
        {
            var key = Convert.ToString(RouteData.Values["id"] ?? Request.QueryString["id"] ?? "new");
            IsNew = key == "new";
            if (!IsNew)
            {
                int id;
                L = int.TryParse(key, out id) ? LeadService.Get(id) : null;
                if (L == null || !CanSee(L)) { RedirectWith("/admin/leads", "That lead does not exist or is assigned to someone else."); return; }
                Title = L.Name;
            }
            else Title = "Add lead";

            if (!IsPostBack) Fill();
            else if (!IsNew && HandleActions()) return;

            if (!IsNew) LoadSideData();
        }

        private bool CanSee(Lead l) { return Me.SeesAllLeads || !l.AssignedTo.HasValue || l.AssignedTo == Me.Id; }

        private void Fill()
        {
            Bind(LType, Lists.Types); Bind(LSource, Lists.Sources); Bind(LStatus, Lists.Statuses); Bind(LPriority, Lists.Priorities);
            LOwner.Items.Add(new ListItem("Unassigned", ""));
            foreach (var u in Auth.ActiveUsers())
                if (Me.SeesAllLeads || u.Id == Me.Id) LOwner.Items.Add(new ListItem(u.Name + (u.Id == Me.Id ? " (me)" : ""), u.Id.ToString()));

            if (IsNew)
            {
                LType.SelectedValue = "General"; LSource.SelectedValue = "Phone"; LStatus.SelectedValue = "New"; LPriority.SelectedValue = "Warm";
                LOwner.SelectedValue = Me.Id.ToString();
                LName.Focus();
                return;
            }
            LName.Text = L.Name; LCompany.Text = L.Company; LPhone.Text = L.Phone; LEmail.Text = L.Email; LCity.Text = L.City; LInterest.Text = L.Interest;
            LNeed.Text = L.Need; LTags.Text = L.Tags; LLostReason.Text = L.LostReason;
            LValue.Text = L.EstValue.HasValue ? L.EstValue.Value.ToString("0", CultureInfo.InvariantCulture) : "";
            Select(LType, L.LeadType); Select(LSource, L.Source); Select(LStatus, L.Status); Select(LPriority, L.Priority);
            Select(LOwner, L.AssignedTo.HasValue ? L.AssignedTo.Value.ToString() : "");
        }

        /// <summary>Buttons outside the details form: log activity, mark follow-up done, change stage, delete.</summary>
        private bool HandleActions()
        {
            var url = "/admin/leads/" + L.Id;
            if (Request.Form["log"] == "1")
            {
                var kind = Lists.ActivityKinds.Contains(Request.Form["kind"]) ? Request.Form["kind"] : "Note";
                var note = (Request.Form["note"] ?? "").Trim();
                var due = Util.ParseInput(Request.Form["due"]);
                if (note.Length == 0 && !due.HasValue) { RedirectWith(url, "Write a note or pick a follow-up date."); return true; }
                if (note.Length > 0) LeadService.AddActivity(L.Id, Me.Id, kind, note, null);
                if (due.HasValue)
                {
                    var task = (Request.Form["dueNote"] ?? "").Trim();
                    LeadService.AddActivity(L.Id, Me.Id, "FollowUp", task.Length > 0 ? task : "Follow up with " + L.Name, due);
                }
                RedirectWith(url, due.HasValue ? "Saved. Follow-up scheduled for " + Util.When(due) + "." : "Saved to the timeline.");
                return true;
            }
            int doneId;
            if (int.TryParse(Request.Form["done"], out doneId))
            {
                if (Db.Scalar<int>("SELECT LeadId FROM CrmActivities WHERE Id = @id", new { id = doneId }) == L.Id) LeadService.CompleteFollowUp(doneId, Me.Id);
                RedirectWith(url, "Follow-up marked done.");
                return true;
            }
            var stage = Request.Form["setStatus"];
            if (!string.IsNullOrEmpty(stage))
            {
                LeadService.SetStatus(L.Id, stage, Me.Id);
                RedirectWith(url, "Moved to " + stage + ".");
                return true;
            }
            if (Request.Form["askReview"] == "1")
            {
                var problem = Reviews.Ask(L, Me.Id);
                RedirectWith(url, problem ?? "Review request sent to " + L.Email + ".");
                return true;
            }
                        if (Request.Form["delete"] == "1" && Me.IsAdmin)
            {
                LeadService.Delete(L.Id);
                RedirectWith("/admin/leads", "Lead deleted.");
                return true;
            }
            return false;
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            var name = LName.Text.Trim();
            if (name.Length < 2) { Fail("Enter the lead's name."); return; }
            if (LPhone.Text.Trim() == "" && LEmail.Text.Trim() == "") { Fail("Add a phone number or an email."); return; }
            if (LEmail.Text.Trim() != "" && !Newsletter.IsEmail(LEmail.Text)) { Fail("That email address does not look right."); return; }
            decimal value = 0;
            var hasValue = LValue.Text.Trim() != "" && decimal.TryParse(LValue.Text.Replace(",", "").Replace("₹", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
            if (LValue.Text.Trim() != "" && !hasValue) { Fail("Expected value must be a number."); return; }
            int owner;
            int? ownerId = int.TryParse(LOwner.SelectedValue, out owner) ? (int?)owner : null;
            if (!Me.SeesAllLeads && ownerId.HasValue && ownerId != Me.Id) ownerId = Me.Id;

            var l = IsNew ? new Lead() : L;
            l.Name = name; l.Company = LCompany.Text; l.Phone = LPhone.Text; l.Email = LEmail.Text; l.City = LCity.Text; l.Interest = LInterest.Text;
            l.Need = LNeed.Text; l.LeadType = LType.SelectedValue; l.Source = LSource.SelectedValue; l.Status = LStatus.SelectedValue;
            l.Priority = LPriority.SelectedValue; l.EstValue = hasValue ? (decimal?)value : null; l.AssignedTo = ownerId; l.Tags = LTags.Text; l.LostReason = LLostReason.Text;

            if (IsNew)
            {
                if (string.IsNullOrWhiteSpace(l.Interest) && !string.IsNullOrWhiteSpace(l.Need) && l.LeadType == "General") l.LeadType = LeadService.Classify(l.Need);
                var id = LeadService.Insert(l, Me.Id);
                LeadService.AddActivity(id, Me.Id, "System", "Lead added by " + Me.Name + " (" + l.Source + ").", null);
                var note = (Request.Form["newNote"] ?? "").Trim();
                if (note.Length > 0) LeadService.AddActivity(id, Me.Id, "Note", note, null);
                var due = Util.ParseInput(Request.Form["newDue"]);
                if (due.HasValue) LeadService.AddActivity(id, Me.Id, "FollowUp", "Follow up with " + l.Name, due);
                if (ownerId.HasValue && ownerId != Me.Id) Mailer.NotifyAssigned(id, ownerId.Value);
                RedirectWith("/admin/leads/" + id, "Lead added.");
                return;
            }
            LeadService.Update(l, Me.Id);
            RedirectWith("/admin/leads/" + l.Id, "Details saved.");
        }

        private void LoadSideData()
        {
            Timeline = LeadService.Activities(L.Id);
            Files = Attachments.ForLead(L.Id);
            ScoreParts = LeadScoring.Parts(L.ScoreJson);
            LeadQuotes = Quotes.ForLead(L.Id);
            int total;
            LeadProposals = Proposals.List(null, L.Id, null, 0, 20, out total);
            if (!string.IsNullOrEmpty(L.VisitorId))
            {
                Visitor = Stats.Visitor(L.VisitorId);
                if (Visitor != null)
                    PagesBefore = Stats.Journey(L.VisitorId, 10).SelectMany(s => s.Steps).Where(s => s.Kind == "page" && s.At <= L.CreatedOn.AddMinutes(1))
                                       .OrderByDescending(s => s.At).Take(10).Reverse().ToList();
            }
            if (!string.IsNullOrEmpty(L.ContextJson))
            {
                try
                {
                    var d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(L.ContextJson);
                    var sb = new StringBuilder();
                    foreach (var kv in d) sb.AppendLine(Humanize(kv.Key) + ": " + Flatten(kv.Value));
                    ContextText = sb.ToString().Trim();
                }
                catch { ContextText = L.ContextJson; }
            }
        }

        private static string Flatten(object v)
        {
            if (v == null) return "";
            var dict = v as IDictionary<string, object>;
            if (dict != null) return string.Join("; ", dict.Select(kv => Humanize(kv.Key) + " = " + Flatten(kv.Value)));
            var list = v as System.Collections.IEnumerable;
            if (list != null && !(v is string)) return string.Join(", ", list.Cast<object>().Select(Flatten));
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        private static string Humanize(string key)
        {
            var s = System.Text.RegularExpressions.Regex.Replace(key ?? "", "([a-z])([A-Z])", "$1 $2").Replace("_", " ");
            return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        protected string StageCss(string s)
        {
            var cur = Array.IndexOf(Stages, L.Status);
            var i = Array.IndexOf(Stages, s);
            if (s == L.Status) return L.Status == "Won" ? "is-won" : L.Status == "Lost" ? "is-lost" : "is-now";
            if (L.Status == "Junk" || L.Status == "Lost" || s == "Lost" || s == "Won") return "";
            return i < cur ? "is-done" : "";
        }

        private void Fail(string message) { ErrorText.Text = Server.HtmlEncode(message); ErrorBox.Visible = true; }
        private static void Bind(DropDownList d, IEnumerable<string> items) { foreach (var i in items) d.Items.Add(i); }
        private static void Select(DropDownList d, string v) { if (v != null && d.Items.FindByValue(v) != null) d.SelectedValue = v; }
    }
}
