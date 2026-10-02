<%@ Page Title="Proposals" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="ProposalList.aspx.cs" Inherits="Yenetch.Web.Admin.ProposalListPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Proposals</h1><p>Proposals you send to clients: they open them online, accept with their name and pay through a secure link. Start one from a lead to fill in the client's details.</p></div>
    <div class="page-head__actions"><a class="btn btn--blue" href="/admin/proposals/new"><%= Icon("plus") %>New proposal</a></div>
</div>
<div class="kpis">
    <div class="kpi"><span>Open</span><b><%= N(Open) %></b><small><%= Money(OpenValue) %> in INR proposals</small></div>
    <div class="kpi"><span>Accepted, 90 days</span><b><%= N(Accepted90) %></b><small><%= Sent90 > 0 ? Util.Pct(Accepted90, Sent90) + " of sent" : "" %></small></div>
    <div class="kpi"><span>Paid, 90 days</span><b><%= N(Paid90) %></b></div>
    <div class="kpi"><span>Opened but not answered</span><b><%= N(Viewed) %></b><small>a good time to call</small></div>
</div>
<nav class="seg" aria-label="Status" style="margin-bottom:12px">
    <a href="<%: With("status", null) %>"<%= Q("status") == "" ? " class=\"is-on\"" : "" %>>All</a>
    <% foreach (var s in Proposals.Statuses) { %><a href="<%: With("status", s) %>"<%= Q("status") == s ? " class=\"is-on\"" : "" %>><%= s %></a><% } %>
</nav>
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b>No proposals yet</b>Open a lead and choose <b>Create proposal</b>, or start a new one.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Proposal</th><th>Client</th><th>Total</th><th>Status</th><th>Opened</th><th>Updated</th></tr></thead>
        <tbody><% foreach (var p in Rows) { %><tr>
            <td><a class="cell-main" href="/admin/proposals/<%= p.Id %>"><%: p.Title %></a><span class="cell-sub"><%: p.Number %><%= p.CreatedByName != null ? " · " + H(p.CreatedByName) : "" %></span></td>
            <td><%: p.ClientName %><span class="cell-sub"><%: p.ClientCompany %></span></td>
            <td class="nowrap"><b><%: p.Money(p.Total) %></b><% if (p.Monthly > 0) { %><span class="cell-sub">then <%: p.Money(p.MonthlyAfter) %>/mo</span><% } %></td>
            <td><%= StatusBadgeFor(p) %></td>
            <td class="nowrap small"><%= p.Views > 0 ? N(p.Views) + "×<span class=\"cell-sub\">" + H(Ago(p.LastViewedOn)) + "</span>" : "<span class=\"muted\">–</span>" %></td>
            <td class="nowrap small"><%: Ago(p.UpdatedOn) %></td>
        </tr><% } %></tbody>
    </table></div>
    <%= Pager(Total, 50) %>
    <% } %>
</section>
</form>
</asp:Content>
