using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class BackupFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// Nightly backups: every table as JSON plus the uploaded files (images, CVs, enquiry attachments), zipped into
    /// App_Data/backups (never served to the web). Keeps the newest 14 by default. Admins download them from /admin/security.
    /// This is a safety copy that works on any host. Keep your host's own SQL Server backups switched on as well.
    /// </summary>
    public static class Backups
    {
        private static readonly object Lock = new object();
        private static volatile bool _running;

        public static string Folder { get { return Util.AppPath("App_Data/backups"); } }
        public static bool Enabled { get { return Settings.Get("backup.enabled") != "0"; } }
        public static int Hour { get { int h; return int.TryParse(Settings.Get("backup.hour"), out h) && h >= 0 && h <= 23 ? h : 2; } }
        public static int Keep { get { int k; return int.TryParse(Settings.Get("backup.keep"), out k) && k >= 1 && k <= 60 ? k : 14; } }
        public static bool IncludeFiles { get { return Settings.Get("backup.files") != "0"; } }
        public static string LastError { get { return Settings.Get("backup.lastError"); } }
        public static bool Running { get { return _running; } }

        public static void Save(bool enabled, int hour, int keep, bool files)
        {
            Settings.Set("backup.enabled", enabled ? "1" : "0");
            Settings.Set("backup.hour", Math.Max(0, Math.Min(23, hour)).ToString(CultureInfo.InvariantCulture));
            Settings.Set("backup.keep", Math.Max(1, Math.Min(60, keep)).ToString(CultureInfo.InvariantCulture));
            Settings.Set("backup.files", files ? "1" : "0");
        }

        /// <summary>Called every minute: one backup per IST day at or after the chosen hour, on a background thread.</summary>
        public static void Tick()
        {
            try
            {
                if (!Enabled || _running) return;
                var ist = Util.Ist(DateTime.UtcNow);
                if (ist.Hour < Hour) return;
                var today = ist.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                lock (Lock)
                {
                    if (Settings.Get("backup.lastRun") == today) return;
                    Settings.Set("backup.lastRun", today);
                }
                System.Threading.Tasks.Task.Run(() => { string err; Run(out err); });
            }
            catch (Exception ex) { Mailer.Log("backup", ex); }
        }

        /// <summary>Makes a backup now. Returns the file name, or null with an error.</summary>
        public static string Run(out string error)
        {
            error = null;
            lock (Lock)
            {
                if (_running) { error = "A backup is already running."; return null; }
                _running = true;
            }
            var name = "yenetch-backup-" + Util.Ist(DateTime.UtcNow).ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".zip";
            var path = Path.Combine(Folder, name);
            var temp = path + ".part";
            try
            {
                Directory.CreateDirectory(Folder);
                using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    var tables = Tables();
                    var counts = new Dictionary<string, int>();
                    foreach (var t in tables) counts[t] = WriteTable(zip, t);
                    var files = 0;
                    if (IncludeFiles)
                        foreach (var dir in new[] { "uploads", "App_Data/resumes", "App_Data/attachments" })
                            files += AddFolder(zip, Util.AppPath(dir), dir);
                    var info = new Dictionary<string, object> {
                        { "createdOn", DateTime.UtcNow.ToString("o") }, { "database", Db.IsSqlite ? "SQLite" : "SQL Server" }, { "tables", counts }, { "files", files },
                        { "howToRestore", "Each table is a JSON array of rows in tables/. Uploaded files keep their site paths in files/. Ask your developer to import them, or restore your host's SQL Server backup." }
                    };
                    var e = zip.CreateEntry("backup-info.json", CompressionLevel.Optimal);
                    using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(new JavaScriptSerializer().Serialize(info));
                }
                File.Move(temp, path);
                Settings.Set("backup.lastError", null);
                Prune();
                return name;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                error = ex.Message;
                Settings.Set("backup.lastError", Util.Ist(DateTime.UtcNow).ToString("d MMM yyyy, h:mm tt", Util.India) + ": " + Util.Cut(ex.Message, 300));
                Mailer.Log("backup", ex);
                return null;
            }
            finally { _running = false; }
        }

        private static List<string> Tables()
        {
            var sql = Db.IsSqlite ? "SELECT name AS N FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
                                  : "SELECT TABLE_NAME AS N FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME";
            // Table names come from the database itself; anything unusual is skipped rather than quoted.
            return Db.Rows(sql).Select(r => r.Str("N")).Where(n => !string.IsNullOrEmpty(n) && n.All(c => char.IsLetterOrDigit(c) || c == '_')).ToList();
        }

        /// <summary>Streams one table into tables/{name}.json without loading it all into memory.</summary>
        private static int WriteTable(ZipArchive zip, string table)
        {
            var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var n = 0;
            var entry = zip.CreateEntry("tables/" + table + ".json", CompressionLevel.Optimal);
            using (var w = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            using (var c = Db.Open())
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM " + (Db.IsSqlite ? "\"" + table + "\"" : "[dbo].[" + table + "]");
                cmd.CommandTimeout = 600;
                using (DbDataReader rd = cmd.ExecuteReader())
                {
                    w.Write("[");
                    var row = new Dictionary<string, object>();
                    while (rd.Read())
                    {
                        row.Clear();
                        for (var i = 0; i < rd.FieldCount; i++)
                        {
                            var v = rd.IsDBNull(i) ? null : rd.GetValue(i);
                            if (v is DateTime) v = ((DateTime)v).ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                            else if (v is byte[]) v = Convert.ToBase64String((byte[])v);
                            else if (v is Guid) v = v.ToString();
                            row[rd.GetName(i)] = v;
                        }
                        w.Write(n == 0 ? "\n" : ",\n");
                        w.Write(js.Serialize(row));
                        n++;
                    }
                    w.Write("\n]");
                }
            }
            return n;
        }

        private static int AddFolder(ZipArchive zip, string folder, string prefix)
        {
            if (!Directory.Exists(folder)) return 0;
            var n = 0;
            foreach (var f in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                var rel = prefix + "/" + f.Substring(folder.Length).TrimStart('\\', '/').Replace('\\', '/');
                var e = zip.CreateEntry("files/" + rel, CompressionLevel.Fastest);
                e.LastWriteTime = File.GetLastWriteTime(f);
                using (var src = File.OpenRead(f))
                using (var dst = e.Open()) src.CopyTo(dst);
                n++;
            }
            return n;
        }

        private static void Prune()
        {
            foreach (var old in List().Skip(Keep))
                try { File.Delete(Path.Combine(Folder, old.Name)); } catch { }
        }

        /// <summary>Backups on disk, newest first.</summary>
        public static List<BackupFile> List()
        {
            if (!Directory.Exists(Folder)) return new List<BackupFile>();
            return new DirectoryInfo(Folder).GetFiles("yenetch-backup-*.zip").OrderByDescending(f => f.Name)
                .Select(f => new BackupFile { Name = f.Name, Size = f.Length, CreatedOn = f.LastWriteTimeUtc }).ToList();
        }

        /// <summary>Full path of a backup by name, or null. Only plain backup names are accepted.</summary>
        public static string PathOf(string name)
        {
            if (string.IsNullOrEmpty(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, @"^yenetch-backup-\d{8}-\d{4}\.zip$")) return null;
            var p = Path.Combine(Folder, name);
            return File.Exists(p) ? p : null;
        }

        public static void Delete(string name) { var p = PathOf(name); if (p != null) File.Delete(p); }

        public static string Size(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024 / 1024).ToString("0.0", CultureInfo.InvariantCulture) + " GB";
            if (bytes >= 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            return Math.Max(1, bytes / 1024) + " KB";
        }
    }
}
