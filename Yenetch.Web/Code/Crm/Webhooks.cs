using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    /// <summary>
    /// Outgoing webhooks: tells another app (for example the billing and project tracking app) when something happens
    /// here: lead.created, quote.created, proposal.sent, proposal.viewed, proposal.accepted, proposal.declined,
    /// payment.received. Set the address and a shared secret in Admin &gt; Integrations. Each call is a JSON POST with
    /// header X-Yenetch-Signature: sha256=HMAC-SHA256(body, secret) in hex, so the other app can check it came from here.
    /// Calls run in the background and never slow the website down; the last 500 are logged in Admin &gt; Integrations.
    /// </summary>
    public static class Webhooks
    {
        public static string Url { get { try { return (Settings.Get("hooks.url") ?? "").Trim(); } catch { return ""; } } }
        public static bool Enabled { get { var u = Url; return u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase); } }

        public static void Fire(string evt, object data)
        {
            if (!Enabled) return;
            var url = Url;
            string secret;
            try { secret = Settings.GetSecret("hooks.secret") ?? ""; } catch { secret = ""; }
            var body = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(new { @event = evt, id = Util.NewId(), createdAt = DateTime.UtcNow.ToString("o"), site = Mailer.SiteUrl, data });
            System.Threading.ThreadPool.QueueUserWorkItem(_ => Send(evt, url, secret, body));
        }

        /// <summary>Sends now and returns the result line (used by the Send test button).</summary>
        public static string Send(string evt, string url, string secret, string body)
        {
            int? status = null; string response;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST"; req.ContentType = "application/json"; req.Timeout = 15000; req.UserAgent = "Yenetch-Webhooks/1";
                req.Headers["X-Yenetch-Event"] = evt;
                if (!string.IsNullOrEmpty(secret)) req.Headers["X-Yenetch-Signature"] = "sha256=" + Sign(body, secret);
                var bytes = Encoding.UTF8.GetBytes(body);
                using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                using (var res = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream())) { status = (int)res.StatusCode; response = sr.ReadToEnd(); }
            }
            catch (WebException ex)
            {
                var res = ex.Response as HttpWebResponse;
                status = res == null ? (int?)null : (int)res.StatusCode;
                response = ex.Message;
            }
            catch (Exception ex) { response = ex.Message; }
            try
            {
                Db.Exec("INSERT INTO WebhookLog (Event, Url, StatusCode, Response, Payload, CreatedOn) VALUES (@evt, @url, @status, @resp, @body, @now)",
                    new { evt, url = Util.Cut(url, 400), status, resp = Util.Cut(response, 1000), body, now = DateTime.UtcNow });
                Db.Exec("DELETE FROM WebhookLog WHERE Id < (SELECT MIN(Id) FROM (SELECT Id FROM WebhookLog ORDER BY Id DESC" + Db.Page(0, 500) + ") x)");
            }
            catch { }
            return (status.HasValue ? "HTTP " + status : "No answer") + ": " + Util.Cut(response, 200);
        }

        public static string Sign(string body, string secret)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant();
        }

        public static List<Row> Log(int max) { return Db.Rows("SELECT Id, Event, Url, StatusCode, Response, CreatedOn FROM WebhookLog ORDER BY Id DESC" + Db.Page(0, max)); }
    }
}
