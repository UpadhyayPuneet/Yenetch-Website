using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Yenetch.Crm
{
    /// <summary>A file sent with a website enquiry. Stored in App_Data/attachments (never served directly).</summary>
    public class LeadFile
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public string FileName { get; set; }
        public string StoredName { get; set; }
        public string ContentType { get; set; }
        public int Size { get; set; }
        public DateTime CreatedOn { get; set; }
        public string FullPath { get { return Util.AppPath("App_Data/attachments/" + StoredName); } }
        public string SizeLabel { get { return Size >= 1024 * 1024 ? (Size / 1048576.0).ToString("0.0") + " MB" : Math.Max(1, Size / 1024) + " KB"; } }
    }

    public static class Attachments
    {
        public const int MaxBytes = 5 * 1024 * 1024;
        private static readonly Dictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", "application/pdf" }, { ".doc", "application/msword" }, { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" }, { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" }, { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".png", "image/png" }, { ".webp", "image/webp" }, { ".txt", "text/plain" }
        };

        /// <summary>Why a file cannot be accepted, or null when it is fine.</summary>
        public static string Problem(HttpPostedFile f)
        {
            if (f == null || f.ContentLength == 0) return "The file is empty.";
            if (f.ContentLength > MaxBytes) return "The file is larger than 5 MB.";
            if (!Types.ContainsKey(Path.GetExtension(f.FileName ?? ""))) return "Send a PDF, Word, Excel, PowerPoint, text or image file.";
            return null;
        }

        public static LeadFile Save(int leadId, HttpPostedFile f)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            var name = Path.GetFileName(f.FileName);
            if (name.Length > 150) name = name.Substring(0, 140) + ext;
            var stored = leadId + "-" + Util.NewId().Substring(0, 12) + ext;
            Directory.CreateDirectory(Util.AppPath("App_Data/attachments"));
            f.SaveAs(Util.AppPath("App_Data/attachments/" + stored));
            var file = new LeadFile { LeadId = leadId, FileName = name, StoredName = stored, ContentType = Types[ext], Size = f.ContentLength, CreatedOn = DateTime.UtcNow };
            file.Id = Db.Insert("INSERT INTO CrmAttachments (LeadId, FileName, StoredName, ContentType, Size, CreatedOn) VALUES (@LeadId, @FileName, @StoredName, @ContentType, @Size, @CreatedOn)", file);
            return file;
        }

        public static List<LeadFile> ForLead(int leadId)
        {
            return Db.Query("SELECT * FROM CrmAttachments WHERE LeadId = @id ORDER BY Id", Map, new { id = leadId });
        }

        public static LeadFile Get(int id) { var r = Db.First("SELECT * FROM CrmAttachments WHERE Id = @id", new { id }); return r == null ? null : Map(r); }

        private static LeadFile Map(Row r)
        {
            return new LeadFile { Id = r.Int("Id"), LeadId = r.Int("LeadId"), FileName = r.Str("FileName"), StoredName = r.Str("StoredName"), ContentType = r.Str("ContentType"), Size = r.Int("Size"), CreatedOn = r.Date("CreatedOn") };
        }
    }
}
