<%@ Page Title="Leads" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Leads.aspx.cs" Inherits="Yenetch.Web.Admin.Leads" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Leads</h1><p><%= N(Total) %> <%= Total == 1 ? "lead" : "leads" %> in this view.</p></div>
    <div class="page-head__actions">
        <a class="btn btn--line" href="/Admin/Export.ashx?what=leads&amp;<%= Att(Request.QueryString.ToString()) %>"><%= Icon("download") %>Export CSV</a>
        <a class="btn btn--blue" href="/admin/leads/new"><%= Icon("plus") %>Add lead</a>
    </div>
</div>

<nav class="chips" aria-label="Views">
    <% foreach (var v in Views) { %>
    <a class="chip<%= View == v.Key ? " is-on" : "" %>" href="<%= Att(With("view", v.Key, "status", null)) %>"><%: v.Label %> <b><%= N(v.Count) %></b></a>
    <% } %>
</nav>

<form class="filters" method="get" action="<%= Request.Url.AbsolutePath %>" data-autosubmit>
    <input type="hidden" name="view" value="<%= Att(View) %>" />
    <input class="field search" type="search" name="q" value="<%= Att(Q("q")) %>" placeholder="Search name, phone, email, need" aria-label="Search leads" />
    <select class="field" name="status" aria-label="Status"><option value="">Any status</option><%= Options(Lists.Statuses, Q("status")) %></select>
    <select class="field" name="type" aria-label="Type"><option value="">Any type</option><%= Options(Lists.Types, Q("type")) %></select>
    <select class="field" name="source" aria-label="Source"><option value="">Any source</option><%= Options(Lists.Sources, Q("source")) %></select>
    <select class="field" name="priority" aria-label="Priority"><option value="">Any priority</option><%= Options(Lists.Priorities, Q("priority")) %></select>
    <% if (Me.SeesAllLeads) { %><select class="field" name="owner" aria-label="Owner"><option value="">Any owner</option><option value="none"<%= Q("owner") == "none" ? " selected" : "" %>>Unassigned</option><% foreach (var u in Team) { %><option value="<%= u.Id %>"<%= Q("owner") == u.Id.ToString() ? " selected" : "" %>><%: u.Name %></option><% } %></select><% } %>
    <select class="field" name="created" aria-label="Received"><option value="">Any time</option><%= Options(new[] { "today", "7d", "30d", "90d" }, Q("created"), new[] { "Today", "Last 7 days", "Last 30 days", "Last 90 days" }) %></select>
    <button class="btn btn--line" type="submit">Filter</button>
    <% if (Filtered) { %><a class="btn btn--ghost" href="/admin/leads?view=<%= Att(View) %>">Clear</a><% } %>
</form>

<form id="form1" runat="server">
<section class="card card--flush">
    <% if (Rows.Count == 0) { %>
    <div class="empty"><b>No leads match</b>Try another view or clear the filters.</div>
    <% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr>
            <th class="check-col"><input type="checkbox" data-check-all="ids" aria-label="Select all" /></th>
            <th><%= Sort("name", "Lead") %></th>
            <th><%= Sort("status", "Status") %></th>
            <th class="hide-md"><%= Sort("type", "Type") %></th>
            <th class="hide-sm"><%= Sort("source", "Source") %></th>
            <th><%= Sort("priority", "Priority") %></th>
            <th class="hide-md"><%= Sort("value", "Value") %></th>
            <th class="hide-sm"><%= Sort("owner", "Owner") %></th>
            <th><%= Sort("followup", "Follow-up") %></th>
            <th><%= Sort("created", "Received") %></th>
        </tr></thead>
        <tbody>
        <% foreach (var l in Rows) { %>
        <tr data-href="/admin/leads/<%= l.Id %>">
            <td class="check-col"><input type="checkbox" name="ids" value="<%= l.Id %>" aria-label="Select <%: l.Name %>" /></td>
            <td><a class="cell-main" href="/admin/leads/<%= l.Id %>"><%: l.Name %></a><span class="cell-sub"><%: l.ContactLine %><%: string.IsNullOrEmpty(l.Interest) ? "" : " · " + l.Interest %></span></td>
            <td><%= StatusBadge(l.Status) %></td>
            <td class="hide-md"><%: l.LeadType %></td>
            <td class="hide-sm"><%: l.Source %><span class="cell-sub"><%: l.Channel %></span></td>
            <td><span class="prio prio--<%= l.Priority %>"><%: l.Priority %></span></td>
            <td class="hide-md num"><%: Money(l.EstValue) %></td>
            <td class="hide-sm nowrap"><%: l.AssignedName ?? "—" %></td>
            <td><%= l.IsOpen ? DueLabel(l.NextFollowUp) : "" %></td>
            <td class="nowrap muted" title="<%: When(l.CreatedOn) %>"><%: Ago(l.CreatedOn) %></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <div class="bulk">
        <span class="muted small">With selected:</span>
        <% if (Me.SeesAllLeads) { %>
        <select class="field" name="bulkOwner" aria-label="Assign to"><option value="">Assign to…</option><option value="none">Unassigned</option><% foreach (var u in Team) { %><option value="<%= u.Id %>"><%: u.Name %></option><% } %></select>
        <% } else { %>
        <select class="field" name="bulkOwner" aria-label="Assign to"><option value="">Assign to…</option><option value="<%= Me.Id %>">Me</option></select>
        <% } %>
        <select class="field" name="bulkStatus" aria-label="Set status"><option value="">Set status…</option><%= Options(Lists.Statuses, "") %></select>
        <button class="btn btn--sm" type="submit" name="bulk" value="apply">Apply</button>
    </div>
    <%= Pager(Total, PageSize) %>
    <% } %>
</section>
</form>
</asp:Content>
