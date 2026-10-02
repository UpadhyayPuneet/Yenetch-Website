<%@ Page Title="Blog" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Posts.aspx.cs" Inherits="Yenetch.Web.Admin.PostsPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Blog</h1><p><%= N(Live) %> articles live on the Insights page<%= Drafts > 0 ? " · " + N(Drafts) + " draft" + (Drafts == 1 ? "" : "s") : "" %>.</p></div>
    <div class="page-head__actions"><a class="btn btn--blue" href="/admin/posts/new"><%= Icon("plus") %>New post</a></div>
</div>
<nav class="seg" aria-label="Filter" style="margin-bottom:16px">
    <a href="/admin/posts"<%= Filter == "" ? " class=\"is-on\" aria-current=\"true\"" : "" %>>All</a>
    <a href="/admin/posts?show=draft"<%= Filter == "draft" ? " class=\"is-on\" aria-current=\"true\"" : "" %>>Drafts</a>
    <a href="/admin/posts?show=live"<%= Filter == "live" ? " class=\"is-on\" aria-current=\"true\"" : "" %>>Published</a>
    <a href="/admin/posts?show=builtin"<%= Filter == "builtin" ? " class=\"is-on\" aria-current=\"true\"" : "" %>>Built-in</a>
</nav>
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b>Nothing here yet</b>Write a post and publish it, or save it as a draft to finish later.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Title</th><th>Status</th><th class="hide-sm">Category</th><th>Date</th></tr></thead>
        <tbody>
        <% foreach (var p in Rows) { %>
        <tr data-href="<%= Att(p.EditUrl) %>">
            <td><a class="cell-main" href="<%= Att(p.EditUrl) %>"><%: p.Title %></a><span class="cell-sub"><%: "/blog/" + p.Slug %><%= p.Row == null ? " · built-in article" : p.BuiltIn != null ? " · edited built-in" : "" %><%= p.Row != null && p.Row.UpdatedByName != null ? " · " + H(p.Row.UpdatedByName) : "" %></span></td>
            <td><span class="badge <%= Badge(p.Status) %>"><%: p.Status %></span></td>
            <td class="hide-sm"><%: p.Category %></td>
            <td class="nowrap muted"><%: Date(p.Date) %></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <% } %>
</section>
<p class="card__note">Built-in articles came with the website. Open one to edit it: your saved copy replaces it on the site. Set a post to Hidden to take it off the site without deleting it.</p>
</asp:Content>
