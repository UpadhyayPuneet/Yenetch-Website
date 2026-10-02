<%@ Page Title="Visitor" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Visitor.aspx.cs" Inherits="Yenetch.Web.Admin.VisitorPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<a class="crumb" href="/admin/visitors"><%= Icon("back") %>Visitors</a>
<div class="page-head">
    <div>
        <h1><%= V.IsLive ? "<i class=\"live-dot\"></i>" : "" %><%: V.LeadName ?? "Visitor " + V.Id.Substring(0, 6) %></h1>
        <p><%: string.IsNullOrEmpty(V.Place) ? "Location unknown" : V.Place %> · <%: V.Device %>, <%: V.Os %>, <%: V.Browser %> · last seen <%: Ago(V.LastSeen) %></p>
    </div>
    <% if (V.LeadId.HasValue) { %><div class="page-head__actions"><a class="btn btn--blue" href="/admin/leads/<%= V.LeadId %>">Open lead</a></div><% } %>
</div>

<div class="kpis">
    <div class="kpi"><span>Visits</span><b><%= V.Sessions %></b><small>since <%: Date(V.FirstSeen) %></small></div>
    <div class="kpi"><span>Pages viewed</span><b><%= V.Pageviews %></b><small><%= V.Sessions > 0 ? (V.Pageviews / (double)V.Sessions).ToString("0.0") : "0" %> per visit</small></div>
    <div class="kpi"><span>Time on site</span><b><%= Dur(TotalSeconds) %></b><small>across all visits</small></div>
    <div class="kpi"><span>First came from</span><b style="font-size:1.125rem"><%: V.Channel %></b><small><%: V.Source %></small></div>
</div>

<div class="grid grid--main">
    <section class="card card--flush">
        <div class="card__head" style="padding-bottom:8px"><h2>Journey</h2><span class="muted small">newest visit first</span></div>
        <% for (var i = 0; i < Sessions.Count; i++) { var s = Sessions[i]; %>
        <details class="session"<%= i == 0 ? " open" : "" %>>
            <summary>
                <span><b><%: When(s.StartedOn) %></b>
                    <span class="session__meta"><span><%: s.Channel %><%: string.IsNullOrEmpty(s.Source) || s.Source == "(direct)" ? "" : " · " + s.Source %></span><%= string.IsNullOrEmpty(s.Campaign) ? "" : "<span>Campaign " + H(s.Campaign) + "</span>" %><span><%= s.Pageviews %> pages</span><span><%= s.Duration %></span></span></span>
                <span class="muted small hide-sm"><%: s.Device %> · <%: s.Place %></span>
            </summary>
            <ol class="steps">
            <% foreach (var st in s.Steps) { %>
                <li class="step <%= st.Kind == "event" ? (st.Title == "lead" ? "step--lead" : "step--event") : "" %>">
                    <time><%: Util.Time(st.At) %></time><i></i>
                    <% if (st.Kind == "page") { %>
                    <span><b><%: st.Title ?? st.Path %></b><small><%: st.Path %></small></span>
                    <span class="step__stats"><%= st.Seconds.HasValue ? Dur(st.Seconds.Value) : "" %><%= st.Scroll.HasValue ? " · " + st.Scroll + "% read" : "" %></span>
                    <% } else { %>
                    <span><b><%: EventText(st.Title) %></b><small><%: st.Label %></small></span><span></span>
                    <% } %>
                </li>
            <% } %>
            </ol>
        </details>
        <% } %>
    </section>
    <div class="stack">
        <section class="card">
            <div class="card__head"><h2>Profile</h2></div>
            <dl class="facts">
                <div><dt>Location</dt><b><%: string.IsNullOrEmpty(V.Place) ? "Unknown" : V.Place %></b></div>
                <div><dt>IP address</dt><b class="mono"><%: V.Ip ?? "—" %></b></div>
                <div><dt>Device</dt><b><%: V.Device %></b></div>
                <div><dt>System</dt><b><%: V.Os %> · <%: V.Browser %></b></div>
                <div><dt>Screen</dt><b><%: Latest == null ? "—" : Latest.Screen %></b></div>
                <div><dt>Language</dt><b><%: Latest == null ? "—" : Latest.Lang %></b></div>
                <div><dt>First visit</dt><b><%: When(V.FirstSeen) %></b></div>
                <div><dt>Landing page</dt><b><%: V.Landing %></b></div>
                <div><dt>Referrer</dt><b><%: First == null || string.IsNullOrEmpty(First.Referrer) ? "None (direct)" : First.Referrer %></b></div>
                <div><dt>Visitor id</dt><b class="mono"><%: V.Id %></b></div>
            </dl>
        </section>
        <section class="card">
            <div class="card__head"><h2>Most viewed</h2></div>
            <%= Bars(TopPages) %>
        </section>
    </div>
</div>
</asp:Content>
