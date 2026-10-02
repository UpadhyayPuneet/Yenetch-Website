using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using Yenetch.Data;

namespace Yenetch.Crm
{
    /// <summary>Mail server and addresses, edited in /admin/email (stored in CmsSettings).</summary>
    public class MailConfig
    {
        public string Host, User, Password, From, FromName, LeadsTo, ReplyTo;
        public int Port = 587;
        public bool Ssl = true, AutoReply = true;

        public bool HasServer { get { return !string.IsNullOrWhiteSpace(Host); } }

        public static MailConfig Load()
        {
            var c = new MailConfig();
            try
            {
                c.Host = Settings.Get("mail.host");
                int port; if (int.TryParse(Settings.Get("mail.port"), out port)) c.Port = port;
                c.Ssl = Settings.Get("mail.ssl") != "0";
                c.User = Settings.Get("mail.user");
                c.Password = Settings.GetSecret("mail.password");
                c.From = Settings.Get("mail.from");
                c.FromName = Settings.Get("mail.fromName");
                c.LeadsTo = Settings.Get("mail.leadsTo");
                c.ReplyTo = Settings.Get("mail.replyTo");
                c.AutoReply = Settings.Get("mail.autoReply") != "0";
            }
            catch { /* database not ready: Web.config values below */ }
            if (string.IsNullOrWhiteSpace(c.From)) c.From = ConfigurationManager.AppSettings["MailFrom"];
            if (string.IsNullOrWhiteSpace(c.LeadsTo)) c.LeadsTo = ConfigurationManager.AppSettings["LeadEmailTo"];
            if (!string.IsNullOrWhiteSpace(c.From) && c.From.Contains("<"))
            {
                // "Name <address>" (older Web.config style): keep the address and use the name if none is set.
                try { var a = new MailAddress(c.From.Trim()); c.From = a.Address; if (string.IsNullOrWhiteSpace(c.FromName)) c.FromName = a.DisplayName; }
                catch (FormatException) { c.From = null; }
            }
            if (string.IsNullOrWhiteSpace(c.FromName)) c.FromName = "Yenetch";
            return c;
        }
    }

    /// <summary>
    /// Outgoing email: new-lead alerts (with any files the visitor attached), the thank-you reply to the visitor,
    /// assignment alerts and newsletters. The mail server is set in /admin/email (for example hello@yenetch.com on the
    /// hosting mail server). Without one, the Web.config mailSettings are used, and with none there either, mail is
    /// saved as .eml files in App_Data/mail. Every message carries the logo as an embedded image and a plain-text version.
    /// </summary>
    public static class Mailer
    {
        public static string SiteUrl { get { return (ConfigurationManager.AppSettings["SiteUrl"] ?? "").TrimEnd('/'); } }
        public static string LeadInbox { get { return MailConfig.Load().LeadsTo; } }
        private static string LogoUrl { get { return SiteUrl + "/assets/img/logo-mark.png"; } }

        public static void Send(string to, string subject, string html, string text = null, Action<MailMessage> customise = null, IEnumerable<LeadFile> files = null)
        {
            var cfg = MailConfig.Load();
            using (var msg = new MailMessage())
            using (var smtp = cfg.HasServer ? new SmtpClient(cfg.Host.Trim(), cfg.Port) : new SmtpClient())
            {
                var from = !string.IsNullOrWhiteSpace(cfg.From) ? cfg.From : (!string.IsNullOrWhiteSpace(cfg.User) ? cfg.User : null);
                if (from != null) msg.From = new MailAddress(from.Trim(), cfg.FromName, Encoding.UTF8);
                if (!string.IsNullOrWhiteSpace(cfg.ReplyTo)) msg.ReplyToList.Add(cfg.ReplyTo.Trim());
                foreach (var addr in to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) msg.To.Add(addr.Trim());
                msg.Subject = subject;
                msg.SubjectEncoding = msg.HeadersEncoding = msg.BodyEncoding = Encoding.UTF8;

                // Plain text first, then HTML with the logo embedded (shows even when the mail app blocks remote images).
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text ?? PlainText(html), Encoding.UTF8, "text/plain"));
                var logo = Util.AppPath("assets/img/logo-mark.png");
                var embed = html.Contains(LogoUrl) && System.IO.File.Exists(logo);
                var view = AlternateView.CreateAlternateViewFromString(embed ? html.Replace(LogoUrl, "cid:yenetch-logo") : html, Encoding.UTF8, "text/html");
                if (embed) view.LinkedResources.Add(new LinkedResource(logo, "image/png") { ContentId = "yenetch-logo", TransferEncoding = System.Net.Mime.TransferEncoding.Base64 });
                msg.AlternateViews.Add(view);

                if (files != null)
                    foreach (var f in files)
                        if (System.IO.File.Exists(f.FullPath)) msg.Attachments.Add(new Attachment(f.FullPath, f.ContentType) { Name = f.FileName });
                if (customise != null) customise(msg);

                if (cfg.HasServer)
                {
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.EnableSsl = cfg.Ssl;
                    smtp.Timeout = 30000;
                    if (!string.IsNullOrWhiteSpace(cfg.User)) { smtp.UseDefaultCredentials = false; smtp.Credentials = new System.Net.NetworkCredential(cfg.User.Trim(), cfg.Password ?? ""); }
                }
                // No SMTP host anywhere yet: write the message to App_Data/mail instead of failing.
                if (smtp.DeliveryMethod == SmtpDeliveryMethod.Network && string.IsNullOrEmpty(smtp.Host)) smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                if (smtp.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory)
                {
                    if (string.IsNullOrEmpty(smtp.PickupDirectoryLocation)) smtp.PickupDirectoryLocation = Util.AppPath("App_Data/mail");
                    System.IO.Directory.CreateDirectory(smtp.PickupDirectoryLocation);
                }
                smtp.Send(msg);
            }
        }

        /// <summary>Emails the sales inbox with the lead and its files. A mail failure never loses the lead: it is already saved.</summary>
        public static void NotifyNewLead(Lead l, IList<LeadFile> files = null)
        {
            var inbox = LeadInbox;
            if (string.IsNullOrEmpty(inbox)) return;
            try
            {
                var rows = Row("Name", l.Name) + Row("Phone", l.Phone) + Row("Email", l.Email) + Row("Interested in", l.Interest) + Row("Type", l.LeadType)
                         + Row("Source", l.Source) + Row("Channel", l.Channel) + Row("City", l.City) + Row("Page", l.Page) + Row("Message", l.Need);
                if (files != null && files.Count > 0) rows += Row("Attached", string.Join("\n", files.Select(f => f.FileName + " (" + f.SizeLabel + ")")));
                var quick = "";
                if (!string.IsNullOrEmpty(l.Phone))
                {
                    var digits = Util.Digits(l.Phone); if (digits.Length == 10) digits = "91" + digits;
                    quick = "<p style=\"margin:16px 0 0;font-size:14px\"><a href=\"tel:+" + digits + "\" style=\"color:#0066FF;text-decoration:none;font-weight:600\">Call</a> &nbsp;·&nbsp; <a href=\"https://wa.me/" + digits + "\" style=\"color:#0066FF;text-decoration:none;font-weight:600\">WhatsApp</a></p>";
                }
                var body = Heading("New enquiry from " + l.Name) + "<p style=\"margin:0 0 16px;color:#48484e\">A new enquiry came in through the website" + (files != null && files.Count > 0 ? ", with " + files.Count + " attached file" + (files.Count == 1 ? "" : "s") : "") + ".</p>"
                         + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%\">" + rows + "</table>" + quick
                         + Button("Open lead", SiteUrl + "/admin/leads/" + l.Id);
                Send(inbox, "New lead: " + l.Name + (string.IsNullOrEmpty(l.Interest) ? "" : " (" + l.Interest + ")"), Wrap("New enquiry from " + l.Name, body, null),
                     customise: m => { if (Newsletter.IsEmail(l.Email)) { m.ReplyToList.Clear(); m.ReplyToList.Add(new MailAddress(l.Email, l.Name)); } }, files: files);
            }
            catch (Exception ex) { Log("lead " + l.Id, ex); }
        }

        /// <summary>Thanks the visitor and tells them what happens next (when they gave an email and auto-reply is on).</summary>
        public static void AutoReply(Lead l)
        {
            if (!Newsletter.IsEmail(l.Email)) return;
            var cfg = MailConfig.Load();
            if (!cfg.AutoReply) return;
            try
            {
                var co = Yenetch.Data.SiteContent.Current.Company ?? new Yenetch.Models.Company();
                var first = (l.Name ?? "").Trim().Split(' ')[0];
                var body = Heading("Thanks" + (first.Length > 0 ? ", " + Util.H(first) : "") + ". We have your message.")
                         + "<p style=\"margin:0 0 16px\">Someone from our team will get back to you within one working day" + (string.IsNullOrEmpty(l.Interest) ? "" : " about <b>" + Util.H(l.Interest) + "</b>") + ".</p>"
                         + (string.IsNullOrWhiteSpace(l.Need) ? "" : "<div style=\"margin:0 0 20px;padding:16px 18px;background:#f5f5f7;border-radius:14px;font-size:14px;color:#48484e\"><b style=\"display:block;color:#1d1d1f;margin-bottom:6px\">Your message</b>" + Util.H(Util.Cut(l.Need, 800)).Replace("\n", "<br>") + "</div>")
                         + "<p style=\"margin:0 0 8px\">Need us sooner?</p>"
                         + (string.IsNullOrEmpty(co.Phone) ? "" : "<p style=\"margin:0 0 4px;font-size:15px\">Call <a href=\"" + Yenetch.Data.Site.PhoneTel + "\" style=\"color:#0066FF;text-decoration:none;font-weight:600\">" + Util.H(co.Phone) + "</a></p>")
                         + (string.IsNullOrEmpty(co.Whatsapp) ? "" : "<p style=\"margin:0 0 4px;font-size:15px\"><a href=\"" + HttpUtility.HtmlAttributeEncode(Yenetch.Data.Site.WhatsApp("Hi Yenetch, I just sent an enquiry.")) + "\" style=\"color:#0066FF;text-decoration:none;font-weight:600\">Chat on WhatsApp</a></p>")
                         + Button("See our work", SiteUrl + "/case-studies")
                         + "<p style=\"margin:24px 0 0;color:#48484e\">Team " + Util.H(co.Name ?? "Yenetch") + "</p>";
                Send(l.Email, "Thanks for contacting " + (co.Name ?? "Yenetch"), Wrap("We have your message and will reply within one working day.", body, null));
            }
            catch (Exception ex) { Log("auto-reply " + l.Id, ex); }
        }

        /// <summary>Sends a test message with the current settings. Returns null on success, or the error.</summary>
        public static string SendTest(string to)
        {
            try
            {
                var body = Heading("Email is working") + "<p style=\"margin:0 0 16px\">This test was sent from the Yenetch website admin at " + Util.H(Util.When(DateTime.UtcNow)) + " (India time).</p>"
                         + "<p style=\"margin:0\">Lead alerts, replies to enquiries and newsletters will now be sent this way.</p>";
                Send(to, "Test email from the Yenetch website", Wrap("Your website email settings work.", body, null));
                return null;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                for (var inner = ex.InnerException; inner != null; inner = inner.InnerException) msg += " " + inner.Message;
                return msg;
            }
        }

        public static void NotifyAssigned(int leadId, int userId)
        {
            try
            {
                var u = Auth.User(userId);
                var l = LeadService.Get(leadId);
                if (u == null || l == null) return;
                var body = Heading(Util.H(l.Name) + " is yours") + "<p style=\"margin:0 0 16px;color:#48484e\">This lead has been assigned to you.</p><table role=\"presentation\" style=\"border-collapse:collapse;width:100%\">"
                         + Row("Phone", l.Phone) + Row("Email", l.Email) + Row("Interested in", l.Interest) + Row("Message", l.Need) + "</table>"
                         + Button("Open lead", SiteUrl + "/admin/leads/" + l.Id);
                Send(u.Email, "Lead assigned to you: " + l.Name, Wrap("Lead assigned", body, null), files: Attachments.ForLead(leadId));
            }
            catch (Exception ex) { Log("assign " + leadId, ex); }
        }

        // ---- Template ----------------------------------------------------------------------------------------

        /// <summary>Branded, table-based email layout that renders in Gmail, Outlook and Apple Mail.</summary>
        public static string Wrap(string preheader, string innerHtml, string unsubscribeUrl)
        {
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"></head>");
            sb.Append("<body style=\"margin:0;background:#f5f5f7;font-family:Inter,-apple-system,Segoe UI,Roboto,Arial,sans-serif;color:#1d1d1f\">");
            sb.Append("<div style=\"display:none;max-height:0;overflow:hidden\">" + Util.H(preheader) + "</div>");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\" style=\"padding:32px 16px\">");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;background:#ffffff;border-radius:18px\">");
            sb.Append("<tr><td style=\"padding:28px 32px 0\"><a href=\"" + SiteUrl + "\" style=\"text-decoration:none;color:#000;font-weight:700;font-size:20px;letter-spacing:-.4px\">");
            sb.Append("<img src=\"" + SiteUrl + "/assets/img/logo-mark.png\" width=\"24\" height=\"24\" alt=\"\" style=\"display:inline-block;margin-right:8px;vertical-align:-5px;border:0\">Yenetch</a></td></tr>");
            sb.Append("<tr><td style=\"padding:24px 32px 32px;font-size:16px;line-height:1.6\">" + innerHtml + "</td></tr></table>");
            sb.Append(Footer());
            if (!string.IsNullOrEmpty(unsubscribeUrl)) sb.Append("<p style=\"font-size:12px;color:#86868b;line-height:1.6;margin:8px 0 0\">You are receiving this because you subscribed on our website or contacted us. <a href=\"" + unsubscribeUrl + "\" style=\"color:#86868b\">Unsubscribe</a></p>");
            sb.Append("</td></tr></table></body></html>");
            return sb.ToString();
        }

        /// <summary>Company details under every email: address, phone, email, WhatsApp and social links, all from Website content.</summary>
        internal static string Footer()
        {
            string name = "Yenetch", address = null, phone = null, email = null, wa = null;
            var social = new List<string>();
            try
            {
                var co = SiteContent.Current.Company;
                if (co != null)
                {
                    if (!string.IsNullOrEmpty(co.Name)) name = co.Name;
                    phone = co.Phone; email = co.Email; wa = co.Whatsapp;
                    if (co.Offices != null && co.Offices.Count > 0)
                        address = string.Join(" · ", co.Offices.Where(o => !string.IsNullOrEmpty(o.City)).Select(o => string.IsNullOrEmpty(o.Address) ? o.City : o.Address.Replace("\n", ", ")));
                    if (co.Social != null)
                        foreach (var x in co.Social.Where(x => !string.IsNullOrEmpty(x.Url) && x.Id != "whatsapp"))
                            social.Add("<a href=\"" + HttpUtility.HtmlAttributeEncode(x.Url) + "\" style=\"color:#48484e;text-decoration:none;font-weight:600\">" + Util.H(string.IsNullOrEmpty(x.Name) ? x.Id : x.Name) + "</a>");
                }
            }
            catch { }
            var a = "style=\"color:#48484e;text-decoration:none\"";
            var contact = new List<string>();
            if (!string.IsNullOrEmpty(phone)) contact.Add("<a href=\"tel:+" + Util.Digits(phone) + "\" " + a + ">" + Util.H(phone) + "</a>");
            if (!string.IsNullOrEmpty(email)) contact.Add("<a href=\"mailto:" + Util.H(email) + "\" " + a + ">" + Util.H(email) + "</a>");
            if (!string.IsNullOrEmpty(wa)) contact.Add("<a href=\"" + HttpUtility.HtmlAttributeEncode(wa) + "\" " + a + ">WhatsApp</a>");
            contact.Add("<a href=\"" + SiteUrl + "\" " + a + ">" + Util.H(SiteUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "")) + "</a>");
            var sb = new StringBuilder("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px\"><tr><td style=\"padding:20px 8px 0;font-size:12px;line-height:1.7;color:#86868b;text-align:center\">");
            sb.Append("<b style=\"color:#1d1d1f\">" + Util.H(name) + "</b>");
            if (!string.IsNullOrEmpty(address)) sb.Append("<br>" + Util.H(address));
            sb.Append("<br>" + string.Join(" &nbsp;·&nbsp; ", contact));
            if (social.Count > 0) sb.Append("<br>" + string.Join(" &nbsp;·&nbsp; ", social));
            return sb.Append("</td></tr></table>").ToString();
        }

        internal static string Row(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return "<tr><td style=\"padding:8px 12px 8px 0;color:#86868b;font-size:14px;vertical-align:top;white-space:nowrap;border-bottom:1px solid #f0f0f2\">" + label
                 + "</td><td style=\"padding:8px 0;font-size:14px;border-bottom:1px solid #f0f0f2\">" + Util.H(value).Replace("\n", "<br>") + "</td></tr>";
        }

        internal static string Heading(string html)
        {
            return "<h1 style=\"margin:0 0 12px;font-size:24px;line-height:1.25;letter-spacing:-.4px;font-weight:700;color:#1d1d1f\">" + html + "</h1>";
        }

        /// <summary>Readable plain-text version of an HTML email (sent alongside it; helps delivery and old mail apps).</summary>
        public static string PlainText(string html)
        {
            var t = System.Text.RegularExpressions.Regex.Replace(html ?? "", @"<(style|script|head)[^>]*>.*?</\1>", "", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            t = System.Text.RegularExpressions.Regex.Replace(t, @"<a [^>]*href=""([^""]+)""[^>]*>(.*?)</a>", "$2 ($1)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            t = System.Text.RegularExpressions.Regex.Replace(t, @"<(br|/p|/h1|/h2|/tr|/div|/li)[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            t = System.Text.RegularExpressions.Regex.Replace(t, @"</td>", "  ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            t = HttpUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", ""));
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[ \t]+\n", "\n");
            return System.Text.RegularExpressions.Regex.Replace(t, @"\n{3,}", "\n\n").Trim();
        }

        public static string Button(string label, string url)
        {
            return "<p style=\"margin:24px 0 0\"><a href=\"" + HttpUtility.HtmlAttributeEncode(url) + "\" style=\"display:inline-block;background:#0066FF;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:600;font-size:15px\">" + Util.H(label) + "</a></p>";
        }

        public static void Log(string what, Exception ex)
        {
            try
            {
                var file = Util.AppPath("App_Data/mail-errors.log");
                System.IO.File.AppendAllText(file, DateTime.UtcNow.ToString("o") + " " + what + ": " + ex.Message + Environment.NewLine);
            }
            catch { }
        }
    }
}
