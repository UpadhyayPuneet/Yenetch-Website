using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Yenetch.Data;

namespace Yenetch.Crm
{
    /// <summary>
    /// The daily summary email. Every day at the chosen time (IST) admins and managers get the last 24 hours: new leads,
    /// today's calls, follow-ups due, leads going cold, website audits, job applications, subscribers and visitors.
    /// Sales users can get their own version with only the leads assigned to them. Settings live in /admin/email.
    /// </summary>
    public static class DailyDigest
    {
        private static readonly object Lock = new object();

        public static bool Enabled { get { return Settings.Get("digest.enabled") != "0"; } }
        public static int Hour { get { int h; return int.TryParse(Settings.Get("digest.hour"), out h) && h >= 0 && h <= 23 ? h : 9; } }
        public static bool ForSales { get { return Settings.Get("digest.sales") != "0"; } }
        /// <summary>Extra addresses (comma or line separated) that get the full team summary.</summary>
        public static string Extra { get { return Settings.Get("digest.extra") ?? ""; } }
        public static string LastSent { get { return Settings.Get("digest.lastSent"); } }

        public static void Save(bool enabled, int hour, bool forSales, string extra)
        {
            Settings.Set("digest.enabled", enabled ? "1" : "0");
            Settings.Set("digest.hour", Math.Max(0, Math.Min(23, hour)).ToString(CultureInfo.InvariantCulture));
            Settings.Set("digest.sales", forSales ? "1" : "0");
            Settings.Set("digest.extra", string.Join(", ", Emails(extra)));
        }

        public static List<string> Emails(string text)
        {
            return (text ?? "").Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().ToLowerInvariant())
                .Where(Newsletter.IsEmail).Distinct().ToList();
        }

        /// <summary>Called every minute. Sends once per IST day, at or after the chosen hour.</summary>
        public static void Tick()
        {
            try
            {
                if (!Enabled) return;
                var ist = Util.Ist(DateTime.UtcNow);
                if (ist.Hour < Hour) return;
                var today = ist.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                lock (Lock)
                {
                    if (LastSent == today) return;
                    // Mark first: a restart or a slow mail server must never send the summary twice.
                    Settings.Set("digest.lastSent", today);
                }
                SendAll();
            }
            catch (Exception ex) { Mailer.Log("daily summary", ex); }
        }

        private class Person { public int? UserId; public string Email; public bool Full; }

        private static List<Person> Recipients()
        {
            var list = new List<Person>();
            foreach (var r in Db.Rows("SELECT Id, Email, Role FROM CrmUsers WHERE IsActive = 1"))
            {
                var role = r.Str("Role");
                if (role == "Sales" && !ForSales) continue;
                list.Add(new Person { UserId = r.Int("Id"), Email = r.Str("Email"), Full = role != "Sales" });
            }
            foreach (var e in Emails(Extra))
                if (!list.Any(p => string.Equals(p.Email, e, StringComparison.OrdinalIgnoreCase))) list.Add(new Person { Email = e, Full = true });
            return list.Where(p => Newsletter.IsEmail(p.Email)).ToList();
        }

        /// <summary>Sends the summary to everyone it is set up for. Returns how many were sent.</summary>
        public static int SendAll()
        {
            var sent = 0;
            var now = DateTime.UtcNow;
            string fullHtml = null, fullSubject = null;
            foreach (var p in Recipients())
            {
                try
                {
                    string subject, html;
                    if (p.Full)
                    {
                        if (fullHtml == null) fullHtml = Build(null, now, out fullSubject);
                        html = fullHtml; subject = fullSubject;
                    }
                    else html = Build(p.UserId, now, out subject);
                    if (html == null) continue; // a sales user with nothing on today
                    Mailer.Send(p.Email, subject, html);
                    sent++;
                }
                catch (Exception ex) { Mailer.Log("daily summary to " + p.Email, ex); }
            }
            return sent;
        }

        /// <summary>Sends the summary to one address now (the Send test button). userId: a sales user's version, or null for the team's.</summary>
        public static string SendTest(string to, int? userId)
        {
            if (!Newsletter.IsEmail(to)) return "Enter a valid email address.";
            try
            {
                string subject;
                var html = Build(userId, DateTime.UtcNow, out subject, true);
                Mailer.Send(to.Trim(), subject, html);
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }

        // ---- Content -----------------------------------------------------------------------------------------

        /// <summary>The summary email. userId: only that user's leads (sales), or null for the whole team. Returns null when a
        /// personal summary would be empty (unless always is set).</summary>
        public static string Build(int? userId, DateTime now, out string subject, bool always = false)
        {
            var since = now.AddHours(-24);
            var todayIst = Util.Ist(now).Date;
            var dayStart = Util.FromIst(todayIst);
            var dayEnd = dayStart.AddDays(1);
            var open = "('" + string.Join("','", Lists.OpenStatuses) + "')";
            var mine = userId.HasValue ? " AND l.AssignedTo = @uid" : "";
            var args = new { since, now, dayStart, dayEnd, uid = userId ?? 0, cold = now.AddDays(-7) };

            var newLeads = Db.Rows("SELECT l.Id, l.Name, l.Source, l.Interest, l.Priority, u.Name AS Owner FROM CrmLeads l LEFT JOIN CrmUsers u ON u.Id = l.AssignedTo WHERE l.CreatedOn >= @since" + mine + " ORDER BY l.Id DESC", args);
            var due = Db.Rows("SELECT l.Id, l.Name, l.Status, l.NextFollowUp, u.Name AS Owner FROM CrmLeads l LEFT JOIN CrmUsers u ON u.Id = l.AssignedTo WHERE l.NextFollowUp < @dayEnd AND l.Status IN " + open + mine + " ORDER BY l.NextFollowUp", args);
            var cold = Db.Rows("SELECT l.Id, l.Name, l.Status, l.UpdatedOn, u.Name AS Owner FROM CrmLeads l LEFT JOIN CrmUsers u ON u.Id = l.AssignedTo WHERE l.Status IN " + open + " AND l.UpdatedOn < @cold AND l.NextFollowUp IS NULL" + mine + " ORDER BY l.UpdatedOn", args);
            var calls = Db.Rows("SELECT b.Id, b.Name, b.Topic, b.Mode, b.StartOn, b.LeadId FROM Bookings b" + (userId.HasValue ? " JOIN CrmLeads l ON l.Id = b.LeadId" : "") + " WHERE b.Status = 'Confirmed' AND b.StartOn >= @dayStart AND b.StartOn < @dayEnd" + mine + " ORDER BY b.StartOn", args);

            var ist = Util.Ist(now);
            var greeting = ist.Hour < 12 ? "Good morning" : ist.Hour < 17 ? "Good afternoon" : "Good evening";
            var sb = new StringBuilder();
            sb.Append(Mailer.Heading(greeting + ". Here is your day."));
            sb.Append("<p style=\"margin:0 0 20px;color:#48484e\">" + Util.H(ist.ToString("dddd d MMMM yyyy", Util.India)) + (userId.HasValue ? " · your leads only" : " · the last 24 hours") + "</p>");

            // Numbers
            var tiles = new List<Tuple<string, string>> { Tuple.Create(newLeads.Count.ToString(), "new leads"), Tuple.Create(calls.Count.ToString(), "calls today"), Tuple.Create(due.Count.ToString(), "follow-ups due") };
            if (!userId.HasValue)
            {
                tiles.Add(Tuple.Create(Util.Num(Stats.Overview(since, now).Visitors), "website visitors"));
                tiles.Add(Tuple.Create(Db.Scalar<int>("SELECT COUNT(*) FROM WebAudits WHERE CreatedOn >= @since", args).ToString(), "website audits"));
                tiles.Add(Tuple.Create(Db.Scalar<int>("SELECT COUNT(*) FROM CrmApplications WHERE CreatedOn >= @since", args).ToString(), "job applications"));
                tiles.Add(Tuple.Create(Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers WHERE Status = 'Active' AND CreatedOn >= @since", args).ToString(), "new subscribers"));
                tiles.Add(Tuple.Create(Db.Scalar<int>("SELECT COUNT(*) FROM ReviewRequests WHERE ClickedOn >= @since", args).ToString(), "review links opened"));
            }
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:separate;border-spacing:0 8px;margin:0 0 12px\">");
            for (var i = 0; i < tiles.Count; i += 4)
            {
                sb.Append("<tr>");
                for (var j = i; j < i + 4; j++)
                {
                    if (j < tiles.Count)
                        sb.Append("<td width=\"25%\" style=\"padding:0 4px\"><div style=\"background:#f5f5f7;border-radius:14px;padding:12px 8px;text-align:center\"><div style=\"font-size:24px;font-weight:700;color:" + (j == 0 && newLeads.Count > 0 ? "#0066FF" : "#1d1d1f") + "\">" + Util.H(tiles[j].Item1) + "</div><div style=\"font-size:12px;color:#86868b;line-height:1.3\">" + Util.H(tiles[j].Item2) + "</div></div></td>");
                    else sb.Append("<td width=\"25%\"></td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");

            var site = Mailer.SiteUrl;
            Func<int, string> lead = id => site + "/admin/leads/" + id;
            if (calls.Count > 0)
                Section(sb, "Calls today", calls.Select(c => Line(Util.Time(c.Date("StartOn")) + " · " + c.Str("Name"), Join(c.Str("Topic"), c.Str("Mode")),
                    c.IntN("LeadId").HasValue ? lead(c.IntN("LeadId").Value) : site + "/admin/bookings/" + c.Int("Id"))));
            if (newLeads.Count > 0)
                Section(sb, "New leads", newLeads.Take(12).Select(l => Line(l.Str("Name") + (l.Str("Priority") == "Hot" ? " (hot)" : ""), Join(l.Str("Source"), l.Str("Interest"), userId.HasValue ? null : (l.Str("Owner") ?? "Unassigned")), lead(l.Int("Id")))),
                    newLeads.Count > 12 ? "And " + (newLeads.Count - 12) + " more in the admin." : null);
            if (due.Count > 0)
                Section(sb, "Follow-ups due", due.Take(12).Select(l =>
                {
                    var when = l.Date("NextFollowUp");
                    var late = when < dayStart ? "Overdue since " + Util.Date(when) : "Today at " + Util.Time(when);
                    return Line(l.Str("Name"), Join(late, l.Str("Status"), userId.HasValue ? null : l.Str("Owner")), lead(l.Int("Id")), when < dayStart);
                }), due.Count > 12 ? "And " + (due.Count - 12) + " more in the admin." : null);
            if (cold.Count > 0)
                Section(sb, "Going cold", cold.Take(6).Select(l => Line(l.Str("Name"), Join("No update for " + (int)(now - l.Date("UpdatedOn")).TotalDays + " days", l.Str("Status"), userId.HasValue ? null : l.Str("Owner")), lead(l.Int("Id")))),
                    "Open leads with no follow-up date and no change in a week. Call them, set a follow-up or close them.");

            if (!userId.HasValue)
            {
                var audits = Db.Rows("SELECT Id, FinalUrl, Url, Score, Email FROM WebAudits WHERE CreatedOn >= @since ORDER BY Id DESC", args);
                if (audits.Count > 0)
                    Section(sb, "Website audits", audits.Take(8).Select(a => Line(Host(a.Str("FinalUrl") ?? a.Str("Url")) + " · " + a.Int("Score") + "/100", a.Str("Email"), site + "/admin/audits/" + a.Int("Id"))));
                var apps = Db.Rows("SELECT Id, Name, Job, Experience, City FROM CrmApplications WHERE CreatedOn >= @since ORDER BY Id DESC", args);
                if (apps.Count > 0)
                    Section(sb, "Job applications", apps.Take(8).Select(a => Line(a.Str("Name"), Join(a.Str("Job"), a.Dbl("Experience") > 0 ? a.Dbl("Experience").ToString("0.#", CultureInfo.InvariantCulture) + " yrs" : null, a.Str("City")), site + "/admin/applications/" + a.Int("Id"))));
                var autos = Db.Scalar<int>("SELECT COUNT(*) FROM AutoQueue WHERE LastSentOn >= @since", args);
                if (autos > 0) sb.Append("<p style=\"margin:16px 0 0;font-size:14px;color:#48484e\">Automatic emails sent: <b>" + autos + "</b>.</p>");
            }

            var empty = newLeads.Count == 0 && calls.Count == 0 && due.Count == 0 && cold.Count == 0;
            if (empty && userId.HasValue && !always) { subject = null; return null; }
            if (empty) sb.Append("<p style=\"margin:8px 0 0;color:#48484e\">Nothing is waiting on you today. A good day to check in with past clients or ask happy ones for a Google review.</p>");
            sb.Append(Mailer.Button("Open the dashboard", site + "/admin"));
            sb.Append("<p style=\"margin:20px 0 0;font-size:12px;color:#86868b\">Sent every day at " + new DateTime(2000, 1, 1, Hour, 0, 0).ToString("h tt", Util.India) + " IST. Change it or switch it off in Admin, Email settings.</p>");

            var parts = new List<string>();
            if (newLeads.Count > 0) parts.Add(newLeads.Count + " new lead" + (newLeads.Count == 1 ? "" : "s"));
            if (calls.Count > 0) parts.Add(calls.Count + " call" + (calls.Count == 1 ? "" : "s") + " today");
            if (due.Count > 0) parts.Add(due.Count + " follow-up" + (due.Count == 1 ? "" : "s") + " due");
            subject = "Daily summary: " + (parts.Count > 0 ? string.Join(", ", parts) : "all clear") + " · " + ist.ToString("d MMM", Util.India);
            return Mailer.Wrap(subject, sb.ToString(), null);
        }

        private static string Host(string url) { Uri u; return Uri.TryCreate(url ?? "", UriKind.Absolute, out u) ? u.Host.Replace("www.", "") : url; }

        private static string Join(params string[] parts) { return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p))); }

        private static string Line(string title, string meta, string url, bool alert = false)
        {
            return "<tr><td style=\"padding:10px 0;border-bottom:1px solid #f0f0f2\"><a href=\"" + System.Web.HttpUtility.HtmlAttributeEncode(url) + "\" style=\"color:#1d1d1f;text-decoration:none;font-weight:600;font-size:15px\">" + Util.H(title) + "</a>"
                 + (string.IsNullOrEmpty(meta) ? "" : "<div style=\"font-size:13px;color:" + (alert ? "#D93025" : "#86868b") + ";margin-top:2px\">" + Util.H(meta) + "</div>") + "</td></tr>";
        }

        private static void Section(StringBuilder sb, string title, IEnumerable<string> lines, string note = null)
        {
            sb.Append("<h2 style=\"font-size:17px;margin:24px 0 4px;color:#1d1d1f\">" + Util.H(title) + "</h2>");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\">" + string.Concat(lines) + "</table>");
            if (!string.IsNullOrEmpty(note)) sb.Append("<p style=\"margin:6px 0 0;font-size:13px;color:#86868b\">" + Util.H(note) + "</p>");
        }
    }
}
