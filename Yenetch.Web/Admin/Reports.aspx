<%@ Page Title="Reports" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="Yenetch.Web.Admin.Reports" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Sales reports</h1><p>Leads received <%: RangeLabel %>.</p></div>
    <div class="page-head__actions"><%= RangePicker() %></div>
</div>

<div class="kpis">
    <div class="kpi"><span>Leads received</span><b><%= N(Total) %></b><%= Delta(Total, PrevTotal) %></div>
    <div class="kpi"><span>Won</span><b><%= N(Won) %></b><small><%= Util.Money(WonValue) %> expected value</small></div>
    <div class="kpi"><span>Win rate</span><b><%= Util.Pct(Won, Won + Lost) %></b><small>won out of closed</small></div>
    <div class="kpi"><span>Still open</span><b><%= N(Open) %></b><small><%= Util.Money(OpenValue) %> in pipeline</small></div>
    <div class="kpi"><span>Median first response</span><b><%= FirstResponse %></b><small>lead received to first call or message</small></div>
</div>

<div class="grid grid--2">
    <section class="card">
        <div class="card__head"><h2>Leads per month</h2><span class="legend"><span><i></i>Leads</span></span></div>
        <div class="chart" data-chart="<%= MonthlyJson %>"></div>
    </section>
    <section class="card">
        <div class="card__head"><h2>Pipeline by stage</h2></div>
        <%= Bars(ByStatus, link: t => "/admin/leads?view=all&status=" + U(t.Label)) %>
    </section>
    <section class="card">
        <div class="card__head"><h2>By source</h2><span class="muted small">leads · won</span></div>
        <%= Bars(BySource, extra: t => N(t.Extra) + " won", link: t => "/admin/leads?view=all&source=" + U(t.Label)) %>
    </section>
    <section class="card">
        <div class="card__head"><h2>By type</h2><span class="muted small">leads · won</span></div>
        <%= Bars(ByType, extra: t => N(t.Extra) + " won", link: t => "/admin/leads?view=all&type=" + U(t.Label)) %>
    </section>
    <section class="card">
        <div class="card__head"><h2>By website channel</h2><span class="muted small">where the visitor first came from</span></div>
        <%= Bars(ByChannel, extra: t => N(t.Extra) + " won") %>
    </section>
    <section class="card">
        <div class="card__head"><h2>Most requested</h2><span class="muted small">service or product</span></div>
        <%= Bars(ByInterest) %>
    </section>
</div>

<section class="card card--flush" style="margin-top:16px">
    <div class="card__head"><h2>Team performance</h2></div>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Owner</th><th class="right">Leads</th><th class="right">Open</th><th class="right">Overdue</th><th class="right">Won</th><th class="right">Lost</th><th class="right">Win rate</th><th class="right">Won value</th><th class="right">Activities logged</th></tr></thead>
        <tbody>
        <% foreach (var o in Owners) { %>
        <tr><td><b><%: o.Name %></b></td><td class="right num"><%= o.Leads %></td><td class="right num"><%= o.Open %></td><td class="right num<%= o.Overdue > 0 ? " due--late" : "" %>"><%= o.Overdue %></td><td class="right num"><%= o.Won %></td><td class="right num"><%= o.Lost %></td><td class="right num"><%= o.WinRate %></td><td class="right num"><%= Util.Money(o.WonValue) %></td><td class="right num"><%= o.Activities %></td></tr>
        <% } %>
        <% if (Owners.Count == 0) { %><tr><td colspan="9" class="empty">No leads in this range.</td></tr><% } %>
        </tbody>
    </table></div>
</section>
</asp:Content>
