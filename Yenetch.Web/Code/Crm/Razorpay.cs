using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Razorpay payment links for proposals. Add the Key ID and Key secret (Razorpay Dashboard &gt; Account &amp; Settings &gt;
    /// API keys) and a webhook secret in Admin &gt; Integrations; then point a Razorpay webhook at
    /// https://your-site/api/razorpay for the event payment_link.paid. The secret key never leaves the server.
    /// Payments are also confirmed when the client returns from Razorpay's checkout, using Razorpay's signature.
    /// </summary>
    public static class Razorpay
    {
        private const string Api = "https://api.razorpay.com/v1/";

        public static string KeyId { get { try { return (Settings.Get("razorpay.keyId") ?? "").Trim(); } catch { return ""; } } }
        private static string KeySecret { get { try { return Settings.GetSecret("razorpay.keySecret") ?? ""; } catch { return ""; } } }
        private static string WebhookSecret { get { try { return Settings.GetSecret("razorpay.webhookSecret") ?? ""; } catch { return ""; } } }

        public static bool Configured { get { return KeyId.Length > 0 && KeySecret.Length > 0; } }
        public static bool IsTestMode { get { return KeyId.StartsWith("rzp_test_", StringComparison.OrdinalIgnoreCase); } }

        /// <summary>Currencies with no minor unit (amounts are sent as whole numbers). Every other currency is sent in 1/100ths.</summary>
        private static readonly HashSet<string> ZeroDecimal = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "JPY", "KRW", "VND", "CLP", "PYG", "UGX", "XAF", "XOF" };
        private static readonly HashSet<string> ThreeDecimal = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KWD", "BHD", "OMR", "JOD" };

        private static long Minor(decimal amount, string currency)
        {
            if (ZeroDecimal.Contains(currency)) return (long)Math.Round(amount, 0);
            // Razorpay takes three-decimal currencies in 1/1000ths, but accepts only amounts ending in 0.
            if (ThreeDecimal.Contains(currency)) return (long)Math.Round(amount * 100m, 0) * 10;
            return (long)Math.Round(amount * 100m, 0);
        }

        private static decimal Major(long minor, string currency)
        {
            if (ZeroDecimal.Contains(currency)) return minor;
            return ThreeDecimal.Contains(currency) ? minor / 1000m : minor / 100m;
        }

        /// <summary>Creates a payment link for the amount due now (or a custom amount). Returns the link, or null with an error.</summary>
        public static string CreateLink(Proposal p, decimal? amountOverride, out string error)
        {
            error = null;
            if (!Configured) { error = "Add your Razorpay keys in Admin > Integrations first."; return null; }
            var amount = amountOverride ?? p.DueNow;
            if (amount <= 0) { error = "The amount must be more than zero."; return null; }
            var attempt = Db.Scalar<int>("SELECT COUNT(*) FROM ProposalEvents WHERE ProposalId = @id AND Kind = 'paylink'", new { id = p.Id });
            var body = new Dictionary<string, object>
            {
                { "amount", Minor(amount, p.Currency) }, { "currency", p.Currency }, { "accept_partial", false },
                { "description", Util.Cut(p.Number + " · " + p.Title, 2000) },
                // Razorpay requires a reference that was never used before, so later links get a suffix.
                { "reference_id", Util.Cut(p.Number + (attempt > 0 ? "-" + (attempt + 1) : ""), 40) },
                { "customer", Customer(p) },
                { "notify", new Dictionary<string, object> { { "sms", false }, { "email", false } } },
                { "reminder_enable", true },
                { "callback_url", p.Url + "?paid=1" }, { "callback_method", "get" },
                { "notes", new Dictionary<string, object> { { "proposal_id", p.Id.ToString(CultureInfo.InvariantCulture) }, { "proposal_number", p.Number } } }
            };
            if (p.ValidUntil.HasValue && p.ValidUntil.Value > DateTime.UtcNow.AddMinutes(20))
                body["expire_by"] = new DateTimeOffset(DateTime.SpecifyKind(p.ValidUntil.Value.AddDays(15), DateTimeKind.Utc)).ToUnixTimeSeconds();
            Dictionary<string, object> res;
            if (!Call("POST", "payment_links", body, out res, out error)) return null;
            var id = S(res, "id"); var url = S(res, "short_url");
            if (string.IsNullOrEmpty(url)) { error = "Razorpay did not return a link."; return null; }
            Db.Exec("UPDATE Proposals SET PayLinkId = @id, PayLinkUrl = @url, PayAmount = @amount, PayStatus = 'created', UpdatedOn = @now WHERE Id = @pid",
                new { id, url, amount, now = DateTime.UtcNow, pid = p.Id });
            Proposals.Event(p.Id, "paylink", "Payment link for " + p.Money(amount) + ": " + url);
            return url;
        }

        private static Dictionary<string, object> Customer(Proposal p)
        {
            var c = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(p.ClientName)) c["name"] = Util.Cut(p.ClientName, 100);
            if (Newsletter.IsEmail(p.ClientEmail)) c["email"] = p.ClientEmail.Trim();
            var digits = Util.Digits(p.ClientPhone);
            if (digits.Length >= 10) c["contact"] = "+" + (digits.Length == 10 ? "91" + digits : digits);
            return c;
        }

        /// <summary>Asks Razorpay for the link's state and records a payment if it was paid. Returns a status line.</summary>
        public static string Refresh(Proposal p)
        {
            if (string.IsNullOrEmpty(p.PayLinkId)) return "There is no payment link yet.";
            Dictionary<string, object> res; string error;
            if (!Call("GET", "payment_links/" + Uri.EscapeDataString(p.PayLinkId), null, out res, out error)) return error;
            var status = S(res, "status");
            long paid; long.TryParse(S(res, "amount_paid"), out paid);
            if (status == "paid") { Proposals.Paid(p, Major(paid, p.Currency), PaymentId(res), "checked with Razorpay"); return "Paid."; }
            Db.Exec("UPDATE Proposals SET PayStatus = @s WHERE Id = @id AND (PayStatus IS NULL OR PayStatus <> 'paid')", new { s = Util.Cut(status, 20), id = p.Id });
            return "Razorpay says: " + status + ".";
        }

        private static string PaymentId(Dictionary<string, object> link)
        {
            object payments;
            if (link.TryGetValue("payments", out payments) && payments is System.Collections.IEnumerable)
                foreach (var x in (System.Collections.IEnumerable)payments) { var d = x as Dictionary<string, object>; if (d != null && S(d, "status") == "captured") return S(d, "payment_id"); }
            return null;
        }

        /// <summary>Checks the signature Razorpay adds when it sends the client back after paying (callback_url).</summary>
        public static bool CallbackValid(string linkId, string referenceId, string status, string paymentId, string signature)
        {
            if (string.IsNullOrEmpty(signature) || !Configured) return false;
            return Same(Hmac(linkId + "|" + referenceId + "|" + status + "|" + paymentId, KeySecret), signature);
        }

        /// <summary>Checks a webhook's X-Razorpay-Signature against the raw body.</summary>
        public static bool WebhookValid(string body, string signature)
        {
            var secret = WebhookSecret;
            return !string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(signature) && Same(Hmac(body, secret), signature);
        }

        /// <summary>Handles a verified webhook. Returns what was done (for the log).</summary>
        public static string HandleWebhook(string body)
        {
            var d = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<Dictionary<string, object>>(body);
            var evt = S(d, "event");
            if (evt != "payment_link.paid") return "ignored " + evt;
            var payload = d.ContainsKey("payload") ? d["payload"] as Dictionary<string, object> : null;
            var link = Entity(payload, "payment_link");
            var payment = Entity(payload, "payment");
            if (link == null) return "no link";
            var p = ProposalForLink(S(link, "id"));
            if (p == null) return "unknown link " + S(link, "id");
            long paid; long.TryParse(S(link, "amount_paid"), out paid);
            Proposals.Paid(p, Major(paid, p.Currency), payment == null ? null : S(payment, "id"), "Razorpay webhook");
            return "paid " + p.Number;
        }

        public static Proposal ProposalForLink(string linkId)
        {
            if (string.IsNullOrEmpty(linkId)) return null;
            var id = Db.Scalar<int?>("SELECT Id FROM Proposals WHERE PayLinkId = @l", new { l = linkId });
            return id.HasValue ? Proposals.Get(id.Value) : null;
        }

        private static Dictionary<string, object> Entity(Dictionary<string, object> payload, string name)
        {
            object o;
            if (payload == null || !payload.TryGetValue(name, out o)) return null;
            var wrap = o as Dictionary<string, object>;
            object e;
            return wrap != null && wrap.TryGetValue("entity", out e) ? e as Dictionary<string, object> : null;
        }

        /// <summary>Checks the keys by listing one payment link. Returns null when they work.</summary>
        public static string Test()
        {
            Dictionary<string, object> res; string error;
            return Call("GET", "payment_links?count=1", null, out res, out error) ? null : error;
        }

        private static bool Call(string method, string path, object body, out Dictionary<string, object> result, out string error)
        {
            result = null; error = null;
            var js = new JavaScriptSerializer();
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(Api + path);
                req.Method = method; req.Timeout = 30000; req.Accept = "application/json";
                req.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(KeyId + ":" + KeySecret));
                if (body != null)
                {
                    req.ContentType = "application/json";
                    var bytes = Encoding.UTF8.GetBytes(js.Serialize(body));
                    using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                }
                using (var res = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream())) result = js.Deserialize<Dictionary<string, object>>(sr.ReadToEnd());
                return true;
            }
            catch (WebException ex)
            {
                var res = ex.Response as HttpWebResponse;
                if (res == null) { error = "Could not reach Razorpay: " + ex.Message; return false; }
                try
                {
                    using (var sr = new StreamReader(res.GetResponseStream()))
                    {
                        var d = js.Deserialize<Dictionary<string, object>>(sr.ReadToEnd());
                        var e = d.ContainsKey("error") ? d["error"] as Dictionary<string, object> : null;
                        error = "Razorpay: " + (e != null ? S(e, "description") : res.StatusDescription);
                    }
                }
                catch { error = "Razorpay answered " + (int)res.StatusCode + "."; }
                if ((int)res.StatusCode == 401) error = "Razorpay did not accept the Key ID and secret. Check them in Admin > Integrations.";
                return false;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        private static string Hmac(string text, string secret)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }

        private static bool Same(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= char.ToLowerInvariant(a[i]) ^ char.ToLowerInvariant(b[i]);
            return diff == 0;
        }

        private static string S(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : null; }
    }
}
