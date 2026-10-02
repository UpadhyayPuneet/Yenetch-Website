<%@ Page Title="Integrations" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Integrations.aspx.cs" Inherits="Yenetch.Web.Admin.IntegrationsPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Integrations</h1><p>Online payments with Razorpay, the client portal of your billing and project app, and webhooks that tell that app about new leads, quotes, proposals and payments.</p></div>
    <div class="page-head__actions"><button class="btn btn--blue" type="submit" name="act" value="save">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card" id="razorpay">
            <div class="card__head"><h2>Razorpay</h2><span class="badge <%= Razorpay.Configured ? "badge--won" : "badge--open" %>"><%= Razorpay.Configured ? (Razorpay.IsTestMode ? "Test mode" : "Live") : "Not set up" %></span></div>
            <p class="muted small" style="margin:0 0 12px">Proposals get a secure payment link (UPI, cards, net banking, wallets). Clients pay the amount due now: the full total, or the deposit you set on the proposal.</p>
            <div class="form-grid">
                <div><label class="label" for="rzId">Key ID</label><input class="field mono" id="rzId" name="rzId" maxlength="60" value="<%: Razorpay.KeyId %>" placeholder="rzp_live_..." autocomplete="off" /></div>
                <div><label class="label" for="rzSecret">Key secret</label><input class="field mono" id="rzSecret" name="rzSecret" type="password" maxlength="100" autocomplete="new-password" placeholder="<%= HasRzSecret ? "Saved. Type to replace" : "" %>" /></div>
                <div class="span-2"><label class="label" for="rzHook">Webhook secret</label><input class="field mono" id="rzHook" name="rzHook" type="password" maxlength="100" autocomplete="new-password" placeholder="<%= HasRzHook ? "Saved. Type to replace" : "Any long random text you also enter in Razorpay" %>" /></div>
            </div>
            <ol class="tips" style="margin-top:12px">
                <li>Razorpay Dashboard &gt; Account &amp; Settings &gt; <b>API keys</b>: generate a key and paste the Key ID and secret above. Use <b>test</b> keys first.</li>
                <li>Razorpay Dashboard &gt; Account &amp; Settings &gt; <b>Webhooks</b> &gt; Add: URL <code><%: SiteUrl %>/api/razorpay</code>, event <b>payment_link.paid</b>, and the same webhook secret as above.</li>
                <li>Payments also show as paid when the client returns from Razorpay, so a missed webhook never loses a payment.</li>
            </ol>
            <div class="form-actions"><% if (Razorpay.Configured) { %><button class="btn btn--line" type="submit" name="act" value="rzTest">Check the keys</button><% } %></div>
        </section>
        <section class="card" id="portal">
            <div class="card__head"><h2>Client portal</h2></div>
            <p class="muted small" style="margin:0 0 12px">The app where you bill clients and both sides track tasks, process and project progress. Accepted proposals and payment receipts link to it.</p>
            <label class="label" for="portal">Portal address</label><input class="field" id="portal" name="portal" maxlength="400" value="<%: Settings_("integrations.portalUrl") %>" placeholder="https://app.yenetch.com/client?email={email}" />
            <p class="fld__help">You can use {email}, {name}, {company}, {proposal} (the number) and {lead} (the lead number) in the address. A proposal can also have its own portal link.</p>
        </section>
        <section class="card card--flush" id="log">
            <div class="card__head"><h2>Webhook log</h2><span class="muted small">Last 30</span></div>
            <% if (Log.Count == 0) { %><div class="empty"><b>Nothing sent yet</b>Calls to your app appear here.</div><% } else { %>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>When</th><th>Event</th><th>Answer</th></tr></thead>
                <tbody><% foreach (var r in Log) { var ok = r.IntN("StatusCode").HasValue && r.Int("StatusCode") < 300; %><tr>
                    <td class="nowrap small"><%: Ago(r.DateN("CreatedOn")) %></td><td><code><%: r.Str("Event") %></code></td>
                    <td><span class="badge <%= ok ? "badge--won" : "badge--lost" %>"><%= r.IntN("StatusCode").HasValue ? r.Int("StatusCode").ToString() : "No answer" %></span><span class="cell-sub"><%: Util.Cut(r.Str("Response"), 90) %></span></td>
                </tr><% } %></tbody>
            </table></div>
            <% } %>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card" id="hooks">
            <div class="card__head"><h2>Webhooks</h2><span class="badge <%= Webhooks.Enabled ? "badge--won" : "badge--open" %>"><%= Webhooks.Enabled ? "On" : "Off" %></span></div>
            <label class="label" for="hookUrl">Send events to</label><input class="field" id="hookUrl" name="hookUrl" maxlength="400" value="<%: Webhooks.Url %>" placeholder="https://app.yenetch.com/api/website-events" />
            <label class="label" for="hookSecret" style="margin-top:10px">Signing secret</label><input class="field mono" id="hookSecret" name="hookSecret" type="password" maxlength="200" autocomplete="new-password" placeholder="<%= HasHookSecret ? "Saved. Type to replace" : "Long random text shared with your app" %>" />
            <p class="fld__help">Events: lead.created, quote.created, proposal.sent, proposal.viewed, proposal.accepted, proposal.declined, payment.received. Each is a JSON POST signed with header X-Yenetch-Signature (sha256 HMAC of the body).</p>
            <div class="form-actions"><button class="btn btn--line" type="submit" name="act" value="hookTest">Save and send a test</button></div>
        </section>
        <section class="card">
            <div class="card__head"><h2>WhatsApp Business API</h2><span class="badge badge--open">Coming</span></div>
            <p class="muted small" style="margin:0">Click-to-WhatsApp works today. When you have WhatsApp Business API access, it can be connected here to send proposals and payment reminders on WhatsApp.</p>
        </section>
    </div>
</div>
</form>
</asp:Content>
