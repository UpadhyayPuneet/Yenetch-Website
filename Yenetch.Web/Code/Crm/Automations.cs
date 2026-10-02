using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class AutoStep
    {
        /// <summary>Wait after the previous email (or after joining, for the first one).</summary>
        public int DelayHours { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class AutoSeries
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Who { get; set; }
        public bool Enabled { get; set; }
        public List<AutoStep> Steps { get; set; }
    }

    /// <summary>
    /// Automatic email series, edited in /admin/automations:
    ///   welcome  new newsletter subscribers;
    ///   lead     new enquiries that left an email (stops when the lead is won, lost or marked junk);
    ///   review   clients marked Won, asking for a Google review (stops once they click the review link).
    /// People are queued in AutoQueue and the minute timer (NewsSender.Tick) sends what is due, inside the hourly email limit.
    /// Every email uses the site's email layout and carries a link to stop these emails.
    /// </summary>
    public static class Automations
    {
        public static readonly string[] Keys = { "welcome", "lead", "review" };
        public const int MaxSteps = 5;

        // ---- Settings ----------------------------------------------------------------------------------------

        public static AutoSeries Get(string key)
        {
            if (!Keys.Contains(key)) return null;
            AutoSeries s = null;
            try { var json = Settings.Get("auto." + key); if (!string.IsNullOrEmpty(json)) s = new JavaScriptSerializer().Deserialize<AutoSeries>(json); }
            catch { }
            var d = Default(key);
            if (s == null) return d;
            s.Key = key; s.Name = d.Name; s.Who = d.Who;
            if (s.Steps == null) s.Steps = new List<AutoStep>();
            return s;
        }

        public static List<AutoSeries> All() { return Keys.Select(Get).ToList(); }

        public static void Save(AutoSeries s) { Settings.Set("auto." + s.Key, new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(s)); }

        private static AutoSeries Default(string key)
        {
            const string P = "<p style=\"margin:0 0 16px\">";
            switch (key)
            {
                case "welcome":
                    return new AutoSeries { Key = key, Name = "Welcome series", Who = "New newsletter subscribers", Enabled = false, Steps = new List<AutoStep> {
                        new AutoStep { DelayHours = 0, Subject = "Welcome to {{company}}",
                            Body = P + "Hi {{first_name}},</p><h2 style=\"font-size:22px;margin:0 0 12px\">Thanks for subscribing.</h2>" + P + "Once a month we send one practical note on marketing, software and growth. No spam, ever.</p>" + P + "While you are here, these are the guides our readers like most:</p><ul style=\"margin:0 0 16px;padding-left:20px\"><li><a href=\"{{site}}/blog\" style=\"color:#0066FF\">Our latest insights</a></li><li><a href=\"{{site}}/case-studies\" style=\"color:#0066FF\">Case studies with real numbers</a></li></ul>" + P + "Team {{company}}</p>" },
                        new AutoStep { DelayHours = 72, Subject = "How is your website really doing?",
                            Body = P + "Hi {{first_name}},</p>" + P + "Most businesses lose leads to small things: a slow page, a missing title, a form that breaks on phones.</p>" + P + "Our free website audit checks your site for SEO, speed, mobile and security in under a minute.</p>{{button:Run a free website audit:{{audit_url}}}}" + P + "Team {{company}}</p>" },
                        new AutoStep { DelayHours = 168, Subject = "Want a second pair of eyes on your growth plan?",
                            Body = P + "Hi {{first_name}},</p>" + P + "If you are planning marketing, a new website or software this quarter, a 30-minute call with a specialist can save weeks.</p>" + P + "It is free, and you will leave with a clear next step.</p>{{button:Book a free call:{{book_url}}}}" + P + "Team {{company}}</p>" } } };
                case "lead":
                    return new AutoSeries { Key = key, Name = "New enquiry follow-up", Who = "People who enquire and leave an email", Enabled = false, Steps = new List<AutoStep> {
                        new AutoStep { DelayHours = 48, Subject = "Results from businesses like yours",
                            Body = P + "Hi {{first_name}},</p>" + P + "Thanks again for getting in touch. While our team prepares for your conversation, here is what we have delivered for others:</p>{{button:See case studies:{{site}}/case-studies}}" + P + "Questions in the meantime? Reply to this email or call {{phone}}.</p>" + P + "Team {{company}}</p>" },
                        new AutoStep { DelayHours = 96, Subject = "Pick a time that suits you",
                            Body = P + "Hi {{first_name}},</p>" + P + "If it is easier, choose a time for a free call with a specialist. It takes 30 minutes and you will leave with a clear plan.</p>{{button:Book a call:{{book_url}}}}" + P + "Team {{company}}</p>" } } };
                default:
                    return new AutoSeries { Key = "review", Name = "Review request", Who = "Clients marked Won", Enabled = false, Steps = new List<AutoStep> {
                        new AutoStep { DelayHours = 72, Subject = "How did we do, {{first_name}}?",
                            Body = P + "Hi {{first_name}},</p>" + P + "Thank you for choosing {{company}}. If you are happy with our work, a short Google review would mean a lot to our team and helps other businesses find us.</p>{{button:Leave a Google review:{{review_url}}}}" + P + "It takes less than a minute. If anything could have been better, just reply and tell us.</p>" + P + "Team {{company}}</p>" },
                        new AutoStep { DelayHours = 120, Subject = "A quick favour?",
                            Body = P + "Hi {{first_name}},</p>" + P + "Just a gentle reminder: if you have a minute, we would love to hear about your experience.</p>{{button:Leave a Google review:{{review_url}}}}" + P + "Thank you,<br>Team {{company}}</p>" } } };
            }
        }

        // ---- Queue -------------------------------------------------------------------------------------------

        /// <summary>Starts a series for someone. Does nothing when the series is off, empty, or they are already in it.</summary>
        public static bool Enroll(string key, string email, string name, int? refId)
        {
            try
            {
                if (!Newsletter.IsEmail(email)) return false;
                var s = Get(key);
                if (s == null || !s.Enabled || !s.Steps.Any(x => !string.IsNullOrWhiteSpace(x.Subject))) return false;
                email = email.Trim().ToLowerInvariant();
                if (Db.Scalar<int>("SELECT COUNT(*) FROM AutoQueue WHERE Sequence = @key AND Email = @email AND Status = 'Active'", new { key, email }) > 0) return false;
                // Someone who asked to stop automatic emails is never added again.
                if (Db.Scalar<int>("SELECT COUNT(*) FROM AutoQueue WHERE Email = @email AND Status = 'Unsubscribed'", new { email }) > 0) return false;
                var now = DateTime.UtcNow;
                Db.Exec("INSERT INTO AutoQueue (Sequence, Email, Name, RefId, Step, DueOn, Status, Token, Attempts, CreatedOn) VALUES (@key, @email, @name, @refId, 0, @due, 'Active', @token, 0, @now)",
                    new { key, email, name = Util.Cut(name, 120), refId, due = now.AddHours(s.Steps[0].DelayHours), token = Util.NewId(), now });
                return true;
            }
            catch (Exception ex) { Mailer.Log("automation enroll " + key, ex); return false; }
        }

        /// <summary>Stops every automatic email to this address (from the link in the email).</summary>
        public static void StopAll(string email)
        {
            Db.Exec("UPDATE AutoQueue SET Status = 'Unsubscribed' WHERE Email = @email AND Status IN ('Active', 'Done', 'Stopped')", new { email = (email ?? "").Trim().ToLowerInvariant() });
        }

        public static string EmailForToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length > 40) return null;
            return Db.Scalar<string>("SELECT Email FROM AutoQueue WHERE Token = @token", new { token });
        }

        public static void Stop(int id) { Db.Exec("UPDATE AutoQueue SET Status = 'Stopped' WHERE Id = @id AND Status = 'Active'", new { id }); }

        public static Dictionary<string, int> Counts(string key)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { "Active", 0 }, { "Done", 0 }, { "Stopped", 0 }, { "Unsubscribed", 0 } };
            foreach (var r in Db.Rows("SELECT Status, COUNT(*) AS N FROM AutoQueue WHERE Sequence = @key GROUP BY Status", new { key })) d[r.Str("Status")] = r.Int("N");
            return d;
        }

        public static List<Row> Recent(string key, int count)
        {
            return Db.Rows("SELECT * FROM AutoQueue WHERE Sequence = @key ORDER BY Id DESC" + Db.Page(0, count), new { key });
        }

        // ---- Sending -----------------------------------------------------------------------------------------

        private static int _busy;

        /// <summary>Sends what is due. Called every minute by the newsletter timer.</summary>
        public static void Tick()
        {
            if (System.Threading.Interlocked.Exchange(ref _busy, 1) == 1) return;
            try
            {
                var now = DateTime.UtcNow;
                var room = NewsSender.PerHour - NewsSender.UsedThisHour() - Db.Scalar<int>("SELECT COUNT(*) FROM AutoQueue WHERE LastSentOn > @since", new { since = now.AddHours(-1) });
                if (room <= 0) return;
                var due = Db.Rows("SELECT * FROM AutoQueue WHERE Status = 'Active' AND DueOn <= @now ORDER BY DueOn" + Db.Page(0, Math.Min(20, room)), new { now });
                var series = Keys.ToDictionary(k => k, Get);
                foreach (var r in due) SendOne(r, series[r.Str("Sequence")] ?? Get("welcome"));
            }
            catch (Exception ex) { Mailer.Log("automations", ex); }
            finally { _busy = 0; }
        }

        private static void SendOne(Row r, AutoSeries s)
        {
            var id = r.Int("Id");
            var step = r.Int("Step");
            var steps = s.Steps.Where(x => !string.IsNullOrWhiteSpace(x.Subject)).ToList();
            if (!s.Enabled || step >= steps.Count) { Db.Exec("UPDATE AutoQueue SET Status = @st WHERE Id = @id", new { st = s.Enabled ? "Done" : "Stopped", id }); return; }
            var why = StopReason(s.Key, r);
            if (why != null) { Db.Exec("UPDATE AutoQueue SET Status = 'Stopped', LastError = @why WHERE Id = @id", new { why, id }); return; }

            var st = steps[step];
            try
            {
                var unsub = Mailer.SiteUrl + "/newsletter/unsubscribe?a=" + r.Str("Token");
                var html = Mailer.Wrap(Fill(st.Subject, r, true), Fill(st.Body, r, false), unsub);
                Mailer.Send(r.Str("Email"), Fill(st.Subject, r, true), html, null, m =>
                {
                    m.Headers.Add("List-Unsubscribe", "<" + unsub + ">");
                    m.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
                });
                var next = step + 1;
                var now = DateTime.UtcNow;
                Db.Exec("UPDATE AutoQueue SET Step = @next, Attempts = 0, LastError = NULL, LastSentOn = @now, DueOn = @due, Status = @status WHERE Id = @id",
                    new { next, now, due = next < steps.Count ? now.AddHours(Math.Max(1, steps[next].DelayHours)) : now, status = next < steps.Count ? "Active" : "Done", id });
                if (s.Key == "lead" && r.IntN("RefId").HasValue)
                    LeadService.AddActivity(r.IntN("RefId").Value, null, "Email", "Automatic email sent: " + Fill(st.Subject, r, true), null);
            }
            catch (Exception ex)
            {
                var attempts = r.Int("Attempts") + 1;
                Mailer.Log("automation " + s.Key + " " + r.Str("Email"), ex);
                // Try again in 30 minutes, then give up on this email after three tries and move on.
                Db.Exec("UPDATE AutoQueue SET Attempts = @a, LastError = @err, DueOn = @due, Step = @step WHERE Id = @id",
                    new { a = attempts >= 3 ? 0 : attempts, err = Util.Cut(ex.Message, 400), due = DateTime.UtcNow.AddMinutes(30), step = attempts >= 3 ? step + 1 : step, id });
            }
        }

        /// <summary>Why someone should not get the next email, or null.</summary>
        private static string StopReason(string key, Row r)
        {
            var email = r.Str("Email");
            if (Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers WHERE Email = @email AND Status = 'Unsubscribed'", new { email }) > 0) return "Unsubscribed from the newsletter";
            var refId = r.IntN("RefId");
            if (key == "welcome" && refId.HasValue && Db.Scalar<int>("SELECT COUNT(*) FROM NewsSubscribers WHERE Id = @id AND Status = 'Active'", new { id = refId.Value }) == 0) return "No longer subscribed";
            if (key == "lead" && refId.HasValue)
            {
                var status = Db.Scalar<string>("SELECT Status FROM CrmLeads WHERE Id = @id", new { id = refId.Value });
                if (status == null) return "Lead deleted";
                if (!Lists.OpenStatuses.Contains(status)) return "Lead is " + status;
                if (Db.Scalar<int>("SELECT COUNT(*) FROM Bookings WHERE Email = @email AND Status = 'Confirmed' AND StartOn > @now", new { email, now = DateTime.UtcNow }) > 0) return "Booked a call";
            }
            if (key == "review" && Db.Scalar<int>("SELECT COUNT(*) FROM ReviewRequests WHERE Email = @email AND ClickedOn IS NOT NULL", new { email }) > 0) return "Opened the review link";
            return null;
        }

        /// <summary>Placeholders: {{name}}, {{first_name}}, {{company}}, {{phone}}, {{site}}, {{book_url}}, {{audit_url}}, {{review_url}},
        /// and {{button:Label:url}} for a branded button.</summary>
        public static string Fill(string text, Row r, bool plain)
        {
            var name = r == null ? "Asha Verma" : r.Str("Name");
            var first = string.IsNullOrWhiteSpace(name) ? "there" : name.Trim().Split(' ')[0];
            string company = "Yenetch", phone = "";
            try { var co = SiteContent.Current.Company; if (co != null) { company = string.IsNullOrEmpty(co.Name) ? company : co.Name; phone = co.Phone ?? ""; } } catch { }
            var review = r == null || plain || (text ?? "").IndexOf("{{review_url}}", StringComparison.Ordinal) < 0 ? Reviews.ReviewUrl : Reviews.TrackedUrl(r.Str("Email"), r.Str("Name"), r.IntN("RefId"), null);
            Func<string, string> enc = v => plain ? v : Util.H(v);
            var t = (text ?? "")
                .Replace("{{name}}", enc(string.IsNullOrWhiteSpace(name) ? "there" : name.Trim())).Replace("{{first_name}}", enc(first))
                .Replace("{{company}}", enc(company)).Replace("{{phone}}", enc(phone)).Replace("{{site}}", Mailer.SiteUrl)
                .Replace("{{book_url}}", Mailer.SiteUrl + "/book").Replace("{{audit_url}}", Mailer.SiteUrl + "/website-audit").Replace("{{review_url}}", review ?? Mailer.SiteUrl);
            if (!plain)
                t = System.Text.RegularExpressions.Regex.Replace(t, @"\{\{button:([^:}]+):([^}]+)\}\}", m => Mailer.Button(System.Web.HttpUtility.HtmlDecode(m.Groups[1].Value), m.Groups[2].Value));
            return t;
        }

        /// <summary>Sends every email of a series to one address now, with sample details (the Send test button).</summary>
        public static void SendTest(AutoSeries s, string to)
        {
            var steps = s.Steps.Where(x => !string.IsNullOrWhiteSpace(x.Subject)).ToList();
            for (var i = 0; i < steps.Count; i++)
                Mailer.Send(to, "[Test " + (i + 1) + "/" + steps.Count + "] " + Fill(steps[i].Subject, null, true), Mailer.Wrap(Fill(steps[i].Subject, null, true), Fill(steps[i].Body, null, false), Mailer.SiteUrl + "/newsletter/unsubscribe"));
        }
    }
}
