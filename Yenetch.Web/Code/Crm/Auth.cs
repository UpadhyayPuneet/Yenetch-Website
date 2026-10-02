using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Security;

namespace Yenetch.Crm
{
    public class CrmUser
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public int OpenLeads { get; set; }

        /// <summary>Admin: everything. Manager: all leads, reports, analytics, newsletter. Sales: own and unassigned leads.</summary>
        public bool IsAdmin { get { return Role == "Admin"; } }
        public bool SeesAllLeads { get { return Role == "Admin" || Role == "Manager"; } }
        public bool CanUseMarketing { get { return Role == "Admin" || Role == "Manager"; } }
        public string Initials { get { return Util.Initials(Name); } }

        internal static CrmUser From(Row r)
        {
            return new CrmUser { Id = r.Int("Id"), Email = r.Str("Email"), Name = r.Str("Name"), Role = r.Str("Role"), IsActive = r.Bool("IsActive"),
                                 LastLoginOn = r.DateN("LastLoginOn"), CreatedOn = r.Date("CreatedOn"), OpenLeads = r.Int("OpenLeads") };
        }
    }

    public static class Auth
    {
        private const int Iterations = 120000;

        // ---- Passwords ---------------------------------------------------------------------------------------

        public static string Hash(string password)
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return "pbkdf2-sha256$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(kdf.GetBytes(32));
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                var p = stored.Split('$');
                if (p.Length != 4 || p[0] != "pbkdf2-sha256") return false;
                var salt = Convert.FromBase64String(p[2]);
                var expected = Convert.FromBase64String(p[3]);
                using (var kdf = new Rfc2898DeriveBytes(password, salt, int.Parse(p[1]), HashAlgorithmName.SHA256))
                {
                    var actual = kdf.GetBytes(expected.Length);
                    var diff = 0;
                    for (var i = 0; i < expected.Length; i++) diff |= expected[i] ^ actual[i];
                    return diff == 0;
                }
            }
            catch { return false; }
        }

        public static string PasswordProblem(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 10) return "Use at least 10 characters.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return "Use letters and at least one number.";
            return null;
        }

        // ---- Users -------------------------------------------------------------------------------------------

        private const string UserSelect = @"SELECT u.*, (SELECT COUNT(*) FROM CrmLeads l WHERE l.AssignedTo = u.Id AND l.Status IN ('New','Contacted','Qualified','Proposal','Negotiation')) AS OpenLeads FROM CrmUsers u";

        public static bool AnyUsers() { return Db.Scalar<int>("SELECT COUNT(*) FROM CrmUsers") > 0; }
        public static List<CrmUser> Users() { return Db.Query(UserSelect + " ORDER BY u.IsActive DESC, u.Name", CrmUser.From); }
        public static List<CrmUser> ActiveUsers() { return Db.Query(UserSelect + " WHERE u.IsActive = 1 ORDER BY u.Name", CrmUser.From); }
        public static CrmUser User(int id) { var r = Db.First(UserSelect + " WHERE u.Id = @id", new { id }); return r == null ? null : CrmUser.From(r); }

        public static int CreateUser(string email, string name, string role, string password)
        {
            if (!Lists.Roles.Contains(role)) throw new ArgumentException("role");
            return Db.Insert("INSERT INTO CrmUsers (Email, Name, Role, PasswordHash, IsActive, FailedLogins, CreatedOn) VALUES (@email, @name, @role, @hash, 1, 0, @now)",
                new { email = email.Trim().ToLowerInvariant(), name = name.Trim(), role, hash = Hash(password), now = DateTime.UtcNow });
        }

        public static void UpdateUser(int id, string name, string role, bool active)
        {
            if (!Lists.Roles.Contains(role)) throw new ArgumentException("role");
            Db.Exec("UPDATE CrmUsers SET Name = @name, Role = @role, IsActive = @active WHERE Id = @id", new { id, name = name.Trim(), role, active });
        }

        public static void SetPassword(int id, string password)
        {
            Db.Exec("UPDATE CrmUsers SET PasswordHash = @hash, FailedLogins = 0, LockedUntil = NULL WHERE Id = @id", new { id, hash = Hash(password) });
        }

        public static bool CheckPassword(int id, string password)
        {
            var hash = Db.Scalar<string>("SELECT PasswordHash FROM CrmUsers WHERE Id = @id", new { id });
            return hash != null && Verify(password, hash);
        }

        // ---- Sign in -----------------------------------------------------------------------------------------

        /// <summary>Checks the password and signs in. Locks the account for 15 minutes after 5 failures.
        /// needsCode: the password was right and a second step (/admin/verify) is needed before the user is signed in.</summary>
        public static string SignIn(string email, string password, bool remember, out bool needsCode)
        {
            needsCode = false;
            var r = Db.First("SELECT * FROM CrmUsers WHERE Email = @email", new { email = (email ?? "").Trim().ToLowerInvariant() });
            if (r == null || !r.Bool("IsActive")) { Verify(password ?? "", "pbkdf2-sha256$" + Iterations + "$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="); return "Email or password is incorrect."; }
            var locked = r.DateN("LockedUntil");
            if (locked.HasValue && locked.Value > DateTime.UtcNow) return "Too many attempts. Try again in " + Util.Span(locked.Value - DateTime.UtcNow) + ".";
            if (!Verify(password ?? "", r.Str("PasswordHash")))
            {
                var fails = r.Int("FailedLogins") + 1;
                Db.Exec("UPDATE CrmUsers SET FailedLogins = @f, LockedUntil = @until WHERE Id = @id",
                    new { f = fails >= 5 ? 0 : fails, until = fails >= 5 ? (DateTime?)DateTime.UtcNow.AddMinutes(15) : null, id = r.Int("Id") });
                return "Email or password is incorrect.";
            }
            var method = TwoFactor.Method(r);
            if (method != null && !TwoFactor.IsTrusted(r))
            {
                Db.Exec("UPDATE CrmUsers SET FailedLogins = 0 WHERE Id = @id", new { id = r.Int("Id") });
                TwoFactor.Start(r.Int("Id"), remember);
                if (method == "email") TwoFactor.SendEmailCode(r);
                needsCode = true;
                return null;
            }
            TwoFactor.Complete(r, remember, false);
            return null;
        }

        public static void IssueCookie(CrmUser u, bool remember)
        {
            var hours = remember ? 24 * 14 : 12;
            var ticket = new FormsAuthenticationTicket(2, u.Email, DateTime.Now, DateTime.Now.AddHours(hours), remember, u.Id.ToString());
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true, Secure = HttpContext.Current.Request.IsSecureConnection, Path = FormsAuthentication.FormsCookiePath
            };
            if (remember) cookie.Expires = ticket.Expiration;
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static void SignOut() { FormsAuthentication.SignOut(); }

        /// <summary>The signed-in, still-active user for this request (checked against the database once per request).</summary>
        public static CrmUser Current
        {
            get
            {
                var ctx = HttpContext.Current;
                if (ctx == null) return null;
                if (ctx.Items.Contains("crm.user")) return ctx.Items["crm.user"] as CrmUser;
                CrmUser u = null;
                var id = ctx.User != null ? ctx.User.Identity as FormsIdentity : null;
                int uid;
                if (id != null && id.IsAuthenticated && int.TryParse(id.Ticket.UserData, out uid))
                {
                    u = User(uid);
                    if (u != null && !u.IsActive) u = null;
                }
                ctx.Items["crm.user"] = u;
                return u;
            }
        }
    }
}
