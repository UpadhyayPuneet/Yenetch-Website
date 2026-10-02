<%@ Page Title="Website content" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Content.aspx.cs" Inherits="Yenetch.Web.Admin.ContentPage" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Website content</h1><p>Everything on the website comes from here. Changes show on the site as soon as you save.</p></div>
    <div class="page-head__actions"><a class="btn btn--line" href="/" target="_blank" rel="noopener"><%= Icon("external") %>View website</a></div>
</div>
<% foreach (var g in Groups) { %>
<h2 class="section-label"><%: g.Key %></h2>
<div class="cms-grid">
    <% foreach (var c in g) { %>
    <a class="cms-card" href="/admin/content/<%= c.Key %>">
        <b><%: c.Title %></b>
        <span><%: c.Description %></span>
        <small><%= c.IsSingle ? "Edit" : N(Count(c.Key)) + (Count(c.Key) == 1 ? " item" : " items") %></small>
    </a>
    <% } %>
    <% if (g.Key == "Company") { %>
    <a class="cms-card" href="/admin/posts"><b>Blog</b><span>Write, schedule and publish articles on the Insights page.</span><small>Open blog</small></a>
    <% if (Me.IsAdmin) { %><a class="cms-card" href="/admin/email"><b>Email settings</b><span>Your mail server, the sender address, where lead alerts go, and the auto-reply to enquiries.</span><small>Set up email</small></a><% } %>
    <% } %>
</div>
<% } %>
</asp:Content>
