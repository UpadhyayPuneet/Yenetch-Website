using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Data;

namespace Yenetch.Crm
{
    public class ProposalItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        /// <summary>one-time, monthly or yearly.</summary>
        public string Billing { get; set; }
        public decimal Amount { get { return Math.Round(Qty * Price, 2); } }
    }

    public class Proposal
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Token { get; set; }
        public int? LeadId { get; set; }
        public int? QuoteId { get; set; }
        public string Title { get; set; }
        public string ClientName { get; set; }
        public string ClientCompany { get; set; }
        public string ClientEmail { get; set; }
        public string ClientPhone { get; set; }
        public string Intro { get; set; }
        public List<ProposalItem> Items { get; set; }
        public string Currency { get; set; }
        public string TaxName { get; set; }
        public decimal TaxPct { get; set; }
        public decimal DiscountPct { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal DepositPct { get; set; }
        public string Terms { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Status { get; set; }
        public DateTime? SentOn { get; set; }
        public DateTime? FirstViewedOn { get; set; }
        public DateTime? LastViewedOn { get; set; }
        public int Views { get; set; }
        public DateTime? AcceptedOn { get; set; }
        public string AcceptedName { get; set; }
        public DateTime? DeclinedOn { get; set; }
        public string DeclineReason { get; set; }
        public string PayLinkId { get; set; }
        public string PayLinkUrl { get; set; }
        public decimal? PayAmount { get; set; }
        public string PayStatus { get; set; }
        public DateTime? PaidOn { get; set; }
        public decimal? AmountPaid { get; set; }
        public string PortalUrl { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public decimal OneTime { get { return (Items ?? new List<ProposalItem>()).Where(i => i.Billing != "monthly" && i.Billing != "yearly").Sum(i => i.Amount); } }
        public decimal Monthly { get { return (Items ?? new List<ProposalItem>()).Where(i => i.Billing == "monthly").Sum(i => i.Amount); } }
        public decimal Yearly { get { return (Items ?? new List<ProposalItem>()).Where(i => i.Billing == "yearly").Sum(i => i.Amount); } }
        /// <summary>Each later month after discount and tax.</summary>
        public decimal MonthlyAfter { get { return Math.Round(Monthly * (1 - DiscountPct / 100m) * (1 + TaxPct / 100m), 2); } }
        public decimal DueNow { get { return DepositPct > 0 && DepositPct < 100 ? Math.Round(Total * DepositPct / 100m, 2) : Total; } }
        public bool IsExpired { get { return ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow && AcceptedOn == null && PaidOn == null; } }
        public bool IsOpen { get { return !AcceptedOn.HasValue && !DeclinedOn.HasValue && !IsExpired; } }
        public bool IsPaid { get { return PayStatus == "paid"; } }
        public string Url { get { return Mailer.SiteUrl + "/proposal/" + Token; } }
        public string Money(decimal v) { return Fx.Format(v, Currency); }

        internal static Proposal From(Row r)
        {
            List<ProposalItem> items;
            try { items = new JavaScriptSerializer().Deserialize<List<ProposalItem>>(r.Str("ItemsJson") ?? "[]") ?? new List<ProposalItem>(); } catch { items = new List<ProposalItem>(); }
            return new Proposal
            {
                Id = r.Int("Id"), Number = r.Str("Number"), Token = r.Str("Token"), LeadId = r.IntN("LeadId"), QuoteId = r.IntN("QuoteId"), Title = r.Str("Title"),
                ClientName = r.Str("ClientName"), ClientCompany = r.Str("ClientCompany"), ClientEmail = r.Str("ClientEmail"), ClientPhone = r.Str("ClientPhone"),
                Intro = r.Str("Intro"), Items = items, Currency = r.Str("Currency") ?? "INR", TaxName = r.Str("TaxName"), TaxPct = r.DecN("TaxPct") ?? 0,
                DiscountPct = r.DecN("DiscountPct") ?? 0, Subtotal = r.DecN("Subtotal") ?? 0, Discount = r.DecN("Discount") ?? 0, Tax = r.DecN("Tax") ?? 0, Total = r.DecN("Total") ?? 0,
                DepositPct = r.DecN("DepositPct") ?? 0, Terms = r.Str("Terms"), ValidUntil = r.DateN("ValidUntil"), Status = r.Str("Status"), SentOn = r.DateN("SentOn"),
                FirstViewedOn = r.DateN("FirstViewedOn"), LastViewedOn = r.DateN("LastViewedOn"), Views = r.Int("Views"), AcceptedOn = r.DateN("AcceptedOn"),
                AcceptedName = r.Str("AcceptedName"), DeclinedOn = r.DateN("DeclinedOn"), DeclineReason = r.Str("DeclineReason"), PayLinkId = r.Str("PayLinkId"),
                PayLinkUrl = r.Str("PayLinkUrl"), PayAmount = r.DecN("PayAmount"), PayStatus = r.Str("PayStatus"), PaidOn = r.DateN("PaidOn"), AmountPaid = r.DecN("AmountPaid"),
                PortalUrl = r.Str("PortalUrl"), CreatedBy = r.IntN("CreatedBy"), CreatedByName = r.Str("CreatedByName"), CreatedOn = r.Date("CreatedOn"), UpdatedOn = r.Date("UpdatedOn")
            };
        }
    }

    /// <summary>
    /// Proposals: built in Admin &gt; Proposals (often straight from a lead or a quote from the plan builder), shared as a
    /// private link (/proposal/{token}), emailed with To, CC and BCC, tracked when the client opens them, accepted online
    /// with a typed signature, and paid through a Razorpay payment link. Every step is logged on the proposal and the lead.
    /// </summary>
    public static class Proposals
    {
        private const string Select = "SELECT p.*, u.Name AS CreatedByName FROM Proposals p LEFT JOIN CrmUsers u ON u.Id = p.CreatedBy";

        public static readonly string[] Statuses = { "Draft", "Sent", "Viewed", "Accepted", "Declined", "Paid", "Expired" };

        public static Proposal Get(int id) { var r = Db.First(Select + " WHERE p.Id = @id", new { id }); return r == null ? null : Proposal.From(r); }

        public static Proposal ByToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 32 || !token.All(Uri.IsHexDigit)) return null;
            var r = Db.First(Select + " WHERE p.Token = @token", new { token });
            return r == null ? null : Proposal.From(r);
        }

        public static List<Proposal> List(string status, int? leadId, int? restrictToUser, int offset, int count, out int total)
        {
            var w = new List<string>(); var a = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(status)) { w.Add("p.Status = @status"); a["status"] = status; }
            if (leadId.HasValue) { w.Add("p.LeadId = @lead"); a["lead"] = leadId.Value; }
            if (restrictToUser.HasValue) { w.Add("(p.CreatedBy = @me OR p.LeadId IN (SELECT Id FROM CrmLeads WHERE AssignedTo = @me))"); a["me"] = restrictToUser.Value; }
            var where = w.Count == 0 ? "" : " WHERE " + string.Join(" AND ", w);
            total = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals p" + where, a);
            return Db.Query(Select + where + " ORDER BY p.UpdatedOn DESC, p.Id DESC" + Db.Page(offset, count), Proposal.From, a);
        }

        public static List<Row> Events(int proposalId)
        {
            return Db.Rows("SELECT e.*, u.Name AS UserName FROM ProposalEvents e LEFT JOIN CrmUsers u ON u.Id = e.UserId WHERE e.ProposalId = @id ORDER BY e.CreatedOn DESC, e.Id DESC", new { id = proposalId });
        }

        public static void Event(int proposalId, string kind, string detail, int? userId = null, string ip = null)
        {
            Db.Exec("INSERT INTO ProposalEvents (ProposalId, Kind, Detail, UserId, Ip, CreatedOn) VALUES (@id, @kind, @detail, @userId, @ip, @now)",
                new { id = proposalId, kind, detail = Util.Cut(detail, 2000), userId, ip, now = DateTime.UtcNow });
        }

        // ---- Defaults -----------------------------------------------------------------------------------------

        public static string DefaultTerms
        {
            get
            {
                try { var v = Settings.Get("proposal.terms"); if (!string.IsNullOrWhiteSpace(v)) return v; } catch { }
                return "Prices are in the currency shown and exclude taxes unless a tax line is shown.\n50% of one-time fees is due on acceptance and the balance on delivery, unless agreed otherwise.\nMonthly fees are billed in advance at the start of each month; cancel any time after the minimum term with 30 days' notice.\nThird-party costs such as ad spend, domains, hosting and paid tools are billed at actuals.\nThe scope is as described above; changes are estimated and agreed in writing before work starts.";
            }
        }

        public static int DefaultValidDays { get { int d; try { return int.TryParse(Settings.Get("proposal.validDays"), out d) && d > 0 ? d : 15; } catch { return 15; } } }

        /// <summary>The client portal address for a proposal: its own link, else the template in Admin &gt; Integrations
        /// with {email}, {name}, {company}, {proposal} and {lead} filled in.</summary>
        public static string PortalFor(Proposal p)
        {
            if (!string.IsNullOrWhiteSpace(p.PortalUrl)) return p.PortalUrl;
            string tpl;
            try { tpl = Settings.Get("integrations.portalUrl"); } catch { tpl = null; }
            if (string.IsNullOrWhiteSpace(tpl)) return null;
            return tpl.Replace("{email}", U(p.ClientEmail)).Replace("{name}", U(p.ClientName)).Replace("{company}", U(p.ClientCompany))
                      .Replace("{proposal}", U(p.Number)).Replace("{lead}", p.LeadId.HasValue ? p.LeadId.Value.ToString(CultureInfo.InvariantCulture) : "");
        }

        private static string U(string s) { return HttpUtility.UrlEncode(s ?? ""); }

        // ---- Create and save ----------------------------------------------------------------------------------

        public static Proposal NewDraft(int? leadId, int? quoteId)
        {
            var p = new Proposal { Status = "Draft", Currency = "INR", TaxName = Pricing.TaxName, TaxPct = Pricing.TaxPct, Terms = DefaultTerms, ValidUntil = Util.FromIst(Util.TodayIst.AddDays(DefaultValidDays + 1)).AddSeconds(-1), Items = new List<ProposalItem>(), DepositPct = 0 };
            var lead = leadId.HasValue ? LeadService.Get(leadId.Value) : null;
            if (lead != null)
            {
                p.LeadId = lead.Id; p.ClientName = lead.Name; p.ClientEmail = lead.Email; p.ClientPhone = lead.Phone; p.ClientCompany = lead.Company;
                p.Title = (string.IsNullOrEmpty(lead.Interest) ? "Proposal" : lead.Interest) + " for " + (string.IsNullOrEmpty(lead.Company) ? lead.Name : lead.Company);
            }
            var quote = quoteId.HasValue ? Quotes.Get(quoteId.Value) : null;
            if (quote != null)
            {
                p.QuoteId = quote.Id; p.LeadId = p.LeadId ?? quote.LeadId; p.Currency = quote.Currency;
                p.ClientName = p.ClientName ?? quote.Name; p.ClientEmail = p.ClientEmail ?? quote.Email; p.ClientPhone = p.ClientPhone ?? quote.Phone; p.ClientCompany = p.ClientCompany ?? quote.Company;
                foreach (var l in quote.Lines)
                {
                    p.Items.Add(new ProposalItem { Name = (l.Kind == "plan" && l.ServiceName != null ? l.ServiceName + ": " : "") + l.Name, Qty = l.Qty, Unit = l.Unit, Price = l.UnitPrice, Billing = l.Billing == "hourly" ? "one-time" : l.Billing });
                    if (l.Setup > 0) p.Items.Add(new ProposalItem { Name = (l.ServiceName ?? l.Name) + ": one-time set-up", Qty = 1, Price = l.Setup, Billing = "one-time" });
                }
                if (quote.DiscountInr > 0 && quote.OneTimeInr + quote.MonthlyInr > 0) p.DiscountPct = Math.Round(quote.DiscountInr / (quote.OneTimeInr + quote.MonthlyInr) * 100m, 1);
            }
            p.Title = p.Title ?? "Proposal";
            p.Intro = "Thank you for the opportunity. This proposal sets out what we will deliver, the timeline and the investment. Questions? Reply to our email or call us any time.";
            return p;
        }

        /// <summary>Recalculates the totals from the lines, discount and tax.</summary>
        public static void Totals(Proposal p)
        {
            p.Items = (p.Items ?? new List<ProposalItem>()).Where(i => i != null && !string.IsNullOrWhiteSpace(i.Name)).Take(60).ToList();
            foreach (var i in p.Items)
            {
                i.Qty = Math.Max(0, Math.Min(100000, i.Qty)); i.Price = Math.Max(0, Math.Min(1000000000m, i.Price));
                i.Billing = i.Billing == "monthly" || i.Billing == "yearly" ? i.Billing : "one-time";
                i.Name = Util.Cut(i.Name.Trim(), 200); i.Description = Util.Cut(i.Description, 1000); i.Unit = Util.Cut(i.Unit, 30);
            }
            p.DiscountPct = Math.Max(0, Math.Min(100, p.DiscountPct));
            p.TaxPct = Math.Max(0, Math.Min(50, p.TaxPct));
            p.DepositPct = Math.Max(0, Math.Min(100, p.DepositPct));
            var round = p.Currency == "INR" ? 0 : 2;
            p.Subtotal = p.OneTime + p.Monthly + p.Yearly;
            p.Discount = Math.Round(p.Subtotal * p.DiscountPct / 100m, round);
            p.Tax = Math.Round((p.Subtotal - p.Discount) * p.TaxPct / 100m, round);
            p.Total = p.Subtotal - p.Discount + p.Tax;
        }

        public static int Save(Proposal p, int userId)
        {
            Totals(p);
            var now = DateTime.UtcNow;
            var args = new Dictionary<string, object>
            {
                { "lead", p.LeadId }, { "quote", p.QuoteId }, { "title", Util.Cut(p.Title, 200) }, { "cn", Util.Cut(p.ClientName, 160) }, { "cc", Util.Cut(p.ClientCompany, 160) },
                { "ce", Util.Cut(p.ClientEmail, 160) }, { "cp", Util.Cut(p.ClientPhone, 40) }, { "intro", p.Intro }, { "items", new JavaScriptSerializer().Serialize(p.Items) },
                { "cur", p.Currency }, { "taxName", Util.Cut(p.TaxName, 40) }, { "taxPct", p.TaxPct }, { "discPct", p.DiscountPct }, { "sub", p.Subtotal }, { "disc", p.Discount },
                { "tax", p.Tax }, { "total", p.Total }, { "dep", p.DepositPct }, { "terms", p.Terms }, { "valid", p.ValidUntil }, { "portal", Util.Cut(p.PortalUrl, 400) }, { "now", now }, { "me", userId }
            };
            if (p.Id == 0)
            {
                p.Token = Util.NewId();
                p.Number = NextNumber();
                args["token"] = p.Token; args["number"] = p.Number;
                p.Id = Db.Insert(@"INSERT INTO Proposals (Number, Token, LeadId, QuoteId, Title, ClientName, ClientCompany, ClientEmail, ClientPhone, Intro, ItemsJson, Currency, TaxName, TaxPct,
                    DiscountPct, Subtotal, Discount, Tax, Total, DepositPct, Terms, ValidUntil, Status, Views, PortalUrl, CreatedBy, CreatedOn, UpdatedOn)
                    VALUES (@number, @token, @lead, @quote, @title, @cn, @cc, @ce, @cp, @intro, @items, @cur, @taxName, @taxPct, @discPct, @sub, @disc, @tax, @total, @dep, @terms, @valid,
                    'Draft', 0, @portal, @me, @now, @now)", args);
                Event(p.Id, "created", "Created", userId);
                if (p.LeadId.HasValue) LeadService.AddActivity(p.LeadId.Value, userId, "System", "Proposal " + p.Number + " created.", null);
            }
            else
            {
                args["id"] = p.Id;
                Db.Exec(@"UPDATE Proposals SET LeadId = @lead, Title = @title, ClientName = @cn, ClientCompany = @cc, ClientEmail = @ce, ClientPhone = @cp, Intro = @intro, ItemsJson = @items,
                    Currency = @cur, TaxName = @taxName, TaxPct = @taxPct, DiscountPct = @discPct, Subtotal = @sub, Discount = @disc, Tax = @tax, Total = @total, DepositPct = @dep,
                    Terms = @terms, ValidUntil = @valid, PortalUrl = @portal, UpdatedOn = @now WHERE Id = @id", args);
                Event(p.Id, "edited", "Edited", userId);
            }
            return p.Id;
        }

        private static string NextNumber()
        {
            var year = Util.TodayIst.Year.ToString(CultureInfo.InvariantCulture);
            var n = Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE Number LIKE @p", new { p = "YN-" + year + "-%" }) + 1;
            string number;
            do { number = "YN-" + year + "-" + n.ToString("0000", CultureInfo.InvariantCulture); n++; }
            while (Db.Scalar<int>("SELECT COUNT(*) FROM Proposals WHERE Number = @n", new { n = number }) > 0);
            return number;
        }

        public static void Delete(int id) { Db.Exec("DELETE FROM ProposalEvents WHERE ProposalId = @id; DELETE FROM Proposals WHERE Id = @id", new { id }); }

        public static Proposal Duplicate(int id, int userId)
        {
            var p = Get(id);
            if (p == null) return null;
            p.Id = 0; p.Title = p.Title + " (copy)"; p.ValidUntil = Util.FromIst(Util.TodayIst.AddDays(DefaultValidDays + 1)).AddSeconds(-1); p.PortalUrl = null;
            Save(p, userId);
            return Get(p.Id);
        }

        // ---- Recipients ---------------------------------------------------------------------------------------

        /// <summary>Addresses offered as you type in To, CC and BCC: the client, the team, and people emailed before.</summary>
        public static List<KeyValuePair<string, string>> Suggestions(Proposal p)
        {
            var list = new List<KeyValuePair<string, string>>();
            Action<string, string> add = (email, label) =>
            {
                if (Newsletter.IsEmail(email) && !list.Any(x => x.Key.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase))) list.Add(new KeyValuePair<string, string>(email.Trim(), label));
            };
            if (p != null) add(p.ClientEmail, (p.ClientName ?? "Client") + " (client)");
            if (p != null && p.LeadId.HasValue)
            {
                var lead = LeadService.Get(p.LeadId.Value);
                if (lead != null) add(lead.Email, lead.Name + " (lead)");
            }
            foreach (var u in Db.Rows("SELECT Name, Email, Role FROM CrmUsers WHERE IsActive = 1 ORDER BY Name")) add(u.Str("Email"), u.Str("Name") + " (" + u.Str("Role") + ")");
            foreach (var r in Db.Rows("SELECT Detail FROM ProposalEvents WHERE Kind = 'sent' ORDER BY Id DESC" + Db.Page(0, 200)))
                foreach (var m in System.Text.RegularExpressions.Regex.Matches(r.Str("Detail") ?? "", @"[^\s,;<>()]+@[^\s,;<>()]+\.[a-z]{2,}", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    add(m.ToString(), m.ToString());
            return list;
        }

        public static List<string> ParseAddresses(string s, out string error)
        {
            error = null;
            var list = (s ?? "").Split(new[] { ',', ';', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var bad = list.FirstOrDefault(x => !Newsletter.IsEmail(x));
            if (bad != null) error = "\"" + bad + "\" is not a valid email address.";
            return list;
        }

        // ---- Sending ------------------------------------------------------------------------------------------

        /// <summary>Emails the proposal link. Returns an error message, or null when sent.</summary>
        public static string Send(Proposal p, List<string> to, List<string> cc, List<string> bcc, string subject, string message, CrmUser by)
        {
            if (to.Count == 0) return "Add at least one address in To.";
            if (to.Count + cc.Count + bcc.Count > 25) return "Send to 25 addresses at most.";
            if (p.Items.Count == 0) return "Add at least one line before sending.";
            subject = string.IsNullOrWhiteSpace(subject) ? "Proposal " + p.Number + ": " + p.Title : Util.Cut(subject.Trim(), 200);
            var body = Mailer.Heading(Util.H(p.Title))
                     + (string.IsNullOrWhiteSpace(message) ? "" : "<div style=\"margin:0 0 16px;white-space:pre-line\">" + Util.H(message.Trim()) + "</div>")
                     + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%\">" + Mailer.Row("Proposal", p.Number) + Mailer.Row("For", string.IsNullOrEmpty(p.ClientCompany) ? p.ClientName : p.ClientName + ", " + p.ClientCompany)
                     + Mailer.Row("Total", p.Money(p.Total) + (p.Monthly > 0 ? " first payment, then " + p.Money(p.MonthlyAfter) + "/month" : "")) + Mailer.Row("Valid until", Util.Date(p.ValidUntil)) + "</table>"
                     + Mailer.Button("View proposal", p.Url)
                     + "<p style=\"margin:16px 0 0;font-size:13px;color:#86868b\">You can read, accept" + (Razorpay.Configured ? " and pay" : "") + " online. Questions? Just reply to this email.</p>";
            try
            {
                Mailer.Send(string.Join(",", to), subject, Mailer.Wrap(p.Title, body, null), customise: m =>
                {
                    foreach (var a in cc) m.CC.Add(a);
                    foreach (var a in bcc) m.Bcc.Add(a);
                    // Replies go to the person who sent it.
                    if (by != null && Newsletter.IsEmail(by.Email)) { m.ReplyToList.Clear(); m.ReplyToList.Add(new MailAddress(by.Email, by.Name)); }
                });
            }
            catch (Exception ex) { Mailer.Log("proposal " + p.Id, ex); return "The email could not be sent: " + ex.Message; }

            var detail = "To: " + string.Join(", ", to) + (cc.Count > 0 ? "\nCC: " + string.Join(", ", cc) : "") + (bcc.Count > 0 ? "\nBCC: " + string.Join(", ", bcc) : "");
            Db.Exec("UPDATE Proposals SET Status = CASE WHEN Status IN ('Draft', 'Expired') THEN 'Sent' ELSE Status END, SentOn = COALESCE(SentOn, @now), UpdatedOn = @now WHERE Id = @id", new { id = p.Id, now = DateTime.UtcNow });
            Event(p.Id, "sent", detail, by == null ? (int?)null : by.Id);
            if (p.LeadId.HasValue)
            {
                LeadService.AddActivity(p.LeadId.Value, by == null ? (int?)null : by.Id, "Email", "Proposal " + p.Number + " sent (" + p.Money(p.Total) + ").\n" + detail, null);
                Db.Exec("UPDATE CrmLeads SET Status = 'Proposal', UpdatedOn = @now WHERE Id = @id AND Status IN ('New','Contacted','Qualified')", new { id = p.LeadId.Value, now = DateTime.UtcNow });
                if (!Db.Scalar<decimal?>("SELECT EstValue FROM CrmLeads WHERE Id = @id", new { id = p.LeadId.Value }).HasValue && p.Currency == "INR")
                    Db.Exec("UPDATE CrmLeads SET EstValue = @v WHERE Id = @id", new { v = p.OneTime + p.Monthly * 12 + p.Yearly, id = p.LeadId.Value });
                LeadScoring.Recalc(p.LeadId.Value);
            }
            Webhooks.Fire("proposal.sent", Hook(p));
            return null;
        }

        // ---- Client actions -----------------------------------------------------------------------------------

        /// <summary>Counts a client view (not views by signed-in staff). The first view alerts the owner.</summary>
        public static void Viewed(Proposal p, string ip)
        {
            var first = !p.FirstViewedOn.HasValue;
            Db.Exec(@"UPDATE Proposals SET Views = Views + 1, LastViewedOn = @now, FirstViewedOn = COALESCE(FirstViewedOn, @now),
                      Status = CASE WHEN Status = 'Sent' THEN 'Viewed' ELSE Status END WHERE Id = @id", new { id = p.Id, now = DateTime.UtcNow });
            // Log the first view and then at most one view an hour, so refreshing does not flood the history.
            var recent = Db.Scalar<int>("SELECT COUNT(*) FROM ProposalEvents WHERE ProposalId = @id AND Kind = 'viewed' AND CreatedOn > @since", new { id = p.Id, since = DateTime.UtcNow.AddHours(-1) });
            if (recent == 0) Event(p.Id, "viewed", first ? "Opened for the first time" : "Opened again", null, ip);
            if (!first) return;
            if (p.LeadId.HasValue) { LeadService.AddActivity(p.LeadId.Value, null, "System", "The client opened proposal " + p.Number + ".", null); LeadScoring.Recalc(p.LeadId.Value); }
            NotifyOwner(p, "opened", "Your proposal " + p.Number + " was just opened by " + (p.ClientName ?? "the client") + ". A quick call now often helps.");
            Webhooks.Fire("proposal.viewed", Hook(p));
        }

        public static string Accept(Proposal p, string name, string ip)
        {
            if (!p.IsOpen) return p.IsExpired ? "This proposal has expired. Please ask us for an updated one." : "This proposal has already been " + (p.AcceptedOn.HasValue ? "accepted." : "declined.");
            name = Util.Cut((name ?? "").Trim(), 160);
            if (name.Length < 2) return "Type your full name to accept.";
            Db.Exec("UPDATE Proposals SET Status = 'Accepted', AcceptedOn = @now, AcceptedName = @name, AcceptedIp = @ip, UpdatedOn = @now WHERE Id = @id AND AcceptedOn IS NULL",
                new { id = p.Id, now = DateTime.UtcNow, name, ip });
            Event(p.Id, "accepted", "Accepted by " + name, null, ip);
            if (p.LeadId.HasValue)
            {
                LeadService.AddActivity(p.LeadId.Value, null, "System", "Proposal " + p.Number + " accepted online by " + name + ".", null);
                if (p.CreatedBy.HasValue) LeadService.SetStatus(p.LeadId.Value, "Won", p.CreatedBy.Value);
                LeadScoring.Recalc(p.LeadId.Value);
            }
            var fresh = Get(p.Id);
            // Create the payment link now if Razorpay is set up and there is none yet, so the client can pay straight away.
            if (Razorpay.Configured && string.IsNullOrEmpty(fresh.PayLinkUrl) && fresh.DueNow > 0)
            {
                string err; Razorpay.CreateLink(fresh, null, out err);
                fresh = Get(p.Id);
            }
            NotifyOwner(fresh, "accepted", (fresh.ClientName ?? "The client") + " accepted proposal " + fresh.Number + " (" + fresh.Money(fresh.Total) + ").");
            ConfirmToClient(fresh);
            Webhooks.Fire("proposal.accepted", Hook(fresh));
            return null;
        }

        public static string Decline(Proposal p, string reason, string ip)
        {
            if (!p.IsOpen) return "This proposal can no longer be declined.";
            reason = Util.Cut((reason ?? "").Trim(), 1000);
            Db.Exec("UPDATE Proposals SET Status = 'Declined', DeclinedOn = @now, DeclineReason = @reason, UpdatedOn = @now WHERE Id = @id", new { id = p.Id, now = DateTime.UtcNow, reason });
            Event(p.Id, "declined", string.IsNullOrEmpty(reason) ? "Declined" : "Declined: " + reason, null, ip);
            if (p.LeadId.HasValue) { LeadService.AddActivity(p.LeadId.Value, null, "System", "Proposal " + p.Number + " declined" + (string.IsNullOrEmpty(reason) ? "." : ": " + reason), null); LeadScoring.Recalc(p.LeadId.Value); }
            NotifyOwner(p, "declined", (p.ClientName ?? "The client") + " declined proposal " + p.Number + (string.IsNullOrEmpty(reason) ? "." : ". Reason: " + reason));
            Webhooks.Fire("proposal.declined", Hook(p));
            return null;
        }

        /// <summary>Marks a payment received (from the Razorpay webhook, the return from checkout, or by hand).</summary>
        public static void Paid(Proposal p, decimal amount, string paymentId, string how, int? userId = null)
        {
            if (p.IsPaid) return;
            Db.Exec("UPDATE Proposals SET PayStatus = 'paid', PaidOn = @now, AmountPaid = @amount, PaymentId = @pid, Status = 'Paid', UpdatedOn = @now WHERE Id = @id AND (PayStatus IS NULL OR PayStatus <> 'paid')",
                new { id = p.Id, now = DateTime.UtcNow, amount, pid = Util.Cut(paymentId, 60) });
            Event(p.Id, "paid", "Payment received: " + p.Money(amount) + (string.IsNullOrEmpty(paymentId) ? "" : " (" + paymentId + ")") + " · " + how, userId);
            if (p.LeadId.HasValue)
            {
                LeadService.AddActivity(p.LeadId.Value, userId, "System", "Payment received for proposal " + p.Number + ": " + p.Money(amount) + ".", null);
                var owner = p.CreatedBy ?? userId;
                if (owner.HasValue) LeadService.SetStatus(p.LeadId.Value, "Won", owner.Value);
            }
            var fresh = Get(p.Id);
            NotifyOwner(fresh, "paid", "Payment of " + fresh.Money(amount) + " received for proposal " + fresh.Number + " from " + (fresh.ClientName ?? "the client") + ".");
            Receipt(fresh, amount, paymentId);
            Webhooks.Fire("payment.received", new { proposal = Hook(fresh), amount, currency = fresh.Currency, paymentId });
        }

        /// <summary>Open proposals past their date are marked Expired (daily).</summary>
        public static void ExpireOld()
        {
            Db.Exec("UPDATE Proposals SET Status = 'Expired' WHERE ValidUntil < @now AND AcceptedOn IS NULL AND DeclinedOn IS NULL AND (PayStatus IS NULL OR PayStatus <> 'paid') AND Status IN ('Draft','Sent','Viewed')", new { now = DateTime.UtcNow });
        }

        public static object Hook(Proposal p)
        {
            return new
            {
                id = p.Id, number = p.Number, title = p.Title, leadId = p.LeadId, status = p.Status, url = p.Url, currency = p.Currency,
                client = new { name = p.ClientName, company = p.ClientCompany, email = p.ClientEmail, phone = p.ClientPhone },
                items = p.Items.Select(i => new { i.Name, i.Description, i.Qty, i.Unit, i.Price, i.Billing, i.Amount }),
                subtotal = p.Subtotal, discount = p.Discount, tax = p.Tax, taxName = p.TaxName, total = p.Total, monthly = p.MonthlyAfter, depositPct = p.DepositPct,
                acceptedOn = p.AcceptedOn, acceptedName = p.AcceptedName, paidOn = p.PaidOn, amountPaid = p.AmountPaid, paymentLink = p.PayLinkUrl
            };
        }

        // ---- Emails -------------------------------------------------------------------------------------------

        private static void NotifyOwner(Proposal p, string what, string text)
        {
            try
            {
                var to = new List<string>();
                if (p.CreatedBy.HasValue) to.Add(Db.Scalar<string>("SELECT Email FROM CrmUsers WHERE Id = @id AND IsActive = 1", new { id = p.CreatedBy.Value }));
                if (p.LeadId.HasValue) to.Add(Db.Scalar<string>("SELECT u.Email FROM CrmLeads l JOIN CrmUsers u ON u.Id = l.AssignedTo WHERE l.Id = @id AND u.IsActive = 1", new { id = p.LeadId.Value }));
                if (what != "opened") to.Add(Mailer.LeadInbox);
                var list = to.Where(Newsletter.IsEmail).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (list.Count == 0) return;
                var body = Mailer.Heading("Proposal " + Util.H(what)) + "<p style=\"margin:0 0 16px;color:#48484e\">" + Util.H(text) + "</p>" + Mailer.Button("Open proposal", Mailer.SiteUrl + "/admin/proposals/" + p.Id);
                Mailer.Send(string.Join(",", list), "Proposal " + p.Number + " " + what + ": " + (p.ClientName ?? p.Title), Mailer.Wrap(text, body, null));
            }
            catch (Exception ex) { Mailer.Log("proposal notify " + p.Id, ex); }
        }

        private static void ConfirmToClient(Proposal p)
        {
            if (!Newsletter.IsEmail(p.ClientEmail)) return;
            try
            {
                var portal = PortalFor(p);
                var body = Mailer.Heading("Thank you, " + Util.H((p.AcceptedName ?? p.ClientName ?? "").Split(' ')[0]) + ".")
                         + "<p style=\"margin:0 0 16px;color:#48484e\">We have your acceptance of proposal " + Util.H(p.Number) + " (" + Util.H(p.Title) + "). Your project lead will be in touch within one working day to plan the kick-off.</p>"
                         + (!string.IsNullOrEmpty(p.PayLinkUrl) && !p.IsPaid ? "<p style=\"margin:0 0 8px\">Amount due now: <b>" + p.Money(p.PayAmount ?? p.DueNow) + "</b></p>" + Mailer.Button("Pay securely", p.PayLinkUrl) : "")
                         + (portal != null ? "<p style=\"margin:24px 0 0;color:#48484e\">Track billing, tasks and project progress in your client portal:</p>" + Mailer.Button("Open client portal", portal) : "")
                         + "<p style=\"margin:16px 0 0;font-size:13px\"><a href=\"" + HttpUtility.HtmlAttributeEncode(p.Url) + "\" style=\"color:#0066FF\">View the accepted proposal</a></p>";
                Mailer.Send(p.ClientEmail, "Accepted: " + p.Title + " (" + p.Number + ")", Mailer.Wrap("Thank you for accepting", body, null));
            }
            catch (Exception ex) { Mailer.Log("proposal confirm " + p.Id, ex); }
        }

        private static void Receipt(Proposal p, decimal amount, string paymentId)
        {
            if (!Newsletter.IsEmail(p.ClientEmail)) return;
            try
            {
                var portal = PortalFor(p);
                var body = Mailer.Heading("Payment received")
                         + "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%\">" + Mailer.Row("Amount", p.Money(amount)) + Mailer.Row("For", p.Number + " · " + p.Title)
                         + Mailer.Row("Payment ID", paymentId) + Mailer.Row("Date", Util.When(DateTime.UtcNow)) + "</table>"
                         + "<p style=\"margin:16px 0 0;color:#48484e\">Thank you. A tax invoice will follow from our accounts team.</p>"
                         + (portal != null ? Mailer.Button("Open client portal", portal) : "");
                Mailer.Send(p.ClientEmail, "Payment received: " + p.Money(amount) + " (" + p.Number + ")", Mailer.Wrap("Payment received", body, null));
            }
            catch (Exception ex) { Mailer.Log("proposal receipt " + p.Id, ex); }
        }
    }
}
