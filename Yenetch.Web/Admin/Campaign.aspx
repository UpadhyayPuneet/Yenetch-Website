<%@ Page Title="Campaign" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Campaign.aspx.cs" Inherits="Yenetch.Web.Admin.CampaignPage" ValidateRequest="false" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/campaigns"><%= Icon("back") %>Campaigns</a>
<% if (IsLocked) { %>
<asp:PlaceHolder ID="LockedError" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="LockedErrorText" runat="server" /></div></asp:PlaceHolder>
<div class="page-head">
    <div><h1><%: C.Subject %></h1><p><%= Summary() %></p></div>
    <div class="page-head__actions">
        <% if (C.Status == "Scheduled") { %>
        <button class="btn btn--blue" type="submit" name="act" value="now" data-confirm="Send this newsletter to <%= N(Active) %> subscribers now?"><%= Icon("send") %>Send now</button>
        <button class="btn btn--line" type="submit" name="act" value="unschedule">Cancel schedule</button>
        <% } else if (C.Status == "Sending" || C.Status == "Queueing") { %>
        <button class="btn btn--line" type="submit" name="act" value="pause">Pause</button>
        <% } else if (C.Status == "Paused") { %>
        <button class="btn btn--blue" type="submit" name="act" value="resume"><%= Icon("send") %>Resume</button>
        <button class="btn btn--danger" type="submit" name="act" value="stop" data-confirm="Stop for good? The <%= N(Counts["Pending"]) %> people not reached yet will not get this email.">Stop</button>
        <% } else if (C.Status == "Sent" && Counts["Failed"] > 0) { %>
        <button class="btn btn--line" type="submit" name="act" value="retry" data-confirm="Try the <%= N(Counts["Failed"]) %> failed addresses again?">Retry <%= N(Counts["Failed"]) %> failed</button>
        <% } %>
    </div>
</div>
<% if (C.Status != "Scheduled") { %>
<div class="kpis">
    <div class="kpi"><span>Recipients</span><b><%= N(C.Recipients) %></b></div>
    <div class="kpi"><span>Sent</span><b><%= N(C.SentCount) %></b></div>
    <div class="kpi"><span>Waiting</span><b><%= N(Counts["Pending"]) %></b><small><%= Counts["Pending"] > 0 ? H(NewsSender.Estimate(Counts["Pending"], C.Status == "Sent" ? 0 : NewsSender.UsedThisHour())) + " left" : "none" %></small></div>
    <div class="kpi<%= C.FailedCount > 0 ? " kpi--alert" : "" %>"><span>Failed</span><b><%= N(C.FailedCount) %></b><small><%= Counts["Skipped"] > 0 ? N(Counts["Skipped"]) + " skipped" : C.FailedCount > 0 ? "listed below" : "no errors" %></small></div>
</div>
<% } %>
<% if (C.Status == "Sending" || C.Status == "Queueing" || C.Status == "Paused") { %><div class="card" style="margin-bottom:16px"><div class="progress"><i style="width:<%= C.Progress %>%"></i></div><p class="muted small" style="margin-top:8px"><%= C.Progress %>% done.<% if (C.Status != "Paused") { %> Sending <%= NewsSender.PerMinute %> a minute, at most <%= N(NewsSender.PerHour) %> an hour (<a href="/admin/email">change</a>). It carries on if you close this page.<% } %></p></div><% } %>
<% if (Problems.Count > 0) { %>
<section class="card card--flush" style="margin-bottom:16px">
    <div class="card__head" style="padding:16px 20px 0"><h2>Not delivered</h2></div>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Email</th><th>Status</th><th>Reason</th></tr></thead>
        <tbody>
        <% foreach (var d in Problems) { %><tr><td><%: d.Email %></td><td><span class="badge <%= d.Status == "Failed" ? "badge--lost" : "badge--open" %>"><%: d.Status %></span></td><td class="muted small"><%: d.Error %></td></tr><% } %>
        </tbody>
    </table></div>
</section>
<% } %>
<iframe class="preview-frame" title="Email as sent" srcdoc="<%= Att(Newsletter.Render(C, null)) %>"></iframe>
<% } else { %>
<div class="page-head">
    <div><h1><%= IsNew ? "New campaign" : "Edit campaign" %></h1><p>Goes to <%= N(Active) %> active subscribers. Every email includes an unsubscribe link.</p></div>
    <div class="page-head__actions">
        <asp:Button ID="SaveButton" runat="server" Text="Save draft" CssClass="btn btn--line" OnClick="SaveButton_Click" />
        <% if (!IsNew) { %><button class="btn btn--blue" type="submit" name="send" value="1" data-confirm="Send this newsletter to <%= N(Active) %> subscribers now? It takes <%= H(NewsSender.Estimate(Active)) %>."><%= Icon("send") %>Send to <%= N(Active) %></button><% } %>
    </div>
</div>
<asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
<div class="editor">
    <section class="card">
        <div class="stack" style="gap:14px">
            <div><label class="label" for="Subject">Subject line</label><asp:TextBox ID="Subject" runat="server" CssClass="field" MaxLength="200" data-preview-subject="" placeholder="e.g. 5 ways to get more leads from Google this quarter" /></div>
            <div><label class="label" for="Preheader">Preview text</label><asp:TextBox ID="Preheader" runat="server" CssClass="field" MaxLength="200" data-preview-preheader="" placeholder="Shown after the subject in the inbox" /></div>
            <div>
                <label class="label" for="Body">Email body (HTML)</label>
                <div class="snippets">
                    <button type="button" class="btn btn--line btn--sm" data-snippet="&lt;h2 style=&quot;font-size:22px;margin:0 0 12px&quot;&gt;Heading&lt;/h2&gt;&#10;">Heading</button>
                    <button type="button" class="btn btn--line btn--sm" data-snippet="&lt;p style=&quot;margin:0 0 16px&quot;&gt;Paragraph&lt;/p&gt;&#10;">Paragraph</button>
                    <button type="button" class="btn btn--line btn--sm" data-snippet="&lt;p style=&quot;margin:24px 0&quot;&gt;&lt;a href=&quot;https://www.yenetch.com/contact&quot; style=&quot;display:inline-block;background:#0066FF;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:600&quot;&gt;Book a call&lt;/a&gt;&lt;/p&gt;&#10;">Button</button>
                    <button type="button" class="btn btn--line btn--sm" data-snippet="&lt;img src=&quot;https://www.yenetch.com/assets/img/og-image.png&quot; alt=&quot;&quot; width=&quot;536&quot; style=&quot;width:100%;height:auto;border-radius:12px;margin:0 0 16px&quot;&gt;&#10;">Image</button>
                    <button type="button" class="btn btn--line btn--sm" data-snippet="{{name}}">First name</button>
                </div>
                <asp:TextBox ID="Body" runat="server" ValidateRequestMode="Disabled" CssClass="field" TextMode="MultiLine" Rows="20" data-preview-source="" spellcheck="false" />
            </div>
            <div class="log-form__row">
                <label class="label" for="TestTo" style="margin:0">Send a test to</label>
                <asp:TextBox ID="TestTo" runat="server" CssClass="field" MaxLength="160" inputmode="email" />
                <asp:Button ID="TestButton" runat="server" Text="Send test" CssClass="btn btn--line" OnClick="TestButton_Click" />
            </div>
            <% if (!IsNew) { %>
            <div class="log-form__row">
                <label class="label" for="SendAt" style="margin:0">Or send later (IST)</label>
                <input class="field" type="datetime-local" id="SendAt" name="sendAt" value="<%: Request.Form["sendAt"] %>" min="<%= Util.InputDateTime(DateTime.UtcNow) %>" />
                <button class="btn btn--line" type="submit" name="send" value="later">Schedule</button>
            </div>
            <p class="muted small" style="margin:-6px 0 0">Sends <%= NewsSender.PerMinute %> a minute, at most <%= N(NewsSender.PerHour) %> an hour, so <%= N(Active) %> subscribers take <%= H(NewsSender.Estimate(Active)) %>. Change the speed in <a href="/admin/email">Email settings</a>.</p>
            <% } %>
            <% if (!IsNew) { %><div><button class="btn btn--danger btn--sm" type="submit" name="delete" value="1" data-confirm="Delete this draft?"><%= Icon("trash") %>Delete draft</button></div><% } %>
        </div>
    </section>
    <section>
        <p class="label">Preview</p>
        <iframe class="preview-frame" title="Email preview" data-preview="" data-shell="<%= Att(Shell) %>"></iframe>
    </section>
</div>
<% } %>
</form>
</asp:Content>
