using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class SavedQuote
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public int? LeadId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Company { get; set; }
        public string Notes { get; set; }
        public string ItemsJson { get; set; }
        public string Currency { get; set; }
        public decimal OneTimeInr { get; set; }
        public decimal MonthlyInr { get; set; }
        public decimal DiscountInr { get; set; }
        public string OfferCode { get; set; }
        public DateTime CreatedOn { get; set; }

        internal static SavedQuote From(Row r)
        {
            return new SavedQuote
            {
                Id = r.Int("Id"), Token = r.Str("Token"), LeadId = r.IntN("LeadId"), Name = r.Str("Name"), Email = r.Str("Email"), Phone = r.Str("Phone"), Company = r.Str("Company"),
                Notes = r.Str("Notes"), ItemsJson = r.Str("ItemsJson"), Currency = r.Str("Currency"), OneTimeInr = r.DecN("OneTimeInr") ?? 0, MonthlyInr = r.DecN("MonthlyInr") ?? 0,
                DiscountInr = r.DecN("DiscountInr") ?? 0, OfferCode = r.Str("OfferCode"), CreatedOn = r.Date("CreatedOn")
            };
        }

        /// <summary>The quote's lines as saved (kind, id, name, qty, prices in the quote currency).</summary>
        public List<QuoteLine> Lines { get { try { return new JavaScriptSerializer().Deserialize<List<QuoteLine>>(ItemsJson ?? "[]") ?? new List<QuoteLine>(); } catch { return new List<QuoteLine>(); } } }
    }

    /// <summary>Quotes visitors build on /pricing. Each one is linked to a lead (a returning visitor's open lead is reused).</summary>
    public static class Quotes
    {
        public static SavedQuote Get(int id) { var r = Db.First("SELECT * FROM Quotes WHERE Id = @id", new { id }); return r == null ? null : SavedQuote.From(r); }
        public static List<SavedQuote> ForLead(int leadId) { return Db.Query("SELECT * FROM Quotes WHERE LeadId = @leadId ORDER BY CreatedOn DESC", SavedQuote.From, new { leadId }); }

        public static SavedQuote Create(QuoteResult q, string name, string email, string phone, string company, string notes, string visitorId, string ip, string page)
        {
            var summary = Pricing.Summary(q);
            var services = string.Join(", ", q.Lines.Where(l => l.ServiceName != null).Select(l => l.ServiceName).Distinct());
            var lead = new Lead
            {
                Name = name, Email = email, Phone = phone, Company = string.IsNullOrWhiteSpace(company) ? null : company, Source = "Plan builder", Priority = "Hot",
                Interest = Util.Cut(services.Length > 0 ? services : "Custom plan", 160), Need = Util.Cut((string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim() + "\n\n") + summary, 2000),
                EstValue = q.OneTimeInr + q.MonthlyInr * 12 - q.DiscountInr, VisitorId = visitorId, Page = page
            };
            var leadId = LeadService.CreateFromTool(lead, "Built a plan on the website: " + Fx.Format(q.DueNow, q.Currency) + " first payment" + (q.Monthly > 0 ? ", then " + Fx.Format(q.Monthly, q.Currency) + "/month" : "") + ".\n" + summary);
            // A returning visitor's open lead is reused: keep the bigger estimate.
            Db.Exec("UPDATE CrmLeads SET EstValue = @v WHERE Id = @id AND (EstValue IS NULL OR EstValue < @v)", new { v = lead.EstValue, id = leadId });

            var token = Util.NewId();
            var js = new JavaScriptSerializer();
            var id = Db.Insert(@"INSERT INTO Quotes (Token, LeadId, Name, Email, Phone, Company, Notes, ItemsJson, Currency, Rate, OneTimeInr, MonthlyInr, DiscountInr, OfferCode, VisitorId, Ip, CreatedOn)
                VALUES (@token, @leadId, @name, @email, @phone, @company, @notes, @items, @currency, @rate, @oneTime, @monthly, @discount, @offer, @vid, @ip, @now)",
                new Dictionary<string, object>
                {
                    { "token", token }, { "leadId", leadId }, { "name", name }, { "email", email }, { "phone", phone }, { "company", string.IsNullOrWhiteSpace(company) ? null : company },
                    { "notes", string.IsNullOrWhiteSpace(notes) ? null : notes }, { "items", js.Serialize(q.Lines) }, { "currency", q.Currency }, { "rate", q.Rate },
                    { "oneTime", q.OneTimeInr }, { "monthly", q.MonthlyInr }, { "discount", q.DiscountInr }, { "offer", q.OfferId }, { "vid", visitorId }, { "ip", ip }, { "now", DateTime.UtcNow }
                });
            var saved = Get(id);
            EmailVisitor(saved, q);
            NotifyTeam(saved, q, leadId);
            LeadScoring.Recalc(leadId);
            Webhooks.Fire("quote.created", new { quoteId = id, leadId, name, email, phone, company, currency = q.Currency, firstPayment = q.DueNow, monthly = q.Monthly, lines = q.Lines.Select(l => new { l.Kind, l.Id, l.Name, service = l.Service, l.Qty, l.Amount, l.Billing }) });
            return saved;
        }

        private static string Table(QuoteResult q)
        {
            var sb = new StringBuilder("<table role=\"presentation\" style=\"border-collapse:collapse;width:100%;font-size:14px\">");
            foreach (var l in q.Lines)
            {
                var label = (l.Kind == "plan" && l.ServiceName != null ? l.ServiceName + " · " : "") + l.Name + (l.Qty > 1 || l.Unit != null ? " × " + l.Qty + (l.Unit != null ? " " + l.Unit + (l.Qty == 1 ? "" : "s") : "") : "");
                var price = (l.IsFrom ? "from " : "") + q.Money(l.Amount) + Pricing.BillingLabel(l.Billing == "hourly" ? "" : l.Billing) + (l.Setup > 0 ? "<br><small style=\"color:#86868b\">+ " + q.Money(l.Setup) + " set-up</small>" : "");
                sb.Append("<tr><td style=\"padding:8px 12px 8px 0;border-bottom:1px solid #f0f0f2\">" + Util.H(label) + "</td><td style=\"padding:8px 0;border-bottom:1px solid #f0f0f2;text-align:right;white-space:nowrap\">" + price + "</td></tr>");
            }
            if (q.Discount > 0) sb.Append("<tr><td style=\"padding:8px 12px 8px 0;color:#0a7d33\">" + Util.H(q.OfferTitle) + "</td><td style=\"padding:8px 0;text-align:right;color:#0a7d33\">−" + q.Money(q.Discount) + "</td></tr>");
            sb.Append("<tr><td style=\"padding:12px 12px 4px 0;font-weight:700\">First payment</td><td style=\"padding:12px 0 4px;text-align:right;font-weight:700\">" + q.Money(q.DueNow) + "</td></tr>");
            if (q.Monthly > 0) sb.Append("<tr><td style=\"padding:4px 12px 4px 0;color:#48484e\">Then each month</td><td style=\"padding:4px 0;text-align:right;color:#48484e\">" + q.Money(q.Monthly) + "</td></tr>");
            return sb.Append("</table>").ToString();
        }

        private static void EmailVisitor(SavedQuote s, QuoteResult q)
        {
            try
            {
                var first = (s.Name ?? "").Split(' ')[0];
                if (!Pricing.PublicPrices)
                {
                    // Prices are not published yet: confirm the plan without figures; the team sends the prices.
                    var items = "<ul style=\"margin:0 0 16px;padding-left:20px\">" + string.Join("", q.Lines.Select(l => "<li>" + Util.H((l.Kind == "plan" && l.ServiceName != null ? l.ServiceName + " · " : "") + l.Name + (l.Unit != null ? " × " + l.Qty + " " + l.Unit + (l.Qty == 1 ? "" : "s") : "")) + "</li>")) + "</ul>";
                    var plain = Mailer.Heading("We have your plan") + "<p style=\"margin:0 0 16px;color:#48484e\">Hi " + Util.H(first) + ", thanks for building your plan. A specialist will call you within one working day and send you prices for:</p>"
                              + items + Mailer.Button("Book a call now", Mailer.SiteUrl + "/book");
                    Mailer.Send(s.Email, "We have your plan, " + first, Mailer.Wrap("We have your plan", plain, null));
                    return;
                }
                var body = Mailer.Heading("Your Yenetch estimate") + "<p style=\"margin:0 0 16px;color:#48484e\">Hi " + Util.H(first) + ", thanks for building your plan. Here is your estimate"
                         + (q.HasFrom ? " (items marked \"from\" are confirmed after a short scoping call)" : "") + ". Prices are before " + Util.H(Pricing.TaxName) + ".</p>"
                         + Table(q) + "<p style=\"margin:16px 0 0;color:#48484e\">A specialist will call you within one working day to confirm the details and send a formal proposal.</p>"
                         + Mailer.Button("Book a call now", Mailer.SiteUrl + "/book") + "<p style=\"margin:16px 0 0;font-size:13px;color:#86868b\">" + Util.H(Pricing.Note) + "</p>";
                Mailer.Send(s.Email, "Your Yenetch estimate: " + q.Money(q.DueNow) + (q.Monthly > 0 ? " + " + q.Money(q.Monthly) + "/month" : ""), Mailer.Wrap("Your estimate from Yenetch", body, null));
            }
            catch (Exception ex) { Mailer.Log("quote email " + s.Id, ex); }
        }

        private static void NotifyTeam(SavedQuote s, QuoteResult q, int leadId)
        {
            var inbox = Mailer.LeadInbox;
            if (string.IsNullOrEmpty(inbox)) return;
            try
            {
                var body = Mailer.Heading("New plan from " + Util.H(s.Name)) + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%\">"
                         + Mailer.Row("Name", s.Name) + Mailer.Row("Phone", s.Phone) + Mailer.Row("Email", s.Email) + Mailer.Row("Company", s.Company) + Mailer.Row("Notes", s.Notes)
                         + Mailer.Row("Currency", q.Currency) + "</table><h2 style=\"font-size:17px;margin:20px 0 8px\">Their plan</h2>" + Table(q)
                         + Mailer.Button("Open lead", Mailer.SiteUrl + "/admin/leads/" + leadId);
                Mailer.Send(inbox, "New plan: " + s.Name + " (" + q.Money(q.DueNow) + ")", Mailer.Wrap("New plan from " + s.Name, body, null),
                    customise: m => { m.ReplyToList.Clear(); m.ReplyToList.Add(new System.Net.Mail.MailAddress(s.Email, s.Name)); });
            }
            catch (Exception ex) { Mailer.Log("quote alert " + s.Id, ex); }
        }
    }
}
