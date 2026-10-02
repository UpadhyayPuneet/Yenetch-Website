using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace Yenetch.Crm
{
    /// <summary>A job application sent from the Careers page. The CV file lives in App_Data/resumes (never served directly).</summary>
    public class JobApplication
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Job { get; set; }
        public string Category { get; set; }
        public decimal Experience { get; set; }
        public string City { get; set; }
        public string Notice { get; set; }
        public string Skills { get; set; }
        public string ResumeUrl { get; set; }
        public string Portfolio { get; set; }
        public string Note { get; set; }
        public string FileName { get; set; }
        public string StoredName { get; set; }
        public string ContentType { get; set; }
        public int Size { get; set; }
        public string Status { get; set; }
        public int Rating { get; set; }
        public string AdminNotes { get; set; }
        public string Ip { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public bool HasFile { get { return !string.IsNullOrEmpty(StoredName); } }
        public string FullPath { get { return Util.AppPath("App_Data/resumes/" + StoredName); } }
        public string ExperienceLabel { get { return Experience <= 0 ? "Fresher" : Experience.ToString("0.#") + (Experience == 1 ? " year" : " years"); } }
        public string SizeLabel { get { return Size >= 1024 * 1024 ? (Size / 1048576.0).ToString("0.0") + " MB" : Math.Max(1, Size / 1024) + " KB"; } }
    }

    public static class Applications
    {
        public static readonly string[] Statuses = { "New", "Shortlisted", "Interview", "Offered", "Hired", "Rejected" };
        public const int MaxBytes = 5 * 1024 * 1024;
        private static readonly Dictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", "application/pdf" }, { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
        };

        /// <summary>Why a CV file cannot be accepted, or null when it is fine.</summary>
        public static string FileProblem(HttpPostedFile f)
        {
            if (f == null || f.ContentLength == 0) return "The CV file is empty.";
            if (f.ContentLength > MaxBytes) return "The CV is larger than 5 MB.";
            if (!Types.ContainsKey(Path.GetExtension(f.FileName ?? ""))) return "Attach the CV as a PDF or Word file.";
            return null;
        }

        public static int Create(JobApplication a, HttpPostedFile cv)
        {
            a.Status = "New";
            a.CreatedOn = a.UpdatedOn = DateTime.UtcNow;
            if (cv != null && cv.ContentLength > 0)
            {
                var ext = Path.GetExtension(cv.FileName).ToLowerInvariant();
                var name = Path.GetFileName(cv.FileName);
                if (name.Length > 150) name = name.Substring(0, 140) + ext;
                a.StoredName = DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Util.NewId().Substring(0, 12) + ext;
                Directory.CreateDirectory(Util.AppPath("App_Data/resumes"));
                cv.SaveAs(a.FullPath);
                a.FileName = name; a.ContentType = Types[ext]; a.Size = cv.ContentLength;
            }
            a.Id = Db.Insert(@"INSERT INTO CrmApplications (Name, Email, Phone, Job, Category, Experience, City, Notice, Skills, ResumeUrl, Portfolio, Note,
                FileName, StoredName, ContentType, Size, Status, Rating, Ip, CreatedOn, UpdatedOn)
                VALUES (@Name, @Email, @Phone, @Job, @Category, @Experience, @City, @Notice, @Skills, @ResumeUrl, @Portfolio, @Note,
                @FileName, @StoredName, @ContentType, @Size, @Status, 0, @Ip, @CreatedOn, @UpdatedOn)", a);
            NotifyTeam(a);
            Confirm(a);
            return a.Id;
        }

        public static JobApplication Get(int id) { var r = Db.First("SELECT * FROM CrmApplications WHERE Id = @id", new { id }); return r == null ? null : Map(r); }

        /// <summary>Filtered, sorted page of applications. Search covers name, email, phone, job, skills, city and notes.</summary>
        public static List<JobApplication> List(string search, string status, string category, string job, decimal minExp, string sort, int offset, int count, out int total)
        {
            var w = new List<string>();
            var a = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                w.Add("(Name LIKE @q OR Email LIKE @q OR Phone LIKE @q OR Job LIKE @q OR Skills LIKE @q OR City LIKE @q OR Note LIKE @q OR AdminNotes LIKE @q)");
                a["q"] = "%" + search.Trim() + "%";
            }
            if (!string.IsNullOrEmpty(status)) { w.Add("Status = @status"); a["status"] = status; }
            if (!string.IsNullOrEmpty(category)) { w.Add("Category = @category"); a["category"] = category; }
            if (!string.IsNullOrEmpty(job)) { w.Add("Job = @job"); a["job"] = job; }
            if (minExp > 0) { w.Add("Experience >= @minExp"); a["minExp"] = minExp; }
            var where = w.Count == 0 ? "" : " WHERE " + string.Join(" AND ", w);
            string order;
            switch (sort)
            {
                case "oldest": order = "CreatedOn ASC"; break;
                case "exp": order = "Experience DESC, CreatedOn DESC"; break;
                case "exp-asc": order = "Experience ASC, CreatedOn DESC"; break;
                case "rating": order = "Rating DESC, CreatedOn DESC"; break;
                case "name": order = "Name ASC"; break;
                default: order = "CreatedOn DESC"; break;
            }
            total = Db.Scalar<int>("SELECT COUNT(*) FROM CrmApplications" + where, a);
            return Db.Query("SELECT * FROM CrmApplications" + where + " ORDER BY " + order + Db.Page(offset, count), Map, a);
        }

        public static Dictionary<string, int> StatusCounts()
        {
            return Db.Rows("SELECT Status, COUNT(*) AS N FROM CrmApplications GROUP BY Status").ToDictionary(r => r.Str("Status"), r => r.Int("N"));
        }

        public static List<string> Distinct(string column)
        {
            if (column != "Category" && column != "Job") return new List<string>();
            return Db.Rows("SELECT DISTINCT " + column + " AS V FROM CrmApplications WHERE " + column + " IS NOT NULL ORDER BY " + column).Select(r => r.Str("V")).ToList();
        }

        public static void Update(int id, string status, int rating, string notes)
        {
            if (!Statuses.Contains(status)) status = "New";
            Db.Exec("UPDATE CrmApplications SET Status = @status, Rating = @rating, AdminNotes = @notes, UpdatedOn = @now WHERE Id = @id",
                new { id, status, rating = Math.Max(0, Math.Min(5, rating)), notes = Util.Cut(notes, 8000), now = DateTime.UtcNow });
        }

        public static void SetStatus(int id, string status)
        {
            if (!Statuses.Contains(status)) return;
            Db.Exec("UPDATE CrmApplications SET Status = @status, UpdatedOn = @now WHERE Id = @id", new { id, status, now = DateTime.UtcNow });
        }

        public static void AddNote(int id, string line)
        {
            var a = Get(id); if (a == null) return;
            var notes = (string.IsNullOrWhiteSpace(a.AdminNotes) ? "" : a.AdminNotes.TrimEnd() + "\n") + line;
            Db.Exec("UPDATE CrmApplications SET AdminNotes = @notes, UpdatedOn = @now WHERE Id = @id", new { id, notes = Util.Cut(notes, 8000), now = DateTime.UtcNow });
        }

        public static void Delete(int id)
        {
            var a = Get(id); if (a == null) return;
            if (a.HasFile && File.Exists(a.FullPath)) File.Delete(a.FullPath);
            Db.Exec("DELETE FROM CrmApplications WHERE Id = @id", new { id });
        }

        // ------------------------------------------------------------------ email

        /// <summary>Where application alerts go: the careers address from Email settings, else the lead alert address.</summary>
        public static string Inbox
        {
            get
            {
                string to = null;
                try { to = Yenetch.Data.Settings.Get("mail.careersTo"); } catch { }
                return string.IsNullOrWhiteSpace(to) ? Mailer.LeadInbox : to;
            }
        }

        private static void NotifyTeam(JobApplication a)
        {
            var inbox = Inbox;
            if (string.IsNullOrEmpty(inbox)) return;
            try
            {
                var rows = Mailer.Row("Name", a.Name) + Mailer.Row("Email", a.Email) + Mailer.Row("Phone", a.Phone) + Mailer.Row("Role", a.Job)
                         + Mailer.Row("Team", a.Category) + Mailer.Row("Experience", a.ExperienceLabel) + Mailer.Row("City", a.City) + Mailer.Row("Notice period", a.Notice)
                         + Mailer.Row("Skills", a.Skills) + Mailer.Row("CV link", a.ResumeUrl) + Mailer.Row("Portfolio / LinkedIn", a.Portfolio)
                         + Mailer.Row("CV file", a.HasFile ? a.FileName + " (" + a.SizeLabel + ", attached)" : null) + Mailer.Row("Note", a.Note);
                var body = Mailer.Heading("New application: " + Util.H(a.Job)) + "<p style=\"margin:0 0 16px;color:#48484e\">" + Util.H(a.Name) + " applied through the Careers page.</p>"
                         + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%\">" + rows + "</table>"
                         + Mailer.Button("Open application", Mailer.SiteUrl + "/admin/applications/" + a.Id);
                Mailer.Send(inbox, "Application: " + a.Job + " - " + a.Name, Mailer.Wrap("New application from " + a.Name, body, null), customise: m =>
                {
                    if (Newsletter.IsEmail(a.Email)) { m.ReplyToList.Clear(); m.ReplyToList.Add(new MailAddress(a.Email, a.Name)); }
                    if (a.HasFile && File.Exists(a.FullPath)) m.Attachments.Add(new Attachment(a.FullPath, a.ContentType) { Name = a.FileName });
                });
            }
            catch (Exception ex) { Mailer.Log("application " + a.Id, ex); }
        }

        private static void Confirm(JobApplication a)
        {
            if (!Newsletter.IsEmail(a.Email)) return;
            try
            {
                var co = Yenetch.Data.SiteContent.Current.Company ?? new Yenetch.Models.Company();
                var first = (a.Name ?? "").Trim().Split(' ')[0];
                var body = Mailer.Heading("Thanks for applying, " + Util.H(first) + ".")
                         + "<p style=\"margin:0 0 16px\">We have your application for <b>" + Util.H(a.Job) + "</b>. Our team reads every one, and if your profile is a fit we will contact you within a week to set up a first conversation.</p>"
                         + "<p style=\"margin:0 0 16px;color:#48484e\">You don't need to apply again. To add anything, just reply to this email.</p>"
                         + Mailer.Button("Learn how we work", Mailer.SiteUrl + "/about")
                         + "<p style=\"margin:24px 0 0;color:#48484e\">Team " + Util.H(co.Name ?? "Yenetch") + "</p>";
                Mailer.Send(a.Email, "We received your application for " + a.Job, Mailer.Wrap("Thanks for applying to " + (co.Name ?? "Yenetch") + ".", body, null));
            }
            catch (Exception ex) { Mailer.Log("application confirm " + a.Id, ex); }
        }

        /// <summary>Emails the candidate from the admin. Returns null on success, or the error.</summary>
        public static string EmailCandidate(JobApplication a, string subject, string message, CrmUser from)
        {
            if (!Newsletter.IsEmail(a.Email)) return "This candidate has no valid email address.";
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message)) return "Write a subject and a message.";
            try
            {
                var paras = string.Join("", message.Trim().Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => "<p style=\"margin:0 0 16px\">" + Util.H(p.Trim()).Replace("\r\n", "<br>").Replace("\n", "<br>") + "</p>"));
                Mailer.Send(a.Email, subject.Trim(), Mailer.Wrap(Util.Cut(message.Trim(), 90), paras, null), customise: m =>
                {
                    if (from != null && Newsletter.IsEmail(from.Email)) { m.ReplyToList.Clear(); m.ReplyToList.Add(new MailAddress(from.Email, from.Name)); }
                });
                AddNote(a.Id, DateTime.UtcNow.ToString("dd MMM yyyy") + " · " + (from != null ? from.Name : "Admin") + " emailed: " + subject.Trim());
                return null;
            }
            catch (Exception ex) { Mailer.Log("application email " + a.Id, ex); return ex.GetBaseException().Message; }
        }

        private static JobApplication Map(Row r)
        {
            return new JobApplication
            {
                Id = r.Int("Id"), Name = r.Str("Name"), Email = r.Str("Email"), Phone = r.Str("Phone"), Job = r.Str("Job"), Category = r.Str("Category"),
                Experience = r.DecN("Experience") ?? 0, City = r.Str("City"), Notice = r.Str("Notice"), Skills = r.Str("Skills"), ResumeUrl = r.Str("ResumeUrl"),
                Portfolio = r.Str("Portfolio"), Note = r.Str("Note"), FileName = r.Str("FileName"), StoredName = r.Str("StoredName"), ContentType = r.Str("ContentType"),
                Size = r.Int("Size"), Status = r.Str("Status") ?? "New", Rating = r.Int("Rating"), AdminNotes = r.Str("AdminNotes"), Ip = r.Str("Ip"),
                CreatedOn = r.Date("CreatedOn"), UpdatedOn = r.Date("UpdatedOn")
            };
        }
    }
}
