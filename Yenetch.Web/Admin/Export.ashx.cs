using System;
using System.Text;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>
    /// CSV downloads for the admin. what=leads exports the same view and filters as the leads list (Sales users get only
    /// their own and unassigned leads); what=subscribers needs an Admin or Manager.
    /// </summary>
    public class ExportHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            ctx.Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            var me = Auth.Current;
            if (me == null) { ctx.Response.Redirect("~/admin/login", false); return; }

            string csv, name;
            var stamp = Util.TodayIst.ToString("yyyy-MM-dd");
            switch ((ctx.Request.QueryString["what"] ?? "").ToLowerInvariant())
            {
                case "leads":
                    var view = (ctx.Request.QueryString["view"] ?? "").Trim();
                    csv = LeadService.Csv(Leads.ForView(ctx.Request.QueryString, me, view == "" ? "open" : view, true));
                    name = "yenetch-leads-" + stamp + ".csv";
                    break;
                case "subscribers":
                    if (!me.CanUseMarketing) { ctx.Response.StatusCode = 403; return; }
                    csv = Newsletter.Csv();
                    name = "yenetch-subscribers-" + stamp + ".csv";
                    break;
                default:
                    ctx.Response.StatusCode = 404;
                    return;
            }

            ctx.Response.ContentType = "text/csv";
            ctx.Response.ContentEncoding = new UTF8Encoding(true);
            ctx.Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + name + "\"");
            // Byte order mark so Excel opens Indian names and the rupee sign correctly.
            ctx.Response.BinaryWrite(new byte[] { 0xEF, 0xBB, 0xBF });
            ctx.Response.Write(csv);
        }
    }
}
