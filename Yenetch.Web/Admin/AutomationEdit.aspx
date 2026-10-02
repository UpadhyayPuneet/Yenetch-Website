<%@ Page Title="Automatic email" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="AutomationEdit.aspx.cs" Inherits="Yenetch.Web.Admin.AutomationEditPage" ValidateRequest="false" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/automations"><%= Icon("back") %>Automatic emails</a>
<div class="page-head">
    <div><h1><%: S.Name %></h1><p>For: <%: S.Who %>. Each email waits the time you choose after the one before it.</p></div>
    <div class="page-head__actions"><label class="check"><input type="checkbox" name="enabled" value="1"<%= S.Enabled ? " checked" : "" %> /> Turned on</label><button class="btn btn--blue" type="submit" name="act" value="save">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<% if (S.Key == "review" && Reviews.ReviewUrl == null) { %><div class="form-error" role="status">Add your Google review link in <a href="/admin/reviews">Google reviews</a> first, or the button will point to the website.</div><% } %>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
    <% for (var i = 0; i < Automations.MaxSteps; i++) { var st = i < S.Steps.Count ? S.Steps[i] : null; var open = st != null || i == S.Steps.Count; %>
        <% if (st == null && i > S.Steps.Count) break; %>
        <section class="card">
            <div class="card__head"><h2><%= st == null ? "+ Add email " + (i + 1) : "Email " + (i + 1) %></h2><% if (st != null) { %><a class="btn btn--sm btn--line" href="/admin/automations/<%= S.Key %>?preview=<%= i %>" target="_blank" rel="noopener">Preview</a><% } %></div>
            <div class="form-grid">
                <div><label class="label" for="d<%= i %>">Send</label><select class="field" id="d<%= i %>" name="d<%= i %>"><% foreach (var h in DelayChoices) { %><option value="<%= h %>"<%= st != null && st.DelayHours == h ? " selected" : "" %>><%= Yenetch.Web.Admin.AutomationListPage.Delay(h) %><%= i == 0 ? "" : " after the previous one" %></option><% } %></select></div>
                <div><label class="label" for="s<%= i %>">Subject</label><input class="field" id="s<%= i %>" name="s<%= i %>" maxlength="150" value="<%: st == null ? "" : st.Subject %>" placeholder="<%= st == null ? "Leave empty to skip" : "" %>" /></div>
                <div class="span-2"><label class="label" for="b<%= i %>">Email text (HTML)</label><textarea class="field field--code" id="b<%= i %>" name="b<%= i %>" rows="<%= st == null ? 4 : 9 %>" spellcheck="false"><%: st == null ? "" : st.Body %></textarea></div>
                <% if (st != null) { %><div class="span-2"><label class="check"><input type="checkbox" name="x<%= i %>" value="1" /> Remove this email</label></div><% } %>
            </div>
        </section>
    <% } %>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>People</h2></div>
            <div class="auto-stats auto-stats--col"><span><b><%= C["Active"] %></b> in progress</span><span><b><%= C["Done"] %></b> finished</span><span><b><%= C["Stopped"] %></b> stopped early</span><span><b><%= C["Unsubscribed"] %></b> unsubscribed</span></div>
            <% if (People.Count > 0) { %>
            <table class="table" style="margin-top:12px"><tbody>
            <% foreach (var p in People) { %><tr><td><b><%: p.Str("Name") ?? p.Str("Email") %></b><span class="cell-sub"><%: p.Str("Email") %></span></td>
                <td class="nowrap"><span class="badge <%= p.Str("Status") == "Active" ? "badge--new" : p.Str("Status") == "Done" ? "badge--won" : "badge--open" %>"><%: p.Str("Status") %></span><span class="cell-sub"><%: p.Str("Status") == "Active" ? "Next: email " + (p.Int("Step") + 1) + ", " + Date(p.DateN("DueOn")) : p.Str("LastError") %></span></td>
                <td class="right"><% if (p.Str("Status") == "Active") { %><button class="btn btn--ghost btn--sm" type="submit" name="stop" value="<%= p.Int("Id") %>">Stop</button><% } %></td></tr><% } %>
            </tbody></table>
            <% } %>
        </section>
        <section class="card">
            <div class="card__head"><h2>Send a test</h2></div>
            <div class="stack" style="gap:10px">
                <input class="field" name="testTo" type="email" value="<%: Me.Email %>" aria-label="Send test to" />
                <button class="btn btn--line" type="submit" name="act" value="test">Send all emails to me now</button>
                <p class="fld__help">Saves first, then sends every email in this series with sample details.</p>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Placeholders</h2></div>
            <ul class="tips mono small">
                <li>{{first_name}} · {{name}} · {{company}} · {{phone}}</li>
                <li>{{site}} · {{book_url}} · {{audit_url}}<%= S.Key == "review" ? " · {{review_url}}" : "" %></li>
                <li>{{button:Book a call:{{book_url}}}}</li>
            </ul>
            <p class="fld__help">The logo, company address, phone, social links and the unsubscribe link are added to every email automatically.</p>
        </section>
    </div>
</div>
</form>
</asp:Content>
