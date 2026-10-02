<%@ Page Title="Visitors" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Visitors.aspx.cs" Inherits="Yenetch.Web.Admin.Visitors" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Visitors</h1><p>People who allowed analytics cookies, newest activity first. Open one to see every visit and page.</p></div>
</div>
<nav class="chips" aria-label="Filter visitors">
    <a class="chip<%= Q("live") == "" && Q("leads") == "" ? " is-on" : "" %>" href="/admin/visitors">All <b><%= N(AllCount) %></b></a>
    <a class="chip<%= Q("live") == "1" ? " is-on" : "" %>" href="/admin/visitors?live=1"><i class="live-dot"></i>On the site now <b><%= N(LiveCount) %></b></a>
    <a class="chip<%= Q("leads") == "1" ? " is-on" : "" %>" href="/admin/visitors?leads=1">Became leads <b><%= N(LeadCount) %></b></a>
</nav>
<form class="filters" method="get" action="/admin/visitors">
    <% if (Q("live") != "") { %><input type="hidden" name="live" value="1" /><% } %><% if (Q("leads") != "") { %><input type="hidden" name="leads" value="1" /><% } %>
    <input class="field search" type="search" name="q" value="<%= Att(Q("q")) %>" placeholder="City, country, IP, source or lead name" aria-label="Search visitors" />
    <button class="btn btn--line" type="submit">Search</button>
</form>
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b>No visitors yet</b>Visitors appear here after they accept analytics cookies on the website.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Visitor</th><th>Location</th><th class="hide-sm">Device</th><th class="hide-sm">First came from</th><th class="right">Visits</th><th class="right">Pages</th><th>Last seen</th></tr></thead>
        <tbody>
        <% foreach (var v in Rows) { %>
        <tr data-href="/admin/visitors/<%= v.Id %>">
            <td><a class="cell-main" href="/admin/visitors/<%= v.Id %>"><%= v.IsLive ? "<i class=\"live-dot\"></i>" : "" %><%: v.LeadName ?? "Visitor " + v.Id.Substring(0, 6) %></a>
                <span class="cell-sub"><%= v.LeadId.HasValue ? "Lead · " : "" %><%: v.Ip %></span></td>
            <td><%: string.IsNullOrEmpty(v.Place) ? "Unknown" : v.Place %></td>
            <td class="hide-sm"><%: v.Device %><span class="cell-sub"><%: v.Os %> · <%: v.Browser %></span></td>
            <td class="hide-sm"><%: v.Channel %><span class="cell-sub"><%: v.Source %></span></td>
            <td class="right num"><%= v.Sessions %></td>
            <td class="right num"><%= v.Pageviews %></td>
            <td class="nowrap muted" title="<%: When(v.LastSeen) %>"><%: Ago(v.LastSeen) %></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <%= Pager(Total, PageSize) %>
    <% } %>
</section>
</asp:Content>
