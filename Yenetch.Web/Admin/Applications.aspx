<%@ Page Title="Applications" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Applications.aspx.cs" Inherits="Yenetch.Web.Admin.ApplicationsPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Job applications</h1><p><%= N(AllCount) %> received · <%= N(Count("New")) %> new · <%= N(Count("Shortlisted")) %> shortlisted.</p></div>
    <div class="page-head__actions"><a class="btn btn--line" href="/careers" target="_blank" rel="noopener"><%= Icon("external") %>Careers page</a></div>
</div>

<nav class="chips" aria-label="Status">
    <a class="chip<%= Q("status") == "" ? " is-on" : "" %>" href="<%= Att(With("status", null, "page", null)) %>">All <b><%= N(AllCount) %></b></a>
    <% foreach (var s in Applications.Statuses) { %><a class="chip<%= Q("status") == s ? " is-on" : "" %>" href="<%= Att(With("status", s, "page", null)) %>"><%: s %> <b><%= N(Count(s)) %></b></a><% } %>
</nav>

<form class="filters" method="get" action="/admin/applications">
    <% if (Q("status") != "") { %><input type="hidden" name="status" value="<%= Att(Q("status")) %>" /><% } %>
    <input class="field search" type="search" name="q" value="<%= Att(Q("q")) %>" placeholder="Search name, email, skills, city, notes" aria-label="Search applications" />
    <select class="field" name="category" aria-label="Team"><option value="">All teams</option><% foreach (var c in Categories) { %><option<%= c == Q("category") ? " selected" : "" %>><%: c %></option><% } %></select>
    <select class="field" name="job" aria-label="Role"><option value="">All roles</option><% foreach (var j in Jobs) { %><option<%= j == Q("job") ? " selected" : "" %>><%: j %></option><% } %></select>
    <select class="field" name="exp" aria-label="Experience"><option value="">Any experience</option><% foreach (var e in new[] { "1", "2", "3", "5", "8", "10" }) { %><option value="<%= e %>"<%= e == Q("exp") ? " selected" : "" %>><%= e %>+ years</option><% } %></select>
    <select class="field" name="sort" aria-label="Sort"><% foreach (var o in Sorts) { %><option value="<%= o.Key %>"<%= o.Key == Sort ? " selected" : "" %>><%: o.Value %></option><% } %></select>
    <button class="btn btn--line" type="submit">Apply filters</button>
    <% if (Filtered) { %><a class="btn btn--ghost" href="/admin/applications">Clear</a><% } %>
</form>

<form id="form1" runat="server">
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b><%= Filtered ? "No applications match these filters" : "No applications yet" %></b>Applications from the Careers page land here, with the CV attached.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Candidate</th><th>Role</th><th class="hide-sm">Experience</th><th class="hide-sm">City</th><th>Status</th><th class="hide-sm">Applied</th><th class="right">Actions</th></tr></thead>
        <tbody>
        <% foreach (var a in Rows) { %>
        <tr>
            <td><a class="cell-main" href="/admin/applications/<%= a.Id %>"><b><%: a.Name %></b></a><span class="cell-sub"><%: a.Email %><%= a.Rating > 0 ? " · " + new string('★', a.Rating) : "" %></span></td>
            <td><%: a.Job %><span class="cell-sub"><%: a.Category %><%: string.IsNullOrEmpty(a.Skills) ? "" : " · " + Util.Cut(a.Skills, 60) %></span></td>
            <td class="hide-sm nowrap"><%: a.ExperienceLabel %></td>
            <td class="hide-sm"><%: a.City %><span class="cell-sub"><%: a.Notice %></span></td>
            <td><span class="badge badge--<%= Css(a.Status) %>"><%: a.Status %></span></td>
            <td class="hide-sm nowrap muted" title="<%: When(a.CreatedOn) %>"><%: Date(a.CreatedOn) %></td>
            <td class="right nowrap">
                <% if (a.HasFile) { %><a class="btn btn--ghost btn--sm" href="/Admin/Resume.ashx?id=<%= a.Id %>" target="_blank" rel="noopener" title="Open CV" aria-label="Open CV of <%: a.Name %>"><%= Icon("note") %></a><% } else if (!string.IsNullOrEmpty(a.ResumeUrl)) { %><a class="btn btn--ghost btn--sm" href="<%: a.ResumeUrl %>" target="_blank" rel="noopener nofollow" title="Open CV link" aria-label="Open CV link of <%: a.Name %>"><%= Icon("external") %></a><% } %>
                <% if (a.Status == "New") { %><button class="btn btn--line btn--sm" type="submit" name="status" value="Shortlisted:<%= a.Id %>">Shortlist</button><button class="btn btn--ghost btn--sm" type="submit" name="status" value="Rejected:<%= a.Id %>">Reject</button><% } %>
            </td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <%= Pager(Total, PageSize) %>
    <% } %>
</section>
</form>
</asp:Content>
