using System;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>Opens or downloads a candidate's CV (/Admin/Resume.ashx?id=, add download=1 to save). Admins and Managers only.</summary>
    public class ResumeHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.Cache.SetCacheability(HttpCacheability.Private);
            ctx.Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            var me = Auth.Current;
            if (me == null) { ctx.Response.Redirect("~/admin/login", false); return; }
            if (!me.CanUseMarketing) { ctx.Response.StatusCode = 403; return; }
            int id;
            var a = int.TryParse(ctx.Request.QueryString["id"], out id) ? Applications.Get(id) : null;
            if (a == null || !a.HasFile || !System.IO.File.Exists(a.FullPath)) { ctx.Response.StatusCode = 404; return; }
            var inline = a.ContentType == "application/pdf" && ctx.Request.QueryString["download"] != "1";
            ctx.Response.ContentType = a.ContentType;
            ctx.Response.AppendHeader("X-Content-Type-Options", "nosniff");
            ctx.Response.AppendHeader("Content-Disposition", (inline ? "inline" : "attachment") + "; filename*=UTF-8''" + Uri.EscapeDataString(a.FileName));
            ctx.Response.TransmitFile(a.FullPath);
        }
    }
}
