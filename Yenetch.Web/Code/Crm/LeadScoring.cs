using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    /// <summary>One reason a lead scored the points it did.</summary>
    public class ScorePart { public string Label { get; set; } public int Points { get; set; } }

    /// <summary>
    /// Lead scoring: every lead gets a score from 0 to 100 so sales can call the hottest ones first. Points come from how
    /// good the contact details are, how the lead came in, the budget, how much of the website they read, and what they did
    /// with a proposal; leads that go quiet lose points. The points for each signal and the Hot/Warm thresholds can be
    /// changed in Admin &gt; Lead scoring. With automatic priority on, a lead's Hot/Warm/Cold follows its score until someone
    /// sets the priority by hand.
    /// </summary>
    public static class LeadScoring
    {
        /// <summary>Signal key, label and default points.</summary>
        public static readonly string[][] Signals =
        {
            new[] { "email", "Gave an email address", "5" },
            new[] { "workEmail", "Uses a company email (not Gmail, Yahoo…)", "8" },
            new[] { "phone", "Gave a phone number", "5" },
            new[] { "company", "Gave a company name", "2" },
            new[] { "need", "Described what they need", "5" },
            new[] { "srcBooking", "Booked a call", "20" },
            new[] { "srcBuilder", "Built a plan with prices", "20" },
            new[] { "srcReferral", "Came by referral", "15" },
            new[] { "srcForm", "Sent the contact form", "10" },
            new[] { "srcAudit", "Ran a website audit", "8" },
            new[] { "srcChat", "Came through the chatbot or solution finder", "8" },
            new[] { "srcOther", "Any other source", "4" },
            new[] { "budget5l", "Budget ₹5 lakh or more", "20" },
            new[] { "budget2l", "Budget ₹2 lakh or more", "15" },
            new[] { "budget75k", "Budget ₹75,000 or more", "10" },
            new[] { "budget25k", "Budget ₹25,000 or more", "5" },
            new[] { "pages10", "Read 10 or more pages", "6" },
            new[] { "pages4", "Read 4 or more pages", "3" },
            new[] { "visits3", "Visited 3 or more times", "5" },
            new[] { "visits2", "Visited twice", "3" },
            new[] { "sawPricing", "Looked at prices or plans", "4" },
            new[] { "sawCases", "Looked at case studies", "2" },
            new[] { "time5m", "Spent 5+ minutes on the website", "3" },
            new[] { "proposalViewed", "Opened a proposal", "8" },
            new[] { "proposalAccepted", "Accepted a proposal", "15" },
            new[] { "quiet14", "No activity for 14 days", "-5" },
            new[] { "quiet30", "No activity for 30 days", "-10" }
        };

        private static readonly HashSet<string> FreeMail = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com", "googlemail.com", "yahoo.com", "yahoo.in", "yahoo.co.in", "hotmail.com", "outlook.com", "live.com", "msn.com", "icloud.com", "me.com",
            "aol.com", "rediffmail.com", "protonmail.com", "proton.me", "zoho.com", "zohomail.in", "gmx.com", "mail.com", "yandex.com", "ymail.com"
        };

        public static Dictionary<string, int> Weights
        {
            get
            {
                var w = Signals.ToDictionary(s => s[0], s => int.Parse(s[2], CultureInfo.InvariantCulture));
                try
                {
                    var saved = Settings.Get("scoring.weights");
                    if (!string.IsNullOrEmpty(saved))
                        foreach (var kv in new JavaScriptSerializer().Deserialize<Dictionary<string, int>>(saved))
                            if (w.ContainsKey(kv.Key)) w[kv.Key] = Math.Max(-50, Math.Min(100, kv.Value));
                }
                catch { }
                return w;
            }
        }

        public static void SaveWeights(Dictionary<string, int> w) { Settings.Set("scoring.weights", new JavaScriptSerializer().Serialize(w)); }

        public static int HotAt { get { return Int("scoring.hot", 70); } }
        public static int WarmAt { get { return Int("scoring.warm", 40); } }
        public static bool AutoPriority { get { try { return Settings.Get("scoring.auto") != "0"; } catch { return true; } } }

        private static int Int(string k, int fallback) { int v; try { return int.TryParse(Settings.Get(k), out v) && v > 0 && v <= 100 ? v : fallback; } catch { return fallback; } }

        public static string Band(int? score)
        {
            if (!score.HasValue) return "";
            return score.Value >= HotAt ? "hot" : score.Value >= WarmAt ? "warm" : "cold";
        }

        /// <summary>Works out a lead's score and saves it (and its priority when automatic priority is on).</summary>
        public static int Recalc(int leadId)
        {
            try
            {
                var lead = LeadService.Get(leadId);
                if (lead == null) return 0;
                List<ScorePart> parts;
                var score = Compute(lead, out parts);
                Db.Exec("UPDATE CrmLeads SET Score = @score, ScoreJson = @json, ScoredOn = @now WHERE Id = @id",
                    new { score, json = new JavaScriptSerializer().Serialize(parts), now = DateTime.UtcNow, id = leadId });
                if (AutoPriority && lead.IsOpen)
                {
                    var priority = score >= HotAt ? "Hot" : score >= WarmAt ? "Warm" : "Cold";
                    Db.Exec("UPDATE CrmLeads SET Priority = @p WHERE Id = @id AND PriorityLocked = 0 AND Priority <> @p", new { p = priority, id = leadId });
                }
                return score;
            }
            catch (Exception ex) { Mailer.Log("lead score " + leadId, ex); return 0; }
        }

        /// <summary>Re-scores open leads (run daily, so engagement and quiet periods are counted).</summary>
        public static void RecalcOpen()
        {
            foreach (var id in Db.Query("SELECT Id FROM CrmLeads WHERE Status IN ('New','Contacted','Qualified','Proposal','Negotiation')", r => r.Int("Id")))
                Recalc(id);
        }

        public static int Compute(Lead l, out List<ScorePart> parts)
        {
            parts = new List<ScorePart>();
            if (l.Status == "Won") { parts.Add(new ScorePart { Label = "Won", Points = 100 }); return 100; }
            if (l.Status == "Lost" || l.Status == "Junk") { parts.Add(new ScorePart { Label = l.Status, Points = 0 }); return 0; }
            var w = Weights;
            var p = parts;
            Action<string> add = k => { int v; if (w.TryGetValue(k, out v) && v != 0) p.Add(new ScorePart { Label = Signals.First(s => s[0] == k)[1], Points = v }); };

            if (Newsletter.IsEmail(l.Email))
            {
                add("email");
                var domain = l.Email.Substring(l.Email.IndexOf('@') + 1).Trim();
                if (!FreeMail.Contains(domain)) add("workEmail");
            }
            if (Util.Digits(l.Phone).Length >= 8) add("phone");
            if (!string.IsNullOrWhiteSpace(l.Company)) add("company");
            if ((l.Need ?? "").Trim().Length >= 40) add("need");

            switch (l.Source)
            {
                case "Booking": add("srcBooking"); break;
                case "Plan builder": add("srcBuilder"); break;
                case "Referral": add("srcReferral"); break;
                case "Contact form": add("srcForm"); break;
                case "Website audit": add("srcAudit"); break;
                case "Chatbot": case "Solution finder": case "Landing page": add("srcChat"); break;
                default: add("srcOther"); break;
            }
            if (l.Source != "Plan builder" && Db.Scalar<int>("SELECT COUNT(*) FROM Quotes WHERE LeadId = @id", new { id = l.Id }) > 0) add("srcBuilder");

            var value = l.EstValue ?? 0;
            if (value >= 500000) add("budget5l");
            else if (value >= 200000) add("budget2l");
            else if (value >= 75000) add("budget75k");
            else if (value >= 25000) add("budget25k");

            // Website behaviour of the visitor behind the lead (only visitors who accepted analytics cookies).
            var visitorIds = Db.Query("SELECT Id FROM WebVisitors WHERE LeadId = @id", r => r.Str("Id"), new { id = l.Id });
            if (!string.IsNullOrEmpty(l.VisitorId) && !visitorIds.Contains(l.VisitorId)) visitorIds.Add(l.VisitorId);
            if (visitorIds.Count > 0)
            {
                int pages = 0, sessions = 0, seconds = 0; bool pricing = false, cases = false;
                foreach (var vid in visitorIds.Take(5))
                {
                    var v = Db.First("SELECT Pageviews, Sessions FROM WebVisitors WHERE Id = @vid", new { vid });
                    if (v != null) { pages += v.Int("Pageviews"); sessions += v.Int("Sessions"); }
                    seconds += Db.Scalar<int>("SELECT COALESCE(SUM(DurationSec), 0) FROM WebPageviews WHERE VisitorId = @vid", new { vid });
                    pricing |= Db.Scalar<int>("SELECT COUNT(*) FROM WebPageviews WHERE VisitorId = @vid AND (Path LIKE '/pricing%' OR Path LIKE '/products/%')", new { vid }) > 0;
                    cases |= Db.Scalar<int>("SELECT COUNT(*) FROM WebPageviews WHERE VisitorId = @vid AND Path LIKE '/case-studies%'", new { vid }) > 0;
                }
                if (pages >= 10) add("pages10"); else if (pages >= 4) add("pages4");
                if (sessions >= 3) add("visits3"); else if (sessions >= 2) add("visits2");
                if (pricing) add("sawPricing");
                if (cases) add("sawCases");
                if (seconds >= 300) add("time5m");
            }

            var proposal = Db.First("SELECT MAX(CASE WHEN AcceptedOn IS NOT NULL THEN 1 ELSE 0 END) AS Accepted, MAX(CASE WHEN FirstViewedOn IS NOT NULL THEN 1 ELSE 0 END) AS Viewed FROM Proposals WHERE LeadId = @id", new { id = l.Id });
            if (proposal != null && proposal.Int("Accepted") == 1) add("proposalAccepted");
            else if (proposal != null && proposal.Int("Viewed") == 1) add("proposalViewed");

            var lastTouch = Db.Scalar<DateTime?>("SELECT MAX(CreatedOn) FROM CrmActivities WHERE LeadId = @id AND Kind <> 'System'", new { id = l.Id }) ?? l.CreatedOn;
            if (l.UpdatedOn > lastTouch) lastTouch = l.UpdatedOn;
            var quiet = (DateTime.UtcNow - lastTouch).TotalDays;
            if (quiet >= 30) add("quiet30"); else if (quiet >= 14) add("quiet14");

            return Math.Max(0, Math.Min(100, parts.Sum(x => x.Points)));
        }

        public static List<ScorePart> Parts(string json)
        {
            try { return new JavaScriptSerializer().Deserialize<List<ScorePart>>(json ?? "[]") ?? new List<ScorePart>(); } catch { return new List<ScorePart>(); }
        }
    }
}
