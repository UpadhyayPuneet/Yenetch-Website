using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Data;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/proposals/{id} (or /new?lead=&amp;quote=): edit a proposal, send it, create a payment link, see its history.</summary>
    public partial class ProposalEditPage : AdminPage
    {
        public override string Section { get { return "proposals"; } }

        protected Proposal P;
        protected List<Row> History = new List<Row>();
        protected string Err, ItemsJson, CatalogOptions, SuggestJson, DefaultPortal, DefaultMessage, SendTo, SendCc, SendBcc;

        protected void Page_Load(object sender, EventArgs e)
        {
            var raw = Convert.ToString(RouteData.Values["id"]);
            int id;
            if (raw == "new")
            {
                int lead, quote;
                int.TryParse(Request.QueryString["lead"], out lead); int.TryParse(Request.QueryString["quote"], out quote);
                if (lead > 0 && !CanSeeLead(lead)) { RedirectWith("/admin/proposals", "That lead belongs to someone else."); return; }
                P = Proposals.NewDraft(lead > 0 ? (int?)lead : null, quote > 0 ? (int?)quote : null);
            }
            else if (int.TryParse(raw, out id)) P = Proposals.Get(id);
            if (P == null) { RedirectWith("/admin/proposals", "Proposal not found."); return; }
            if (P.Id > 0 && !Me.SeesAllLeads && P.CreatedBy != Me.Id && !(P.LeadId.HasValue && CanSeeLead(P.LeadId.Value))) { RedirectWith("/admin/proposals", "That proposal belongs to someone else."); return; }

            if (IsPostBack) { Handle(); if (Response.IsRequestBeingRedirected) return; }
            Page.Title = P.Id == 0 ? "New proposal" : P.Number;
            ItemsJson = ItemsJson ?? new JavaScriptSerializer().Serialize(P.Items.Select(i => new { name = i.Name, description = i.Description, qty = i.Qty, unit = i.Unit, price = i.Price, billing = i.Billing ?? "one-time" }));
            if (P.Id > 0) History = Proposals.Events(P.Id);
            CatalogOptions = Catalog();
            SuggestJson = new JavaScriptSerializer().Serialize(Proposals.Suggestions(P).Select(s => new { email = s.Key, label = s.Value }));
            DefaultPortal = Settings.Get("integrations.portalUrl");
            SendTo = SendTo ?? P.ClientEmail;
            DefaultMessage = DefaultMessage ?? "Hi " + ((P.ClientName ?? "").Split(' ')[0]) + ",\n\nThank you for your time. Here is our proposal for " + P.Title + ". You can read it, accept it online and pay securely from the link below.\n\nHappy to answer any questions.\n\n" + Me.Name;
        }

        private bool CanSeeLead(int leadId)
        {
            if (Me.SeesAllLeads) return true;
            var owner = Db.Scalar<int?>("SELECT AssignedTo FROM CrmLeads WHERE Id = @id", new { id = leadId });
            return !owner.HasValue || owner.Value == Me.Id;
        }

        private void Handle()
        {
            var f = Request.Form;
            var act = f["act"];
            if (act == "delete" && P.Id > 0) { Proposals.Delete(P.Id); RedirectWith(P.LeadId.HasValue ? "/admin/leads/" + P.LeadId : "/admin/proposals", "Proposal deleted."); return; }
            if (act == "copy" && P.Id > 0) { var c = Proposals.Duplicate(P.Id, Me.Id); RedirectWith("/admin/proposals/" + c.Id, "Copied as " + c.Number + "."); return; }

            if (!ReadForm(f)) return;
            if (act == "markpaid")
            {
                decimal amount;
                if (!decimal.TryParse(f["manualAmount"], NumberStyles.Float, CultureInfo.InvariantCulture, out amount) || amount <= 0) { Err = "Enter the amount received."; return; }
                Proposals.Save(P, Me.Id);
                Proposals.Paid(Proposals.Get(P.Id), amount, Util.Cut((f["manualRef"] ?? "").Trim(), 60), "recorded by " + Me.Name, Me.Id);
                RedirectWith("/admin/proposals/" + P.Id + "#pay", "Payment recorded.");
                return;
            }
            Proposals.Save(P, Me.Id);
            var saved = Proposals.Get(P.Id);
            if (act == "send")
            {
                string e1, e2, e3;
                var to = Proposals.ParseAddresses(f["to"], out e1); var cc = Proposals.ParseAddresses(f["ccs"], out e2); var bcc = Proposals.ParseAddresses(f["bcc"], out e3);
                SendTo = f["to"]; SendCc = f["ccs"]; SendBcc = f["bcc"]; DefaultMessage = f["message"];
                var bad = e1 ?? e2 ?? e3;
                if (bad != null) { Err = bad; return; }
                var error = Proposals.Send(saved, to, cc, bcc, f["subject"], f["message"], Me);
                if (error != null) { Err = error; return; }
                RedirectWith("/admin/proposals/" + P.Id, "Proposal sent to " + string.Join(", ", to) + ".");
                return;
            }
            if (act == "paylink")
            {
                decimal amount; decimal? custom = null;
                if (!string.IsNullOrWhiteSpace(f["payAmount"]))
                {
                    if (!decimal.TryParse(f["payAmount"], NumberStyles.Float, CultureInfo.InvariantCulture, out amount) || amount <= 0) { Err = "Enter a valid amount for the payment link."; return; }
                    custom = amount;
                }
                string error;
                var url = Razorpay.CreateLink(saved, custom, out error);
                if (url == null) { Err = error; return; }
                RedirectWith("/admin/proposals/" + P.Id + "#pay", "Payment link created. It is shown on the proposal page and in the acceptance email.");
                return;
            }
            if (act == "paycheck") { RedirectWith("/admin/proposals/" + P.Id + "#pay", Razorpay.Refresh(saved)); return; }
            RedirectWith("/admin/proposals/" + P.Id, "Proposal saved.");
        }

        private bool ReadForm(System.Collections.Specialized.NameValueCollection f)
        {
            P.Title = (f["title"] ?? "").Trim();
            if (P.Title.Length < 3) { Err = "Give the proposal a title."; return false; }
            P.ClientName = Clean(f["cn"]); P.ClientCompany = Clean(f["cc"]); P.ClientEmail = Clean(f["ce"]); P.ClientPhone = Clean(f["cp"]);
            if (!string.IsNullOrEmpty(P.ClientEmail) && !Newsletter.IsEmail(P.ClientEmail)) { Err = "The client email is not valid."; return false; }
            P.Intro = Util.Cut((f["intro"] ?? "").Trim(), 6000);
            P.Terms = Util.Cut((f["terms"] ?? "").Replace("\r", "").Trim(), 6000);
            P.Currency = Fx.Known.ContainsKey(f["cur"] ?? "") ? f["cur"] : "INR";
            P.TaxName = Util.Cut((f["taxName"] ?? "").Trim(), 40);
            decimal d;
            P.DiscountPct = decimal.TryParse(f["disc"], NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0;
            P.TaxPct = decimal.TryParse(f["taxPct"], NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0;
            P.DepositPct = decimal.TryParse(f["dep"], NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0;
            DateTime valid;
            P.ValidUntil = DateTime.TryParseExact(f["valid"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valid) ? (DateTime?)Util.FromIst(valid.AddDays(1)).AddSeconds(-1) : null;
            var portal = (f["portal"] ?? "").Trim();
            if (portal.Length > 0 && !portal.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { Err = "The portal link must start with https://"; return false; }
            P.PortalUrl = portal.Length > 0 ? portal : null;
            int lead;
            if (P.Id == 0 && int.TryParse(f["leadId"], out lead) && lead > 0 && CanSeeLead(lead)) P.LeadId = lead;
            ItemsJson = f["items"] ?? "[]";
            try
            {
                var rows = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(ItemsJson) ?? new List<Dictionary<string, object>>();
                P.Items = rows.Select(r => new ProposalItem
                {
                    Name = S(r, "name"), Description = S(r, "description"), Unit = S(r, "unit"), Billing = S(r, "billing"),
                    Qty = Dec(S(r, "qty"), 1), Price = Dec(S(r, "price"), 0)
                }).Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
            }
            catch { Err = "The lines could not be read. Reload the page and try again."; return false; }
            return true;
        }

        private static string Clean(string s) { s = (s ?? "").Trim(); return s.Length == 0 ? null : s; }
        private static string S(Dictionary<string, object> d, string k) { object v; return d.TryGetValue(k, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : null; }
        private static decimal Dec(string s, decimal fallback) { decimal v; return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback; }

        /// <summary>Plans and extras for the "Add from plans &amp; extras" menu, with INR prices and per-currency rates.</summary>
        private static string Catalog()
        {
            var sb = new StringBuilder();
            var js = new JavaScriptSerializer();
            var services = SiteContent.Current.Services;
            foreach (var g in Yenetch.Data.Pricing.Plans.GroupBy(p => p.Service))
            {
                var svc = services.FirstOrDefault(s => s.Slug == g.Key);
                var label = svc == null ? "Plans" : svc.Name;
                sb.Append("<optgroup label=\"").Append(H(label)).Append("\">");
                foreach (var p in g)
                    sb.Append("<option value=\"").Append(H(js.Serialize(new { name = (svc == null ? "" : ShortName(svc.Name) + ": ") + p.Name + " plan", description = string.Join(", ", (p.Features ?? new List<string>()).Take(5)), qty = p.Billing == "hourly" ? 20 : 1, unit = p.Billing == "hourly" ? "hour" : "", price = p.Price, billing = p.Billing == "hourly" ? "one-time" : p.Billing ?? "one-time", setup = p.SetupFee })))
                      .Append("\">").Append(H(p.Name)).Append(" · ").Append(H(Fx.Format(p.Price, "INR"))).Append(H(Yenetch.Data.Pricing.BillingLabel(p.Billing))).Append("</option>");
                sb.Append("</optgroup>");
            }
            if (Yenetch.Data.Pricing.Addons.Count > 0)
            {
                sb.Append("<optgroup label=\"Extras\">");
                foreach (var a in Yenetch.Data.Pricing.Addons)
                    sb.Append("<option value=\"").Append(H(js.Serialize(new { name = a.Name, description = a.Description, qty = Math.Max(1, a.Min), unit = a.Unit, price = a.Price, billing = a.IsMonthly ? "monthly" : "one-time", setup = 0 })))
                      .Append("\">").Append(H(a.Name)).Append(" · ").Append(H(Fx.Format(a.Price, "INR"))).Append(" per ").Append(H(a.Unit ?? "unit")).Append("</option>");
                sb.Append("</optgroup>");
            }
            // Rates for converting catalogue prices when the proposal is in another currency.
            var rates = Fx.Rates;
            sb.Append("<option disabled hidden data-rates=\"").Append(H(js.Serialize(Fx.Known.Keys.Where(rates.ContainsKey).ToDictionary(k => k, k => rates[k])))).Append("\"></option>");
            return sb.ToString();
        }

        private static string ShortName(string n) { var i = n.IndexOf(" (", StringComparison.Ordinal); return i > 0 ? n.Substring(0, i) : n; }

        protected static string Label(string kind)
        {
            switch (kind)
            {
                case "created": return "Created"; case "edited": return "Edited"; case "sent": return "Sent"; case "viewed": return "Opened by the client";
                case "accepted": return "Accepted"; case "declined": return "Declined"; case "paylink": return "Payment link"; case "paid": return "Paid";
                default: return kind;
            }
        }
    }
}
