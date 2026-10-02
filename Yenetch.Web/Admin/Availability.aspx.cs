using System;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/availability: booking hours, call length, days off and booking page text. Admins and managers.</summary>
    public partial class AvailabilityPage : AdminPage
    {
        public override string Section { get { return "bookings"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        protected BookingSettings S;
        protected string Err, NextSlot;
        protected int OpenDays, OpenSlots;

        protected void Page_Load(object sender, EventArgs e)
        {
            S = Bookings.Config;
            if (IsPostBack && Request.Form["save"] == "1") { Save(); if (Response.IsRequestBeingRedirected) return; }
            var open = Bookings.OpenSlots(S);
            OpenDays = open.Count;
            OpenSlots = open.Sum(d => d.Value.Count);
            if (open.Count > 0)
            {
                var first = open.First();
                NextSlot = DateTime.ParseExact(first.Key + " " + first.Value[0], "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture).ToString("dddd d MMM, h:mm tt", Util.India);
            }
        }

        private void Save()
        {
            var f = Request.Form;
            Func<string, int, int> num = (k, def) => { int v; return int.TryParse(f[k], out v) ? v : def; };
            var s = new BookingSettings
            {
                Enabled = f["enabled"] == "1", SlotMinutes = Math.Max(10, Math.Min(240, num("slot", 30))), BufferMinutes = Math.Max(0, Math.Min(120, num("buffer", 15))),
                MinNoticeHours = Math.Max(0, Math.Min(168, num("notice", 4))), DaysAhead = Math.Max(1, Math.Min(90, num("ahead", 21))), MaxPerDay = Math.Max(0, Math.Min(99, num("max", 8))),
                Week = Enumerable.Range(0, 7).Select(d => new WorkDay { On = f["on" + d] == "1", Hours = (f["hours" + d] ?? "").Trim() }).ToList(),
                Closed = (f["closed"] ?? "").Trim(), Modes = (f["modes"] ?? "").Trim(), MeetingLink = (f["link"] ?? "").Trim(), NotifyTo = (f["notify"] ?? "").Trim(),
                Heading = Util.Cut((f["heading"] ?? "").Trim(), 120), Intro = Util.Cut((f["intro"] ?? "").Trim(), 600)
            };
            S = s;
            for (var d = 0; d < 7; d++)
                if (s.Week[d].On && (string.IsNullOrEmpty(s.Week[d].Hours) || Bookings.Ranges(s.Week[d].Hours) == null)) { Err = "Check the hours for " + DayNames[d] + ". Write them like 10:00-13:00, 14:00-18:30."; return; }
            string problem;
            Bookings.ClosedDays(s.Closed, out problem);
            if (problem != null) { Err = problem; return; }
            if (!string.IsNullOrEmpty(s.MeetingLink) && !s.MeetingLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { Err = "The meeting link must start with https://"; return; }
            if (!string.IsNullOrEmpty(s.NotifyTo) && DailyDigest.Emails(s.NotifyTo).Count == 0) { Err = "Enter a valid email for booking alerts, or leave it empty."; return; }
            if (string.IsNullOrEmpty(s.Heading)) s.Heading = Bookings.Default().Heading;
            if (string.IsNullOrEmpty(s.Intro)) s.Intro = Bookings.Default().Intro;
            Bookings.Save(s);
            RedirectWith("/admin/availability", "Availability saved. The booking page shows the new times straight away.");
        }
    }
}
