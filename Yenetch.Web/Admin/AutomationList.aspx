<%@ Page Title="Automatic emails" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="AutomationList.aspx.cs" Inherits="Yenetch.Web.Admin.AutomationListPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Automatic emails</h1><p>Short email series that go out on their own: a welcome for new subscribers, a follow-up for new enquiries and a Google review request for won clients.</p></div>
</div>
<div class="auto-cards">
<% foreach (var s in Series) { var c = Automations.Counts(s.Key); %>
    <a class="card auto-card" href="/admin/automations/<%= s.Key %>">
        <div class="card__head"><h2><%: s.Name %></h2><span class="badge <%= s.Enabled ? "badge--won" : "badge--open" %>"><%= s.Enabled ? "On" : "Off" %></span></div>
        <p class="muted small" style="margin:0 0 12px">For: <%: s.Who %> · <%= s.Steps.Count %> email<%= s.Steps.Count == 1 ? "" : "s" %></p>
        <ol class="auto-steps"><% var total = 0; foreach (var st in s.Steps) { total += st.DelayHours; %><li><span><%: Delay(total) %><%= total > 0 ? " from joining" : "" %></span><b><%: st.Subject %></b></li><% } %></ol>
        <div class="auto-stats"><span><b><%= c["Active"] %></b> in progress</span><span><b><%= c["Done"] %></b> finished</span><span><b><%= c["Stopped"] %></b> stopped early</span><span><b><%= c["Unsubscribed"] %></b> unsubscribed</span></div>
    </a>
<% } %>
</div>
<section class="card" style="margin-top:16px">
    <div class="card__head"><h2>How it works</h2></div>
    <ul class="tips">
        <li>Each person gets a series once. Anyone who unsubscribes never gets another automatic email.</li>
        <li>The enquiry follow-up stops by itself once the lead is won, lost or books a call. The review request stops once they open the review link.</li>
        <li>Automatic emails share the hourly sending limit with newsletters (Email settings), so your mail server is never overloaded.</li>
        <li>Every email uses the website look, with your logo, address, phone and an unsubscribe link.</li>
    </ul>
</section>
</asp:Content>
