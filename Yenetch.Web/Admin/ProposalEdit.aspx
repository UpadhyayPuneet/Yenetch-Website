<%@ Page Title="Proposal" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="ProposalEdit.aspx.cs" Inherits="Yenetch.Web.Admin.ProposalEditPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Yenetch.Crm" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><a class="crumb" href="<%= P.LeadId.HasValue ? "/admin/leads/" + P.LeadId : "/admin/proposals" %>"><%= Icon("back") %><%= P.LeadId.HasValue ? "Lead" : "Proposals" %></a>
        <h1><%: P.Id == 0 ? "New proposal" : P.Title %></h1>
        <p><% if (P.Id > 0) { %><%: P.Number %> · <%= Yenetch.Web.Admin.ProposalListPage.StatusBadgeFor(P) %><% if (P.Views > 0) { %> · opened <%= N(P.Views) %> time<%= P.Views == 1 ? "" : "s" %>, last <%: Ago(P.LastViewedOn) %><% } %><% } else { %>Fill in the lines, then save. You can send it once it is saved.<% } %></p></div>
    <div class="page-head__actions">
        <% if (P.Id > 0) { %><a class="btn btn--line" href="/proposal/<%= P.Token %>" target="_blank" rel="noopener"><%= Icon("external") %>Preview</a>
        <button class="btn btn--line" type="button" data-copy="<%: P.Url %>">Copy client link</button><% } %>
        <button class="btn btn--blue" type="submit" name="act" value="save">Save</button>
    </div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<input type="hidden" name="items" data-items-json value="<%: ItemsJson %>" />
<input type="hidden" name="leadId" value="<%= P.LeadId %>" />
<input type="hidden" name="quoteId" value="<%= P.QuoteId %>" />
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Client</h2><% if (P.LeadId.HasValue) { %><a class="small" href="/admin/leads/<%= P.LeadId %>">Lead #<%= P.LeadId %></a><% } %></div>
            <div class="form-grid">
                <div class="span-2"><label class="label" for="title">Proposal title</label><input class="field" id="title" name="title" maxlength="200" required value="<%: P.Title %>" /></div>
                <div><label class="label" for="cn">Client name</label><input class="field" id="cn" name="cn" maxlength="160" value="<%: P.ClientName %>" /></div>
                <div><label class="label" for="cc">Company</label><input class="field" id="cc" name="cc" maxlength="160" value="<%: P.ClientCompany %>" /></div>
                <div><label class="label" for="ce">Client email</label><input class="field" id="ce" name="ce" type="email" maxlength="160" value="<%: P.ClientEmail %>" /></div>
                <div><label class="label" for="cp">Client phone</label><input class="field" id="cp" name="cp" maxlength="40" value="<%: P.ClientPhone %>" /></div>
                <div class="span-2"><label class="label" for="intro">Introduction</label><textarea class="field" id="intro" name="intro" rows="4" maxlength="6000"><%: P.Intro %></textarea></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>What you will deliver</h2>
                <div class="pe-tools"><label class="sr-only" for="catalog">Add from plans and extras</label><select class="field" id="catalog" data-catalog><option value="">Add from plans &amp; extras…</option><%= CatalogOptions %></select>
                <button class="btn btn--line btn--sm" type="button" data-add-line><%= Icon("plus") %>Blank line</button></div></div>
            <div class="table-wrap"><table class="table pe-lines">
                <thead><tr><th>Item</th><th style="width:80px">Qty</th><th style="width:90px">Unit</th><th style="width:130px">Price</th><th style="width:130px">Billed</th><th style="width:110px">Amount</th><th style="width:36px"></th></tr></thead>
                <tbody data-lines></tbody>
            </table></div>
            <div class="form-grid" style="margin-top:14px">
                <div><label class="label" for="cur">Currency</label><select class="field" id="cur" name="cur" data-currency><% foreach (var c in Fx.Known.Keys) { %><option<%= P.Currency == c ? " selected" : "" %>><%= c %></option><% } %></select>
                    <p class="fld__help">Lines added from plans are converted from INR at today's rate.</p></div>
                <div><label class="label" for="disc">Discount (%)</label><input class="field" id="disc" name="disc" inputmode="decimal" maxlength="6" value="<%= P.DiscountPct.ToString("0.##") %>" data-calc /></div>
                <div><label class="label" for="taxName">Tax name</label><input class="field" id="taxName" name="taxName" maxlength="40" value="<%: P.TaxName %>" /></div>
                <div><label class="label" for="taxPct">Tax (%)</label><input class="field" id="taxPct" name="taxPct" inputmode="decimal" maxlength="6" value="<%= P.TaxPct.ToString("0.##") %>" data-calc /><p class="fld__help">Usually 0 for clients outside India.</p></div>
                <div><label class="label" for="dep">Due on acceptance (%)</label><input class="field" id="dep" name="dep" inputmode="decimal" maxlength="6" value="<%= P.DepositPct.ToString("0.##") %>" data-calc /><p class="fld__help">0 or 100 = the full first payment; 50 = half now.</p></div>
                <div><label class="label" for="valid">Valid until</label><input class="field" id="valid" name="valid" type="date" value="<%= P.ValidUntil.HasValue ? Util.Ist(P.ValidUntil.Value).ToString("yyyy-MM-dd") : "" %>" /></div>
            </div>
            <dl class="pe-totals" data-totals></dl>
        </section>
        <section class="card">
            <div class="card__head"><h2>Terms</h2></div>
            <textarea class="field" name="terms" rows="7" maxlength="6000" aria-label="Terms"><%: P.Terms %></textarea>
            <p class="fld__help">One term per line. Defaults are set in Pricing &amp; currency.</p>
            <label class="label" for="portal" style="margin-top:12px">Client portal link (optional)</label>
            <input class="field" id="portal" name="portal" maxlength="400" value="<%: P.PortalUrl %>" placeholder="<%: string.IsNullOrEmpty(DefaultPortal) ? "https://app.yenetch.com/projects/..." : "Default: " + DefaultPortal %>" />
            <p class="fld__help">Shown to the client after they accept and in their receipt, to track billing, tasks and progress.</p>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <% if (P.Id > 0) { %>
        <section class="card" id="send">
            <div class="card__head"><h2>Send by email</h2><% if (P.SentOn.HasValue) { %><span class="muted small">Sent <%: Ago(P.SentOn) %></span><% } %></div>
            <div class="rcpt-fields" data-suggest="<%: SuggestJson %>">
                <label class="label" for="to">To</label><input class="field" id="to" name="to" data-recipients autocomplete="off" value="<%: To %>" />
                <label class="label" for="ccs">CC</label><input class="field" id="ccs" name="ccs" data-recipients autocomplete="off" value="<%: Cc %>" />
                <label class="label" for="bcc">BCC</label><input class="field" id="bcc" name="bcc" data-recipients autocomplete="off" value="<%: Bcc %>" />
            </div>
            <p class="fld__help">Type a name or address; team members, the client and people you emailed before are suggested. Press Enter or comma to add.</p>
            <label class="label" for="subject">Subject</label><input class="field" id="subject" name="subject" maxlength="200" value="<%: "Proposal " + P.Number + ": " + P.Title %>" />
            <label class="label" for="message" style="margin-top:10px">Message</label><textarea class="field" id="message" name="message" rows="5" maxlength="4000"><%: DefaultMessage %></textarea>
            <div class="form-actions"><button class="btn btn--blue" type="submit" name="act" value="send"><%= Icon("send") %>Save and send</button></div>
            <p class="muted small" style="margin:8px 0 0">The client gets a button to open the proposal. Replies come to you (<%: Me.Email %>).</p>
        </section>
        <section class="card" id="pay">
            <div class="card__head"><h2>Payment</h2><%= P.IsPaid ? "<span class=\"badge badge--won\">Paid</span>" : !string.IsNullOrEmpty(P.PayLinkUrl) ? "<span class=\"badge badge--new\">Link ready</span>" : "" %></div>
            <% if (P.IsPaid) { %><p class="small" style="margin:0"><b><%: P.Money(P.AmountPaid ?? 0) %></b> received <%: When(P.PaidOn) %>.</p>
            <% } else { %>
            <p class="small" style="margin:0 0 10px">Due on acceptance: <b data-due><%: P.Money(P.DueNow) %></b><%= P.Monthly > 0 ? ", then " + H(P.Money(P.MonthlyAfter)) + " a month" : "" %>.</p>
            <% if (!string.IsNullOrEmpty(P.PayLinkUrl)) { %><p class="small" style="margin:0 0 10px"><a href="<%: P.PayLinkUrl %>" target="_blank" rel="noopener"><%: P.PayLinkUrl %></a> (<%: P.Money(P.PayAmount ?? 0) %>, <%: P.PayStatus %>)</p><% } %>
            <% if (Razorpay.Configured) { %>
            <div class="form-grid"><div><label class="label" for="payAmount">Amount for the link</label><input class="field" id="payAmount" name="payAmount" inputmode="decimal" maxlength="14" placeholder="<%= P.DueNow.ToString("0.##") %>" /></div></div>
            <div class="form-actions"><button class="btn btn--line" type="submit" name="act" value="paylink"><%= string.IsNullOrEmpty(P.PayLinkUrl) ? "Create payment link" : "Create a new link" %></button>
                <% if (!string.IsNullOrEmpty(P.PayLinkId)) { %><button class="btn btn--line" type="submit" name="act" value="paycheck">Check payment</button><% } %></div>
            <% } else { %><p class="muted small">Add your Razorpay keys in <a href="/admin/integrations">Integrations</a> to send payment links. A link is also created automatically when the client accepts.</p><% } %>
            <details style="margin-top:12px"><summary class="small">Record a payment received another way</summary>
                <div class="form-grid" style="margin-top:8px"><div><label class="label" for="manualAmount">Amount received</label><input class="field" id="manualAmount" name="manualAmount" inputmode="decimal" maxlength="14" /></div>
                <div><label class="label" for="manualRef">Reference</label><input class="field" id="manualRef" name="manualRef" maxlength="60" placeholder="UTR or cheque no." /></div></div>
                <div class="form-actions"><button class="btn btn--line" type="submit" name="act" value="markpaid" data-confirm="Mark this proposal as paid?">Mark as paid</button></div></details>
            <% } %>
        </section>
        <section class="card">
            <div class="card__head"><h2>History</h2></div>
            <ul class="timeline pe-history"><% foreach (var ev in Events) { %>
                <li><b><%: Label(ev.Str("Kind")) %></b> <span class="muted small"><%: When(ev.DateN("CreatedOn")) %><%= ev.Str("UserName") != null ? " · " + H(ev.Str("UserName")) : "" %></span>
                    <% if (!string.IsNullOrEmpty(ev.Str("Detail")) && ev.Str("Kind") != "edited" && ev.Str("Kind") != "created") { %><div class="small" style="white-space:pre-line;color:var(--ink-2)"><%: ev.Str("Detail") %></div><% } %></li>
            <% } %></ul>
            <% if (P.AcceptedOn.HasValue) { %><p class="small" style="margin:10px 0 0">Accepted by <b><%: P.AcceptedName %></b> on <%: When(P.AcceptedOn) %>.</p><% } %>
            <% if (P.DeclinedOn.HasValue) { %><p class="small" style="margin:10px 0 0">Declined <%: When(P.DeclinedOn) %><%= string.IsNullOrEmpty(P.DeclineReason) ? "" : ": " + H(P.DeclineReason) %></p><% } %>
        </section>
        <section class="card">
            <div class="form-actions" style="margin:0"><button class="btn btn--line" type="submit" name="act" value="copy">Duplicate</button>
                <button class="btn btn--line btn--danger" type="submit" name="act" value="delete" data-confirm="Delete this proposal? The client link will stop working.">Delete</button></div>
        </section>
        <% } else { %>
        <section class="card"><div class="card__head"><h2>Next</h2></div><p class="small" style="margin:0">Save the proposal to send it, preview it as the client sees it, and create a payment link.</p></section>
        <% } %>
    </div>
</div>
</form>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server"><script src="/assets/admin/proposal.js?v=11" defer></script></asp:Content>
