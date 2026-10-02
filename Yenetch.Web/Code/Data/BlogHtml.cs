using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Yenetch.Models;

namespace Yenetch.Data
{
    /// <summary>Reads structure out of an article's HTML body: the table of contents (h2 with id)
    /// and the FAQ (h3 + p pairs after the "Frequently asked questions" heading) for FAQPage schema.</summary>
    public static class BlogHtml
    {
        private static readonly Regex H2 = new Regex("<h2[^>]*\\sid=\"([^\"]+)\"[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex QA = new Regex("<h3[^>]*>(.*?)</h3>\\s*<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex Tags = new Regex("<[^>]+>");

        private static string Text(string html) { return WebUtility.HtmlDecode(Tags.Replace(html ?? "", "")).Trim(); }

        public static List<TocItem> Toc(string body)
        {
            return H2.Matches(body ?? "").Cast<Match>()
                .Select(m => new TocItem { Id = m.Groups[1].Value, Text = Text(m.Groups[2].Value) }).ToList();
        }

        public static List<Faq> Faqs(string body)
        {
            body = body ?? "";
            var at = body.IndexOf("Frequently asked questions", System.StringComparison.OrdinalIgnoreCase);
            if (at < 0) return new List<Faq>();
            return QA.Matches(body.Substring(at)).Cast<Match>()
                .Select(m => new Faq { Q = Text(m.Groups[1].Value), A = Text(m.Groups[2].Value) }).ToList();
        }
    }
}
