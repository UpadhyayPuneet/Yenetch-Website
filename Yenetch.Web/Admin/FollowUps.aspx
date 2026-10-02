<%@ Page Title="Follow-ups" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="FollowUps.aspx.cs" Inherits="Yenetch.Web.Admin.FollowUps" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Follow-ups</h1><p>Calls, emails and meetings promised to leads. Schedule them from a lead's timeline.</p></div>
    <% if (Me.SeesAllLeads) { %>
    <nav class="seg" aria-label="Whose follow-ups"><a href="<%= Att(With("who", null)) %>"<%= Who == "me" ? " class=\"is-on\"" : "" %>>Mine</a><a href="<%= Att(With("who", "all")) %>"<%= Who == "all" ? " class=\"is-on\"" : "" %>>Everyone</a></nav>
    <% } %>
</div>

<nav class="chips" aria-label="When">
    <% foreach (var t in Tabs) { %><a class="chip<%= Tab == t.Item1 ? " is-on" : "" %>" href="<%= Att(With("tab", t.Item1)) %>"><%: t.Item2 %> <b><%= t.Item3 %></b></a><% } %>
</nav>

<form id="form1" runat="server">
<section class="card card--flush">
    <% if (Tasks.Count == 0) { %>
    <div class="empty"><b><%= Tab == "overdue" ? "Nothing overdue" : "Nothing here" %></b><%= Tab == "overdue" ? "Every promise is on time. Nice work." : "Schedule a follow-up from any lead's timeline." %></div>
    <% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Due</th><th>Lead</th><th>Task</th><th class="hide-sm">Owner</th><th class="right">Actions</th></tr></thead>
        <tbody>
        <% foreach (var a in Tasks) { %>
        <tr data-href="/admin/leads/<%= a.LeadId %>">
            <td class="nowrap"><%= DueLabel(a.DueOn) %></td>
            <td><a class="cell-main" href="/admin/leads/<%= a.LeadId %>"><%: a.LeadName %></a><span class="cell-sub"><%: a.LeadStatus %></span></td>
            <td><%: a.Body %></td>
            <td class="hide-sm"><%: a.OwnerName ?? a.UserName ?? "Unassigned" %></td>
            <td class="right nowrap">
                <button class="btn btn--line btn--sm" type="submit" name="snooze" value="<%= a.Id %>" title="Move to tomorrow">+1 day</button>
                <button class="btn btn--sm" type="submit" name="done" value="<%= a.Id %>"><%= Icon("check") %>Done</button>
            </td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <% } %>
</section>
</form>
</asp:Content>
