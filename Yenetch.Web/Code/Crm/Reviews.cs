using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class GoogleReview
    {
        public string Author { get; set; }
        public string Photo { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; }
        public string When { get; set; }
    }

    public class ReviewSummary
    {
        public double Rating { get; set; }
        public int Count { get; set; }
        public List<GoogleReview> Reviews { get; set; }
        public DateTime FetchedOn { get; set; }
    }

    /// <summary>
    /// Google reviews: asks happy clients for a review (by hand from a lead, or automatically through the "review" email series)
    /// with a tracked link (/r/{token}), and shows the rating on the website. The rating comes from the Google Places API when an
    /// API key is set (fetched twice a day; Google's free monthly allowance easily covers that), otherwise from the numbers typed
    /// in /admin/reviews.
    /// </summary>
    public static class Reviews
    {
        // ---- Settings ----------------------------------------------------------------------------------------

        public static string PlaceId { get { return Settings.Get("reviews.placeId"); } }
        public static bool ShowOnSite { get { return Settings.Get("reviews.show") == "1"; } }

        /// <summary>Where "Leave a review" goes: the link typed in the admin, or Google's write-review page for the Place ID.</summary>
        public static string ReviewUrl
        {
            get
            {
                var link = Settings.Get("reviews.link");
                if (!string.IsNullOrWhiteSpace(link)) return link.Trim();
                var place = PlaceId;
                return string.IsNullOrWhiteSpace(place) ? null : "https://search.google.com/local/writereview?placeid=" + HttpUtility.UrlEncode(place.Trim());
            }
        }

        /// <summary>Google's page listing every review, for "Read all reviews".</summary>
        public static string ReadUrl
        {
            get
            {
                var place = PlaceId;
                return string.IsNullOrWhiteSpace(place) ? ReviewUrl : "https://search.google.com/local/reviews?placeid=" + HttpUtility.UrlEncode(place.Trim());
            }
        }

        // ---- Requests ----------------------------------------------------------------------------------------

        /// <summary>A link that records the click, then forwards to Google. Reuses an unclicked link sent to the same person.</summary>
        public static string TrackedUrl(string email, string name, int? leadId, int? userId)
        {
            if (ReviewUrl == null || !Newsletter.IsEmail(email)) return ReviewUrl;
            email = email.Trim().ToLowerInvariant();
            var token = Db.Scalar<string>("SELECT Token FROM ReviewRequests WHERE Email = @email AND ClickedOn IS NULL AND SentOn > @since ORDER BY Id DESC" + Db.Page(0, 1),
                new { email, since = DateTime.UtcNow.AddDays(-60) });
            if (token == null)
            {
                token = Util.NewId();
                Db.Exec("INSERT INTO ReviewRequests (LeadId, Name, Email, Token, SentOn, SentBy) VALUES (@leadId, @name, @email, @token, @now, @userId)",
                    new { leadId, name = Util.Cut(name, 120), email, token, now = DateTime.UtcNow, userId });
            }
            return Mailer.SiteUrl + "/r/" + token;
        }

        /// <summary>Emails a review request now (the button on a lead). Returns null, or why it could not be sent.</summary>
        public static string Ask(Lead l, int userId)
        {
            if (!Newsletter.IsEmail(l.Email)) return "This lead has no email address.";
            if (ReviewUrl == null) return "Add your Google review link in Reviews first.";
            var s = Automations.Get("review");
            var step = s.Steps.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Subject)) ?? new AutoStep { Subject = "How did we do?", Body = "<p>{{button:Leave a Google review:{{review_url}}}}</p>" };
            var row = new Row { { "Name", l.Name }, { "Email", l.Email }, { "RefId", l.Id }, { "Sequence", "review" } };
            var url = TrackedUrl(l.Email, l.Name, l.Id, userId);
            var body = Automations.Fill(step.Body.Replace("{{review_url}}", url), row, false);
            try
            {
                Mailer.Send(l.Email, Automations.Fill(step.Subject, row, true), Mailer.Wrap(Automations.Fill(step.Subject, row, true), body, null));
                LeadService.AddActivity(l.Id, userId, "Email", "Asked for a Google review.", null);
                return null;
            }
            catch (Exception ex) { return "The email could not be sent: " + ex.Message; }
        }

        /// <summary>Records the click on /r/{token} and returns where to send the visitor.</summary>
        public static string Click(string token)
        {
            if (!string.IsNullOrEmpty(token) && token.Length <= 40)
                Db.Exec("UPDATE ReviewRequests SET ClickedOn = @now WHERE Token = @token AND ClickedOn IS NULL", new { now = DateTime.UtcNow, token });
            return ReviewUrl ?? Mailer.SiteUrl;
        }

        public static List<Row> Requests(int count)
        {
            return Db.Rows("SELECT r.*, u.Name AS SentByName FROM ReviewRequests r LEFT JOIN CrmUsers u ON u.Id = r.SentBy ORDER BY r.Id DESC" + Db.Page(0, count));
        }

        /// <summary>When a lead is marked Won, the review series starts for them (if it is turned on).</summary>
        public static void OnWon(Lead l)
        {
            if (l != null && Newsletter.IsEmail(l.Email) && ReviewUrl != null) Automations.Enroll("review", l.Email, l.Name, l.Id);
        }

        // ---- Rating on the website ---------------------------------------------------------------------------

        public static ReviewSummary Summary
        {
            get
            {
                var cached = HttpRuntime.Cache["reviews.summary"] as ReviewSummary;
                if (cached != null) return cached;
                ReviewSummary s = null;
                try { var json = Settings.Get("reviews.cache"); if (!string.IsNullOrEmpty(json)) s = new JavaScriptSerializer().Deserialize<ReviewSummary>(json); } catch { }
                double manual; int manualCount;
                if ((s == null || s.Count == 0) && double.TryParse(Settings.Get("reviews.rating"), NumberStyles.Float, CultureInfo.InvariantCulture, out manual) && int.TryParse(Settings.Get("reviews.count"), out manualCount) && manual > 0)
                    s = new ReviewSummary { Rating = manual, Count = manualCount, Reviews = new List<GoogleReview>() };
                s = s ?? new ReviewSummary { Reviews = new List<GoogleReview>() };
                if (s.Reviews == null) s.Reviews = new List<GoogleReview>();
                HttpRuntime.Cache.Insert("reviews.summary", s, null, DateTime.UtcNow.AddMinutes(30), System.Web.Caching.Cache.NoSlidingExpiration);
                return s;
            }
        }

        public static void Invalidate() { HttpRuntime.Cache.Remove("reviews.summary"); }

        /// <summary>Fetches rating and latest reviews from Google. Returns null, or the error. Called twice a day and from the admin.</summary>
        public static string Refresh()
        {
            var key = Settings.GetSecret("reviews.apiKey");
            var place = PlaceId;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(place)) return "Add the Place ID and an API key first.";
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var req = (HttpWebRequest)WebRequest.Create("https://places.googleapis.com/v1/places/" + Uri.EscapeDataString(place.Trim()) + "?languageCode=en");
                req.Headers["X-Goog-Api-Key"] = key.Trim();
                req.Headers["X-Goog-FieldMask"] = "rating,userRatingCount,reviews";
                req.Timeout = 15000;
                string json;
                using (var res = (HttpWebResponse)req.GetResponse())
                using (var rd = new System.IO.StreamReader(res.GetResponseStream(), Encoding.UTF8)) json = rd.ReadToEnd();
                var d = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                var s = new ReviewSummary { FetchedOn = DateTime.UtcNow, Reviews = new List<GoogleReview>() };
                object v;
                if (d.TryGetValue("rating", out v)) s.Rating = Convert.ToDouble(v, CultureInfo.InvariantCulture);
                if (d.TryGetValue("userRatingCount", out v)) s.Count = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                if (d.TryGetValue("reviews", out v) && v is object[])
                    foreach (Dictionary<string, object> r in (object[])v)
                    {
                        var g = new GoogleReview();
                        object x;
                        if (r.TryGetValue("rating", out x)) g.Rating = Convert.ToInt32(x, CultureInfo.InvariantCulture);
                        if (r.TryGetValue("relativePublishTimeDescription", out x)) g.When = Convert.ToString(x);
                        if (r.TryGetValue("text", out x) && x is Dictionary<string, object>) g.Text = Convert.ToString(((Dictionary<string, object>)x)["text"]);
                        if (r.TryGetValue("authorAttribution", out x) && x is Dictionary<string, object>)
                        {
                            var a = (Dictionary<string, object>)x; object n;
                            if (a.TryGetValue("displayName", out n)) g.Author = Convert.ToString(n);
                            if (a.TryGetValue("photoUri", out n)) g.Photo = Convert.ToString(n);
                        }
                        s.Reviews.Add(g);
                    }
                Settings.Set("reviews.cache", new JavaScriptSerializer().Serialize(s));
                Invalidate();
                return null;
            }
            catch (WebException ex)
            {
                var detail = "";
                try { using (var rd = new System.IO.StreamReader(ex.Response.GetResponseStream())) detail = rd.ReadToEnd(); } catch { }
                var m = System.Text.RegularExpressions.Regex.Match(detail, "\"message\":\\s*\"([^\"]+)\"");
                return "Google said: " + (m.Success ? m.Groups[1].Value : ex.Message);
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>Refreshes from Google at most every 12 hours (called by the minute timer).</summary>
        public static void RefreshIfStale()
        {
            if (string.IsNullOrWhiteSpace(PlaceId) || string.IsNullOrEmpty(Settings.Get("reviews.apiKey"))) return;
            DateTime last;
            if (DateTime.TryParse(Settings.Get("reviews.lastTry"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out last) && last > DateTime.UtcNow.AddHours(-12)) return;
            Settings.Set("reviews.lastTry", DateTime.UtcNow.ToString("o"));
            var err = Refresh();
            if (err != null) Mailer.Log("google reviews", new Exception(err));
        }

        /// <summary>The rating block for the website (home and contact pages). Empty until reviews are switched on in the admin.</summary>
        public static string WidgetHtml()
        {
            try
            {
                if (!ShowOnSite) return "";
                var s = Summary;
                if (s.Count <= 0 || s.Rating <= 0) return "";
                var sb = new StringBuilder("<section class=\"sec--tight greviews\" aria-labelledby=\"greviews-title\"><div class=\"wrap\"><div class=\"greviews__head fx-up\">");
                sb.Append("<div><span class=\"kicker\">Google reviews</span><h2 id=\"greviews-title\"><b class=\"greviews__score\">" + s.Rating.ToString("0.0", CultureInfo.InvariantCulture) + "</b>")
                  .Append(Stars(s.Rating)).Append("<span class=\"greviews__count\">from " + s.Count.ToString("N0", CultureInfo.InvariantCulture) + " reviews</span></h2></div>");
                sb.Append("<div class=\"greviews__actions\">");
                if (ReadUrl != null) sb.Append("<a class=\"btn btn--line\" href=\"" + HttpUtility.HtmlAttributeEncode(ReadUrl) + "\" target=\"_blank\" rel=\"noopener\">Read reviews on Google</a>");
                if (ReviewUrl != null) sb.Append("<a class=\"btn btn--blue\" href=\"" + HttpUtility.HtmlAttributeEncode(ReviewUrl) + "\" target=\"_blank\" rel=\"noopener\">Write a review</a>");
                sb.Append("</div></div>");
                var good = s.Reviews.Where(r => r.Rating >= 4 && !string.IsNullOrWhiteSpace(r.Text)).Take(3).ToList();
                if (good.Count > 0)
                {
                    sb.Append("<div class=\"greviews__list fx-stagger\">");
                    foreach (var r in good)
                        sb.Append("<figure class=\"greview\">" + Stars(r.Rating) + "<blockquote>" + HttpUtility.HtmlEncode(Util.Cut(r.Text, 320)) + "</blockquote><figcaption>"
                                  + (string.IsNullOrEmpty(r.Photo) ? "<span class=\"greview__i\">" + HttpUtility.HtmlEncode((r.Author ?? "G").Substring(0, 1)) + "</span>" : "<img src=\"" + HttpUtility.HtmlAttributeEncode(r.Photo) + "\" alt=\"\" width=\"36\" height=\"36\" loading=\"lazy\" referrerpolicy=\"no-referrer\">")
                                  + "<span><b>" + HttpUtility.HtmlEncode(r.Author) + "</b><small>" + HttpUtility.HtmlEncode(r.When) + " on Google</small></span></figcaption></figure>");
                    sb.Append("</div>");
                }
                return sb.Append("</div></section>").ToString();
            }
            catch { return ""; }
        }

        private static string Stars(double rating)
        {
            var sb = new StringBuilder("<span class=\"stars\" role=\"img\" aria-label=\"" + rating.ToString("0.0", CultureInfo.InvariantCulture) + " out of 5 stars\">");
            for (var i = 1; i <= 5; i++) sb.Append("<i class=\"" + (rating >= i - 0.25 ? "on" : rating >= i - 0.75 ? "half" : "") + "\"></i>");
            return sb.Append("</span>").ToString();
        }
    }
}
