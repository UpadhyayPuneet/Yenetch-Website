using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Hosting;

namespace Yenetch.Crm
{
    public class Subscriber
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public string Token { get; set; }
        public string VisitorId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UnsubscribedOn { get; set; }

        internal static Subscriber From(Row r)
        {
            return new Subscriber { Id = r.Int("Id"), Email = r.Str("Email"), Name = r.Str("Name"), Status = r.Str("Status"), Source = r.Str("Source"), Token = r.Str("Token"),
                                    VisitorId = r.Str("VisitorId"), CreatedOn = r.Date("CreatedOn"), UnsubscribedOn = r.DateN("UnsubscribedOn") };
        }
    }

    public class Campaign
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Preheader { get; set; }
        public string BodyHtml { get; set; }
        public string Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime? SentOn { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public int Recipients { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public string AuthorName { get; set; }
        public int Progress { get { return Recipients == 0 ? 0 : (SentCount + FailedCount) * 100 / Recipients; } }

        internal static Campaign From(Row r)
        {
            return new Campaign { Id = r.Int("Id"), Subject = r.Str("Subject"), Preheader = r.Str("Preheader"), BodyHtml = r.Str("BodyHtml"), Status = r.Str("Status"),
                                  CreatedOn = r.Date("CreatedOn"), UpdatedOn = r.Date("UpdatedOn"), SentOn = r.DateN("SentOn"), ScheduledFor = r.DateN("ScheduledFor"), Recipients = r.Int("Recipients"),
                                  SentCount = r.Int("SentCount"), FailedCount = r.Int("FailedCount"), AuthorName = r.Str("AuthorName") };
        }
    }

    public static class Newsletter
    {
        private static readonly Regex EmailRx = new Regex(@"^[^\s@<>,;]+@[^\s@<>,;]+\.[a-z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsEmail(string s) { return !string.IsNullOrEmpty(s) && s.Length <= 160 && EmailRx.IsMatch(s.Trim()); }

        // ---- Subscribers -------------------------------------------------------------------------------------

        /// <summary>Adds or re-activates a subscriber. Returns false when the address is invalid.</summary>
        public static bool Subscribe(string email, string name, string source, string visitorId)
        {
            if (!IsEmail(email)) return false;
            email = email.Trim().ToLowerInvariant();
            var existing = Db.First("SELECT Id, Status FROM NewsSubscribers WHERE Email = @email", new { email });
            if (existing == null)
            {
                try
                {
                    var id = Db.Insert("INSERT INTO NewsSubscribers (Email, Name, Status, Source, Token, VisitorId, CreatedOn) VALUES (@email, @name, 'Active', @source, @token, @vid, @now)",
                        new { email, name = Util.Cut(name, 120), source = Util.Cut(source, 40), token = Util.NewId(), vid = visitorId, now = DateTime.UtcNow });
                    Automations.Enroll("welcome", email, name, id);
                }
                catch { /* double submit: already inserted */ }
            }
            else if (existing.Str("Status") != "Active")
                Db.Exec("UPDATE NewsSubscribers SET Status = 'Active', UnsubscribedOn = NULL WHERE Id = @id", new { id = existing.Int("Id") });
            if (!string.IsNullOrEmpty(visitorId)) Analytics.RecordServerEvent(visitorId, "newsletter", source, null);
            return true;
        }

        public static Subscriber ByToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length > 40) return null;
            var r = Db.First("SELECT * FROM NewsSubscribers WHERE Token = @token", new { token });
            return r == null ? null : Subscriber.From(r);
        }

        public static Subscriber ByEmail(string email)
        {
            var r = Db.First("SELECT * FROM NewsSubscribers WHERE Email = @email", new { email = (email ?? "").Trim().ToLowerInvariant() });
            return r == null ? null : Subscriber.From(r);
        }

        public static void Unsubscribe(int id)
        {
            Db.Exec("UPDATE NewsSubscribers SET Status = 'Unsubscribed', UnsubscribedOn = @now WHERE Id = @id AND Status = 'Active'", new { id, now = DateTime.UtcNow });
        }

        public static void Resubscribe(int id) { Db.Exec("UPDATE NewsSubscribers SET Status = 'Active', UnsubscribedOn = NULL WHERE Id = @id", new { id }); }
        public static void Delete(int id) { Db.Exec("DELETE FROM NewsSubscribers WHERE Id = @id", new { id }); }

        public static List<Subscriber> List(string search, string status, int offset, int count, out int total)
        {
            var w = new List<string>();
            var a = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(search)) { w.Add("(Email LIKE @q OR Name LIKE @q)"); a["q"] = "%" + search.Trim() + "%"; }
            if (!string.IsNullOrEmpty(status)) { w.Add("Status = @status"); a["status"] = status; }
            var where = w.Count == 0 ? "" : " WHERE " + string.Join(" AND ", w);
            total = Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers" + where, a);
            return Db.Query("SELECT * FROM NewsSubscribers" + where + " ORDER BY CreatedOn DESC" + Db.Page(offset, count), Subscriber.From, a);
        }

        public static int ActiveCount() { return Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers WHERE Status = 'Active'"); }

        /// <summary>Adds every address found in pasted text or a CSV. Returns (added, skipped).</summary>
        public static Tuple<int, int> Import(string text, string source)
        {
            int added = 0, skipped = 0;
            foreach (Match m in Regex.Matches(text ?? "", @"[^\s@<>,;""']+@[^\s@<>,;""']+\.[a-z]{2,}", RegexOptions.IgnoreCase))
            {
                var email = m.Value.ToLowerInvariant();
                if (Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers WHERE Email = @email", new { email }) > 0) { skipped++; continue; }
                if (Subscribe(email, null, source, null)) added++; else skipped++;
            }
            return Tuple.Create(added, skipped);
        }

        public static string Csv()
        {
            var sb = new StringBuilder("Email,Name,Status,Source,Subscribed (IST),Unsubscribed (IST)\r\n");
            foreach (var s in Db.Query("SELECT * FROM NewsSubscribers ORDER BY CreatedOn DESC", Subscriber.From))
                sb.Append(string.Join(",", new[] { s.Email, s.Name, s.Status, s.Source, Util.Ist(s.CreatedOn).ToString("yyyy-MM-dd HH:mm"),
                    s.UnsubscribedOn.HasValue ? Util.Ist(s.UnsubscribedOn.Value).ToString("yyyy-MM-dd HH:mm") : "" }.Select(Util.CsvCell))).Append("\r\n");
            return sb.ToString();
        }

        // ---- Campaigns ---------------------------------------------------------------------------------------

        private const string CampaignSelect = "SELECT c.*, u.Name AS AuthorName FROM NewsCampaigns c LEFT JOIN CrmUsers u ON u.Id = c.CreatedBy";

        public static List<Campaign> Campaigns() { return Db.Query(CampaignSelect + " ORDER BY c.CreatedOn DESC", Crm.Campaign.From); }
        public static Campaign Campaign(int id) { var r = Db.First(CampaignSelect + " WHERE c.Id = @id", new { id }); return r == null ? null : Crm.Campaign.From(r); }

        public static int SaveCampaign(int id, string subject, string preheader, string bodyHtml, int userId)
        {
            var now = DateTime.UtcNow;
            if (id == 0)
                return Db.Insert("INSERT INTO NewsCampaigns (Subject, Preheader, BodyHtml, Status, CreatedBy, CreatedOn, UpdatedOn) VALUES (@subject, @preheader, @body, 'Draft', @userId, @now, @now)",
                    new { subject, preheader, body = bodyHtml, userId, now });
            Db.Exec("UPDATE NewsCampaigns SET Subject = @subject, Preheader = @preheader, BodyHtml = @body, UpdatedOn = @now WHERE Id = @id AND Status = 'Draft'",
                new { subject, preheader, body = bodyHtml, now, id });
            return id;
        }

        public static void DeleteCampaign(int id) { Db.Exec("DELETE FROM NewsCampaigns WHERE Id = @id AND Status = 'Draft'", new { id }); }

        public static string Render(Campaign c, Subscriber s)
        {
            var unsub = Mailer.SiteUrl + "/newsletter/unsubscribe?t=" + (s == null ? "preview" : s.Token);
            var body = (c.BodyHtml ?? "").Replace("{{name}}", s == null || string.IsNullOrEmpty(s.Name) ? "there" : Util.H(s.Name.Split(' ')[0]))
                                         .Replace("{{unsubscribe}}", unsub);
            return Mailer.Wrap(c.Preheader ?? c.Subject, body, unsub);
        }

        public static void SendTest(int campaignId, string to)
        {
            var c = Campaign(campaignId);
            Mailer.Send(to, "[Test] " + c.Subject, Render(c, null));
        }

        /// <summary>Sends now (or at the scheduled time) through the queue in NewsSender. False when the campaign is not a draft.</summary>
        public static bool StartSending(int campaignId) { return NewsSender.Start(campaignId, null); }
    }
}
