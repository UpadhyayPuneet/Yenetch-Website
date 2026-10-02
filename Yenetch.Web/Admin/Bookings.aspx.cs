using System;
using System.Collections.Generic;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/bookings: calls booked on /book.</summary>
    public partial class BookingsPage : AdminPage
    {
        public override string Section { get { return "bookings"; } }

        protected const int PageSize = 30;
        protected static readonly string[][] Views = { new[] { "upcoming", "Upcoming" }, new[] { "past", "Past" }, new[] { "cancelled", "Cancelled" }, new[] { "all", "All" } };
        protected List<Booking> Rows;
        protected int Total, Upcoming;
        protected string View;

        protected void Page_Load(object sender, EventArgs e)
        {
            View = Q("view") == "" ? "upcoming" : Q("view");
            Rows = Bookings.List(View, (PageNo - 1) * PageSize, PageSize, out Total);
            Upcoming = Bookings.UpcomingCount();
        }

        public static string Css(string status)
        {
            return status == "Confirmed" ? "badge--new" : status == "Completed" ? "badge--won" : status == "Cancelled" ? "badge--lost" : "badge--hot";
        }

        protected static string DayHint(Booking b)
        {
            var d = (Util.Ist(b.StartOn).Date - Util.TodayIst).TotalDays;
            if (b.Status != "Confirmed") return "";
            if (d == 0) return b.StartOn < DateTime.UtcNow ? "Earlier today" : "Today";
            if (d == 1) return "Tomorrow";
            if (d > 1 && d < 7) return "In " + d + " days";
            return "";
        }
    }
}
