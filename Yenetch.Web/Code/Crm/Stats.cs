using System;
using System.Collections.Generic;
using System.Linq;

namespace Yenetch.Crm
{
    /// <summary>A label with a count, for ranked lists and simple charts.</summary>
    public class Tally
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public double Extra { get; set; }
        public double Extra2 { get; set; }
        public double Share { get; set; }   // 0-100 of the largest value, for bar widths
        public string Pct { get; set; }     // share of the total

        public static List<Tally> Rank(List<Row> rows, string label = "Label", string value = "Value")
        {
            var list = rows.Select(r => new Tally { Label = r.Str(label) ?? "(not set)", Value = r.Dbl(value), Extra = r.Dbl("Extra"), Extra2 = r.Dbl("Extra2") }).ToList();
            var max = list.Count == 0 ? 0 : list.Max(t => t.Value);
            var sum = list.Sum(t => t.Value);
            foreach (var t in list) { t.Share = max <= 0 ? 0 : Math.Round(t.Value * 100 / max, 1); t.Pct = Util.Pct(t.Value, sum); }
            return list;
        }
    }

    public class DayPoint { public string Day { get; set; } public double Visitors { get; set; } public double Sessions { get; set; } public double Pageviews { get; set; } public double Leads { get; set; } }

    public class Overview
    {
        public double Visitors, Sessions, Pageviews, Leads, Bounce, AvgDuration, PagesPerSession, Live, Won;
        public double PrevVisitors, PrevLeads, PrevPageviews, PrevSessions;
        public string Conversion { get { return Util.Pct(Leads, Visitors); } }
    }

    /// <summary>Reporting queries for the admin dashboard, analytics and lead reports. Ranges are [from, to) in UTC.</summary>
    public static class Stats
    {
        private static object R(DateTime from, DateTime to) { return new { from, to }; }

        public static Overview Overview(DateTime from, DateTime to)
        {
            var o = new Overview();
            var s = Db.First(@"SELECT COUNT(*) AS Sessions,
                    COUNT(DISTINCT VisitorId) AS Uniques,
                    SUM(CASE WHEN Consent = 0 THEN 1 ELSE 0 END) AS Anon,
                    SUM(CASE WHEN Consent = 1 THEN 1 ELSE 0 END) AS Tracked,
                    SUM(CASE WHEN Consent = 1 AND Pageviews <= 1 THEN 1 ELSE 0 END) AS Bounces,
                    AVG(CASE WHEN Consent = 1 THEN " + Db.Seconds("StartedOn", "LastOn") + @" * 1.0 END) AS AvgDur,
                    AVG(CASE WHEN Consent = 1 THEN Pageviews * 1.0 END) AS Pps
                  FROM WebSessions WHERE StartedOn >= @from AND StartedOn < @to", R(from, to));
            o.Sessions = s.Dbl("Sessions");
            o.Visitors = s.Dbl("Uniques") + s.Dbl("Anon");
            o.Bounce = s.Dbl("Tracked") > 0 ? s.Dbl("Bounces") * 100 / s.Dbl("Tracked") : 0;
            o.AvgDuration = s.Dbl("AvgDur");
            o.PagesPerSession = s.Dbl("Pps");
            o.Pageviews = Db.Scalar<double>("SELECT COUNT(*) FROM WebPageviews WHERE ViewedOn >= @from AND ViewedOn < @to", R(from, to));
            o.Leads = Db.Scalar<double>("SELECT COUNT(*) FROM CrmLeads WHERE CreatedOn >= @from AND CreatedOn < @to", R(from, to));
            o.Won = Db.Scalar<double>("SELECT COUNT(*) FROM CrmLeads WHERE Status = 'Won' AND UpdatedOn >= @from AND UpdatedOn < @to", R(from, to));
            o.Live = Db.Scalar<double>("SELECT COUNT(*) FROM WebSessions WHERE LastOn >= @since", new { since = DateTime.UtcNow.AddMinutes(-5) });

            var span = to - from;
            var p = Db.First(@"SELECT COUNT(*) AS Sessions, COUNT(DISTINCT VisitorId) AS Uniques, SUM(CASE WHEN Consent = 0 THEN 1 ELSE 0 END) AS Anon
                               FROM WebSessions WHERE StartedOn >= @from AND StartedOn < @to", R(from - span, from));
            o.PrevSessions = p.Dbl("Sessions");
            o.PrevVisitors = p.Dbl("Uniques") + p.Dbl("Anon");
            o.PrevPageviews = Db.Scalar<double>("SELECT COUNT(*) FROM WebPageviews WHERE ViewedOn >= @from AND ViewedOn < @to", R(from - span, from));
            o.PrevLeads = Db.Scalar<double>("SELECT COUNT(*) FROM CrmLeads WHERE CreatedOn >= @from AND CreatedOn < @to", R(from - span, from));
            return o;
        }

        /// <summary>Change against the previous period of equal length, as "+12%" / "-4%" / "".</summary>
        public static string Delta(double now, double before)
        {
            if (before <= 0) return now > 0 ? "new" : "";
            var d = (now - before) * 100 / before;
            return (d >= 0 ? "+" : "−") + Math.Abs(Math.Round(d)).ToString("0") + "%";
        }

        public static List<DayPoint> Daily(DateTime from, DateTime to)
        {
            var days = new SortedDictionary<string, DayPoint>();
            for (var d = Util.Ist(from).Date; d < Util.Ist(to); d = d.AddDays(1)) days[d.ToString("yyyy-MM-dd")] = new DayPoint { Day = d.ToString("yyyy-MM-dd") };
            var day = Db.LocalDay("StartedOn");
            foreach (var r in Db.Rows("SELECT " + day + " AS D, COUNT(*) AS S, COUNT(DISTINCT VisitorId) + SUM(CASE WHEN Consent = 0 THEN 1 ELSE 0 END) AS V FROM WebSessions WHERE StartedOn >= @from AND StartedOn < @to GROUP BY " + day, R(from, to)))
                if (days.ContainsKey(r.Str("D"))) { days[r.Str("D")].Sessions = r.Dbl("S"); days[r.Str("D")].Visitors = r.Dbl("V"); }
            day = Db.LocalDay("ViewedOn");
            foreach (var r in Db.Rows("SELECT " + day + " AS D, COUNT(*) AS N FROM WebPageviews WHERE ViewedOn >= @from AND ViewedOn < @to GROUP BY " + day, R(from, to)))
                if (days.ContainsKey(r.Str("D"))) days[r.Str("D")].Pageviews = r.Dbl("N");
            day = Db.LocalDay("CreatedOn");
            foreach (var r in Db.Rows("SELECT " + day + " AS D, COUNT(*) AS N FROM CrmLeads WHERE CreatedOn >= @from AND CreatedOn < @to GROUP BY " + day, R(from, to)))
                if (days.ContainsKey(r.Str("D"))) days[r.Str("D")].Leads = r.Dbl("N");
            return days.Values.ToList();
        }

        private static readonly HashSet<string> SessionDims = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Channel", "Source", "Medium", "Campaign", "Country", "Region", "City", "Device", "Browser", "Os", "Landing", "ExitPage", "Screen", "Lang" };

        /// <summary>Sessions by one dimension. Extra = leads-converting visitors, Extra2 = bounce % (tracked sessions).</summary>
        public static List<Tally> By(string dim, DateTime from, DateTime to, int top = 10)
        {
            if (!SessionDims.Contains(dim)) throw new ArgumentException("dim");
            var col = "s." + dim;
            return Tally.Rank(Db.Rows(@"SELECT " + col + @" AS Label, COUNT(*) AS Value,
                    COUNT(DISTINCT CASE WHEN v.LeadId IS NOT NULL THEN s.VisitorId END) AS Extra,
                    100.0 * SUM(CASE WHEN s.Consent = 1 AND s.Pageviews <= 1 THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN s.Consent = 1 THEN 1 ELSE 0 END), 0) AS Extra2
                  FROM WebSessions s LEFT JOIN WebVisitors v ON v.Id = s.VisitorId
                  WHERE s.StartedOn >= @from AND s.StartedOn < @to AND " + col + @" IS NOT NULL
                  GROUP BY " + col + " ORDER BY COUNT(*) DESC" + Db.Page(0, top), R(from, to)));
        }

        /// <summary>Pages by views. Extra = average seconds on page, Extra2 = average scroll depth.</summary>
        public static List<Tally> Pages(DateTime from, DateTime to, int top = 15)
        {
            return Tally.Rank(Db.Rows(@"SELECT Path AS Label, COUNT(*) AS Value, AVG(DurationSec * 1.0) AS Extra, AVG(ScrollPct * 1.0) AS Extra2
                  FROM WebPageviews WHERE ViewedOn >= @from AND ViewedOn < @to GROUP BY Path ORDER BY COUNT(*) DESC" + Db.Page(0, top), R(from, to)));
        }

        public static List<Tally> Events(DateTime from, DateTime to, int top = 15)
        {
            return Tally.Rank(Db.Rows(@"SELECT Name AS Label, COUNT(*) AS Value, COUNT(DISTINCT VisitorId) AS Extra FROM WebEvents
                  WHERE OccurredOn >= @from AND OccurredOn < @to GROUP BY Name ORDER BY COUNT(*) DESC" + Db.Page(0, top), R(from, to)));
        }

        public static List<Tally> EventLabels(string name, DateTime from, DateTime to, int top = 10)
        {
            return Tally.Rank(Db.Rows(@"SELECT Label, COUNT(*) AS Value FROM WebEvents WHERE Name = @name AND OccurredOn >= @from AND OccurredOn < @to AND Label IS NOT NULL
                  GROUP BY Label ORDER BY COUNT(*) DESC" + Db.Page(0, top), new { name, from, to }));
        }

        /// <summary>Pageviews per IST hour of day (24 entries).</summary>
        public static List<Tally> Hours(DateTime from, DateTime to)
        {
            var h = Db.LocalHour("ViewedOn");
            var found = Db.Rows("SELECT " + h + " AS H, COUNT(*) AS N FROM WebPageviews WHERE ViewedOn >= @from AND ViewedOn < @to GROUP BY " + h, R(from, to))
                          .ToDictionary(r => r.Int("H"), r => r.Dbl("N"));
            var rows = Enumerable.Range(0, 24).Select(i => { var r = new Row(); r["Label"] = i.ToString("00"); double n; r["Value"] = found.TryGetValue(i, out n) ? n : 0; return r; }).ToList();
            return Tally.Rank(rows);
        }

        // ---- Visitors ---------------------------------------------------------------------------------------

        public class VisitorRow
        {
            public string Id { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public int Sessions { get; set; }
            public int Pageviews { get; set; }
            public string Place { get; set; }
            public string Device { get; set; }
            public string Browser { get; set; }
            public string Os { get; set; }
            public string Channel { get; set; }
            public string Source { get; set; }
            public string Landing { get; set; }
            public string Ip { get; set; }
            public int? LeadId { get; set; }
            public string LeadName { get; set; }
            public bool IsLive { get { return (DateTime.UtcNow - LastSeen).TotalMinutes < 5; } }

            internal static VisitorRow From(Row r)
            {
                return new VisitorRow
                {
                    Id = r.Str("Id"), FirstSeen = r.Date("FirstSeen"), LastSeen = r.Date("LastSeen"), Sessions = r.Int("Sessions"), Pageviews = r.Int("Pageviews"),
                    Place = string.Join(", ", new[] { r.Str("City"), r.Str("Region"), r.Str("Country") }.Where(x => !string.IsNullOrEmpty(x)).Distinct()),
                    Device = r.Str("Device"), Browser = r.Str("Browser"), Os = r.Str("Os"), Channel = r.Str("FirstChannel"), Source = r.Str("FirstSource"),
                    Landing = r.Str("FirstLanding"), Ip = r.Str("LastIp"), LeadId = r.IntN("LeadId"), LeadName = r.Str("LeadName")
                };
            }
        }

        public static List<VisitorRow> Visitors(string search, bool leadsOnly, bool liveOnly, int offset, int count, out int total)
        {
            var w = new List<string>();
            var a = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(search)) { w.Add("(v.City LIKE @q OR v.Country LIKE @q OR v.LastIp LIKE @q OR v.FirstSource LIKE @q OR v.Id LIKE @q OR l.Name LIKE @q)"); a["q"] = "%" + search.Trim() + "%"; }
            if (leadsOnly) w.Add("v.LeadId IS NOT NULL");
            if (liveOnly) { w.Add("v.LastSeen >= @since"); a["since"] = DateTime.UtcNow.AddMinutes(-5); }
            var where = w.Count == 0 ? "" : " WHERE " + string.Join(" AND ", w);
            total = Db.Scalar<int>("SELECT COUNT(*) FROM WebVisitors v LEFT JOIN CrmLeads l ON l.Id = v.LeadId" + where, a);
            return Db.Query("SELECT v.*, l.Name AS LeadName FROM WebVisitors v LEFT JOIN CrmLeads l ON l.Id = v.LeadId" + where + " ORDER BY v.LastSeen DESC" + Db.Page(offset, count), VisitorRow.From, a);
        }

        public static VisitorRow Visitor(string id)
        {
            var r = Db.First("SELECT v.*, l.Name AS LeadName FROM WebVisitors v LEFT JOIN CrmLeads l ON l.Id = v.LeadId WHERE v.Id = @id", new { id });
            return r == null ? null : VisitorRow.From(r);
        }

        public class JourneyStep
        {
            public DateTime At { get; set; }
            public string Kind { get; set; }   // page | event
            public string Path { get; set; }
            public string Title { get; set; }
            public int? Seconds { get; set; }
            public int? Scroll { get; set; }
            public string Label { get; set; }
        }

        public class SessionView
        {
            public string Id { get; set; }
            public DateTime StartedOn { get; set; }
            public DateTime LastOn { get; set; }
            public int Pageviews { get; set; }
            public string Channel { get; set; }
            public string Source { get; set; }
            public string Campaign { get; set; }
            public string Referrer { get; set; }
            public string Device { get; set; }
            public string Browser { get; set; }
            public string Os { get; set; }
            public string Screen { get; set; }
            public string Place { get; set; }
            public string Ip { get; set; }
            public string Lang { get; set; }
            public string Duration { get { return Util.Duration((LastOn - StartedOn).TotalSeconds); } }
            public List<JourneyStep> Steps { get; set; }
        }

        /// <summary>Every session of a visitor, newest first, each with its pages and events in order.</summary>
        public static List<SessionView> Journey(string visitorId, int maxSessions = 30)
        {
            var sessions = Db.Query("SELECT * FROM WebSessions WHERE VisitorId = @id ORDER BY StartedOn DESC" + Db.Page(0, maxSessions), r => new SessionView
            {
                Id = r.Str("Id"), StartedOn = r.Date("StartedOn"), LastOn = r.Date("LastOn"), Pageviews = r.Int("Pageviews"), Channel = r.Str("Channel"),
                Source = r.Str("Source"), Campaign = r.Str("Campaign"), Referrer = r.Str("Referrer"), Device = r.Str("Device"), Browser = r.Str("Browser"),
                Os = r.Str("Os"), Screen = r.Str("Screen"), Ip = r.Str("Ip"), Lang = r.Str("Lang"),
                Place = string.Join(", ", new[] { r.Str("City"), r.Str("Region"), r.Str("Country") }.Where(x => !string.IsNullOrEmpty(x)).Distinct()),
                Steps = new List<JourneyStep>()
            }, new { id = visitorId });
            var byId = sessions.ToDictionary(s => s.Id);
            foreach (var r in Db.Rows("SELECT SessionId, ViewedOn, Path, Title, DurationSec, ScrollPct FROM WebPageviews WHERE VisitorId = @id ORDER BY ViewedOn", new { id = visitorId }))
            {
                SessionView s;
                if (r.Str("SessionId") != null && byId.TryGetValue(r.Str("SessionId"), out s))
                    s.Steps.Add(new JourneyStep { At = r.Date("ViewedOn"), Kind = "page", Path = r.Str("Path"), Title = r.Str("Title"), Seconds = r.IntN("DurationSec"), Scroll = r.IntN("ScrollPct") });
            }
            foreach (var r in Db.Rows("SELECT SessionId, OccurredOn, Name, Label, Path FROM WebEvents WHERE VisitorId = @id ORDER BY OccurredOn", new { id = visitorId }))
            {
                SessionView s;
                var sid = r.Str("SessionId");
                // Server events (lead submitted) have no session: attach to the session running at that time.
                if (sid == null) { var at = r.Date("OccurredOn"); s = sessions.FirstOrDefault(x => x.StartedOn <= at && at <= x.LastOn.AddMinutes(30)); }
                else byId.TryGetValue(sid, out s);
                if (s != null) s.Steps.Add(new JourneyStep { At = r.Date("OccurredOn"), Kind = "event", Path = r.Str("Path"), Title = r.Str("Name"), Label = r.Str("Label") });
            }
            foreach (var s in sessions) s.Steps = s.Steps.OrderBy(x => x.At).ToList();
            return sessions;
        }

        // ---- Lead reports ------------------------------------------------------------------------------------

        public static List<Tally> LeadsBy(string column, DateTime from, DateTime to)
        {
            var allowed = new[] { "Status", "LeadType", "Source", "Channel", "Priority", "City", "Interest" };
            if (!allowed.Contains(column)) throw new ArgumentException("column");
            return Tally.Rank(Db.Rows("SELECT COALESCE(" + column + @", '(not set)') AS Label, COUNT(*) AS Value,
                    SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) AS Extra, SUM(CASE WHEN Status = 'Won' THEN COALESCE(EstValue, 0) ELSE 0 END) AS Extra2
                  FROM CrmLeads WHERE CreatedOn >= @from AND CreatedOn < @to GROUP BY COALESCE(" + column + ", '(not set)') ORDER BY COUNT(*) DESC", R(from, to)));
        }

        public class OwnerRow
        {
            public string Name { get; set; }
            public int Leads { get; set; }
            public int Open { get; set; }
            public int Won { get; set; }
            public int Lost { get; set; }
            public decimal WonValue { get; set; }
            public int Overdue { get; set; }
            public int Activities { get; set; }
            public string WinRate { get { return Util.Pct(Won, Won + Lost); } }
        }

        public static List<OwnerRow> ByOwner(DateTime from, DateTime to)
        {
            var open = "'" + string.Join("','", Lists.OpenStatuses) + "'";
            return Db.Query(@"SELECT COALESCE(u.Name, 'Unassigned') AS Name, COUNT(*) AS Leads,
                    SUM(CASE WHEN l.Status IN (" + open + @") THEN 1 ELSE 0 END) AS OpenN,
                    SUM(CASE WHEN l.Status = 'Won' THEN 1 ELSE 0 END) AS Won, SUM(CASE WHEN l.Status = 'Lost' THEN 1 ELSE 0 END) AS Lost,
                    SUM(CASE WHEN l.Status = 'Won' THEN COALESCE(l.EstValue, 0) ELSE 0 END) AS WonValue,
                    SUM(CASE WHEN l.Status IN (" + open + @") AND l.NextFollowUp < @now THEN 1 ELSE 0 END) AS Overdue,
                    (SELECT COUNT(*) FROM CrmActivities a WHERE a.UserId = u.Id AND a.CreatedOn >= @from AND a.CreatedOn < @to AND a.Kind NOT IN ('System','Status','Assign')) AS Acts
                  FROM CrmLeads l LEFT JOIN CrmUsers u ON u.Id = l.AssignedTo
                  WHERE l.CreatedOn >= @from AND l.CreatedOn < @to GROUP BY u.Id, u.Name ORDER BY COUNT(*) DESC",
                r => new OwnerRow { Name = r.Str("Name"), Leads = r.Int("Leads"), Open = r.Int("OpenN"), Won = r.Int("Won"), Lost = r.Int("Lost"),
                                    WonValue = r.DecN("WonValue") ?? 0, Overdue = r.Int("Overdue"), Activities = r.Int("Acts") },
                new { from, to, now = DateTime.UtcNow });
        }

        public static List<Tally> LeadsByMonth(int months)
        {
            var from = Util.FromIst(new DateTime(Util.TodayIst.Year, Util.TodayIst.Month, 1).AddMonths(-(months - 1)));
            var m = Db.LocalMonth("CreatedOn");
            var found = Db.Rows("SELECT " + m + " AS M, COUNT(*) AS N, SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) AS W FROM CrmLeads WHERE CreatedOn >= @from GROUP BY " + m, new { from })
                          .ToDictionary(r => r.Str("M"), r => r);
            var rows = new List<Row>();
            for (var i = 0; i < months; i++)
            {
                var d = Util.Ist(from).AddMonths(i);
                var key = d.ToString("yyyy-MM");
                Row f;
                var r = new Row(); r["Label"] = d.ToString("MMM yy"); r["Value"] = found.TryGetValue(key, out f) ? f.Dbl("N") : 0; r["Extra"] = f != null ? f.Dbl("W") : 0;
                rows.Add(r);
            }
            return Tally.Rank(rows);
        }

        /// <summary>Median hours from a lead arriving to its first call, email, WhatsApp or meeting.</summary>
        public static double? MedianFirstResponseHours(DateTime from, DateTime to)
        {
            var list = Db.Rows(@"SELECT l.CreatedOn AS C, MIN(a.CreatedOn) AS F FROM CrmLeads l JOIN CrmActivities a ON a.LeadId = l.Id
                    AND a.Kind IN ('Call','Email','WhatsApp','Meeting') WHERE l.CreatedOn >= @from AND l.CreatedOn < @to GROUP BY l.Id, l.CreatedOn", R(from, to))
                .Select(r => (r.Date("F") - r.Date("C")).TotalHours).Where(h => h >= 0).OrderBy(h => h).ToList();
            if (list.Count == 0) return null;
            return list.Count % 2 == 1 ? list[list.Count / 2] : (list[list.Count / 2 - 1] + list[list.Count / 2]) / 2;
        }
    }
}
