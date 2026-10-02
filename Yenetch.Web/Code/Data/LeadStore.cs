using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;

namespace Yenetch.Data
{
    /// <summary>Appends leads to App_Data/leads.jsonl. Replace the body with an INSERT into dbo.Leads when the DB is live.</summary>
    public static class LeadStore
    {
        private static readonly object FileLock = new object();

        public static void Save(IDictionary<string, object> lead)
        {
            lead["receivedAt"] = DateTime.UtcNow.ToString("o");
            var line = new JavaScriptSerializer().Serialize(lead);
            var file = HttpContext.Current.Server.MapPath("~/App_Data/leads.jsonl");
            lock (FileLock) File.AppendAllText(file, line + Environment.NewLine);
        }
    }
}
