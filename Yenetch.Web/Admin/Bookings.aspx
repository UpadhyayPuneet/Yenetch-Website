<%@ Page Title="Bookings" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Bookings.aspx.cs" Inherits="Yenetch.Web.Admin.BookingsPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Booked calls</h1><p><%= N(Upcoming) %> upcoming. Visitors book at <a href="/book" target="_blank" rel="noopener">/book</a>; each booking also creates a lead.</p></div>
    <div class="page-head__actions"><% if (Me.CanUseMarketing) { %><a class="btn btn--line" href="/admin/availability"><%= Icon("clock") %>Availability</a><% } %><a class="btn btn--line" href="/book" target="_blank" rel="noopener"><%= Icon("external") %>Booking page</a></div>
</div>
<% if (!Bookings.Config.Enabled) { %><div class="form-error" role="status">Online booking is switched off, so visitors cannot book. Turn it on in <a href="/admin/availability">Availability</a>.</div><% } %>
<nav class="chips" aria-label="View">
    <% foreach (var v in Views) { %><a class="chip<%= View == v[0] ? " is-on" : "" %>" href="<%= Att(With("view", v[0] == "upcoming" ? null : v[0])) %>"><%= v[1] %></a><% } %>
</nav>
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b>No calls here yet</b>When someone books a time on the website it shows here, and you get an email with a calendar invite.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>When (IST)</th><th>Person</th><th class="hide-sm">Topic</th><th class="hide-sm">How</th><th>Status</th></tr></thead>
        <tbody>
        <% foreach (var b in Rows) { %>
        <tr data-href="/admin/bookings/<%= b.Id %>">
            <td class="nowrap"><a class="cell-main" href="/admin/bookings/<%= b.Id %>"><%: Util.Ist(b.StartOn).ToString("ddd d MMM, h:mm tt", Util.India) %></a><span class="cell-sub"><%: DayHint(b) %></span></td>
            <td><b><%: b.Name %></b><span class="cell-sub"><%: b.Company ?? b.Email %></span></td>
            <td class="hide-sm"><%: b.Topic %></td>
            <td class="hide-sm"><%: b.Mode %></td>
            <td><span class="badge <%= Css(b.Status) %>"><%: b.Status %></span></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <%= Pager(Total, PageSize) %>
    <% } %>
</section>
</asp:Content>
