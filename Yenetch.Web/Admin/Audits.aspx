<%@ Page Title="Website audits" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Audits.aspx.cs" Inherits="Yenetch.Web.Admin.AuditsPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Website audits</h1><p>Every audit run on <a href="/website-audit" target="_blank" rel="noopener">/website-audit</a>. Each one is also a lead, and the visitor gets the report by email.</p></div>
    <div class="page-head__actions"><a class="btn btn--line" href="/website-audit" target="_blank" rel="noopener"><%= Icon("external") %>Audit tool</a></div>
</div>
<div class="kpis">
    <div class="kpi"><span>Audits, last 30 days</span><b><%= N(Last30) %></b></div>
    <div class="kpi"><span>Average score</span><b><%= Avg30 %></b><small>out of 100</small></div>
    <div class="kpi"><span>Scored under 60</span><b><%= N(Weak30) %></b><small>the hottest prospects</small></div>
</div>
<section class="card card--flush">
    <form class="filters" style="padding:14px 14px 0;margin:0" method="get" action="/admin/audits">
        <input class="field search" type="search" name="q" value="<%= Att(Q("q")) %>" placeholder="Search website, email or name" aria-label="Search audits" />
        <button class="btn btn--line" type="submit">Search</button>
    </form>
    <% if (Rows.Count == 0) { %><div class="empty"><b>No audits yet</b>Share the free audit tool in ads, posts and emails. Every audit becomes a lead.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Website</th><th>Score</th><th>Person</th><th class="hide-sm">When</th></tr></thead>
        <tbody>
        <% foreach (var r in Rows) { %>
        <tr data-href="/admin/audits/<%= r.Int("Id") %>">
            <td><a class="cell-main" href="/admin/audits/<%= r.Int("Id") %>"><%: Host(r.Str("FinalUrl") ?? r.Str("Url")) %></a><span class="cell-sub"><%: r.Str("Url") %></span></td>
            <td><span class="score score--<%= Grade(r.Int("Score")) %>"><%= r.Int("Score") %></span></td>
            <td><b><%: r.Str("Name") ?? r.Str("Email") %></b><span class="cell-sub"><%: r.Str("Email") %><%: string.IsNullOrEmpty(r.Str("Phone")) ? "" : " · " + r.Str("Phone") %></span></td>
            <td class="hide-sm nowrap muted"><%: When(r.DateN("CreatedOn")) %></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <%= Pager(Total, PageSize) %>
    <% } %>
</section>
</asp:Content>
