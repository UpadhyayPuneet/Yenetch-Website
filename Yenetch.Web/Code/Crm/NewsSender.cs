using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Web.Hosting;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class Delivery
    {
        public int Id { get; set; }
        public int SubscriberId { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public int Attempts { get; set; }
        public string Error { get; set; }
        public DateTime? SentOn { get; set; }

        internal static Delivery From(Row r)
        {
            return new Delivery { Id = r.Int("Id"), SubscriberId = r.Int("SubscriberId"), Email = r.Str("Email"), Status = r.Str("Status"),
                                  Attempts = r.Int("Attempts"), Error = r.Str("Error"), SentOn = r.DateN("SentOn") };
        }
    }

    /// <summary>
    /// Newsletter sending queue. Each campaign gets one NewsDeliveries row per recipient, then a background worker sends them at the
    /// speed set in /admin/email (per minute and per hour, to stay inside the mail host's limits). Because progress lives in the
    /// database, a send can be paused, resumed, scheduled, and carries on by itself after the site restarts.
    /// Campaign status: Draft, Scheduled, Queueing, Sending, Paused, Sent.
    /// </summary>
    public static class NewsSender
    {
        public const int DefaultPerMinute = 20, DefaultPerHour = 200, MaxAttempts = 3;
        private static readonly object Gate = new object();
        private static readonly HashSet<int> Running = new HashSet<int>();
        private static Timer _timer;

        public static int PerMinute { get { return Limit("news.perMinute", DefaultPerMinute, 1, 600); } }
        public static int PerHour { get { return Limit("news.perHour", DefaultPerHour, 10, 100000); } }

        private static int Limit(string name, int fallback, int min, int max)
        {
            int v;
            return int.TryParse(Settings.Get(name), out v) ? Math.Max(min, Math.Min(max, v)) : fallback;
        }

        /// <summary>Emails sent or tried in the last hour, across all campaigns (what the hourly cap counts).</summary>
        public static int UsedThisHour()
        {
            return Db.Scalar<int>("SELECT COUNT(*) FROM NewsDeliveries WHERE SentOn > @since AND Status IN ('Sent', 'Failed')", new { since = DateTime.UtcNow.AddHours(-1) });
        }

        /// <summary>Rough time to send this many emails at the current limits, e.g. "about 25 minutes".</summary>
        public static string Estimate(int count, int usedThisHour = 0)
        {
            if (count <= 0) return "a moment";
            int perMinute = PerMinute, perHour = PerHour;
            // What still fits in this hour goes at the per-minute speed; the rest waits for the hourly cap to free up.
            var now = Math.Min(count, Math.Max(0, perHour - usedThisHour));
            var later = count - now;
            var minutes = (double)now / perMinute + (later > 0 ? Math.Ceiling((double)later / perHour) * 60 : 0);
            if (minutes < 2) return "a minute or two";
            if (minutes < 90) return "about " + Math.Ceiling(minutes) + " minutes";
            var hours = minutes / 60;
            return hours < 36 ? "about " + Math.Ceiling(hours) + " hours" : "about " + Math.Ceiling(hours / 24) + " days";
        }

        /// <summary>Checks every minute for scheduled campaigns that are due and for sends interrupted by a restart.</summary>
        public static void StartTimer()
        {
            if (_timer != null || !Db.IsConfigured) return;
            _timer = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1));
        }

        public static void Tick()
        {
            try
            {
                Db.EnsureSchema();
                Db.Exec("UPDATE NewsCampaigns SET Status = 'Queueing', SentOn = @now WHERE Status = 'Scheduled' AND ScheduledFor <= @now", new { now = DateTime.UtcNow });
                foreach (var r in Db.Rows("SELECT Id FROM NewsCampaigns WHERE Status IN ('Queueing', 'Sending')")) Kick(r.Int("Id"));
            }
            catch (Exception ex) { Mailer.Log("newsletter timer", ex); }
            // The same minute timer runs the other scheduled work. Each one catches its own errors.
            Automations.Tick();
            Bookings.Tick();
            DailyDigest.Tick();
            Backups.Tick();
            try { Reviews.RefreshIfStale(); } catch (Exception ex) { Mailer.Log("reviews", ex); }
        }

        // ---- Actions from the campaign page ------------------------------------------------------------------

        /// <summary>Sends a draft now, or schedules it when <paramref name="at"/> is in the future (UTC).</summary>
        public static bool Start(int campaignId, DateTime? at)
        {
            var now = DateTime.UtcNow;
            if (at.HasValue && at.Value > now.AddMinutes(1))
                return Db.Exec("UPDATE NewsCampaigns SET Status = 'Scheduled', ScheduledFor = @at WHERE Id = @id AND Status = 'Draft'", new { at, id = campaignId }) > 0;
            var changed = Db.Exec("UPDATE NewsCampaigns SET Status = 'Queueing', SentOn = @now, ScheduledFor = NULL, SentCount = 0, FailedCount = 0 WHERE Id = @id AND Status IN ('Draft', 'Scheduled')",
                new { now, id = campaignId });
            if (changed > 0) Kick(campaignId);
            return changed > 0;
        }

        public static void Unschedule(int id) { Db.Exec("UPDATE NewsCampaigns SET Status = 'Draft', ScheduledFor = NULL WHERE Id = @id AND Status = 'Scheduled'", new { id }); }

        public static void Pause(int id) { Db.Exec("UPDATE NewsCampaigns SET Status = 'Paused' WHERE Id = @id AND Status = 'Sending'", new { id }); }

        public static void Resume(int id)
        {
            if (Db.Exec("UPDATE NewsCampaigns SET Status = 'Sending' WHERE Id = @id AND Status = 'Paused'", new { id }) > 0) Kick(id);
        }

        /// <summary>Stops a paused or running send for good. Recipients not reached yet are marked Skipped.</summary>
        public static void Stop(int id)
        {
            if (Db.Exec("UPDATE NewsCampaigns SET Status = 'Sent' WHERE Id = @id AND Status IN ('Sending', 'Paused')", new { id }) == 0) return;
            Db.Exec("UPDATE NewsDeliveries SET Status = 'Skipped', Error = 'Stopped' WHERE CampaignId = @id AND Status = 'Pending'", new { id });
            UpdateCounts(id);
        }

        /// <summary>Puts failed recipients of a finished campaign back in the queue.</summary>
        public static int RetryFailed(int id)
        {
            if (Db.Scalar<int>("SELECT COUNT(*) FROM NewsCampaigns WHERE Id = @id AND Status = 'Sent'", new { id }) == 0) return 0;
            var n = Db.Exec("UPDATE NewsDeliveries SET Status = 'Pending', Attempts = 0, Error = NULL WHERE CampaignId = @id AND Status = 'Failed'", new { id });
            if (n > 0 && Db.Exec("UPDATE NewsCampaigns SET Status = 'Sending' WHERE Id = @id AND Status = 'Sent'", new { id }) > 0) { UpdateCounts(id); Kick(id); }
            return n;
        }

        public static Dictionary<string, int> Counts(int id)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { "Pending", 0 }, { "Sent", 0 }, { "Failed", 0 }, { "Skipped", 0 } };
            foreach (var r in Db.Rows("SELECT Status, COUNT(*) AS N FROM NewsDeliveries WHERE CampaignId = @id GROUP BY Status", new { id })) d[r.Str("Status")] = r.Int("N");
            return d;
        }

        public static List<Delivery> Problems(int id, int count)
        {
            return Db.Query("SELECT * FROM NewsDeliveries WHERE CampaignId = @id AND Status IN ('Failed', 'Skipped') ORDER BY Id" + Db.Page(0, count), Delivery.From, new { id });
        }

        // ---- Worker ------------------------------------------------------------------------------------------

        private static void Kick(int id)
        {
            lock (Gate) { if (!Running.Add(id)) return; }
            Action<CancellationToken> work = token =>
            {
                try { Work(id, token); }
                catch (Exception ex) { Mailer.Log("campaign " + id, ex); }
                finally { lock (Gate) Running.Remove(id); }
            };
            // On IIS, queued work is told when the app pool shuts down; the minute timer picks the send up again after the restart.
            // Mono does not run queued work, so use a plain task there.
            if (Type.GetType("Mono.Runtime") == null) HostingEnvironment.QueueBackgroundWorkItem(work);
            else System.Threading.Tasks.Task.Run(() => work(CancellationToken.None));
        }

        private static string StatusOf(int id) { return Db.Scalar<string>("SELECT Status FROM NewsCampaigns WHERE Id = @id", new { id }); }

        private static void Work(int id, CancellationToken cancel)
        {
            var c = Newsletter.Campaign(id);
            if (c == null) return;
            if (c.Status == "Queueing")
            {
                // Idempotent, so a restart halfway through only adds the rows that are missing.
                Db.Exec(@"INSERT INTO NewsDeliveries (CampaignId, SubscriberId, Email, Status, Attempts)
                          SELECT @id, s.Id, s.Email, 'Pending', 0 FROM NewsSubscribers s
                          WHERE s.Status = 'Active' AND NOT EXISTS (SELECT 1 FROM NewsDeliveries d WHERE d.CampaignId = @id AND d.SubscriberId = s.Id)", new { id });
                Db.Exec("UPDATE NewsCampaigns SET Status = 'Sending' WHERE Id = @id AND Status = 'Queueing'", new { id });
                UpdateCounts(id);
            }

            var backoff = 0;
            while (!cancel.IsCancellationRequested)
            {
                if (StatusOf(id) != "Sending") { UpdateCounts(id); return; }

                // Hourly cap across all campaigns, counted from the queue itself so it survives restarts.
                var used = UsedThisHour();
                if (used >= PerHour) { if (cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(60))) return; continue; }

                var batch = Db.Rows(@"SELECT d.Id, d.Email, d.Attempts, s.Status AS SubStatus, s.Name, s.Token FROM NewsDeliveries d
                                      LEFT JOIN NewsSubscribers s ON s.Id = d.SubscriberId
                                      WHERE d.CampaignId = @id AND d.Status = 'Pending' ORDER BY d.Id" + Db.Page(0, Math.Min(10, PerHour - used)), new { id });
                if (batch.Count == 0)
                {
                    Db.Exec("UPDATE NewsCampaigns SET Status = 'Sent' WHERE Id = @id AND Status = 'Sending'", new { id });
                    UpdateCounts(id);
                    return;
                }

                c = Newsletter.Campaign(id);
                foreach (var r in batch)
                {
                    if (cancel.IsCancellationRequested || StatusOf(id) != "Sending") break;
                    var did = r.Int("Id");
                    if (r.Str("SubStatus") != "Active")
                    {
                        Db.Exec("UPDATE NewsDeliveries SET Status = 'Skipped', Error = 'Unsubscribed before sending' WHERE Id = @did", new { did });
                        continue;
                    }
                    var started = DateTime.UtcNow;
                    var s = new Subscriber { Email = r.Str("Email"), Name = r.Str("Name"), Token = r.Str("Token") };
                    try
                    {
                        var unsub = Mailer.SiteUrl + "/newsletter/unsubscribe?t=" + s.Token;
                        Mailer.Send(s.Email, c.Subject, Newsletter.Render(c, s), null, m =>
                        {
                            m.Headers.Add("List-Unsubscribe", "<" + unsub + ">");
                            m.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
                        });
                        Db.Exec("UPDATE NewsDeliveries SET Status = 'Sent', Attempts = Attempts + 1, Error = NULL, SentOn = @now WHERE Id = @did", new { did, now = DateTime.UtcNow });
                        backoff = 0;
                    }
                    catch (Exception ex)
                    {
                        var attempts = r.Int("Attempts") + 1;
                        var retry = Transient(ex) && attempts < MaxAttempts;
                        Db.Exec("UPDATE NewsDeliveries SET Status = @st, Attempts = @attempts, Error = @err, SentOn = @now WHERE Id = @did",
                            new { st = retry ? "Pending" : "Failed", attempts, err = Util.Cut(ex.Message, 400), now = DateTime.UtcNow, did });
                        Mailer.Log("campaign " + id + " " + s.Email, ex);
                        if (retry)
                        {
                            // The mail server is busy or limiting us: wait longer each time (2, 4, 8... up to 30 minutes).
                            backoff = Math.Min(backoff == 0 ? 2 : backoff * 2, 30);
                            UpdateCounts(id);
                            if (cancel.WaitHandle.WaitOne(TimeSpan.FromMinutes(backoff))) return;
                            break;
                        }
                    }
                    var wait = TimeSpan.FromMilliseconds(60000.0 / PerMinute) - (DateTime.UtcNow - started);
                    if (wait > TimeSpan.Zero && cancel.WaitHandle.WaitOne(wait)) return;
                }
                UpdateCounts(id);
            }
        }

        /// <summary>Errors worth retrying later: the server is busy, rate limiting, or could not be reached.</summary>
        private static bool Transient(Exception ex)
        {
            var smtp = ex as SmtpException;
            if (smtp == null) return ex is System.IO.IOException || ex is System.Net.Sockets.SocketException || ex is TimeoutException;
            switch (smtp.StatusCode)
            {
                case SmtpStatusCode.ServiceNotAvailable:
                case SmtpStatusCode.MailboxBusy:
                case SmtpStatusCode.LocalErrorInProcessing:
                case SmtpStatusCode.InsufficientStorage:
                case SmtpStatusCode.GeneralFailure:
                    return true;
                default:
                    return false;
            }
        }

        private static void UpdateCounts(int id)
        {
            var n = Counts(id);
            var total = n.Values.Sum();
            if (total == 0) return; // campaigns sent before the queue existed keep their numbers
            Db.Exec("UPDATE NewsCampaigns SET Recipients = @total, SentCount = @sent, FailedCount = @failed WHERE Id = @id",
                new { total, sent = n["Sent"], failed = n["Failed"], id });
        }
    }
}
