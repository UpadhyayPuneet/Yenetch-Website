using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>
    /// Base class for every admin page: requires a signed-in, active user, applies role rules, protects postbacks
    /// against cross-site requests, and offers formatting helpers for the markup.
    /// </summary>
    public class AdminPage : Page
    {
        protected CrmUser Me { get; private set; }

        /// <summary>Override to limit a page to some roles.</summary>
        protected virtual bool Allowed(CrmUser u) { return true; }

        /// <summary>Key of the sidebar item to highlight.</summary>
        public virtual string Section { get { return ""; } }

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            Db.EnsureSchema();
            if (!Auth.AnyUsers()) { Response.Redirect("~/admin/setup", true); return; }
            Me = Auth.Current;
            if (Me == null) { Response.Redirect("~/admin/login?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl), true); return; }
            if (!Allowed(Me)) { Response.Redirect("~/admin?denied=1", true); return; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            // Ties ViewState and postbacks to this user, so another site cannot submit forms on their behalf.
            if (Me != null) ViewStateUserKey = "u" + Me.Id + "|" + (Request.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName] ?? new HttpCookie("x")).Value;
        }

        /// <summary>Pages redirect with endResponse:false (no ThreadAbortException), so skip rendering once a redirect is set.</summary>
        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (Response.IsRequestBeingRedirected) return;
            base.Render(writer);
        }

        // ---- Messages ---------------------------------------------------------------------------------------

        /// <summary>Shows a one-off confirmation on the next page (read by Admin.Master).</summary>
        protected void Flash(string message)
        {
            Response.Cookies.Add(new HttpCookie("yn_flash", HttpUtility.UrlEncode(message)) { HttpOnly = true, Path = "/admin" });
        }

        protected void RedirectWith(string url, string message)
        {
            if (message != null) Flash(message);
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ---- Query string -----------------------------------------------------------------------------------

        protected string Q(string key) { return (Request.QueryString[key] ?? "").Trim(); }
        protected int QInt(string key, int fallback = 0) { int v; return int.TryParse(Q(key), out v) ? v : fallback; }
        protected int PageNo { get { return Math.Max(1, QInt("page", 1)); } }

        /// <summary>Current URL with some query values replaced (null removes a key). Resets paging unless "page" is given.</summary>
        protected string With(params string[] kv)
        {
            var q = HttpUtility.ParseQueryString(Request.Url.Query);
            if (!kv.Contains("page")) q.Remove("page");
            for (var i = 0; i + 1 < kv.Length; i += 2)
            {
                if (string.IsNullOrEmpty(kv[i + 1])) q.Remove(kv[i]); else q[kv[i]] = kv[i + 1];
            }
            var s = q.ToString();
            return Request.Url.AbsolutePath + (s.Length > 0 ? "?" + s : "");
        }

        // ---- Date ranges ------------------------------------------------------------------------------------

        private DateTime? _from, _to;
        protected string RangeKey { get { var r = Q("range"); return r == "" ? DefaultRange : r; } }
        protected virtual string DefaultRange { get { return "30d"; } }

        /// <summary>Start of the selected range (UTC), from ?range=today|7d|30d|90d|12m|custom&from=&to= in IST days.</summary>
        protected DateTime From { get { Resolve(); return _from.Value; } }
        protected DateTime To { get { Resolve(); return _to.Value; } }

        private void Resolve()
        {
            if (_from.HasValue) return;
            var today = Util.TodayIst;
            DateTime start = today.AddDays(-29), end = today.AddDays(1);
            switch (RangeKey)
            {
                case "today": start = today; break;
                case "yesterday": start = today.AddDays(-1); end = today; break;
                case "7d": start = today.AddDays(-6); break;
                case "90d": start = today.AddDays(-89); break;
                case "12m": start = today.AddMonths(-12).AddDays(1); break;
                case "custom":
                    DateTime f, t;
                    if (DateTime.TryParseExact(Q("from"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out f)) start = f;
                    if (DateTime.TryParseExact(Q("to"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out t)) end = t.AddDays(1);
                    if (end <= start) end = start.AddDays(1);
                    break;
            }
            _from = Util.FromIst(start);
            _to = Util.FromIst(end);
        }

        protected string RangeLabel
        {
            get
            {
                var a = Util.Ist(From); var b = Util.Ist(To).AddDays(-1);
                return a.Date == b.Date ? a.ToString("d MMM yyyy", Util.India) : a.ToString("d MMM", Util.India) + " – " + b.ToString("d MMM yyyy", Util.India);
            }
        }

        /// <summary>The range picker used on analytics and report pages.</summary>
        protected string RangePicker()
        {
            var opts = new[] { "today", "Today", "7d", "7 days", "30d", "30 days", "90d", "90 days", "12m", "12 months" };
            var html = "<nav class=\"seg\" aria-label=\"Date range\">";
            for (var i = 0; i < opts.Length; i += 2)
                html += "<a href=\"" + Att(With("range", opts[i], "from", null, "to", null)) + "\"" + (RangeKey == opts[i] ? " class=\"is-on\" aria-current=\"true\"" : "") + ">" + opts[i + 1] + "</a>";
            return html + "</nav>";
        }

        // ---- Formatting helpers for markup -----------------------------------------------------------------

        protected static string H(object o) { return HttpUtility.HtmlEncode(Convert.ToString(o, CultureInfo.InvariantCulture)); }
        protected static string Att(object o) { return HttpUtility.HtmlAttributeEncode(Convert.ToString(o, CultureInfo.InvariantCulture)); }
        protected static string U(object o) { return HttpUtility.UrlEncode(Convert.ToString(o, CultureInfo.InvariantCulture)); }
        protected static string Json(object o) { return HttpUtility.HtmlAttributeEncode(new JavaScriptSerializer().Serialize(o)); }
        protected static string Date(object d) { return Util.Date(d as DateTime?); }
        protected static string When(object d) { return Util.When(d as DateTime?); }
        protected static string Ago(object d) { return Util.Ago(d as DateTime?); }
        protected static string Money(object v) { return Util.Money(v as decimal?); }
        protected static string N(double v) { return Util.Num(v); }
        protected static string Dur(double s) { return Util.Duration(s); }
        protected static string Initials(string name) { return Util.Initials(name); }
        protected static string StatusBadge(string status) { return "<span class=\"badge badge--" + Util.StatusCss(status) + "\">" + H(status) + "</span>"; }

        protected static string DueLabel(DateTime? due)
        {
            if (!due.HasValue) return "";
            var d = Util.Ist(due.Value).Date;
            var cls = due.Value < DateTime.UtcNow ? "due due--late" : d == Util.TodayIst ? "due due--today" : "due";
            var text = d == Util.TodayIst ? "Today " + Util.Time(due) : d == Util.TodayIst.AddDays(1) ? "Tomorrow " + Util.Time(due) : Util.When(due);
            if (due.Value < DateTime.UtcNow) text = "Overdue · " + text;
            return "<span class=\"" + cls + "\">" + H(text) + "</span>";
        }

        protected static string Delta(double now, double before)
        {
            var d = Stats.Delta(now, before);
            if (d == "") return "<small>No earlier data</small>";
            var cls = d == "new" ? "up" : d.StartsWith("+") ? "up" : "down";
            return "<small class=\"" + cls + "\">" + (d == "new" ? "New this period" : d + " vs previous period") + "</small>";
        }

        /// <summary>Ranked list with bars. valueFmt formats the main number, extra adds a secondary figure.</summary>
        protected static string Bars(IEnumerable<Tally> rows, string empty = "No data for this range yet.", Func<Tally, string> link = null, Func<Tally, string> extra = null, Func<Tally, string> label = null)
        {
            var list = rows.ToList();
            if (list.Count == 0) return "<p class=\"empty small\">" + H(empty) + "</p>";
            var sb = new System.Text.StringBuilder("<div class=\"bars\">");
            foreach (var t in list)
            {
                var text = label != null ? label(t) : t.Label;
                var name = link != null ? "<a href=\"" + Att(link(t)) + "\">" + H(text) + "</a>" : H(text);
                sb.Append("<div class=\"bar\" style=\"--w:" + t.Share.ToString(CultureInfo.InvariantCulture) + "%\"><span class=\"bar__label\" title=\"" + Att(text) + "\">" + name
                          + "</span><span class=\"bar__value\">" + N(t.Value) + (extra != null ? "<small>" + extra(t) + "</small>" : "<small>" + t.Pct + "</small>") + "</span></div>");
            }
            return sb.Append("</div>").ToString();
        }

        protected string Pager(int total, int size)
        {
            var pages = (int)Math.Ceiling(total / (double)size);
            var from = total == 0 ? 0 : (PageNo - 1) * size + 1;
            var to = Math.Min(total, PageNo * size);
            var html = "<div class=\"pager\"><span>" + N(from) + "–" + N(to) + " of " + N(total) + "</span><div>";
            if (PageNo > 1) html += "<a class=\"btn btn--line btn--sm\" href=\"" + Att(With("page", (PageNo - 1).ToString())) + "\">Previous</a>";
            if (PageNo < pages) html += "<a class=\"btn btn--line btn--sm\" href=\"" + Att(With("page", (PageNo + 1).ToString())) + "\">Next</a>";
            return html + "</div></div>";
        }

        /// <summary>Inline icon from a small built-in set.</summary>
        public static string Icon(string name)
        {
            string p;
            if (!Icons.TryGetValue(name, out p)) return "";
            return "<svg class=\"i\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">" + p + "</svg>";
        }

        private static readonly Dictionary<string, string> Icons = new Dictionary<string, string>
        {
            { "home", "<path d=\"M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z\"/>" },
            { "leads", "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M19 8v6M22 11h-6\"/>" },
            { "tasks", "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"3\"/><path d=\"M16 2v4M8 2v4M3 10h18M8 15l2.5 2.5L16 13\"/>" },
            { "reports", "<path d=\"M3 3v18h18\"/><path d=\"M7 15v2M11 11v6M15 7v10M19 12v5\"/>" },
            { "analytics", "<path d=\"M22 12h-4l-3 9L9 3l-3 9H2\"/>" },
            { "visitors", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18\"/>" },
            { "subscribers", "<rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"3\"/><path d=\"m22 7-10 6L2 7\"/>" },
            { "campaigns", "<path d=\"m22 2-7 20-4-9-9-4z\"/><path d=\"M22 2 11 13\"/>" },
            { "team", "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8\"/>" },
            { "account", "<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21a8 8 0 0 1 16 0\"/>" },
            { "logout", "<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9\"/>" },
            { "plus", "<path d=\"M12 5v14M5 12h14\"/>" },
            { "phone", "<path d=\"M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.4 1.8.7 2.7a2 2 0 0 1-.5 2.1L8 9.8a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 1.8.6 2.7.7a2 2 0 0 1 1.7 2z\"/>" },
            { "whatsapp", "<path d=\"M3 21l1.7-5A8.5 8.5 0 1 1 8 19.3z\"/><path d=\"M9 9.5c0 3 2.5 5.5 5.5 5.5l1.2-1.2-1.8-.9-.8.7A4 4 0 0 1 10.8 11l.7-.8-.9-1.8z\"/>" },
            { "mail", "<rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"3\"/><path d=\"m22 7-10 6L2 7\"/>" },
            { "download", "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3\"/>" },
            { "menu", "<path d=\"M3 6h18M3 12h18M3 18h18\"/>" },
            { "note", "<path d=\"M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z\"/><path d=\"M14 3v6h6M8 13h8M8 17h5\"/>" },
            { "meeting", "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"3\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/>" },
            { "back", "<path d=\"m15 18-6-6 6-6\"/>" },
            { "check", "<path d=\"m5 12 5 5 9-10\"/>" },
            { "clock", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>" },
            { "trash", "<path d=\"M3 6h18M8 6V4h8v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6\"/>" },
            { "external", "<path d=\"M15 3h6v6M10 14 21 3M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/>" },
            { "status", "<path d=\"M20 6 9 17l-5-5\"/>" },
            { "assign", "<circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M2 21v-2a4 4 0 0 1 4-4h6M16 19l2 2 4-4\"/>" },
            { "system", "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1\"/>" },
            { "globe", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18\"/>" },
            { "device", "<rect x=\"5\" y=\"2\" width=\"14\" height=\"20\" rx=\"3\"/><path d=\"M11 18h2\"/>" },
            { "content", "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"3\"/><path d=\"M3 9h18M9 21V9\"/>" },
            { "blog", "<path d=\"M12 20h9\"/><path d=\"M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z\"/>" },
            { "careers", "<rect x=\"2\" y=\"7\" width=\"20\" height=\"14\" rx=\"3\"/><path d=\"M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2M2 13h20\"/>" },
            { "star", "<path d=\"m12 3 2.8 5.7 6.2.9-4.5 4.4 1 6.2L12 17.3 6.5 20.2l1-6.2L3 9.6l6.2-.9z\"/>" },
            { "calendar", "<rect x=\"3\" y=\"4\" width=\"18\" height=\"17\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18M8 14h2M14 14h2M8 17h2\"/>" },
            { "gauge", "<path d=\"M12 14l4-4\"/><path d=\"M3.3 19a10 10 0 1 1 17.4 0z\"/>" },
            { "auto", "<path d=\"M4 4h16v12H5.2L4 17.2z\"/><path d=\"m9 8 2 2-2 2M13 12h3\"/>" },
            { "landing", "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 9h18M8 13h8M8 17h5\"/>" },
            { "code", "<path d=\"m8 6-6 6 6 6M16 6l6 6-6 6\"/>" },
            { "robot", "<rect x=\"4\" y=\"8\" width=\"16\" height=\"12\" rx=\"2\"/><path d=\"M12 4v4M9 13h.01M15 13h.01M9 17h6\"/>" },
            { "shield", "<path d=\"M12 3 4 6v6c0 5 3.4 8.3 8 9 4.6-.7 8-4 8-9V6z\"/><path d=\"m9 12 2 2 4-4\"/>" },
            { "send", "<path d=\"m22 2-7 20-4-9-9-4z\"/><path d=\"M22 2 11 13\"/>" }
        };

        public static string ActivityIcon(string kind)
        {
            switch (kind)
            {
                case "Call": return Icon("phone");
                case "Email": return Icon("mail");
                case "WhatsApp": return Icon("whatsapp");
                case "Meeting": return Icon("meeting");
                case "FollowUp": return Icon("clock");
                case "Status": return Icon("status");
                case "Assign": return Icon("assign");
                case "System": return Icon("system");
                default: return Icon("note");
            }
        }
    }
}
