using System;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>Downloads a file attached to a lead (/Admin/Attachment.ashx?id=). Sales users only get files of leads they can see.</summary>
    public class AttachmentHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.Cache.SetCacheability(HttpCacheability.Private);
            ctx.Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            var me = Auth.Current;
            if (me == null) { ctx.Response.Redirect("~/admin/login", false); return; }
            int id;
            var f = int.TryParse(ctx.Request.QueryString["id"], out id) ? Attachments.Get(id) : null;
            var lead = f == null ? null : LeadService.Get(f.LeadId);
            if (f == null || lead == null || !System.IO.File.Exists(f.FullPath)) { ctx.Response.StatusCode = 404; return; }
            if (!me.SeesAllLeads && lead.AssignedTo.HasValue && lead.AssignedTo != me.Id) { ctx.Response.StatusCode = 403; return; }
            var inline = f.ContentType.StartsWith("image/") || f.ContentType == "application/pdf";
            ctx.Response.ContentType = f.ContentType;
            ctx.Response.AppendHeader("X-Content-Type-Options", "nosniff");
            ctx.Response.AppendHeader("Content-Disposition", (inline ? "inline" : "attachment") + "; filename*=UTF-8''" + Uri.EscapeDataString(f.FileName));
            ctx.Response.TransmitFile(f.FullPath);
        }
    }
}
