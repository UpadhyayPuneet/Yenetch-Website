using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Hosting;

namespace Yenetch.Crm
{
    /// <summary>
    /// Small ADO.NET helper for the CRM, analytics and newsletter tables.
    /// Uses the "CrmDb" connection string. SQL Server is the default; SQLite (providerName containing "Sqlite")
    /// is supported for local testing and small single-server installs. SQL differences live in the Dialect members.
    /// All dates are stored in UTC.
    /// </summary>
    public static class Db
    {
        private static readonly ConnectionStringSettings Settings = ConfigurationManager.ConnectionStrings["CrmDb"];
        private static DbProviderFactory _factory;

        public static bool IsConfigured { get { return Settings != null && !string.IsNullOrWhiteSpace(Settings.ConnectionString); } }
        public static bool IsSqlite { get { return IsConfigured && (Settings.ProviderName ?? "").IndexOf("Sqlite", StringComparison.OrdinalIgnoreCase) >= 0; } }

        private static DbProviderFactory Factory
        {
            get { return _factory ?? (_factory = DbProviderFactories.GetFactory(string.IsNullOrEmpty(Settings.ProviderName) ? "System.Data.SqlClient" : Settings.ProviderName)); }
        }

        public static DbConnection Open()
        {
            var c = Factory.CreateConnection();
            c.ConnectionString = HostingEnvironment.IsHosted
                ? Settings.ConnectionString.Replace("|DataDirectory|", Util.AppPath("App_Data"))
                : Settings.ConnectionString;
            c.Open();
            if (IsSqlite) using (var cmd = c.CreateCommand()) { cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;"; cmd.ExecuteNonQuery(); }
            return c;
        }

        // ---- SQL dialect -------------------------------------------------------------------------------------

        /// <summary>Paging clause placed after ORDER BY.</summary>
        public static string Page(int offset, int count)
        {
            return IsSqlite
                ? " LIMIT " + count + " OFFSET " + offset
                : " OFFSET " + offset + " ROWS FETCH NEXT " + count + " ROWS ONLY";
        }

        /// <summary>Calendar day of a UTC column in India Standard Time, as a sortable yyyy-MM-dd value.</summary>
        public static string LocalDay(string col)
        {
            return IsSqlite
                ? "date(" + col + ", '+330 minutes')"
                : "CONVERT(char(10), DATEADD(minute, 330, " + col + "), 23)";
        }

        /// <summary>Calendar month (yyyy-MM) of a UTC column in IST.</summary>
        public static string LocalMonth(string col)
        {
            return IsSqlite
                ? "strftime('%Y-%m', " + col + ", '+330 minutes')"
                : "CONVERT(char(7), DATEADD(minute, 330, " + col + "), 23)";
        }

        /// <summary>Hour of day (0-23) of a UTC column in IST.</summary>
        public static string LocalHour(string col)
        {
            return IsSqlite
                ? "CAST(strftime('%H', " + col + ", '+330 minutes') AS INTEGER)"
                : "DATEPART(hour, DATEADD(minute, 330, " + col + "))";
        }

        /// <summary>Whole seconds between two columns.</summary>
        public static string Seconds(string from, string to)
        {
            return IsSqlite
                ? "CAST((julianday(" + to + ") - julianday(" + from + ")) * 86400 AS INTEGER)"
                : "DATEDIFF(second, " + from + ", " + to + ")";
        }

        private static string LastId { get { return IsSqlite ? "SELECT last_insert_rowid();" : "SELECT CAST(SCOPE_IDENTITY() AS INT);"; } }

        // ---- Commands ----------------------------------------------------------------------------------------

        public static int Exec(string sql, object args = null)
        {
            using (var c = Open())
            using (var cmd = Command(c, sql, args)) return cmd.ExecuteNonQuery();
        }

        /// <summary>INSERT and return the new identity value.</summary>
        public static int Insert(string sql, object args = null)
        {
            using (var c = Open())
            using (var cmd = Command(c, sql.TrimEnd(' ', ';') + "; " + LastId, args))
                return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public static T Scalar<T>(string sql, object args = null)
        {
            using (var c = Open())
            using (var cmd = Command(c, sql, args))
            {
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return default(T);
                var t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return (T)Convert.ChangeType(v, t, CultureInfo.InvariantCulture);
            }
        }

        public static List<Row> Rows(string sql, object args = null)
        {
            var list = new List<Row>();
            using (var c = Open())
            using (var cmd = Command(c, sql, args))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var row = new Row();
                    for (var i = 0; i < r.FieldCount; i++) row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                    list.Add(row);
                }
            }
            return list;
        }

        public static Row First(string sql, object args = null)
        {
            var rows = Rows(sql, args);
            return rows.Count > 0 ? rows[0] : null;
        }

        public static List<T> Query<T>(string sql, Func<Row, T> map, object args = null)
        {
            return Rows(sql, args).ConvertAll(r => map(r));
        }

        private static DbCommand Command(DbConnection c, string sql, object args)
        {
            var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 60;
            if (args == null) return cmd;
            var dict = args as IDictionary<string, object>;
            if (dict != null) foreach (var kv in dict) Add(cmd, kv.Key, kv.Value);
            else foreach (var p in args.GetType().GetProperties()) Add(cmd, p.Name, p.GetValue(args, null));
            return cmd;
        }

        private static void Add(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = "@" + name;
            if (value is DateTime) { p.DbType = DbType.DateTime2; if (IsSqlite) { p.DbType = DbType.String; value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); } }
            else if (value is bool && IsSqlite) value = (bool)value ? 1 : 0;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        // ---- Schema ------------------------------------------------------------------------------------------

        private static readonly object SchemaLock = new object();
        private static bool _ready;

        /// <summary>
        /// Creates the tables on first use (App_Data/sql/crm.*.sql, then later additions such as cms.*.sql).
        /// On SQL Server it also creates the database when it is missing and the login is allowed to (true for LocalDB).
        /// </summary>
        public static void EnsureSchema()
        {
            if (_ready || !IsConfigured) return;
            lock (SchemaLock)
            {
                if (_ready) return;
                if (!IsSqlite) CreateDatabaseIfMissing();
                var exists = IsSqlite
                    ? Scalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'CrmUsers'") > 0
                    : Scalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name = 'CrmUsers'") > 0;
                if (!exists) RunScript("crm");
                // Later additions, applied to databases created before they existed.
                if (!TableExists("CmsPosts")) RunScript("cms");
                if (!TableExists("CmsItems")) RunScript("content");
                if (!TableExists("CrmApplications")) RunScript("careers");
                if (!TableExists("NewsDeliveries")) RunScript("sending");
                if (!TableExists("Bookings")) RunScript("growth");
                _ready = true;
            }
        }

        private static bool TableExists(string name)
        {
            return IsSqlite
                ? Scalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @n", new { n = name }) > 0
                : Scalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name = @n", new { n = name }) > 0;
        }

        /// <summary>Runs App_Data/sql/{name}.sqlserver.sql or {name}.sqlite.sql in one transaction.</summary>
        private static void RunScript(string name)
        {
            var file = Util.AppPath("App_Data/sql/" + name + (IsSqlite ? ".sqlite.sql" : ".sqlserver.sql"));
            var script = File.ReadAllText(file);
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                foreach (var stmt in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    var body = StripComments(stmt);
                    if (body.Length == 0) continue;
                    using (var cmd = c.CreateCommand()) { cmd.Transaction = tx; cmd.CommandText = body; cmd.ExecuteNonQuery(); }
                }
                tx.Commit();
            }
        }

        private static void CreateDatabaseIfMissing()
        {
            try { using (Open()) { } return; }
            catch (DbException ex) when (ex.Message.IndexOf("Cannot open database", StringComparison.OrdinalIgnoreCase) >= 0) { }

            var b = Factory.CreateConnectionStringBuilder();
            b.ConnectionString = Settings.ConnectionString;
            var key = b.ContainsKey("Initial Catalog") ? "Initial Catalog" : "Database";
            var name = Convert.ToString(b[key]);
            if (string.IsNullOrEmpty(name) || !Regex.IsMatch(name, @"^[A-Za-z0-9_\-]+$")) throw new InvalidOperationException("Create the CrmDb database first.");
            b[key] = "master";
            using (var c = Factory.CreateConnection())
            {
                c.ConnectionString = b.ConnectionString;
                c.Open();
                using (var cmd = c.CreateCommand()) { cmd.CommandText = "IF DB_ID('" + name + "') IS NULL CREATE DATABASE [" + name + "]"; cmd.ExecuteNonQuery(); }
            }
            // The failed login above can stay cached in the connection pool; clear it so the next open sees the new database.
            System.Data.SqlClient.SqlConnection.ClearAllPools();
        }

        private static string StripComments(string sql)
        {
            var lines = new List<string>();
            foreach (var line in sql.Split('\n'))
            {
                var t = line.TrimEnd('\r');
                if (t.TrimStart().StartsWith("--", StringComparison.Ordinal)) continue;
                var i = t.IndexOf("--", StringComparison.Ordinal);
                lines.Add(i >= 0 ? t.Substring(0, i) : t);
            }
            return string.Join("\n", lines).Trim();
        }
    }

    /// <summary>One result row, with typed getters that tolerate provider differences (SQLite returns long and string dates).</summary>
    public class Row : Dictionary<string, object>
    {
        public Row() : base(StringComparer.OrdinalIgnoreCase) { }

        public string Str(string k) { object v; return TryGetValue(k, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : null; }
        public int Int(string k) { object v; return TryGetValue(k, out v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0; }
        public int? IntN(string k) { object v; return TryGetValue(k, out v) && v != null ? (int?)Convert.ToInt32(v, CultureInfo.InvariantCulture) : null; }
        public long Long(string k) { object v; return TryGetValue(k, out v) && v != null ? Convert.ToInt64(v, CultureInfo.InvariantCulture) : 0; }
        public double Dbl(string k) { object v; return TryGetValue(k, out v) && v != null ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : 0; }
        public decimal? DecN(string k) { object v; return TryGetValue(k, out v) && v != null ? (decimal?)Convert.ToDecimal(v, CultureInfo.InvariantCulture) : null; }
        public bool Bool(string k) { object v; return TryGetValue(k, out v) && v != null && Convert.ToBoolean(v is string ? (object)((string)v == "1" || ((string)v).ToLowerInvariant() == "true") : v, CultureInfo.InvariantCulture); }
        public DateTime Date(string k) { var d = DateN(k); return d ?? DateTime.MinValue; }

        public DateTime? DateN(string k)
        {
            object v;
            if (!TryGetValue(k, out v) || v == null) return null;
            if (v is DateTime) return DateTime.SpecifyKind((DateTime)v, DateTimeKind.Utc);
            DateTime d;
            return DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out d) ? (DateTime?)d : null;
        }
    }
}
