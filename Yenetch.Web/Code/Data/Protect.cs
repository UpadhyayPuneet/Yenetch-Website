using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Yenetch.Data
{
    /// <summary>
    /// Keeps email addresses and phone numbers away from address-harvesting bots without getting in people's way.
    /// The page HTML carries them encoded or broken up with hidden decoy text; the site script (site.js) turns them back
    /// into normal, clickable links as the page loads, so visitors see and use them exactly as before.
    /// Bots that read the raw HTML get nothing usable.
    /// </summary>
    public static class Protect
    {
        private const byte Key = 0x5A;

        /// <summary>XOR + base64. Not encryption: just enough that scrapers' patterns do not match. site.js decodes it.</summary>
        public static string Encode(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var b = Encoding.UTF8.GetBytes(s);
            for (var i = 0; i < b.Length; i++) b[i] ^= Key;
            return Convert.ToBase64String(b);
        }

        /// <summary>Encodes the company's email, phone and WhatsApp link in the data sent to the browser ("~" prefix marks them).</summary>
        public static void EncodeCompany(Dictionary<string, object> site)
        {
            object co;
            if (!site.TryGetValue("company", out co) || !(co is Dictionary<string, object>)) return;
            var d = (Dictionary<string, object>)co;
            foreach (var k in new[] { "email", "phone", "whatsapp" })
            {
                object v;
                if (d.TryGetValue(k, out v) && v is string && ((string)v).Length > 0) d[k] = "~" + Encode((string)v);
            }
        }

        private static readonly Regex Skip = new Regex(@"(<script\b.*?</script>|<style\b.*?</style>|<textarea\b.*?</textarea>|<head\b.*?</head>|<title\b.*?</title>|<[^>]*>)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Href = new Regex(@"href=""(mailto:[^""]+|tel:[^""]+|https?://(?:wa\.me|api\.whatsapp\.com|wa\.link)/[^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Email = new Regex(@"(?<![\w.%+-])([A-Za-z0-9._%+-]{1,64})@([A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,24})\b", RegexOptions.Compiled);
        private static readonly Regex Phone = new Regex(@"(?<![\w/#])(\+?\d[\d \-()]{8,18}\d)(?![\w/])", RegexOptions.Compiled);

        private const string Decoy = "<span class=\"yx-h\" style=\"display:none\" aria-hidden=\"true\">{0}</span>";

        /// <summary>Rewrites a rendered public page: contact links are encoded, visible addresses and numbers get decoy text.</summary>
        public static string Html(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            var sb = new StringBuilder(html.Length + 512);
            var pos = 0;
            foreach (Match m in Skip.Matches(html))
            {
                if (m.Index > pos) sb.Append(Text(html.Substring(pos, m.Index - pos)));
                var tag = m.Value;
                // Only ordinary tags carry links; scripts, styles, the head and form fields are left as they are.
                if (tag.StartsWith("<a", StringComparison.OrdinalIgnoreCase) || tag.StartsWith("<area", StringComparison.OrdinalIgnoreCase))
                    tag = Href.Replace(tag, x => "href=\"#!" + Encode(System.Net.WebUtility.HtmlDecode(x.Groups[1].Value)) + "\" data-yx=\"1\"");
                sb.Append(tag);
                pos = m.Index + m.Length;
            }
            if (pos < html.Length) sb.Append(Text(html.Substring(pos)));
            return sb.ToString();
        }

        private static string Text(string t)
        {
            if (t.IndexOf('@') >= 0)
                t = Email.Replace(t, m => m.Groups[1].Value + string.Format(Decoy, ".nospam") + "&#64;" + m.Groups[2].Value);
            t = Phone.Replace(t, m =>
            {
                var v = m.Groups[1].Value;
                var digits = v.Count(char.IsDigit);
                if (digits < 10 || digits > 13) return v;
                var cut = v.Length / 2;
                return v.Substring(0, cut) + string.Format(Decoy, "-0") + v.Substring(cut);
            });
            return t;
        }
    }
}
