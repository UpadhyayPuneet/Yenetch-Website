using System;
using System.Collections.Generic;
using System.Globalization;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/reviews: Google review link, live rating, and the review requests sent. Admins and managers.</summary>
    public partial class ReviewsAdminPage : AdminPage
    {
        public override string Section { get { return "reviews"; } }
        protected override bool Allowed(CrmUser u) { return u.CanUseMarketing; }

        protected ReviewSummary Sum;
        protected List<Row> Rows;
        protected AutoSeries Auto;
        protected int Sent90, Clicked90;
        protected bool HasKey;
        protected string Err, Link, Rating, Count;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            Sum = Reviews.Summary;
            Rows = Reviews.Requests(50);
            Auto = Automations.Get("review");
            HasKey = !string.IsNullOrEmpty(Settings.Get("reviews.apiKey"));
            if (Link == null) { Link = Settings.Get("reviews.link"); Rating = Settings.Get("reviews.rating"); Count = Settings.Get("reviews.count"); }
            var since = new { since = DateTime.UtcNow.AddDays(-90) };
            Sent90 = Db.Scalar<int>("SELECT COUNT(*) FROM ReviewRequests WHERE SentOn >= @since", since);
            Clicked90 = Db.Scalar<int>("SELECT COUNT(*) FROM ReviewRequests WHERE SentOn >= @since AND ClickedOn IS NOT NULL", since);
        }

        private void Handle()
        {
            var f = Request.Form;
            if (f["act"] == "refresh")
            {
                var error = Reviews.Refresh();
                if (error != null) { Err = error; return; }
                RedirectWith("/admin/reviews", "Rating updated from Google.");
                return;
            }
            Link = (f["link"] ?? "").Trim(); Rating = (f["rating"] ?? "").Trim(); Count = (f["count"] ?? "").Trim();
            if (Link != "" && !Link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { Err = "The review link must start with https://"; return; }
            double r; int c;
            if (Rating != "" && (!double.TryParse(Rating, NumberStyles.Float, CultureInfo.InvariantCulture, out r) || r < 1 || r > 5)) { Err = "The rating must be a number from 1 to 5, like 4.8."; return; }
            if (Count != "" && (!int.TryParse(Count, out c) || c < 0)) { Err = "The number of reviews must be a whole number."; return; }
            Settings.Set("reviews.link", Link == "" ? null : Link);
            Settings.Set("reviews.placeId", string.IsNullOrWhiteSpace(f["place"]) ? null : f["place"].Trim());
            if (!string.IsNullOrWhiteSpace(f["key"])) Settings.SetSecret("reviews.apiKey", f["key"].Trim());
            Settings.Set("reviews.rating", Rating == "" ? null : Rating);
            Settings.Set("reviews.count", Count == "" ? null : Count);
            Settings.Set("reviews.show", f["show"] == "1" ? "1" : "0");
            Reviews.Invalidate();
            string msg = "Saved.";
            if (!string.IsNullOrWhiteSpace(f["key"]))
            {
                var error = Reviews.Refresh();
                msg = error == null ? "Saved and connected to Google. Your rating is up to date." : "Saved, but Google did not answer: " + error;
            }
            RedirectWith("/admin/reviews", msg);
        }
    }
}
