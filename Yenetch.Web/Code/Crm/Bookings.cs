using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class WorkDay
    {
        public bool On { get; set; }
        /// <summary>Opening hours in IST, for example "10:00-13:00, 14:00-18:30".</summary>
        public string Hours { get; set; }
    }

    public class BookingSettings
    {
        public bool Enabled { get; set; }
        public int SlotMinutes { get; set; }
        public int BufferMinutes { get; set; }
        public int MinNoticeHours { get; set; }
        public int DaysAhead { get; set; }
        public int MaxPerDay { get; set; }
        /// <summary>Sunday first, like DayOfWeek.</summary>
        public List<WorkDay> Week { get; set; }
        /// <summary>Days off, one per line: 2026-10-20, or a range 2026-12-24 to 2026-12-31.</summary>
        public string Closed { get; set; }
        /// <summary>How the call happens, one per line. The first is the default.</summary>
        public string Modes { get; set; }
        public string MeetingLink { get; set; }
        public string NotifyTo { get; set; }
        public string Heading { get; set; }
        public string Intro { get; set; }
    }

    public class Booking
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Company { get; set; }
        public string Topic { get; set; }
        public string Notes { get; set; }
        public string Mode { get; set; }
        public DateTime StartOn { get; set; }
        public DateTime EndOn { get; set; }
        public string Status { get; set; }
        public string Token { get; set; }
        public int? LeadId { get; set; }
        public int Reminded { get; set; }
        public string AdminNotes { get; set; }
        public DateTime CreatedOn { get; set; }
        public string WhenText { get { return Util.Ist(StartOn).ToString("dddd d MMMM yyyy, h:mm tt", Util.India) + " IST"; } }
        public bool IsUpcoming { get { return Status == "Confirmed" && StartOn > DateTime.UtcNow; } }

        internal static Booking From(Row r)
        {
            return new Booking
            {
                Id = r.Int("Id"), Name = r.Str("Name"), Email = r.Str("Email"), Phone = r.Str("Phone"), Company = r.Str("Company"), Topic = r.Str("Topic"),
                Notes = r.Str("Notes"), Mode = r.Str("Mode"), StartOn = r.Date("StartOn"), EndOn = r.Date("EndOn"), Status = r.Str("Status"), Token = r.Str("Token"),
                LeadId = r.IntN("LeadId"), Reminded = r.Int("Reminded"), AdminNotes = r.Str("AdminNotes"), CreatedOn = r.Date("CreatedOn")
            };
        }
    }

    /// <summary>
    /// Free consultation booking: opening hours and rules from /admin/availability, open slots for /book (via /api/slots),
    /// bookings saved with a lead, confirmation and calendar invite (.ics) to the visitor and the team, reminders a day and an hour
    /// before, and a cancel link. All times are India time (IST) for the business; the page also shows the visitor's own time.
    /// </summary>
    public static class Bookings
    {
        public static readonly string[] Statuses = { "Confirmed", "Completed", "No-show", "Cancelled" };
        private static readonly object Gate = new object();

        // ---- Settings ----------------------------------------------------------------------------------------

        public static BookingSettings Default()
        {
            var week = new List<WorkDay>();
            for (var d = 0; d < 7; d++) week.Add(new WorkDay { On = d >= 1 && d <= 5 || d == 6, Hours = d == 6 ? "10:00-14:00" : "10:00-13:00, 14:00-18:30" });
            week[0].On = false;
            return new BookingSettings
            {
                Enabled = true, SlotMinutes = 30, BufferMinutes = 15, MinNoticeHours = 4, DaysAhead = 21, MaxPerDay = 8, Week = week,
                Modes = "Google Meet video call\nPhone call\nVisit our office", Heading = "Book a free consultation",
                Intro = "Pick a time for a 30-minute call with a specialist. Tell us what you want to achieve, and you will leave with a clear next step. No cost, no obligation."
            };
        }

        public static BookingSettings Config
        {
            get
            {
                BookingSettings s = null;
                try { var json = Settings.Get("booking.settings"); if (!string.IsNullOrEmpty(json)) s = new JavaScriptSerializer().Deserialize<BookingSettings>(json); } catch { }
                var d = Default();
                if (s == null) return d;
                if (s.Week == null || s.Week.Count != 7) s.Week = d.Week;
                if (s.SlotMinutes < 10) s.SlotMinutes = 30;
                return s;
            }
        }

        public static void Save(BookingSettings s) { Settings.Set("booking.settings", new JavaScriptSerializer().Serialize(s)); }

        public static List<string> Modes(BookingSettings s) { return Lines(s.Modes).DefaultIfEmpty("Video call").ToList(); }

        private static List<string> Lines(string s) { return (s ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList(); }

        /// <summary>Parses "10:00-13:00, 14:00-18:30" into minute ranges. Null when a part is not understood.</summary>
        public static List<Tuple<int, int>> Ranges(string hours)
        {
            var list = new List<Tuple<int, int>>();
            foreach (var part in (hours ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var m = Regex.Match(part.Trim(), @"^(\d{1,2})[:.](\d{2})\s*(?:-|–|to)\s*(\d{1,2})[:.](\d{2})$");
                if (!m.Success) return null;
                int a = int.Parse(m.Groups[1].Value) * 60 + int.Parse(m.Groups[2].Value), b = int.Parse(m.Groups[3].Value) * 60 + int.Parse(m.Groups[4].Value);
                if (a >= b || b > 24 * 60) return null;
                list.Add(Tuple.Create(a, b));
            }
            return list;
        }

        /// <summary>Closed days (IST dates) from the admin list.</summary>
        public static HashSet<DateTime> ClosedDays(string text, out string problem)
        {
            problem = null;
            var set = new HashSet<DateTime>();
            foreach (var line in Lines(text))
            {
                var m = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2})(?:\s*(?:to|-|–)\s*(\d{4}-\d{2}-\d{2}))?");
                DateTime a, b;
                if (!m.Success || !DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out a)) { problem = "Write days off as 2026-10-20 or 2026-12-24 to 2026-12-31. Check \"" + line + "\"."; continue; }
                b = a;
                if (m.Groups[2].Success && !DateTime.TryParseExact(m.Groups[2].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out b)) { problem = "Check the date range \"" + line + "\"."; continue; }
                for (var d = a; d <= b && (d - a).TotalDays < 400; d = d.AddDays(1)) set.Add(d);
            }
            return set;
        }

        // ---- Availability ------------------------------------------------------------------------------------

        /// <summary>Open slots per IST day, for the next DaysAhead days. Key yyyy-MM-dd, value HH:mm start times.</summary>
        public static SortedDictionary<string, List<string>> OpenSlots(BookingSettings s)
        {
            var result = new SortedDictionary<string, List<string>>();
            if (!s.Enabled) return result;
            string ignored;
            var closed = ClosedDays(s.Closed, out ignored);
            var nowIst = Util.Ist(DateTime.UtcNow);
            var earliest = nowIst.AddHours(Math.Max(0, s.MinNoticeHours));
            var lastDay = nowIst.Date.AddDays(Math.Max(1, Math.Min(90, s.DaysAhead)));
            var taken = Db.Query("SELECT StartOn, EndOn FROM Bookings WHERE Status = 'Confirmed' AND EndOn > @from AND StartOn < @to",
                r => Tuple.Create(Util.Ist(r.Date("StartOn")), Util.Ist(r.Date("EndOn"))), new { from = DateTime.UtcNow, to = Util.FromIst(lastDay.AddDays(1)) });
            for (var day = nowIst.Date; day <= lastDay; day = day.AddDays(1))
            {
                var wd = s.Week[(int)day.DayOfWeek];
                if (!wd.On || closed.Contains(day)) continue;
                var ranges = Ranges(wd.Hours);
                if (ranges == null) continue;
                var booked = taken.Count(t => t.Item1.Date == day);
                if (s.MaxPerDay > 0 && booked >= s.MaxPerDay) continue;
                var slots = new List<string>();
                foreach (var r in ranges)
                    for (var m = r.Item1; m + s.SlotMinutes <= r.Item2; m += s.SlotMinutes + Math.Max(0, s.BufferMinutes))
                    {
                        var start = day.AddMinutes(m);
                        var end = start.AddMinutes(s.SlotMinutes);
                        if (start < earliest) continue;
                        var buf = Math.Max(0, s.BufferMinutes);
                        if (taken.Any(t => start < t.Item2.AddMinutes(buf) && end.AddMinutes(buf) > t.Item1)) continue;
                        slots.Add(start.ToString("HH:mm", CultureInfo.InvariantCulture));
                    }
                if (slots.Count > 0) result[day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)] = slots;
            }
            return result;
        }

        /// <summary>JSON for /book: open days and slots, call types and topics.</summary>
        public static string SlotsJson()
        {
            var s = Config;
            var topics = new List<string>();
            try { topics = SiteContent.Current.Services.Select(x => x.Name).Concat(SiteContent.Current.Products.Select(p => p.Name)).ToList(); } catch { }
            topics.Add("Something else");
            return new JavaScriptSerializer().Serialize(new Dictionary<string, object>
            {
                { "enabled", s.Enabled }, { "minutes", s.SlotMinutes }, { "tz", "Asia/Kolkata" }, { "days", OpenSlots(s) },
                { "modes", Modes(s) }, { "topics", topics }
            });
        }

        // ---- Booking -----------------------------------------------------------------------------------------

        /// <summary>Books a slot. Returns the booking, or sets error.</summary>
        public static Booking Book(string name, string email, string phone, string company, string topic, string notes, string mode, string date, string time,
                                   string visitorId, string ip, out string error)
        {
            error = null;
            var s = Config;
            if (!s.Enabled) { error = "Online booking is closed right now. Please call or WhatsApp us."; return null; }
            if ((name ?? "").Trim().Length < 2) { error = "Enter your name."; return null; }
            if (!Newsletter.IsEmail(email)) { error = "Enter a valid email address."; return null; }
            if (Util.Digits(phone).Length < 8) { error = "Enter a phone number we can reach you on."; return null; }
            var modes = Modes(s);
            if (!modes.Contains(mode)) mode = modes[0];
            Booking b;
            lock (Gate) // two people choosing the same slot at the same moment: only one gets it
            {
                List<string> open;
                if (!OpenSlots(s).TryGetValue(date ?? "", out open) || !open.Contains(time ?? "")) { error = "Sorry, that time was just taken. Please pick another."; return null; }
                var startIst = DateTime.ParseExact(date + " " + time, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                var now = DateTime.UtcNow;
                b = new Booking
                {
                    Name = Util.Cut(name.Trim(), 120), Email = email.Trim().ToLowerInvariant(), Phone = Util.Cut(phone.Trim(), 40), Company = Util.Cut((company ?? "").Trim(), 160),
                    Topic = Util.Cut((topic ?? "").Trim(), 160), Notes = Util.Cut((notes ?? "").Trim(), 2000), Mode = mode, StartOn = Util.FromIst(startIst),
                    EndOn = Util.FromIst(startIst.AddMinutes(s.SlotMinutes)), Status = "Confirmed", Token = Util.NewId(), CreatedOn = now
                };
                b.Id = Db.Insert(@"INSERT INTO Bookings (Name, Email, Phone, Company, Topic, Notes, Mode, StartOn, EndOn, Status, Token, Reminded, VisitorId, Ip, CreatedOn, UpdatedOn)
                    VALUES (@Name, @Email, @Phone, @Company, @Topic, @Notes, @Mode, @StartOn, @EndOn, 'Confirmed', @Token, 0, @vid, @ip, @now, @now)",
                    new Dictionary<string, object> { { "Name", b.Name }, { "Email", b.Email }, { "Phone", b.Phone }, { "Company", b.Company == "" ? null : b.Company },
                        { "Topic", b.Topic == "" ? null : b.Topic }, { "Notes", b.Notes == "" ? null : b.Notes }, { "Mode", b.Mode }, { "StartOn", b.StartOn }, { "EndOn", b.EndOn },
                        { "Token", b.Token }, { "vid", Util.Cut(visitorId, 40) }, { "ip", Util.Cut(ip, 64) }, { "now", now } });
            }
            try
            {
                var lead = new Lead { Name = b.Name, Email = b.Email, Phone = b.Phone, Company = b.Company, Interest = b.Topic, Need = b.Notes, Source = "Booking", VisitorId = Util.Cut(visitorId, 40), Page = "/book" };
                b.LeadId = LeadService.CreateFromTool(lead, "Booked a call for " + b.WhenText + " (" + b.Mode + ").");
                Db.Exec("UPDATE Bookings SET LeadId = @lead WHERE Id = @id", new { lead = b.LeadId, id = b.Id });
                // A follow-up task on the call itself, so it shows in Follow-ups.
                LeadService.AddActivity(b.LeadId.Value, null, "FollowUp", "Call booked online: " + (b.Topic ?? "consultation") + " (" + b.Mode + ")", b.StartOn);
                LeadService.SyncNextFollowUp(b.LeadId.Value);
            }
            catch (Exception ex) { Mailer.Log("booking lead " + b.Id, ex); }
            Confirm(b, s);
            NotifyTeam(b, s, "New booking");
            return b;
        }

        public static Booking ByToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length > 40) return null;
            var r = Db.First("SELECT * FROM Bookings WHERE Token = @token", new { token });
            return r == null ? null : Booking.From(r);
        }

        public static Booking Get(int id) { var r = Db.First("SELECT * FROM Bookings WHERE Id = @id", new { id }); return r == null ? null : Booking.From(r); }

        /// <summary>Cancels a booking (by the visitor from their email, or by the team). Frees the slot and tells the other side.</summary>
        public static bool Cancel(Booking b, bool byVisitor, int? userId = null)
        {
            if (b == null || b.Status != "Confirmed") return false;
            Db.Exec("UPDATE Bookings SET Status = 'Cancelled', UpdatedOn = @now WHERE Id = @id AND Status = 'Confirmed'", new { now = DateTime.UtcNow, id = b.Id });
            b.Status = "Cancelled";
            if (b.LeadId.HasValue) LeadService.AddActivity(b.LeadId.Value, userId, "System", (byVisitor ? "The visitor cancelled" : "Cancelled") + " the call booked for " + b.WhenText + ".", null);
            var s = Config;
            if (byVisitor) NotifyTeam(b, s, "Booking cancelled");
            else SendToVisitor(b, "Your call on " + Util.Ist(b.StartOn).ToString("d MMM", Util.India) + " is cancelled",
                Mailer.Heading("Your call is cancelled") + "<p style=\"margin:0 0 16px\">We had to cancel the call booked for <b>" + Util.H(b.WhenText) + "</b>. Sorry for the change.</p><p style=\"margin:0 0 16px\">Please pick another time that suits you.</p>" + Mailer.Button("Book another time", Mailer.SiteUrl + "/book"), Ics(b, true));
            return true;
        }

        public static void SetStatus(int id, string status, string notes, int userId)
        {
            var b = Get(id);
            if (b == null || !Statuses.Contains(status)) return;
            if (status == "Cancelled" && b.Status == "Confirmed") { Cancel(b, false, userId); }
            else Db.Exec("UPDATE Bookings SET Status = @status, UpdatedOn = @now WHERE Id = @id", new { status, now = DateTime.UtcNow, id });
            Db.Exec("UPDATE Bookings SET AdminNotes = @notes WHERE Id = @id", new { notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), id });
            if (status != b.Status && b.LeadId.HasValue && status != "Cancelled") LeadService.AddActivity(b.LeadId.Value, userId, "Meeting", "Booked call marked " + status + ".", null);
        }

        public static List<Booking> List(string view, int offset, int count, out int total)
        {
            var now = DateTime.UtcNow;
            string where, order = "StartOn";
            switch (view)
            {
                case "past": where = "StartOn < @now AND Status <> 'Cancelled'"; order = "StartOn DESC"; break;
                case "cancelled": where = "Status = 'Cancelled'"; order = "StartOn DESC"; break;
                case "all": where = "1 = 1"; order = "StartOn DESC"; break;
                default: where = "StartOn >= @today AND Status = 'Confirmed'"; break;
            }
            var args = new { now, today = Util.FromIst(Util.TodayIst) };
            total = Db.Scalar<int>("SELECT COUNT(*) FROM Bookings WHERE " + where, args);
            return Db.Query("SELECT * FROM Bookings WHERE " + where + " ORDER BY " + order + Db.Page(offset, count), Booking.From, args);
        }

        public static int UpcomingCount() { return Db.Scalar<int>("SELECT COUNT(*) FROM Bookings WHERE Status = 'Confirmed' AND StartOn >= @now", new { now = DateTime.UtcNow }); }

        // ---- Emails ------------------------------------------------------------------------------------------

        private static string Details(Booking b, BookingSettings s, bool forTeam)
        {
            var rows = Mailer.Row("When", b.WhenText) + Mailer.Row("Length", s.SlotMinutes + " minutes") + Mailer.Row("How", b.Mode)
                     + (b.Mode.IndexOf("video", StringComparison.OrdinalIgnoreCase) >= 0 || b.Mode.IndexOf("meet", StringComparison.OrdinalIgnoreCase) >= 0 || b.Mode.IndexOf("zoom", StringComparison.OrdinalIgnoreCase) >= 0
                        ? Mailer.Row("Join link", string.IsNullOrWhiteSpace(s.MeetingLink) ? "We will email the link before the call." : s.MeetingLink.Trim()) : "")
                     + Mailer.Row("Topic", b.Topic);
            if (forTeam) rows += Mailer.Row("Name", b.Name) + Mailer.Row("Company", b.Company) + Mailer.Row("Phone", b.Phone) + Mailer.Row("Email", b.Email) + Mailer.Row("Notes", b.Notes);
            return "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%;margin:0 0 8px\">" + rows + "</table>";
        }

        private static void Confirm(Booking b, BookingSettings s)
        {
            var first = b.Name.Split(' ')[0];
            var body = Mailer.Heading("You're booked, " + Util.H(first) + ".")
                     + "<p style=\"margin:0 0 16px\">Thanks for booking a call with us. The invite is attached, so you can add it to your calendar in one tap.</p>"
                     + Details(b, s, false)
                     + (b.Mode.IndexOf("office", StringComparison.OrdinalIgnoreCase) >= 0 ? OfficeLine() : "")
                     + "<p style=\"margin:16px 0 0;color:#48484e;font-size:14px\">Can't make it? <a href=\"" + Mailer.SiteUrl + "/book/cancel?t=" + b.Token + "\" style=\"color:#0066FF\">Cancel or pick another time</a>.</p>";
            SendToVisitor(b, "Confirmed: your call with " + Company + " on " + Util.Ist(b.StartOn).ToString("d MMM, h:mm tt", Util.India), body, Ics(b, false));
        }

        private static string OfficeLine()
        {
            try
            {
                var o = SiteContent.Current.Company.Offices.FirstOrDefault();
                if (o == null) return "";
                return "<p style=\"margin:8px 0 0;font-size:14px\">Our office: " + Util.H(o.Address ?? o.City) + (string.IsNullOrEmpty(o.Map) ? "" : " · <a href=\"" + HttpUtility.HtmlAttributeEncode(o.Map) + "\" style=\"color:#0066FF\">Directions</a>") + "</p>";
            }
            catch { return ""; }
        }

        private static string Company { get { try { return SiteContent.Current.Company.Name ?? "Yenetch"; } catch { return "Yenetch"; } } }

        private static void SendToVisitor(Booking b, string subject, string body, string ics)
        {
            try
            {
                Mailer.Send(b.Email, subject, Mailer.Wrap(subject, body, null), null, m => AttachIcs(m, ics));
            }
            catch (Exception ex) { Mailer.Log("booking email " + b.Id, ex); }
        }

        private static void NotifyTeam(Booking b, BookingSettings s, string what)
        {
            var to = !string.IsNullOrWhiteSpace(s.NotifyTo) ? s.NotifyTo : Mailer.LeadInbox;
            if (string.IsNullOrWhiteSpace(to)) return;
            try
            {
                var body = Mailer.Heading(Util.H(what) + ": " + Util.H(b.Name)) + Details(b, s, true)
                         + (b.LeadId.HasValue ? Mailer.Button("Open lead", Mailer.SiteUrl + "/admin/leads/" + b.LeadId) : Mailer.Button("Open bookings", Mailer.SiteUrl + "/admin/bookings"));
                Mailer.Send(to, what + ": " + b.Name + ", " + Util.Ist(b.StartOn).ToString("d MMM h:mm tt", Util.India), Mailer.Wrap(what, body, null), null, m =>
                {
                    m.ReplyToList.Clear(); m.ReplyToList.Add(new System.Net.Mail.MailAddress(b.Email, b.Name));
                    AttachIcs(m, Ics(b, b.Status == "Cancelled"));
                });
            }
            catch (Exception ex) { Mailer.Log("booking team email " + b.Id, ex); }
        }

        private static void AttachIcs(System.Net.Mail.MailMessage m, string ics)
        {
            if (ics == null) return;
            var a = new System.Net.Mail.Attachment(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(ics)), "invite.ics", "text/calendar");
            m.Attachments.Add(a);
        }

        /// <summary>Calendar file (RFC 5545) for the call. A cancellation keeps the same UID so calendars remove it.</summary>
        public static string Ics(Booking b, bool cancel)
        {
            Func<DateTime, string> t = d => d.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            Func<string, string> e = v => (v ?? "").Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
            var s = Config;
            var where = b.Mode.IndexOf("office", StringComparison.OrdinalIgnoreCase) >= 0 ? OfficeAddress() : !string.IsNullOrWhiteSpace(s.MeetingLink) ? s.MeetingLink.Trim() : b.Mode;
            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Yenetch//Booking//EN\r\nCALSCALE:GREGORIAN\r\nMETHOD:").Append(cancel ? "CANCEL" : "PUBLISH").Append("\r\n");
            sb.Append("BEGIN:VEVENT\r\nUID:booking-").Append(b.Id).Append("@").Append(new Uri(Mailer.SiteUrl).Host).Append("\r\n");
            sb.Append("DTSTAMP:").Append(t(DateTime.UtcNow)).Append("\r\nDTSTART:").Append(t(b.StartOn)).Append("\r\nDTEND:").Append(t(b.EndOn)).Append("\r\n");
            sb.Append("SUMMARY:").Append(e(Company + " call with " + b.Name)).Append("\r\n");
            sb.Append("DESCRIPTION:").Append(e((b.Topic ?? "Consultation") + " (" + b.Mode + ")\nCancel or change: " + Mailer.SiteUrl + "/book/cancel?t=" + b.Token)).Append("\r\n");
            sb.Append("LOCATION:").Append(e(where)).Append("\r\n");
            sb.Append("STATUS:").Append(cancel ? "CANCELLED" : "CONFIRMED").Append("\r\nSEQUENCE:").Append(cancel ? 1 : 0).Append("\r\n");
            if (!cancel) sb.Append("BEGIN:VALARM\r\nTRIGGER:-PT15M\r\nACTION:DISPLAY\r\nDESCRIPTION:Call in 15 minutes\r\nEND:VALARM\r\n");
            sb.Append("END:VEVENT\r\nEND:VCALENDAR\r\n");
            return sb.ToString();
        }

        private static string OfficeAddress()
        {
            try { var o = SiteContent.Current.Company.Offices.FirstOrDefault(); return o == null ? "Our office" : (o.Address ?? o.City).Replace("\n", ", "); } catch { return "Our office"; }
        }

        // ---- Reminders ---------------------------------------------------------------------------------------

        /// <summary>Reminds the visitor a day before and an hour before. Called every minute.</summary>
        public static void Tick()
        {
            try
            {
                var now = DateTime.UtcNow;
                var s = Config;
                foreach (var b in Db.Query("SELECT * FROM Bookings WHERE Status = 'Confirmed' AND StartOn > @now AND StartOn < @soon AND Reminded < 2", Booking.From, new { now, soon = now.AddHours(25) }))
                {
                    var left = b.StartOn - now;
                    int flag;
                    string when;
                    if (left <= TimeSpan.FromMinutes(70) && b.Reminded < 2) { flag = 2; when = "in about an hour"; }
                    else if (left <= TimeSpan.FromHours(24.5) && b.Reminded < 1 && b.CreatedOn < now.AddHours(-3)) { flag = 1; when = "tomorrow"; }
                    else continue;
                    // Mark first, so a slow mail server can never cause a second reminder.
                    if (Db.Exec("UPDATE Bookings SET Reminded = @flag WHERE Id = @id AND Reminded < @flag", new { flag, id = b.Id }) == 0) continue;
                    if (flag == 2 && b.CreatedOn > now.AddHours(-2)) continue; // booked minutes ago: no need
                    var body = Mailer.Heading("See you " + when + ", " + Util.H(b.Name.Split(' ')[0]) + ".")
                             + "<p style=\"margin:0 0 16px\">A quick reminder of your call with " + Util.H(Company) + ".</p>" + Details(b, s, false)
                             + "<p style=\"margin:16px 0 0;color:#48484e;font-size:14px\">Can't make it? <a href=\"" + Mailer.SiteUrl + "/book/cancel?t=" + b.Token + "\" style=\"color:#0066FF\">Cancel or pick another time</a>.</p>";
                    SendToVisitor(b, "Reminder: your call " + when + " at " + Util.Ist(b.StartOn).ToString("h:mm tt", Util.India), body, null);
                }
            }
            catch (Exception ex) { Mailer.Log("booking reminders", ex); }
        }
    }
}
