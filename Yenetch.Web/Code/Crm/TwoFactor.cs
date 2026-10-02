using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Security;
using Yenetch.Data;

namespace Yenetch.Crm
{
    /// <summary>A browser a user has signed in from (CrmUsers.KnownDevices, JSON).</summary>
    public class KnownDevice
    {
        /// <summary>SHA-256 of the device cookie. The cookie itself is never stored.</summary>
        public string Hash { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }
        public string Place { get; set; }
        public DateTime FirstOn { get; set; }
        public DateTime LastOn { get; set; }
        /// <summary>Skip the second step on this browser until then ("Trust this browser for 30 days").</summary>
        public DateTime? TrustedUntil { get; set; }
    }

    /// <summary>
    /// Two-step sign-in for the admin. After the password, the user enters a 6-digit code from an authenticator app
    /// (Google Authenticator, Microsoft Authenticator, Authy: free, works offline, RFC 6238), a one-time backup code, or a
    /// code sent by email. Admins can require it for everyone in /admin/security; users without an app then get email codes.
    /// Also records the browsers each user signs in from and emails a sign-in alert for a new one.
    /// </summary>
    public static class TwoFactor
    {
        private const string PendingCookie = "yn_2fa";
        private const string DeviceCookie = "yn_dev";
        private const string Issuer = "Yenetch";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        // ---- Settings ----------------------------------------------------------------------------------------

        /// <summary>Every user must pass a second step (email code when they have not set up an app).</summary>
        public static bool RequiredForAll { get { return Settings.Get("security.require2fa") == "1"; } set { Settings.Set("security.require2fa", value ? "1" : "0"); } }
        public static bool Alerts { get { return Settings.Get("security.alerts") != "0"; } set { Settings.Set("security.alerts", value ? "1" : "0"); } }
        public static bool AllowTrust { get { return Settings.Get("security.trust") != "0"; } set { Settings.Set("security.trust", value ? "1" : "0"); } }

        /// <summary>"app", "email" or null (off) for a user row.</summary>
        public static string Method(Row r)
        {
            var m = r.Str("TwoFactor");
            if (m == "app" && !string.IsNullOrEmpty(r.Str("TwoFactorSecret"))) return "app";
            if (m == "email") return "email";
            return RequiredForAll ? "email" : null;
        }

        public static Row UserRow(int id) { return Db.First("SELECT * FROM CrmUsers WHERE Id = @id", new { id }); }

        // ---- TOTP (RFC 6238) ---------------------------------------------------------------------------------

        public static string NewSecret()
        {
            var b = new byte[20];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            return Base32(b);
        }

        public static string Base32(byte[] data)
        {
            const string a = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var sb = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b; bits += 8;
                while (bits >= 5) { sb.Append(a[(buffer >> (bits - 5)) & 31]); bits -= 5; }
            }
            if (bits > 0) sb.Append(a[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static byte[] FromBase32(string s)
        {
            const string a = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            s = (s ?? "").ToUpperInvariant().Replace(" ", "").TrimEnd('=');
            var bytes = new List<byte>();
            int buffer = 0, bits = 0;
            foreach (var c in s)
            {
                var v = a.IndexOf(c);
                if (v < 0) throw new FormatException("Not base32");
                buffer = (buffer << 5) | v; bits += 5;
                if (bits >= 8) { bytes.Add((byte)((buffer >> (bits - 8)) & 255)); bits -= 8; }
            }
            return bytes.ToArray();
        }

        public static string Code(byte[] key, long step)
        {
            var msg = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(msg);
            using (var h = new HMACSHA1(key))
            {
                var hash = h.ComputeHash(msg);
                var o = hash[hash.Length - 1] & 15;
                var bin = ((hash[o] & 127) << 24) | (hash[o + 1] << 16) | (hash[o + 2] << 8) | hash[o + 3];
                return (bin % 1000000).ToString("000000", CultureInfo.InvariantCulture);
            }
        }

        private static long Step(DateTime utc) { return (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds / 30; }

        /// <summary>Checks a 6-digit code, allowing one step of clock drift each way. A code can only be used once.</summary>
        public static bool CheckTotp(string secret, string code, int userId)
        {
            code = Util.Digits(code);
            if (code.Length != 6 || string.IsNullOrEmpty(secret)) return false;
            byte[] key;
            try { key = FromBase32(secret); } catch { return false; }
            var now = Step(DateTime.UtcNow);
            for (var d = -1; d <= 1; d++)
            {
                if (!Same(Code(key, now + d), code)) continue;
                var used = "totp.used." + userId + "." + (now + d);
                if (HttpRuntime.Cache[used] != null) return false;
                HttpRuntime.Cache.Insert(used, true, null, DateTime.UtcNow.AddMinutes(3), System.Web.Caching.Cache.NoSlidingExpiration);
                return true;
            }
            return false;
        }

        private static bool Same(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        /// <summary>The otpauth:// address shown as a QR code to the authenticator app.</summary>
        public static string SetupUri(string secret, string email)
        {
            return "otpauth://totp/" + Uri.EscapeDataString(Issuer + ":" + email) + "?secret=" + secret + "&issuer=" + Uri.EscapeDataString(Issuer) + "&algorithm=SHA1&digits=6&period=30";
        }

        public static string Protect(string secret) { return Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(secret), "totp")); }
        public static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return null;
            try { return Encoding.UTF8.GetString(MachineKey.Unprotect(Convert.FromBase64String(stored), "totp")); } catch { return null; }
        }

        /// <summary>Turns the authenticator app on after the user typed a correct code. Returns the new backup codes.</summary>
        public static List<string> EnableApp(int userId, string secret)
        {
            var codes = NewBackupCodes();
            Db.Exec("UPDATE CrmUsers SET TwoFactor = 'app', TwoFactorSecret = @s, BackupCodes = @b WHERE Id = @id",
                new { s = Protect(secret), b = Json.Serialize(codes.Select(HashCode).ToList()), id = userId });
            return codes;
        }

        public static void EnableEmail(int userId)
        {
            Db.Exec("UPDATE CrmUsers SET TwoFactor = 'email', TwoFactorSecret = NULL, BackupCodes = NULL WHERE Id = @id", new { id = userId });
        }

        public static void Disable(int userId)
        {
            Db.Exec("UPDATE CrmUsers SET TwoFactor = NULL, TwoFactorSecret = NULL, BackupCodes = NULL, EmailCode = NULL, EmailCodeUntil = NULL WHERE Id = @id", new { id = userId });
            ForgetTrust(userId);
        }

        // ---- Backup codes ------------------------------------------------------------------------------------

        public static List<string> NewBackupCodes()
        {
            const string a = "abcdefghjkmnpqrstuvwxyz23456789";
            var list = new List<string>();
            using (var rng = RandomNumberGenerator.Create())
                for (var i = 0; i < 10; i++)
                {
                    var b = new byte[10];
                    rng.GetBytes(b);
                    var s = new string(b.Select(x => a[x % a.Length]).ToArray());
                    list.Add(s.Substring(0, 5) + "-" + s.Substring(5));
                }
            return list;
        }

        public static List<string> RegenerateBackupCodes(int userId)
        {
            var codes = NewBackupCodes();
            Db.Exec("UPDATE CrmUsers SET BackupCodes = @b WHERE Id = @id", new { b = Json.Serialize(codes.Select(HashCode).ToList()), id = userId });
            return codes;
        }

        private static string HashCode(string code)
        {
            var norm = (code ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace(" ", "");
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("backup:" + norm)));
        }

        public static int BackupCodesLeft(Row r)
        {
            try { return string.IsNullOrEmpty(r.Str("BackupCodes")) ? 0 : Json.Deserialize<List<string>>(r.Str("BackupCodes")).Count; } catch { return 0; }
        }

        /// <summary>Uses up a backup code. True when it matched.</summary>
        private static bool UseBackupCode(Row r, string code)
        {
            List<string> list;
            try { list = Json.Deserialize<List<string>>(r.Str("BackupCodes") ?? "[]"); } catch { return false; }
            var h = HashCode(code);
            if (!list.Remove(h)) return false;
            // Conditional update: two requests with the same code cannot both succeed.
            return Db.Exec("UPDATE CrmUsers SET BackupCodes = @b WHERE Id = @id AND BackupCodes = @old", new { b = Json.Serialize(list), id = r.Int("Id"), old = r.Str("BackupCodes") }) > 0;
        }

        // ---- Email codes -------------------------------------------------------------------------------------

        /// <summary>Emails a 6-digit code valid for 10 minutes. Returns an error, or null. Limited to one email a minute.</summary>
        public static string SendEmailCode(Row r)
        {
            var until = r.DateN("EmailCodeUntil");
            if (until.HasValue && until.Value > DateTime.UtcNow.AddMinutes(9)) return "We just sent a code. Please wait a minute before asking for another.";
            int n;
            using (var rng = RandomNumberGenerator.Create()) { var b = new byte[4]; rng.GetBytes(b); n = (int)(BitConverter.ToUInt32(b, 0) % 1000000); }
            var code = n.ToString("000000", CultureInfo.InvariantCulture);
            Db.Exec("UPDATE CrmUsers SET EmailCode = @c, EmailCodeUntil = @u WHERE Id = @id", new { c = HashCode("email" + code), u = DateTime.UtcNow.AddMinutes(10), id = r.Int("Id") });
            try
            {
                var body = Mailer.Heading("Your sign-in code") + "<p style=\"margin:0 0 16px\">Use this code to finish signing in to the Yenetch admin. It works for 10 minutes.</p>"
                         + "<p style=\"margin:0 0 20px;font-size:34px;font-weight:700;letter-spacing:8px;font-family:Menlo,Consolas,monospace;color:#1d1d1f\">" + code + "</p>"
                         + "<p style=\"margin:0;font-size:14px;color:#48484e\">If you did not try to sign in, someone may know your password. Change it in My account and tell your admin.</p>";
                Mailer.Send(r.Str("Email"), "Your Yenetch sign-in code: " + code, Mailer.Wrap("Your sign-in code is " + code, body, null));
                return null;
            }
            catch (Exception ex) { Mailer.Log("2fa email", ex); return "We could not send the email. Check the email settings, or use your authenticator app or a backup code."; }
        }

        private static bool CheckEmailCode(Row r, string code)
        {
            code = Util.Digits(code);
            var until = r.DateN("EmailCodeUntil");
            if (code.Length != 6 || !until.HasValue || until.Value < DateTime.UtcNow || string.IsNullOrEmpty(r.Str("EmailCode"))) return false;
            if (!Same(r.Str("EmailCode"), HashCode("email" + code))) return false;
            Db.Exec("UPDATE CrmUsers SET EmailCode = NULL, EmailCodeUntil = NULL WHERE Id = @id", new { id = r.Int("Id") });
            return true;
        }

        // ---- The second step ---------------------------------------------------------------------------------

        public class Pending { public int UserId; public bool Remember; public DateTime Until; }

        /// <summary>Called after a correct password when a second step is needed. Remembers who is signing in for 10 minutes.</summary>
        public static void Start(int userId, bool remember)
        {
            var until = DateTime.UtcNow.AddMinutes(10);
            var raw = userId + "|" + (remember ? "1" : "0") + "|" + until.Ticks.ToString(CultureInfo.InvariantCulture);
            var value = Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(raw), "2fa-pending"));
            var ctx = HttpContext.Current;
            ctx.Response.Cookies.Add(new HttpCookie(PendingCookie, value) { HttpOnly = true, Secure = ctx.Request.IsSecureConnection, Path = "/admin" });
        }

        public static Pending Current()
        {
            var c = HttpContext.Current.Request.Cookies[PendingCookie];
            if (c == null || string.IsNullOrEmpty(c.Value)) return null;
            try
            {
                var p = Encoding.UTF8.GetString(MachineKey.Unprotect(Convert.FromBase64String(c.Value), "2fa-pending")).Split('|');
                var until = new DateTime(long.Parse(p[2], CultureInfo.InvariantCulture), DateTimeKind.Utc);
                if (until < DateTime.UtcNow) return null;
                return new Pending { UserId = int.Parse(p[0], CultureInfo.InvariantCulture), Remember = p[1] == "1", Until = until };
            }
            catch { return null; }
        }

        public static void Clear()
        {
            HttpContext.Current.Response.Cookies.Add(new HttpCookie(PendingCookie, "") { Path = "/admin", Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true });
        }

        /// <summary>Checks the second-step code (app code, backup code or email code) and finishes signing in.
        /// Returns an error, or null when signed in. Five wrong codes lock the account for 15 minutes.</summary>
        public static string Verify(Pending p, string code, bool trustBrowser, out bool usedBackup)
        {
            usedBackup = false;
            var r = UserRow(p.UserId);
            if (r == null || !r.Bool("IsActive")) { Clear(); return "Your sign-in has expired. Please start again."; }
            var locked = r.DateN("LockedUntil");
            if (locked.HasValue && locked.Value > DateTime.UtcNow) { Clear(); return "Too many attempts. Try again in " + Util.Span(locked.Value - DateTime.UtcNow) + "."; }
            code = (code ?? "").Trim();
            var ok = false;
            if (Util.Digits(code).Length == 6 && code.Replace(" ", "").Length <= 7)
                ok = (r.Str("TwoFactor") == "app" && CheckTotp(Unprotect(r.Str("TwoFactorSecret")), code, p.UserId)) || CheckEmailCode(r, code);
            else if (code.Replace("-", "").Replace(" ", "").Length == 10)
                ok = usedBackup = UseBackupCode(r, code);
            if (!ok)
            {
                var fails = r.Int("FailedLogins") + 1;
                Db.Exec("UPDATE CrmUsers SET FailedLogins = @f, LockedUntil = @until WHERE Id = @id",
                    new { f = fails >= 5 ? 0 : fails, until = fails >= 5 ? (DateTime?)DateTime.UtcNow.AddMinutes(15) : null, id = p.UserId });
                if (fails >= 5) { Clear(); return "Too many wrong codes. Try again in 15 minutes."; }
                return "That code is not right. Check the newest code in your app or email and try again.";
            }
            Clear();
            Complete(r, p.Remember, trustBrowser && AllowTrust);
            return null;
        }

        /// <summary>Signs the user in: issues the cookie, records the browser and sends a sign-in alert for a new one.</summary>
        public static void Complete(Row r, bool remember, bool trustBrowser)
        {
            var ctx = HttpContext.Current;
            var ip = Analytics.ClientIp(ctx.Request) ?? "";
            Db.Exec("UPDATE CrmUsers SET FailedLogins = 0, LockedUntil = NULL, LastLoginOn = @now, LastLoginIp = @ip WHERE Id = @id", new { now = DateTime.UtcNow, ip = Util.Cut(ip, 64), id = r.Int("Id") });
            Auth.IssueCookie(CrmUser.From(r), remember);
            try { RememberDevice(r, ip, trustBrowser); } catch (Exception ex) { Mailer.Log("device", ex); }
        }

        /// <summary>True when this browser was trusted for this user within the last 30 days.</summary>
        public static bool IsTrusted(Row r)
        {
            var h = DeviceHash(false);
            if (h == null) return false;
            return Devices(r).Any(d => d.Hash == h && d.TrustedUntil.HasValue && d.TrustedUntil.Value > DateTime.UtcNow);
        }

        public static List<KnownDevice> Devices(Row r)
        {
            try { return string.IsNullOrEmpty(r.Str("KnownDevices")) ? new List<KnownDevice>() : Json.Deserialize<List<KnownDevice>>(r.Str("KnownDevices")); }
            catch { return new List<KnownDevice>(); }
        }

        public static void ForgetTrust(int userId)
        {
            var r = UserRow(userId);
            if (r == null) return;
            var list = Devices(r);
            foreach (var d in list) d.TrustedUntil = null;
            Db.Exec("UPDATE CrmUsers SET KnownDevices = @k WHERE Id = @id", new { k = Json.Serialize(list), id = userId });
        }

        private static string DeviceHash(bool create)
        {
            var ctx = HttpContext.Current;
            var c = ctx.Request.Cookies[DeviceCookie];
            var id = c != null && c.Value != null && c.Value.Length == 32 ? c.Value : null;
            if (id == null && !create) return null;
            if (id == null) id = Util.NewId();
            // Refresh the cookie each sign-in so it lives a year from the last use.
            ctx.Response.Cookies.Add(new HttpCookie(DeviceCookie, id) { HttpOnly = true, Secure = ctx.Request.IsSecureConnection, Path = "/admin", Expires = DateTime.UtcNow.AddYears(1) });
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("dev:" + id)));
        }

        private static void RememberDevice(Row r, string ip, bool trust)
        {
            var ctx = HttpContext.Current;
            var hash = DeviceHash(true);
            var list = Devices(r);
            var ua = UserAgent.Parse(ctx.Request.UserAgent ?? "");
            var name = string.Join(" on ", new[] { ua.Browser, ua.Os }.Where(s => !string.IsNullOrEmpty(s)));
            if (name == "") name = "Unknown browser";
            string place = null;
            try { var g = Geo.Lookup(ip, ctx.Request, null); if (g != null) place = string.Join(", ", new[] { g.City, g.Region, Geo.CountryName(g.Country) }.Where(s => !string.IsNullOrEmpty(s)).Distinct()); } catch { }
            var d = list.FirstOrDefault(x => x.Hash == hash);
            var isNew = d == null;
            if (isNew) { d = new KnownDevice { Hash = hash, FirstOn = DateTime.UtcNow }; list.Add(d); }
            d.Name = name; d.Ip = ip; d.Place = place; d.LastOn = DateTime.UtcNow;
            if (trust) d.TrustedUntil = DateTime.UtcNow.AddDays(30);
            list = list.OrderByDescending(x => x.LastOn).Take(10).ToList();
            Db.Exec("UPDATE CrmUsers SET KnownDevices = @k WHERE Id = @id", new { k = Json.Serialize(list), id = r.Int("Id") });
            // The first browser ever recorded is not news; any later new one is.
            if (isNew && list.Count > 1 && Alerts) SignInAlert(r, d);
        }

        private static void SignInAlert(Row r, KnownDevice d)
        {
            try
            {
                var when = Util.When(DateTime.UtcNow) + " IST";
                var body = Mailer.Heading("New sign-in to your account") + "<p style=\"margin:0 0 16px\">Your Yenetch admin account was just used to sign in from a browser we have not seen before.</p>"
                         + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%;margin:0 0 16px\">" + Mailer.Row("When", when) + Mailer.Row("Browser", d.Name) + Mailer.Row("Place", d.Place) + Mailer.Row("IP address", d.Ip) + "</table>"
                         + "<p style=\"margin:0;font-size:15px\"><b>Was this you?</b> Then there is nothing to do.</p>"
                         + "<p style=\"margin:8px 0 0;font-size:15px\"><b>Not you?</b> Change your password now and turn on two-step sign-in in My account, then tell your admin.</p>"
                         + Mailer.Button("Open My account", Mailer.SiteUrl + "/admin/account");
                Mailer.Send(r.Str("Email"), "New sign-in to your Yenetch admin account", Mailer.Wrap("A new browser signed in to your account at " + when + ".", body, null));
            }
            catch (Exception ex) { Mailer.Log("sign-in alert", ex); }
        }
    }
}
