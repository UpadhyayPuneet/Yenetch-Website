using System;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/bookings/{id}: one booked call. Change its status (completed, no-show, cancelled) and keep notes.</summary>
    public partial class BookingDetailPage : AdminPage
    {
        public override string Section { get { return "bookings"; } }
        protected Booking B;

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            int.TryParse(Convert.ToString(RouteData.Values["id"]), out id);
            B = Bookings.Get(id);
            if (B == null) { RedirectWith("/admin/bookings", "That booking was not found."); return; }
            Title = B.Name + " · Booking";
            if (IsPostBack && Request.Form["save"] == "1")
            {
                var status = Request.Form["status"];
                if (status == "Cancelled" && B.Status == "Confirmed" && B.StartOn < DateTime.UtcNow) status = "No-show";
                Bookings.SetStatus(B.Id, status, Request.Form["notes"], Me.Id);
                RedirectWith("/admin/bookings/" + B.Id, status == "Cancelled" && B.Status == "Confirmed" ? "Booking cancelled. We emailed " + B.Email + "." : "Booking saved.");
            }
        }

        protected static string Css(string status) { return BookingsPage.Css(status); }
    }
}
