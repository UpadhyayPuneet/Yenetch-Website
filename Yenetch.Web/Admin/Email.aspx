<%@ Page Title="Email settings" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Email.aspx.cs" Inherits="Yenetch.Web.Admin.EmailPage" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Email settings</h1><p>How the website sends email: lead alerts, the thank-you reply to people who enquire, assignment alerts and newsletters.</p></div>
    <div class="page-head__actions"><asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn btn--blue" OnClick="SaveButton_Click" /></div>
</div>
<asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
<asp:PlaceHolder ID="OkBox" runat="server" Visible="false"><div class="form-ok" role="status"><asp:Literal ID="OkText" runat="server" /></div></asp:PlaceHolder>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Mail server</h2><span class="badge <%= Configured ? "badge--won" : "badge--hot" %>"><%= Configured ? "Set up" : "Not set up" %></span></div>
            <div class="form-grid">
                <div class="span-2"><label class="label" for="Host">Outgoing mail server (SMTP)</label><asp:TextBox ID="Host" runat="server" CssClass="field" MaxLength="200" placeholder="mail.yenetch.com" /></div>
                <div><label class="label" for="Port">Port</label><asp:TextBox ID="Port" runat="server" CssClass="field" MaxLength="5" inputmode="numeric" placeholder="587" /></div>
                <div><label class="label" for="Ssl">Security</label>
                    <asp:DropDownList ID="Ssl" runat="server" CssClass="field"><asp:ListItem Value="1">SSL/TLS (STARTTLS)</asp:ListItem><asp:ListItem Value="0">None</asp:ListItem></asp:DropDownList></div>
                <div><label class="label" for="SmtpUser">Username</label><asp:TextBox ID="SmtpUser" runat="server" CssClass="field" MaxLength="200" placeholder="hello@yenetch.com" autocomplete="off" /></div>
                <div><label class="label" for="Password">Password</label><asp:TextBox ID="Password" runat="server" CssClass="field" TextMode="Password" MaxLength="200" autocomplete="new-password" />
                    <p class="muted small" style="margin-top:6px"><%= HasPassword ? "Saved. Leave empty to keep it." : "Stored encrypted." %></p></div>
            </div>
            <p class="card__note">For email hosted with GenX Web Hosting, use the details from your hosting control panel (usually mail.yenetch.com, port 587, SSL on, and the full email address as username). Port 465 is not supported by .NET; use 587, or 25 if 587 is blocked.</p>
        </section>
        <section class="card">
            <div class="card__head"><h2>Addresses</h2></div>
            <div class="form-grid">
                <div><label class="label" for="FromAddr">Send from</label><asp:TextBox ID="FromAddr" runat="server" CssClass="field" MaxLength="200" placeholder="hello@yenetch.com" /></div>
                <div><label class="label" for="FromName">Sender name</label><asp:TextBox ID="FromName" runat="server" CssClass="field" MaxLength="100" placeholder="Yenetch" /></div>
                <div><label class="label" for="LeadsTo">Send lead alerts to</label><asp:TextBox ID="LeadsTo" runat="server" CssClass="field" MaxLength="300" placeholder="leads@yenetch.com" />
                    <p class="muted small" style="margin-top:6px">Separate several addresses with commas.</p></div>
                <div><label class="label" for="CareersTo">Send job applications to (optional)</label><asp:TextBox ID="CareersTo" runat="server" CssClass="field" MaxLength="300" placeholder="Same as lead alerts" /></div>
                <div><label class="label" for="ReplyTo">Replies go to (optional)</label><asp:TextBox ID="ReplyTo" runat="server" CssClass="field" MaxLength="200" placeholder="Same as Send from" /></div>
                <div class="span-2"><label class="check"><asp:CheckBox ID="AutoReply" runat="server" /> Send a thank-you email to people who enquire and leave an email address</label></div>
            </div>
        </section>
        <section class="card" id="daily">
            <div class="card__head"><h2>Daily summary email</h2><label class="check"><input type="checkbox" name="dg_on" value="1"<%= Yenetch.Crm.DailyDigest.Enabled ? " checked" : "" %> /> On</label></div>
            <p class="muted small" style="margin:0 0 12px">One email a day with new leads, today's calls, follow-ups due, leads going cold, website audits, job applications, subscribers and visitors.</p>
            <div class="form-grid">
                <div><label class="label" for="dg_hour">Send at (IST)</label><select class="field" id="dg_hour" name="dg_hour"><% for (var h = 5; h <= 22; h++) { %><option value="<%= h %>"<%= h == Yenetch.Crm.DailyDigest.Hour ? " selected" : "" %>><%= new DateTime(2000, 1, 1, h, 0, 0).ToString("h tt") %></option><% } %></select></div>
                <div><label class="label" for="dg_extra">Also send to (optional)</label><input class="field" id="dg_extra" name="dg_extra" value="<%: Yenetch.Crm.DailyDigest.Extra %>" placeholder="owner@yenetch.com" /></div>
                <div class="span-2"><label class="check"><input type="checkbox" name="dg_sales" value="1"<%= Yenetch.Crm.DailyDigest.ForSales ? " checked" : "" %> /> Sales users get their own summary with only their leads (skipped on days with nothing for them)</label></div>
            </div>
            <p class="muted small" style="margin-top:10px">Admins and managers get the full summary.<%= Yenetch.Crm.DailyDigest.LastSent != null ? " Last sent " + Yenetch.Crm.DailyDigest.LastSent + "." : "" %></p>
            <div class="form-actions"><button class="btn btn--line" type="submit" name="digestTest" value="1">Save and send me today's summary</button></div>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Newsletter speed</h2></div>
            <div class="form-grid">
                <div><label class="label" for="PerMinute">Emails a minute</label><asp:TextBox ID="PerMinute" runat="server" CssClass="field" MaxLength="3" inputmode="numeric" /></div>
                <div><label class="label" for="PerHour">At most an hour</label><asp:TextBox ID="PerHour" runat="server" CssClass="field" MaxLength="6" inputmode="numeric" /></div>
            </div>
            <p class="muted small" style="margin-top:10px">Most shared hosts allow a few hundred emails an hour. Ask your host for the limit and stay a little under it. If the mail server says it is busy, sending waits and tries again on its own.</p>
        </section>
        <section class="card">
            <div class="card__head"><h2>Send a test</h2></div>
            <label class="label" for="TestTo">To</label>
            <asp:TextBox ID="TestTo" runat="server" CssClass="field" MaxLength="200" inputmode="email" />
            <asp:Button ID="TestButton" runat="server" Text="Save and send test" CssClass="btn btn--line" OnClick="TestButton_Click" style="margin-top:10px" />
            <p class="muted small" style="margin-top:10px">Saves the settings first. If it fails, the reason from the mail server is shown here.</p>
        </section>
        <section class="card">
            <div class="card__head"><h2>What gets sent</h2></div>
            <ul class="tips">
                <li><b>Lead alert</b> to your team for every enquiry, with the visitor's details and any file they attached. Reply goes straight to the visitor.</li>
                <li><b>Thank-you reply</b> to the visitor, with your phone and WhatsApp.</li>
                <li><b>Assignment alert</b> to a teammate when a lead is assigned to them.</li>
                <li><b>Newsletters</b> from Campaigns, and <b>automatic emails</b> from Automatic emails.</li>
                <li><b>Booking confirmations</b>, reminders and calendar invites, and <b>website audit reports</b>.</li>
                <li><b>Daily summary</b> to the team, <b>sign-in codes</b> and <b>sign-in alerts</b>.</li>
            </ul>
            <p class="muted small" style="margin-top:10px">Every email uses the Yenetch layout with the logo embedded, plus a plain-text version for better delivery.</p>
        </section>
    </div>
</div>
</form>
</asp:Content>
