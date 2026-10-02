using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Yenetch.Crm
{
    /// <summary>Pick-lists used by the lead pages and reports. Edit here to add a status, source or type.</summary>
    public static class Lists
    {
        public static readonly string[] Statuses = { "New", "Contacted", "Qualified", "Proposal", "Negotiation", "Won", "Lost", "Junk" };
        public static readonly string[] OpenStatuses = { "New", "Contacted", "Qualified", "Proposal", "Negotiation" };
        public static readonly string[] Types = { "Marketing", "Development", "Talent", "Product", "General" };
        public static readonly string[] Sources = { "Contact form", "Chatbot", "Solution finder", "Booking", "Website audit", "Plan builder", "Landing page", "Manual", "Phone", "WhatsApp", "Email", "Referral", "Event", "Other" };
        public static readonly string[] Priorities = { "Hot", "Warm", "Cold" };
        public static readonly string[] ActivityKinds = { "Note", "Call", "Email", "WhatsApp", "Meeting" };
        public static readonly string[] Roles = { "Admin", "Manager", "Sales" };
    }

    public class Lead
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Company { get; set; }
        public string City { get; set; }
        public string Need { get; set; }
        public string Interest { get; set; }
        public string LeadType { get; set; }
        public string Source { get; set; }
        public string Channel { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public decimal? EstValue { get; set; }
        public int? AssignedTo { get; set; }
        public string AssignedName { get; set; }
        public DateTime? NextFollowUp { get; set; }
        public string LostReason { get; set; }
        public string Tags { get; set; }
        public string VisitorId { get; set; }
        public string Page { get; set; }
        public string ContextJson { get; set; }
        /// <summary>Lead score 0-100 (see LeadScoring) and the reasons behind it.</summary>
        public int? Score { get; set; }
        public string ScoreJson { get; set; }

        public bool IsOpen { get { return Lists.OpenStatuses.Contains(Status); } }
        public bool IsOverdue { get { return IsOpen && NextFollowUp.HasValue && NextFollowUp.Value < DateTime.UtcNow; } }
        public string ContactLine { get { return string.Join(" · ", new[] { Phone, Email }.Where(s => !string.IsNullOrEmpty(s))); } }
        public string WhatsAppUrl { get { var d = Util.Digits(Phone); return d.Length >= 10 ? "https://wa.me/" + (d.Length == 10 ? "91" + d : d) : null; } }

        internal static Lead From(Row r)
        {
            return new Lead
            {
                Id = r.Int("Id"), CreatedOn = r.Date("CreatedOn"), UpdatedOn = r.Date("UpdatedOn"), Name = r.Str("Name"), Email = r.Str("Email"),
                Phone = r.Str("Phone"), Company = r.Str("Company"), City = r.Str("City"), Need = r.Str("Need"), Interest = r.Str("Interest"),
                LeadType = r.Str("LeadType"), Source = r.Str("Source"), Channel = r.Str("Channel"), Status = r.Str("Status"), Priority = r.Str("Priority"),
                EstValue = r.DecN("EstValue"), AssignedTo = r.IntN("AssignedTo"), AssignedName = r.Str("AssignedName"), NextFollowUp = r.DateN("NextFollowUp"),
                LostReason = r.Str("LostReason"), Tags = r.Str("Tags"), VisitorId = r.Str("VisitorId"), Page = r.Str("Page"), ContextJson = r.Str("ContextJson"),
                Score = r.IntN("Score"), ScoreJson = r.Str("ScoreJson")
            };
        }
    }

    public class Activity
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public string LeadName { get; set; }
        public string LeadStatus { get; set; }
        public string OwnerName { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string Kind { get; set; }
        public string Body { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? DueOn { get; set; }
        public DateTime? DoneOn { get; set; }
        public bool IsTask { get { return DueOn.HasValue; } }
        public bool IsOverdue { get { return DueOn.HasValue && !DoneOn.HasValue && DueOn.Value < DateTime.UtcNow; } }

        internal static Activity From(Row r)
        {
            return new Activity
            {
                Id = r.Int("Id"), LeadId = r.Int("LeadId"), LeadName = r.Str("LeadName"), LeadStatus = r.Str("LeadStatus"), OwnerName = r.Str("OwnerName"), UserId = r.IntN("UserId"),
                UserName = r.Str("UserName"), Kind = r.Str("Kind"), Body = r.Str("Body"), CreatedOn = r.Date("CreatedOn"), DueOn = r.DateN("DueOn"), DoneOn = r.DateN("DoneOn")
            };
        }
    }

    /// <summary>Filters for the lead list and CSV export.</summary>
    public class LeadFilter
    {
        public string Search, Status, LeadType, Source, Priority, Sort;
        public int? AssignedTo;
        public bool Unassigned, OpenOnly, OverdueOnly;
        public DateTime? From, To;
        /// <summary>Set for Sales users: they only see their own and unassigned leads.</summary>
        public int? RestrictToUser;
    }

    public static class LeadService
    {
        private const string Select = "SELECT l.*, u.Name AS AssignedName FROM CrmLeads l LEFT JOIN CrmUsers u ON u.Id = l.AssignedTo";

        private static readonly Dictionary<string, string> SortColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "created", "l.CreatedOn" }, { "name", "l.Name" }, { "status", "l.Status" }, { "type", "l.LeadType" }, { "source", "l.Source" },
            { "priority", "CASE l.Priority WHEN 'Hot' THEN 0 WHEN 'Warm' THEN 1 ELSE 2 END" }, { "value", "l.EstValue" },
            { "followup", "CASE WHEN l.NextFollowUp IS NULL THEN 1 ELSE 0 END, l.NextFollowUp" }, { "owner", "u.Name" }, { "updated", "l.UpdatedOn" }, { "score", "COALESCE(l.Score,-1)" }
        };

        // ---- Create ------------------------------------------------------------------------------------------

        /// <summary>Lead from the website (contact form, chatbot, finder). Links the visitor, classifies it and emails the sales inbox.</summary>
        public static int CreateFromWebsite(string name, string contact, string need, string topic, string sourceKey, string page, string visitorId, string contextJson,
                                            IEnumerable<System.Web.HttpPostedFile> uploads = null)
        {
            var lead = new Lead
            {
                Name = Util.Cut(name, 120), Need = Util.Cut(need, 2000), Interest = Util.Cut(topic, 160), Page = Util.Cut(page, 300),
                Source = SourceFromKey(sourceKey), Status = "New", Priority = "Warm", ContextJson = contextJson, VisitorId = Util.Cut(visitorId, 40)
            };
            SplitContact(contact, lead);
            lead.LeadType = Classify(topic + " " + need);
            if (!string.IsNullOrEmpty(lead.VisitorId))
            {
                var v = Db.First("SELECT FirstChannel, City FROM WebVisitors WHERE Id = @id", new { id = lead.VisitorId });
                if (v != null) { lead.Channel = v.Str("FirstChannel"); lead.City = v.Str("City"); }
            }
            lead.Id = Insert(lead, null);
            AddActivity(lead.Id, null, "System", "Lead received from the website (" + lead.Source + (string.IsNullOrEmpty(lead.Page) ? "" : ", " + lead.Page) + ").", null);
            if (!string.IsNullOrEmpty(lead.VisitorId))
            {
                Db.Exec("UPDATE WebVisitors SET LeadId = @lead WHERE Id = @id AND LeadId IS NULL", new { lead = lead.Id, id = lead.VisitorId });
                Analytics.RecordServerEvent(lead.VisitorId, "lead", lead.Source, page);
            }
            var files = new List<LeadFile>();
            foreach (var f in uploads ?? Enumerable.Empty<System.Web.HttpPostedFile>())
            {
                if (f == null || f.ContentLength == 0) continue;
                var problem = Attachments.Problem(f);
                if (problem != null) { AddActivity(lead.Id, null, "System", "A file was not accepted (" + System.IO.Path.GetFileName(f.FileName) + "): " + problem, null); continue; }
                try { files.Add(Attachments.Save(lead.Id, f)); }
                catch (Exception ex) { Mailer.Log("attachment for lead " + lead.Id, ex); }
            }
            if (files.Count > 0) AddActivity(lead.Id, null, "System", "Attached: " + string.Join(", ", files.Select(x => x.FileName)) + ".", null);
            Mailer.NotifyNewLead(lead, files);
            Mailer.AutoReply(lead);
            Automations.Enroll("lead", lead.Email, lead.Name, lead.Id);
            return lead.Id;
        }

        /// <summary>Lead from a website tool (booking, website audit) that sends its own emails: saves it, links the visitor and
        /// logs a note, without the generic thank-you reply. The tool notifies the team itself.</summary>
        public static int CreateFromTool(Lead lead, string note)
        {
            lead.Status = lead.Status ?? "New";
            lead.Priority = lead.Priority ?? "Hot";
            if (string.IsNullOrEmpty(lead.LeadType)) lead.LeadType = Classify(lead.Interest + " " + lead.Need);
            if (!string.IsNullOrEmpty(lead.VisitorId))
            {
                var v = Db.First("SELECT FirstChannel, City FROM WebVisitors WHERE Id = @id", new { id = lead.VisitorId });
                if (v != null) { lead.Channel = v.Str("FirstChannel"); if (string.IsNullOrEmpty(lead.City)) lead.City = v.Str("City"); }
            }
            // The same person coming back (same email) updates their open lead instead of creating a duplicate.
            if (Newsletter.IsEmail(lead.Email))
            {
                var open = Db.Scalar<int?>("SELECT Id FROM CrmLeads WHERE Email = @email AND Status IN ('New','Contacted','Qualified','Proposal','Negotiation') ORDER BY Id DESC" + Db.Page(0, 1), new { email = lead.Email.Trim() });
                if (open.HasValue)
                {
                    Db.Exec("UPDATE CrmLeads SET UpdatedOn = @now, Priority = 'Hot' WHERE Id = @id", new { now = DateTime.UtcNow, id = open.Value });
                    AddActivity(open.Value, null, "System", note, null);
                    return open.Value;
                }
            }
            lead.Id = Insert(lead, null);
            AddActivity(lead.Id, null, "System", note, null);
            if (!string.IsNullOrEmpty(lead.VisitorId))
            {
                Db.Exec("UPDATE WebVisitors SET LeadId = @lead WHERE Id = @id AND LeadId IS NULL", new { lead = lead.Id, id = lead.VisitorId });
                Analytics.RecordServerEvent(lead.VisitorId, "lead", lead.Source, lead.Page);
            }
            return lead.Id;
        }

        public static int Insert(Lead l, int? userId)
        {
            var id = InsertRow(l, userId);
            l.Id = id;
            LeadScoring.Recalc(id);
            Webhooks.Fire("lead.created", new { leadId = id, l.Name, l.Email, l.Phone, l.Company, l.City, l.Need, l.Interest, type = l.LeadType, l.Source, l.Channel, l.Status, l.EstValue, l.Page });
            return id;
        }

        private static int InsertRow(Lead l, int? userId)
        {
            var now = DateTime.UtcNow;
            return Db.Insert(@"INSERT INTO CrmLeads (CreatedOn, UpdatedOn, Name, Email, Phone, Company, City, Need, Interest, LeadType, Source, Channel, Status, Priority,
                EstValue, AssignedTo, NextFollowUp, LostReason, Tags, VisitorId, Page, ContextJson, CreatedBy)
                VALUES (@now, @now, @Name, @Email, @Phone, @Company, @City, @Need, @Interest, @LeadType, @Source, @Channel, @Status, @Priority,
                @EstValue, @AssignedTo, @NextFollowUp, @LostReason, @Tags, @VisitorId, @Page, @ContextJson, @CreatedBy)",
                new Dictionary<string, object>
                {
                    { "now", now }, { "Name", l.Name }, { "Email", Blank(l.Email) }, { "Phone", Blank(l.Phone) }, { "Company", Blank(l.Company) }, { "City", Blank(l.City) },
                    { "Need", Blank(l.Need) }, { "Interest", Blank(l.Interest) }, { "LeadType", l.LeadType ?? "General" }, { "Source", l.Source ?? "Manual" },
                    { "Channel", Blank(l.Channel) }, { "Status", l.Status ?? "New" }, { "Priority", l.Priority ?? "Warm" }, { "EstValue", l.EstValue },
                    { "AssignedTo", l.AssignedTo }, { "NextFollowUp", l.NextFollowUp }, { "LostReason", Blank(l.LostReason) }, { "Tags", Blank(l.Tags) },
                    { "VisitorId", Blank(l.VisitorId) }, { "Page", Blank(l.Page) }, { "ContextJson", Blank(l.ContextJson) }, { "CreatedBy", userId }
                });
        }

        // ---- Read --------------------------------------------------------------------------------------------

        public static Lead Get(int id)
        {
            var r = Db.First(Select + " WHERE l.Id = @id", new { id });
            return r == null ? null : Lead.From(r);
        }

        public static List<Lead> List(LeadFilter f, int offset, int count, out int total)
        {
            var args = new Dictionary<string, object>();
            var where = Where(f, args);
            total = Db.Scalar<int>("SELECT COUNT(*) FROM CrmLeads l LEFT JOIN CrmUsers u ON u.Id = l.AssignedTo" + where, args);
            string col;
            var sort = f.Sort ?? "-created";
            var desc = sort.StartsWith("-", StringComparison.Ordinal);
            if (!SortColumns.TryGetValue(sort.TrimStart('-'), out col)) { col = "l.CreatedOn"; desc = true; }
            var order = string.Join(", ", col.Split(new[] { ", " }, StringSplitOptions.None).Select(c => c + (desc ? " DESC" : " ASC")));
            return Db.Query(Select + where + " ORDER BY " + order + ", l.Id DESC" + Db.Page(offset, count), Lead.From, args);
        }

        private static string Where(LeadFilter f, Dictionary<string, object> a)
        {
            var w = new List<string>();
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                w.Add("(l.Name LIKE @q OR l.Email LIKE @q OR l.Phone LIKE @q OR l.Company LIKE @q OR l.Need LIKE @q OR l.Interest LIKE @q OR l.Tags LIKE @q)");
                a["q"] = "%" + f.Search.Trim() + "%";
            }
            if (!string.IsNullOrEmpty(f.Status)) { w.Add("l.Status = @status"); a["status"] = f.Status; }
            if (!string.IsNullOrEmpty(f.LeadType)) { w.Add("l.LeadType = @type"); a["type"] = f.LeadType; }
            if (!string.IsNullOrEmpty(f.Source)) { w.Add("l.Source = @source"); a["source"] = f.Source; }
            if (!string.IsNullOrEmpty(f.Priority)) { w.Add("l.Priority = @priority"); a["priority"] = f.Priority; }
            if (f.AssignedTo.HasValue) { w.Add("l.AssignedTo = @assigned"); a["assigned"] = f.AssignedTo.Value; }
            if (f.Unassigned) w.Add("l.AssignedTo IS NULL");
            if (f.OpenOnly) w.Add("l.Status IN ('" + string.Join("','", Lists.OpenStatuses) + "')");
            if (f.OverdueOnly) { w.Add("l.NextFollowUp < @now AND l.Status IN ('" + string.Join("','", Lists.OpenStatuses) + "')"); a["now"] = DateTime.UtcNow; }
            if (f.From.HasValue) { w.Add("l.CreatedOn >= @from"); a["from"] = f.From.Value; }
            if (f.To.HasValue) { w.Add("l.CreatedOn < @to"); a["to"] = f.To.Value; }
            if (f.RestrictToUser.HasValue) { w.Add("(l.AssignedTo = @me OR l.AssignedTo IS NULL)"); a["me"] = f.RestrictToUser.Value; }
            return w.Count == 0 ? "" : " WHERE " + string.Join(" AND ", w);
        }

        public static List<Activity> Activities(int leadId)
        {
            return Db.Query(@"SELECT a.*, u.Name AS UserName FROM CrmActivities a LEFT JOIN CrmUsers u ON u.Id = a.UserId
                              WHERE a.LeadId = @leadId ORDER BY a.CreatedOn DESC, a.Id DESC", Activity.From, new { leadId });
        }

        /// <summary>Open follow-up tasks, optionally for one user (tasks they created or on leads assigned to them).</summary>
        public static List<Activity> FollowUps(int? userId, DateTime? dueBefore, int max = 200, DateTime? dueFrom = null)
        {
            var args = new Dictionary<string, object>();
            var sql = @"SELECT a.*, u.Name AS UserName, l.Name AS LeadName, l.Status AS LeadStatus, o.Name AS OwnerName FROM CrmActivities a
                        LEFT JOIN CrmUsers o ON o.Id = (SELECT AssignedTo FROM CrmLeads x WHERE x.Id = a.LeadId)
                        JOIN CrmLeads l ON l.Id = a.LeadId LEFT JOIN CrmUsers u ON u.Id = a.UserId
                        WHERE a.DueOn IS NOT NULL AND a.DoneOn IS NULL";
            if (userId.HasValue) { sql += " AND (l.AssignedTo = @uid OR (l.AssignedTo IS NULL AND a.UserId = @uid))"; args["uid"] = userId.Value; }
            if (dueBefore.HasValue) { sql += " AND a.DueOn < @before"; args["before"] = dueBefore.Value; }
            if (dueFrom.HasValue) { sql += " AND a.DueOn >= @after"; args["after"] = dueFrom.Value; }
            return Db.Query(sql + " ORDER BY a.DueOn" + Db.Page(0, max), Activity.From, args);
        }

        // ---- Update ------------------------------------------------------------------------------------------

        public static void Update(Lead l, int userId)
        {
            var old = Get(l.Id);
            Db.Exec(@"UPDATE CrmLeads SET UpdatedOn = @now, Name = @Name, Email = @Email, Phone = @Phone, Company = @Company, City = @City, Need = @Need,
                      Interest = @Interest, LeadType = @LeadType, Source = @Source, Status = @Status, Priority = @Priority, EstValue = @EstValue,
                      AssignedTo = @AssignedTo, NextFollowUp = @NextFollowUp, LostReason = @LostReason, Tags = @Tags WHERE Id = @Id",
                new Dictionary<string, object>
                {
                    { "now", DateTime.UtcNow }, { "Id", l.Id }, { "Name", l.Name }, { "Email", Blank(l.Email) }, { "Phone", Blank(l.Phone) }, { "Company", Blank(l.Company) },
                    { "City", Blank(l.City) }, { "Need", Blank(l.Need) }, { "Interest", Blank(l.Interest) }, { "LeadType", l.LeadType }, { "Source", l.Source },
                    { "Status", l.Status }, { "Priority", l.Priority }, { "EstValue", l.EstValue }, { "AssignedTo", l.AssignedTo }, { "NextFollowUp", l.NextFollowUp },
                    { "LostReason", Blank(l.LostReason) }, { "Tags", Blank(l.Tags) }
                });
            if (old == null) return;
            // A priority set by hand stays; automatic priority from the lead score no longer changes it.
            if (old.Priority != l.Priority) Db.Exec("UPDATE CrmLeads SET PriorityLocked = 1 WHERE Id = @id", new { id = l.Id });
            if (old.Status != l.Status || old.EstValue != l.EstValue || old.Email != l.Email || old.Phone != l.Phone) LeadScoring.Recalc(l.Id);
            if (old.Status != l.Status) AddActivity(l.Id, userId, "Status", old.Status + " → " + l.Status + (l.Status == "Lost" && !string.IsNullOrEmpty(l.LostReason) ? " (" + l.LostReason + ")" : ""), null);
            if (old.AssignedTo != l.AssignedTo) LogAssignment(l.Id, l.AssignedTo, userId);
        }

        public static void Assign(int leadId, int? toUser, int byUser)
        {
            Db.Exec("UPDATE CrmLeads SET AssignedTo = @to, UpdatedOn = @now WHERE Id = @id", new { to = toUser, now = DateTime.UtcNow, id = leadId });
            LogAssignment(leadId, toUser, byUser);
        }

        public static void SetStatus(int leadId, string status, int byUser)
        {
            var old = Get(leadId);
            if (old == null || old.Status == status || !Lists.Statuses.Contains(status)) return;
            Db.Exec("UPDATE CrmLeads SET Status = @status, UpdatedOn = @now WHERE Id = @id", new { status, now = DateTime.UtcNow, id = leadId });
            AddActivity(leadId, byUser, "Status", old.Status + " → " + status, null);
            LeadScoring.Recalc(leadId);
            if (status == "Won") { old.Status = status; Reviews.OnWon(old); }
        }

        private static void LogAssignment(int leadId, int? toUser, int byUser)
        {
            var who = toUser.HasValue ? Db.Scalar<string>("SELECT Name FROM CrmUsers WHERE Id = @id", new { id = toUser.Value }) : null;
            AddActivity(leadId, byUser, "Assign", who == null ? "Unassigned" : "Assigned to " + who, null);
            if (toUser.HasValue && toUser.Value != byUser) Mailer.NotifyAssigned(leadId, toUser.Value);
        }

        public static void Delete(int id) { Db.Exec("DELETE FROM CrmActivities WHERE LeadId = @id; DELETE FROM CrmLeads WHERE Id = @id", new { id }); }

        // ---- Activities and follow-ups ------------------------------------------------------------------------

        public static int AddActivity(int leadId, int? userId, string kind, string body, DateTime? dueOn)
        {
            var id = Db.Insert("INSERT INTO CrmActivities (LeadId, UserId, Kind, Body, CreatedOn, DueOn) VALUES (@leadId, @userId, @kind, @body, @now, @dueOn)",
                new { leadId, userId, kind, body = Util.Cut(body, 2000), now = DateTime.UtcNow, dueOn });
            if (kind != "System") Db.Exec("UPDATE CrmLeads SET UpdatedOn = @now WHERE Id = @id", new { now = DateTime.UtcNow, id = leadId });
            if (dueOn.HasValue) SyncNextFollowUp(leadId);
            // First human touch moves a new lead to Contacted.
            if (kind == "Call" || kind == "Email" || kind == "WhatsApp" || kind == "Meeting")
                Db.Exec("UPDATE CrmLeads SET Status = 'Contacted' WHERE Id = @id AND Status = 'New'", new { id = leadId });
            return id;
        }

        public static void CompleteFollowUp(int activityId, int userId)
        {
            var leadId = Db.Scalar<int>("SELECT LeadId FROM CrmActivities WHERE Id = @id", new { id = activityId });
            Db.Exec("UPDATE CrmActivities SET DoneOn = @now WHERE Id = @id AND DoneOn IS NULL", new { now = DateTime.UtcNow, id = activityId });
            if (leadId > 0) SyncNextFollowUp(leadId);
        }

        /// <summary>Moves an open follow-up to the same time a number of days later (or tomorrow if it is already late).</summary>
        public static void Snooze(int activityId, int days)
        {
            var r = Db.First("SELECT LeadId, DueOn FROM CrmActivities WHERE Id = @id AND DoneOn IS NULL", new { id = activityId });
            if (r == null) return;
            var due = r.Date("DueOn");
            var next = due < DateTime.UtcNow ? DateTime.UtcNow.Date.Add(due.TimeOfDay).AddDays(days) : due.AddDays(days);
            if (next < DateTime.UtcNow) next = next.AddDays(1);
            Db.Exec("UPDATE CrmActivities SET DueOn = @next WHERE Id = @id", new { next, id = activityId });
            SyncNextFollowUp(r.Int("LeadId"));
        }

        /// <summary>The lead's NextFollowUp mirrors its earliest open follow-up task.</summary>
        public static void SyncNextFollowUp(int leadId)
        {
            var next = Db.First("SELECT MIN(DueOn) AS Next FROM CrmActivities WHERE LeadId = @id AND DueOn IS NOT NULL AND DoneOn IS NULL", new { id = leadId });
            Db.Exec("UPDATE CrmLeads SET NextFollowUp = @next WHERE Id = @id", new { next = next == null ? null : next.DateN("Next"), id = leadId });
        }

        // ---- Export ------------------------------------------------------------------------------------------

        public static string Csv(LeadFilter f)
        {
            int total;
            var rows = List(f, 0, 100000, out total);
            var sb = new StringBuilder();
            sb.AppendLine("Id,Created (IST),Name,Phone,Email,Company,City,Type,Interest,Source,Channel,Status,Priority,Value (INR),Owner,Next follow-up (IST),Need,Tags");
            foreach (var l in rows)
                sb.AppendLine(string.Join(",", new object[] {
                    l.Id, Util.Ist(l.CreatedOn).ToString("yyyy-MM-dd HH:mm"), l.Name, l.Phone, l.Email, l.Company, l.City, l.LeadType, l.Interest, l.Source, l.Channel,
                    l.Status, l.Priority, l.EstValue, l.AssignedName, l.NextFollowUp.HasValue ? Util.Ist(l.NextFollowUp.Value).ToString("yyyy-MM-dd HH:mm") : "", l.Need, l.Tags
                }.Select(v => Util.CsvCell(Convert.ToString(v, CultureInfo.InvariantCulture)))));
            return sb.ToString();
        }

        // ---- Helpers -----------------------------------------------------------------------------------------

        public static string SourceFromKey(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "chatbot": return "Chatbot";
                case "finder": return "Solution finder";
                case "contact-form": return "Contact form";
                default: return Lists.Sources.FirstOrDefault(s => s.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? "Contact form";
            }
        }

        /// <summary>Sorts a lead into a vertical from the service, product or words it mentions.</summary>
        public static string Classify(string text)
        {
            var t = (text ?? "").ToLowerInvariant();
            if (Regex.IsMatch(t, @"ecomm|billing|pos\b|studio ai|yenetch office|leads crm|product")) return "Product";
            if (Regex.IsMatch(t, @"staff|augment|dedicated team|resourc|hire|hiring|talent|developer on contract|outsourc")) return "Talent";
            if (Regex.IsMatch(t, @"seo|social|ads|marketing|media|lead gen|content|influencer|brand|orm|google|meta|campaign|performance")) return "Marketing";
            if (Regex.IsMatch(t, @"web|app|software|crm|erp|develop|ui|ux|design|cloud|devops|ai|maintenance|it infra|website|mobile")) return "Development";
            return "General";
        }

        private static void SplitContact(string contact, Lead l)
        {
            var c = (contact ?? "").Trim();
            var email = Regex.Match(c, @"[^\s,;]+@[^\s,;]+\.[a-z]{2,}", RegexOptions.IgnoreCase);
            if (email.Success) l.Email = Util.Cut(email.Value, 160);
            var rest = email.Success ? c.Replace(email.Value, " ") : c;
            if (Util.Digits(rest).Length >= 7) l.Phone = Util.Cut(Regex.Replace(rest, @"[^\d+\s-]", "").Trim(), 40);
            if (l.Email == null && l.Phone == null) l.Phone = Util.Cut(c, 40);
        }

        private static object Blank(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
    }
}
