<%@ Page Title="Campaigns" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Campaigns.aspx.cs" Inherits="Yenetch.Web.Admin.Campaigns" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Newsletter campaigns</h1><p>Write, test and send emails to <%= N(Active) %> active subscribers.</p></div>
    <div class="page-head__actions"><a class="btn btn--blue" href="/admin/campaigns/new"><%= Icon("plus") %>New campaign</a></div>
</div>
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b>No campaigns yet</b>Write your first newsletter. You can send a test to yourself before it goes to everyone.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Subject</th><th>Status</th><th class="right">Recipients</th><th class="right">Sent</th><th class="right hide-sm">Failed</th><th>Date</th></tr></thead>
        <tbody>
        <% foreach (var c in Rows) { %>
        <tr data-href="/admin/campaigns/<%= c.Id %>">
            <td><a class="cell-main" href="/admin/campaigns/<%= c.Id %>"><%: c.Subject %></a><span class="cell-sub"><%: c.AuthorName %></span></td>
            <td><span class="badge <%= c.Status == "Sent" ? "badge--won" : c.Status == "Sending" || c.Status == "Queueing" ? "badge--hot" : c.Status == "Scheduled" ? "badge--new" : c.Status == "Paused" ? "badge--lost" : "badge--open" %>"><%: c.Status %></span></td>
            <td class="right num"><%= c.Status == "Draft" ? "—" : N(c.Recipients) %></td>
            <td class="right num"><%= c.Status == "Draft" ? "—" : N(c.SentCount) %></td>
            <td class="right num hide-sm"><%= c.Status == "Draft" ? "—" : N(c.FailedCount) %></td>
            <td class="nowrap muted"><%: c.Status == "Scheduled" ? "Sends " + When(c.ScheduledFor) : c.SentOn.HasValue ? When(c.SentOn) : "Edited " + Ago(c.UpdatedOn) %></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <% } %>
</section>
</asp:Content>
