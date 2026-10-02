<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Yenetch.Web.Admin.Dashboard" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1><%: Greeting %>, <%: Me.Name.Split(' ')[0] %>.</h1><p><%: Util.Ist(DateTime.UtcNow).ToString("dddd, d MMMM", Util.India) %> · the last 30 days at a glance.</p></div>
    <div class="page-head__actions"><a class="btn btn--blue" href="/admin/leads/new"><%= Icon("plus") %>Add lead</a></div>
</div>

<div class="kpis">
    <% if (Me.CanUseMarketing) { %>
    <a class="kpi" href="/admin/analytics"><span>Visitors</span><b><%= N(O.Visitors) %></b><%= Delta(O.Visitors, O.PrevVisitors) %></a>
    <a class="kpi" href="/admin/reports"><span>New leads</span><b><%= N(O.Leads) %></b><%= Delta(O.Leads, O.PrevLeads) %></a>
    <div class="kpi"><span>Visitor to lead</span><b><%= O.Conversion %></b><small><%= N(O.Won) %> won this period</small></div>
    <a class="kpi" href="/admin/visitors?live=1"><span><i class="live-dot"></i>On the site now</span><b><%= N(O.Live) %></b><small>active in the last 5 minutes</small></a>
    <% } %>
    <a class="kpi" href="/admin/leads?view=<%= Me.SeesAllLeads ? "open" : "mine" %>"><span><%= Me.SeesAllLeads ? "Open leads" : "My open leads" %></span><b><%= N(OpenLeads) %></b><small><%= N(Unassigned) %> unassigned</small></a>
    <a class="kpi<%= OverdueCount > 0 ? " kpi--alert" : "" %>" href="/admin/follow-ups"><span>Overdue follow-ups</span><b><%= N(OverdueCount) %></b><small><%= N(DueToday) %> more due today</small></a>
</div>

<div class="grid grid--main">
    <div class="stack">
        <% if (Me.CanUseMarketing) { %>
        <section class="card">
            <div class="card__head"><h2>Traffic and leads</h2><div class="seg chart-tabs" data-chart-tabs="dash-chart"><button type="button" class="is-on">Visitors</button><button type="button">Pageviews</button><button type="button">Leads</button></div></div>
            <div class="chart" id="dash-chart" data-chart="<%= ChartJson %>"></div>
        </section>
        <% } %>
        <section class="card card--flush">
            <div class="card__head"><h2>Newest leads</h2><a href="/admin/leads">All leads</a></div>
            <% if (Newest.Count == 0) { %><div class="empty"><b>No leads yet</b>Enquiries from the website, chatbot and solution finder will appear here.</div><% } else { %>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Lead</th><th>Status</th><th class="hide-sm">Source</th><th class="hide-sm">Owner</th><th>Received</th></tr></thead>
                <tbody>
                <% foreach (var l in Newest) { %>
                <tr data-href="/admin/leads/<%= l.Id %>">
                    <td><a class="cell-main" href="/admin/leads/<%= l.Id %>"><%: l.Name %></a><span class="cell-sub"><%: l.Interest ?? l.Need %></span></td>
                    <td><%= StatusBadge(l.Status) %></td>
                    <td class="hide-sm"><%: l.Source %></td>
                    <td class="hide-sm"><%: l.AssignedName ?? "Unassigned" %></td>
                    <td class="nowrap muted"><%: Ago(l.CreatedOn) %></td>
                </tr>
                <% } %>
                </tbody>
            </table></div>
            <% } %>
        </section>
    </div>
    <div class="stack">
        <section class="card">
            <div class="card__head"><h2>My follow-ups</h2><a href="/admin/follow-ups">See all</a></div>
            <% if (Tasks.Count == 0) { %><p class="empty small"><b>You are all caught up</b>Nothing due today.</p><% } else { %>
            <div class="timeline">
            <% foreach (var t in Tasks) { %>
                <a class="tl tl--task" href="/admin/leads/<%= t.LeadId %>" style="text-decoration:none">
                    <span class="tl__icon"><%= Icon("clock") %></span>
                    <span><span class="tl__top"><b><%: t.LeadName %></b><%= DueLabel(t.DueOn) %></span><span class="tl__body"><%: t.Body %></span></span>
                </a>
            <% } %>
            </div>
            <% } %>
        </section>
        <% if (Me.CanUseMarketing) { %>
        <section class="card">
            <div class="card__head"><h2>Where visitors come from</h2><a href="/admin/analytics">Analytics</a></div>
            <%= Bars(Channels) %>
        </section>
        <section class="card">
            <div class="card__head"><h2>Top pages</h2><a href="/admin/analytics#pages">More</a></div>
            <%= Bars(TopPages) %>
        </section>
        <% } %>
    </div>
</div>
</asp:Content>
