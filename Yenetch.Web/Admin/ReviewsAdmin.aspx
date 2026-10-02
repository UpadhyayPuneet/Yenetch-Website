<%@ Page Title="Google reviews" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="ReviewsAdmin.aspx.cs" Inherits="Yenetch.Web.Admin.ReviewsAdminPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Google reviews</h1><p>Ask happy clients for a Google review and show your rating on the website.</p></div>
    <div class="page-head__actions"><button class="btn btn--blue" type="submit" name="act" value="save">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="kpis">
    <div class="kpi"><span>Google rating</span><b><%= Sum.Rating > 0 ? Sum.Rating.ToString("0.0") : "–" %></b><small><%= Sum.Count > 0 ? N(Sum.Count) + " reviews" : "not connected yet" %></small></div>
    <div class="kpi"><span>Requests sent, 90 days</span><b><%= N(Sent90) %></b></div>
    <div class="kpi"><span>Opened the review link</span><b><%= N(Clicked90) %></b><small><%= Sent90 > 0 ? Util.Pct(Clicked90, Sent90) : "" %></small></div>
</div>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Your Google Business Profile</h2></div>
            <div class="form-grid">
                <div class="span-2"><label class="label" for="link">Review link</label><input class="field" id="link" name="link" value="<%: Link %>" placeholder="https://g.page/r/XXXXXXXX/review" />
                    <p class="fld__help">In Google Business Profile, click <b>Ask for reviews</b> and copy the link. Leave empty to build it from the Place ID.</p></div>
                <div><label class="label" for="place">Place ID</label><input class="field mono" id="place" name="place" value="<%: Reviews.PlaceId %>" placeholder="ChIJ..." />
                    <p class="fld__help">Find it with Google's free Place ID Finder. Needed for the live rating.</p></div>
                <div><label class="label" for="key">Google Places API key</label><input class="field mono" id="key" name="key" type="password" autocomplete="off" placeholder="<%= HasKey ? "Saved. Type to replace" : "AIza..." %>" />
                    <p class="fld__help">Optional. Fetches the live rating and latest reviews twice a day. Google's free monthly allowance covers this.</p></div>
                <div><label class="label" for="rating">Rating (if no API key)</label><input class="field" id="rating" name="rating" inputmode="decimal" maxlength="3" value="<%: Rating %>" placeholder="4.9" /></div>
                <div><label class="label" for="count">Number of reviews</label><input class="field" id="count" name="count" inputmode="numeric" maxlength="6" value="<%: Count %>" placeholder="57" /></div>
                <div class="span-2"><label class="check"><input type="checkbox" name="show" value="1"<%= Reviews.ShowOnSite ? " checked" : "" %> /> Show the rating and reviews on the home, contact and landing pages</label></div>
            </div>
            <div class="form-actions"><% if (HasKey) { %><button class="btn btn--line" type="submit" name="act" value="refresh">Refresh from Google now</button><% } %>
                <% if (Sum.FetchedOn > DateTime.MinValue) { %><span class="muted small">Last fetched <%: When(Sum.FetchedOn) %></span><% } %></div>
        </section>
        <section class="card card--flush">
            <div class="card__head"><h2>Review requests</h2></div>
            <% if (Rows.Count == 0) { %><div class="empty"><b>No requests yet</b>Use <b>Ask for a review</b> on a won lead, or turn on the automatic review request.</div><% } else { %>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Client</th><th>Sent</th><th>Opened</th></tr></thead>
                <tbody>
                <% foreach (var r in Rows) { %><tr>
                    <td><% if (r.IntN("LeadId").HasValue) { %><a class="cell-main" href="/admin/leads/<%= r.IntN("LeadId") %>"><%: r.Str("Name") ?? r.Str("Email") %></a><% } else { %><b><%: r.Str("Name") ?? r.Str("Email") %></b><% } %><span class="cell-sub"><%: r.Str("Email") %></span></td>
                    <td class="nowrap"><%: Date(r.DateN("SentOn")) %><span class="cell-sub"><%: r.Str("SentByName") ?? "Automatic" %></span></td>
                    <td><%= r.DateN("ClickedOn").HasValue ? "<span class=\"badge badge--won\">" + Date(r.DateN("ClickedOn")) + "</span>" : "<span class=\"muted\">Not yet</span>" %></td>
                </tr><% } %>
                </tbody>
            </table></div>
            <% } %>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Automatic request</h2><span class="badge <%= Auto.Enabled ? "badge--won" : "badge--open" %>"><%= Auto.Enabled ? "On" : "Off" %></span></div>
            <p class="muted small" style="margin:0 0 12px">When a lead is marked <b>Won</b>, they get a friendly review request 3 days later, and one reminder if they have not opened the link.</p>
            <a class="btn btn--line" href="/admin/automations/review">Edit the emails</a>
        </section>
        <section class="card">
            <div class="card__head"><h2>Tips for more reviews</h2></div>
            <ul class="tips">
                <li>Ask within a week of a result, while the client is happiest.</li>
                <li>Reply to every review on Google, good or bad. It helps your ranking in Maps.</li>
                <li>Never offer gifts or discounts for reviews. Google removes them.</li>
            </ul>
        </section>
    </div>
</div>
</form>
</asp:Content>
