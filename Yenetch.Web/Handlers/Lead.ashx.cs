using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;

namespace Yenetch.Web.Handlers
{
    /// <summary>Receives leads from the chatbot, solution finder and contact form as JSON, and saves them to the CRM (CrmLeads).</summary>
    public class LeadHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            if (ctx.Request.HttpMethod != "POST") { ctx.Response.StatusCode = 405; return; }

            string body;
            using (var sr = new StreamReader(ctx.Request.InputStream)) body = sr.ReadToEnd();
            if (body.Length > 20000) { ctx.Response.StatusCode = 413; return; }

            var ser = new JavaScriptSerializer();
            Dictionary<string, object> lead;
            try { lead = ser.Deserialize<Dictionary<string, object>>(body); }
            catch { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"ok\":false}"); return; }

            // Honeypot: the site's forms never fill "website"; bots do.
            if (Get(lead, "website", 200).Length > 0) { ctx.Response.Write("{\"ok\":true}"); return; }

            var name = Get(lead, "name", 120);
            var contact = Get(lead, "contact", 160);
            var source = Get(lead, "source", 40);

            Db.EnsureSchema();

            // Newsletter signups from the site footer and blog go to the subscriber list, not the lead pipeline.
            if (source == "newsletter")
            {
                var ok = Newsletter.Subscribe(contact, null, "Website" + (Get(lead, "page", 300).Length > 0 ? " " + Get(lead, "page", 300) : ""), Analytics.CookieId(ctx.Request, Analytics.VisitorCookie));
                ctx.Response.StatusCode = ok ? 200 : 422;
                ctx.Response.Write(ok ? "{\"ok\":true}" : "{\"ok\":false,\"error\":\"Enter a valid email address\"}");
                return;
            }

            if (name.Length < 2 || contact.Length < 6) { ctx.Response.StatusCode = 422; ctx.Response.Write("{\"ok\":false,\"error\":\"name and contact are required\"}"); return; }

            // Everything else the widget sent (finder answers, chat transcript summary) is kept as context.
            var extra = new Dictionary<string, object>(lead);
            foreach (var k in new[] { "name", "contact", "need", "topic", "source", "page" }) extra.Remove(k);
            var context = extra.Count > 0 ? ser.Serialize(extra) : null;

            try
            {
                LeadService.CreateFromWebsite(name, contact, Get(lead, "need", 2000), Get(lead, "topic", 160), source, Get(lead, "page", 300),
                    Analytics.CookieId(ctx.Request, Analytics.VisitorCookie), context);
            }
            catch (Exception ex)
            {
                // The database is down: keep the lead in App_Data/leads.jsonl so it is never lost.
                lead["ip"] = ctx.Request.UserHostAddress;
                lead["error"] = ex.Message;
                Yenetch.Data.LeadStore.Save(lead);
            }
            ctx.Response.Write("{\"ok\":true}");
        }

        private static string Get(Dictionary<string, object> d, string key, int max)
        {
            object v;
            var s = d.TryGetValue(key, out v) && v != null ? Convert.ToString(v).Trim() : "";
            return s.Length > max ? s.Substring(0, max) : s;
        }
    }
}
