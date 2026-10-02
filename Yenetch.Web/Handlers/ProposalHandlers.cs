using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Web.Handlers
{
    /// <summary>/api/proposal: the client accepts or declines a proposal from its page.</summary>
    public class ProposalHandler : JsonPostHandler
    {
        protected override void Handle(HttpContext ctx, Dictionary<string, object> d)
        {
            if (!Guard.Allow(ctx.Request, "proposal", 20, TimeSpan.FromHours(1))) { Write(ctx, 429, new { ok = false, error = "Too many attempts. Please try again later." }); return; }
            var p = Proposals.ByToken(S(d, "token", 40));
            if (p == null) { Write(ctx, 404, new { ok = false, error = "Proposal not found." }); return; }
            var ip = Analytics.ClientIp(ctx.Request);
            string error;
            switch (S(d, "action", 10))
            {
                case "accept":
                    if (S(d, "agree", 5) != "1" && S(d, "agree", 5).ToLowerInvariant() != "true") { Write(ctx, 422, new { ok = false, error = "Tick the box to confirm you agree." }); return; }
                    error = Proposals.Accept(p, S(d, "name", 160), ip);
                    break;
                case "decline":
                    error = Proposals.Decline(p, S(d, "reason", 1000), ip);
                    break;
                default: Write(ctx, 400, new { ok = false }); return;
            }
            if (error != null) { Write(ctx, 422, new { ok = false, error }); return; }
            var fresh = Proposals.Get(p.Id);
            Write(ctx, 200, new { ok = true, payUrl = fresh.IsPaid ? null : fresh.PayLinkUrl, portal = Proposals.PortalFor(fresh) });
        }
    }

    /// <summary>/api/razorpay: Razorpay webhooks (payment_link.paid). Only requests signed with the webhook secret are accepted.</summary>
    public class RazorpayWebhookHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.TrySkipIisCustomErrors = true;
            if (ctx.Request.HttpMethod != "POST" || ctx.Request.ContentLength > 200000) { ctx.Response.StatusCode = 405; return; }
            string body;
            using (var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) body = sr.ReadToEnd();
            if (!Razorpay.WebhookValid(body, ctx.Request.Headers["X-Razorpay-Signature"])) { ctx.Response.StatusCode = 401; ctx.Response.Write("{\"ok\":false}"); return; }
            try { Db.EnsureSchema(); Razorpay.HandleWebhook(body); }
            catch (Exception ex) { Mailer.Log("razorpay webhook", ex); ctx.Response.StatusCode = 500; ctx.Response.Write("{\"ok\":false}"); return; }
            ctx.Response.Write("{\"ok\":true}");
        }
    }
}
