using System;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Yenetch.Crm
{
    public static class Util
    {
        /// <summary>Absolute path of a file in the site folder. Works outside a request too (background sends, timers), unlike MapPath.</summary>
        public static string AppPath(string relative)
        {
            return System.IO.Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        /// <summary>India Standard Time offset. Stored dates are UTC; the admin shows IST.</summary>
        public static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);
        public static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");

        public static DateTime Ist(DateTime utc) { return utc.Add(IstOffset); }
        public static DateTime FromIst(DateTime ist) { return DateTime.SpecifyKind(ist.Subtract(IstOffset), DateTimeKind.Utc); }
        public static DateTime TodayIst { get { return Ist(DateTime.UtcNow).Date; } }

        public static string Date(DateTime? utc) { return utc.HasValue ? Ist(utc.Value).ToString("d MMM yyyy", India) : ""; }
        public static string When(DateTime? utc) { return utc.HasValue ? Ist(utc.Value).ToString("d MMM yyyy, h:mm tt", India) : ""; }
        public static string Time(DateTime? utc) { return utc.HasValue ? Ist(utc.Value).ToString("h:mm tt", India) : ""; }
        public static string InputDateTime(DateTime? utc) { return utc.HasValue ? Ist(utc.Value).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture) : ""; }

        /// <summary>Parses a datetime-local or date input (IST) to UTC.</summary>
        public static DateTime? ParseInput(string s)
        {
            DateTime d;
            if (string.IsNullOrWhiteSpace(s)) return null;
            var formats = new[] { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd" };
            return DateTime.TryParseExact(s.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? (DateTime?)FromIst(d) : null;
        }

        /// <summary>"3 min ago", "2 h ago", "5 d ago", then a date.</summary>
        public static string Ago(DateTime? utc)
        {
            if (!utc.HasValue) return "";
            var s = DateTime.UtcNow - utc.Value;
            if (s.TotalSeconds < 0) return "in " + Span(-s);
            if (s.TotalMinutes < 1) return "just now";
            if (s.TotalDays > 30) return Date(utc);
            return Span(s) + " ago";
        }

        public static string Span(TimeSpan s)
        {
            if (s.TotalMinutes < 60) return Math.Max(1, (int)s.TotalMinutes) + " min";
            if (s.TotalHours < 24) return (int)s.TotalHours + " h";
            return (int)s.TotalDays + " d";
        }

        /// <summary>Seconds as "1m 20s".</summary>
        public static string Duration(double seconds)
        {
            var s = (int)Math.Round(seconds);
            if (s < 60) return s + "s";
            if (s < 3600) return (s / 60) + "m " + (s % 60).ToString("00") + "s";
            return (s / 3600) + "h " + (s % 3600 / 60).ToString("00") + "m";
        }

        public static string Money(decimal? v) { return v.HasValue ? "₹" + v.Value.ToString("#,##0", India) : ""; }
        public static string Num(double v) { return v.ToString("#,##0", India); }
        public static string Pct(double part, double whole) { return whole <= 0 ? "0%" : (part * 100 / whole).ToString(part * 100 / whole < 10 ? "0.#" : "0", CultureInfo.InvariantCulture) + "%"; }

        public static string Cut(string s, int max)
        {
            if (s == null) return null;
            s = s.Trim();
            return s.Length > max ? s.Substring(0, max) : s;
        }

        public static string Digits(string s) { return new string((s ?? "").Where(char.IsDigit).ToArray()); }

        public static string NewId() { return Guid.NewGuid().ToString("N"); }

        public static string H(object o) { return HttpUtility.HtmlEncode(Convert.ToString(o, CultureInfo.InvariantCulture)); }

        /// <summary>CSV cell, quoted, with spreadsheet formula injection neutralised.</summary>
        public static string CsvCell(string v)
        {
            v = v ?? "";
            if (v.Length > 0 && "=+-@\t\r".IndexOf(v[0]) >= 0) v = "'" + v;
            return "\"" + v.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }

        public static string Initials(string name)
        {
            var parts = (name ?? "?").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "?" : (parts[0].Substring(0, 1) + (parts.Length > 1 ? parts[parts.Length - 1].Substring(0, 1) : "")).ToUpperInvariant();
        }

        /// <summary>CSS modifier for a lead status badge.</summary>
        public static string StatusCss(string status)
        {
            switch (status)
            {
                case "New": return "new";
                case "Won": return "won";
                case "Lost": case "Junk": return "lost";
                case "Proposal": case "Negotiation": return "hot";
                default: return "open";
            }
        }
    }
}
