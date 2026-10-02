using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Yenetch.Data
{
    /// <summary>
    /// Cleans HTML written in the admin editors (blog posts, legal pages, rich text fields) before it is saved:
    /// keeps formatting, headings, lists, links, images, tables and YouTube/Vimeo embeds, and removes scripts, styles,
    /// forms, event handlers (onclick, onerror…) and javascript: links. Even if an admin account were misused, page text
    /// could not run code in visitors' browsers.
    /// </summary>
    public static class HtmlSanitizer
    {
        private static readonly HashSet<string> Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "h1", "h2", "h3", "h4", "h5", "h6", "strong", "b", "em", "i", "u", "s", "a", "ul", "ol", "li", "blockquote", "code", "pre",
            "hr", "img", "figure", "figcaption", "picture", "source", "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "span", "div",
            "sup", "sub", "mark", "small", "details", "summary", "iframe", "aside", "section", "dl", "dt", "dd", "abbr", "cite", "time", "kbd", "del", "ins"
        };

        /// <summary>Elements removed together with everything inside them.</summary>
        private static readonly string[] Drop = { "script", "style", "noscript", "template", "object", "embed", "applet", "form", "textarea", "select", "svg", "math", "frameset", "frame" };

        private static readonly HashSet<string> Attrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "srcset", "sizes", "alt", "title", "class", "id", "target", "rel", "width", "height", "colspan", "rowspan", "scope",
            "loading", "decoding", "type", "media", "datetime", "allow", "allowfullscreen", "frameborder", "start", "reversed", "open", "lang", "dir", "name"
        };

        private static readonly Regex Comment = new Regex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex Tag = new Regex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)\b((?:[^>""']|""[^""]*""|'[^']*')*)>", RegexOptions.Compiled);
        private static readonly Regex Attr = new Regex(@"([a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*(?:=\s*(""[^""]*""|'[^']*'|[^\s""'>]+))?", RegexOptions.Compiled);
        private static readonly Regex Embed = new Regex(@"^https://(www\.)?(youtube\.com/embed/|youtube-nocookie\.com/embed/|player\.vimeo\.com/video/|www\.google\.com/maps/embed)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Clean(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            html = Comment.Replace(html, "");
            foreach (var t in Drop)
                html = Regex.Replace(html, "<" + t + @"\b[^>]*>.*?</" + t + @"\s*>|<" + t + @"\b[^>]*/?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            // Frames from anywhere but the allowed video and map sites go, with their closing tag.
            html = Regex.Replace(html, @"<iframe\b([^>]*)>(.*?)</iframe\s*>", m =>
            {
                var src = Regex.Match(m.Groups[1].Value, @"\bsrc\s*=\s*[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
                return src.Success && Embed.IsMatch(HttpUtility.HtmlDecode(src.Groups[1].Value)) ? m.Value : "";
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return Tag.Replace(html, m =>
            {
                var closing = m.Groups[1].Value == "/";
                var name = m.Groups[2].Value.ToLowerInvariant();
                if (!Tags.Contains(name)) return "";
                if (closing) return "</" + name + ">";
                var sb = new StringBuilder("<" + name);
                string src = null;
                foreach (Match a in Attr.Matches(m.Groups[3].Value))
                {
                    var an = a.Groups[1].Value.ToLowerInvariant();
                    if (!Attrs.Contains(an)) continue;
                    var raw = a.Groups[2].Success ? a.Groups[2].Value : "";
                    if (raw.Length >= 2 && (raw[0] == '"' || raw[0] == '\'')) raw = raw.Substring(1, raw.Length - 2);
                    var value = HttpUtility.HtmlDecode(raw);
                    if ((an == "href" || an == "src") && !SafeUrl(value)) continue;
                    if (an == "srcset" && value.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (an == "target" && value != "_blank") continue;
                    if (an == "src") src = value;
                    sb.Append(' ').Append(an);
                    if (a.Groups[2].Success) sb.Append("=\"").Append(HttpUtility.HtmlAttributeEncode(value)).Append('"');
                }
                // Embedded frames only from YouTube, Vimeo and Google Maps.
                if (name == "iframe" && (src == null || !Embed.IsMatch(src))) return "";
                if (name == "a" && sb.ToString().Contains(" target=\"_blank\"") && !sb.ToString().Contains(" rel=")) sb.Append(" rel=\"noopener\"");
                var selfClosing = m.Value.EndsWith("/>");
                return sb.Append(selfClosing && (name == "br" || name == "hr" || name == "img" || name == "source") ? " />" : ">").ToString();
            });
        }

        private static bool SafeUrl(string url)
        {
            var u = (url ?? "").Trim();
            if (u.Length == 0 || u.StartsWith("#") || (u.StartsWith("/") && !u.StartsWith("//"))) return true;
            // Control characters and spaces inside the scheme are a common trick ("java\tscript:").
            var compact = new string(u.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray()).ToLowerInvariant();
            return compact.StartsWith("https://") || compact.StartsWith("http://") || compact.StartsWith("mailto:") || compact.StartsWith("tel:")
                || (!compact.Contains(":") && !compact.StartsWith("//"));
        }
    }
}
