<%@ Page Title="Content" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="ContentList.aspx.cs" Inherits="Yenetch.Web.Admin.ContentListPage" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/content"><%= Icon("back") %>Website content</a>
<div class="page-head">
    <div><h1><%: C.Title %></h1><p><%: C.Description %></p></div>
    <% if (!C.Fixed) { %><div class="page-head__actions"><a class="btn btn--blue" href="/admin/content/<%= C.Key %>/new"><%= Icon("plus") %>Add</a></div><% } %>
</div>
<section class="card card--flush">
    <% if (Rows.Count == 0) { %><div class="empty"><b>Nothing here yet</b>Add the first item.</div><% } else { %>
    <div class="table-wrap"><table class="table">
        <thead><tr><th>Name</th><th>Status</th><% if (!C.IsMap) { %><th class="right">Order</th><% } %><th class="hide-sm">Updated</th></tr></thead>
        <tbody>
        <% for (var i = 0; i < Rows.Count; i++) { var r = Rows[i]; %>
        <tr data-href="/admin/content/<%= C.Key %>/<%= r.Id %>">
            <td><a class="cell-main" href="/admin/content/<%= C.Key %>/<%= r.Id %>"><%: r.Title %></a><% if (!string.IsNullOrEmpty(r.ItemKey) && r.ItemKey != r.Title) { %><span class="cell-sub"><%: r.ItemKey %></span><% } %></td>
            <td><button type="submit" class="badge <%= r.IsActive ? "badge--won" : "badge--open" %> badge-btn" name="op" value="toggle:<%= r.Id %>" title="<%= r.IsActive ? "Shown on the site. Click to hide." : "Hidden. Click to show." %>"><%= r.IsActive ? "Shown" : "Hidden" %></button></td>
            <% if (!C.IsMap) { %><td class="right nowrap">
                <button type="submit" class="btn btn--ghost btn--sm" name="op" value="up:<%= r.Id %>" aria-label="Move up"<%= i == 0 ? " disabled" : "" %>>↑</button>
                <button type="submit" class="btn btn--ghost btn--sm" name="op" value="down:<%= r.Id %>" aria-label="Move down"<%= i == Rows.Count - 1 ? " disabled" : "" %>>↓</button>
            </td><% } %>
            <td class="nowrap muted hide-sm"><%: Ago(r.UpdatedOn) %><%= r.UpdatedByName != null ? " · " + H(r.UpdatedByName) : "" %></td>
        </tr>
        <% } %>
        </tbody>
    </table></div>
    <% } %>
</section>
</form>
</asp:Content>
