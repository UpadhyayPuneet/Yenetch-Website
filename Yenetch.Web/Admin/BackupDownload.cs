using System;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/backup?f=name: downloads a backup zip. Admins only.</summary>
    public class BackupDownload : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            var me = Auth.Current;
            if (me == null || !me.IsAdmin) { ctx.Response.StatusCode = 403; return; }
            var path = Backups.PathOf(ctx.Request.QueryString["f"]);
            if (path == null) { ctx.Response.StatusCode = 404; return; }
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            ctx.Response.ContentType = "application/zip";
            ctx.Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + System.IO.Path.GetFileName(path) + "\"");
            ctx.Response.TransmitFile(path);
        }
    }
}
