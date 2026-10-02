<%@ Page Title="Analytics" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Analytics.aspx.cs" Inherits="Yenetch.Web.Admin.AnalyticsPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Website analytics</h1><p><%: RangeLabel %> · first-party data from yenetch.com.</p></div>
    <div class="page-head__actions"><%= RangePicker() %></div>
</div>

<div class="kpis">
    <div class="kpi"><span>Visitors</span><b><%= N(O.Visitors) %></b><%= Delta(O.Visitors, O.PrevVisitors) %></div>
    <div class="kpi"><span>Visits</span><b><%= N(O.Sessions) %></b><%= Delta(O.Sessions, O.PrevSessions) %></div>
    <div class="kpi"><span>Pageviews</span><b><%= N(O.Pageviews) %></b><%= Delta(O.Pageviews, O.PrevPageviews) %></div>
    <div class="kpi"><span>Pages per visit</span><b><%= O.PagesPerSession.ToString("0.0") %></b><small>visitors who allowed analytics</small></div>
    <div class="kpi"><span>Avg. visit length</span><b><%= Dur(O.AvgDuration) %></b><small>bounce rate <%= O.Bounce.ToString("0") %>%</small></div>
    <div class="kpi"><span>Leads</span><b><%= N(O.Leads) %></b><small><%= O.Conversion %> of visitors</small></div>
    <a class="kpi" href="/admin/visitors?live=1"><span><i class="live-dot"></i>Right now</span><b><%= N(O.Live) %></b><small>active in the last 5 minutes</small></a>
</div>

<section class="card">
    <div class="card__head"><h2>Over time</h2><div class="seg chart-tabs" data-chart-tabs="traffic"><button type="button" class="is-on">Visitors</button><button type="button">Visits</button><button type="button">Pageviews</button><button type="button">Leads</button></div></div>
    <div class="chart" id="traffic" data-chart="<%= DailyJson %>"></div>
</section>

<div class="grid grid--3" style="margin-top:16px">
    <section class="card"><div class="card__head"><h2>Channels</h2><span class="muted small">visits · leads</span></div><%= Bars(Channels, extra: t => N(t.Extra) + " leads") %></section>
    <section class="card"><div class="card__head"><h2>Sources</h2></div><%= Bars(Sources) %></section>
    <section class="card"><div class="card__head"><h2>Campaigns</h2><span class="muted small">utm_campaign</span></div><%= Bars(Campaigns, "No tagged campaigns yet. Add utm_campaign to ad and email links.") %></section>
</div>

<section class="card" id="pages" style="margin-top:16px">
    <div class="card__head"><h2>Pages</h2><span class="muted small">views · average time · scroll depth</span></div>
    <%= Bars(Pages, link: t => t.Label, extra: t => Dur(t.Extra) + " · " + t.Extra2.ToString("0") + "%") %>
</section>

<div class="grid grid--2" style="margin-top:16px">
    <section class="card"><div class="card__head"><h2>Landing pages</h2><span class="muted small">where visits start · bounce</span></div><%= Bars(Landing, extra: t => t.Extra2.ToString("0") + "% bounce") %></section>
    <section class="card"><div class="card__head"><h2>Exit pages</h2><span class="muted small">last page of a visit</span></div><%= Bars(Exits) %></section>
</div>

<div class="grid grid--3" style="margin-top:16px">
    <section class="card"><div class="card__head"><h2>Countries</h2></div><%= Bars(Countries) %></section>
    <section class="card"><div class="card__head"><h2>Cities</h2></div><%= Bars(Cities, "City appears when the site runs behind Cloudflare or GeoIpToken is set.") %></section>
    <section class="card"><div class="card__head"><h2>Languages</h2></div><%= Bars(Languages) %></section>
    <section class="card"><div class="card__head"><h2>Devices</h2></div><%= Bars(Devices) %></section>
    <section class="card"><div class="card__head"><h2>Browsers</h2></div><%= Bars(Browsers) %></section>
    <section class="card"><div class="card__head"><h2>Operating systems</h2></div><%= Bars(Oses) %></section>
</div>

<div class="grid grid--2" style="margin-top:16px">
    <section class="card">
        <div class="card__head"><h2>Actions</h2><span class="muted small">times · people</span></div>
        <%= Bars(EventRows, "No clicks recorded yet.", extra: t => N(t.Extra) + " people", label: EventName) %>
    </section>
    <section class="card">
        <div class="card__head"><h2>Busiest hours</h2><span class="muted small">pageviews by hour, IST</span></div>
        <div class="chart" style="height:200px" data-chart="<%= HoursJson %>"></div>
    </section>
</div>
<% if (TopCtas.Count > 0) { %>
<section class="card" style="margin-top:16px"><div class="card__head"><h2>Buttons clicked most</h2></div><%= Bars(TopCtas) %></section>
<% } %>
<p class="card__note">Visitors who choose "Necessary only" in the cookie banner are counted once per visit without cookies or IP, so pages per visit, visit length and bounce rate use only visitors who allowed analytics.</p>
</asp:Content>
