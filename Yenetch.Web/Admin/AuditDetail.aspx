<%@ Page Title="Website audit" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="AuditDetail.aspx.cs" Inherits="Yenetch.Web.Admin.AuditDetailPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<%@ Import Namespace="System.Linq" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<a class="crumb" href="/admin/audits"><%= Icon("back") %>Website audits</a>
<div class="page-head">
    <div><h1><%: Yenetch.Web.Admin.AuditsPage.Host(Row.Str("FinalUrl") ?? Row.Str("Url")) %></h1><p><%: When(Row.DateN("CreatedOn")) %> · <a href="<%: Row.Str("FinalUrl") ?? Row.Str("Url") %>" target="_blank" rel="noopener nofollow">Open website</a></p></div>
    <div class="page-head__actions"><% if (Row.IntN("LeadId").HasValue) { %><a class="btn btn--blue" href="/admin/leads/<%= Row.IntN("LeadId") %>">Open lead</a><% } %></div>
</div>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <% if (R == null) { %><section class="card"><p>The report for this audit could not be read.</p></section><% } else { %>
        <section class="card">
            <div class="audit-sum">
                <span class="score score--lg score--<%= Yenetch.Web.Admin.AuditsPage.Grade(R.Score) %>"><%= R.Score %></span>
                <div><h2 style="margin:0"><%: R.Headline %></h2>
                <p class="muted small" style="margin:4px 0 0"><%= R.Checks.Count(c => c.Status == "fail") %> to fix · <%= R.Checks.Count(c => c.Status == "warn") %> to improve · <%= R.Checks.Count(c => c.Status == "pass") %> passed</p></div>
            </div>
            <div class="abars">
            <% foreach (var c in R.Categories) { var sc = Convert.ToInt32(c["score"]); %>
                <div class="abar"><span><%: c["name"] %></span><i><s style="width:<%= sc %>%" class="abar--<%= Yenetch.Web.Admin.AuditsPage.Grade(sc) %>"></s></i><b><%= sc %></b></div>
            <% } %>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Findings</h2></div>
            <ul class="findings">
            <% foreach (var c in R.Checks.OrderBy(c => c.Status == "fail" ? 0 : c.Status == "warn" ? 1 : 2)) { %>
                <li class="finding finding--<%= c.Status %>"><b><%: c.Title %></b><% if (!string.IsNullOrEmpty(c.Detail)) { %><p><%: c.Detail %></p><% } %><% if (c.Status != "pass" && !string.IsNullOrEmpty(c.Fix)) { %><p><b>Fix:</b> <%: c.Fix %></p><% } %></li>
            <% } %>
            </ul>
        </section>
        <% } %>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Person</h2></div>
            <dl class="facts">
                <div><dt>Name</dt><b><%: Row.Str("Name") ?? "–" %></b></div>
                <div><dt>Email</dt><b><a href="mailto:<%: Row.Str("Email") %>"><%: Row.Str("Email") %></a></b></div>
                <div><dt>Phone</dt><b><%: Row.Str("Phone") ?? "–" %></b></div>
            </dl>
        </section>
        <% if (R != null && R.Stats != null) { %>
        <section class="card">
            <div class="card__head"><h2>Page facts</h2></div>
            <dl class="facts">
                <% foreach (var kv in R.Stats) { %><div><dt><%: StatName(kv.Key) %></dt><b><%: kv.Value %></b></div><% } %>
            </dl>
        </section>
        <% } %>
        <section class="card">
            <div class="card__head"><h2>Next step</h2></div>
            <p class="muted small" style="margin:0">Call within a day while the report is fresh. Open with the top two fixes and offer a free call to walk through the rest.</p>
        </section>
    </div>
</div>
</asp:Content>
