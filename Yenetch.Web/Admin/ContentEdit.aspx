<%@ Page Title="Edit" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="ContentEdit.aspx.cs" Inherits="Yenetch.Web.Admin.ContentEditPage" ValidateRequest="false" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server" data-cms-page="">
<a class="crumb" href="<%= C.IsSingle ? "/admin/content" : "/admin/content/" + C.Key %>"><%= Icon("back") %><%: C.IsSingle ? "Website content" : C.Title %></a>
<div class="page-head">
    <div><h1><%: Heading %></h1><p><%: C.Description %></p></div>
    <div class="page-head__actions">
        <% if (ViewUrl != null) { %><a class="btn btn--line" href="<%= Att(ViewUrl) %>" target="_blank" rel="noopener"><%= Icon("external") %>View on website</a><% } %>
        <asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn btn--blue" OnClick="SaveButton_Click" />
    </div>
</div>
<asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
<div class="grid grid--main">
    <section class="card"><div class="cms-form" data-cms-form="" data-schema="<%= Json(C.Fields) %>" data-value="<%= Att(ValueJson) %>" data-choices="<%= Json(Choices) %>" data-photos="<%= Json(Yenetch.Data.Photos.Map()) %>"></div></section>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>On the website</h2></div>
            <% if (!C.IsSingle) { %>
            <label class="check"><input type="checkbox" name="active" value="1"<%= Active ? " checked" : "" %> /> Show on the website</label>
            <p class="muted small" style="margin-top:6px">Untick to hide it without deleting.</p>
            <% } else { %><p class="muted small">Used across the whole website.</p><% } %>
            <% if (Item.Id > 0 && !C.IsSingle && !C.Fixed) { %><div style="margin-top:14px"><button class="btn btn--danger btn--sm" type="submit" name="delete" value="1" data-confirm="Delete this permanently? To keep it but take it off the site, untick Show on the website instead."><%= Icon("trash") %>Delete</button></div><% } %>
            <% if (Item.Id > 0) { %><p class="muted small" style="margin-top:14px">Last saved <%: Ago(Item.UpdatedOn) %><%= Item.UpdatedByName != null ? " by " + H(Item.UpdatedByName) : "" %>.</p><% } %>
        </section>
        <section class="card"><div class="card__head"><h2>Tips</h2></div>
            <ul class="tips">
                <li>Images: upload a photo, or keep a built-in one. Wide photos work best.</li>
                <li>Lists: one entry per line.</li>
                <li>Use the arrows to reorder repeated blocks such as offices or FAQs.</li>
            </ul></section>
    </div>
</div>
<input type="hidden" name="data" data-cms-data="" />
</form>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server"><script src="/assets/admin/editor.js?v=3" defer></script><script src="/assets/admin/content.js?v=1" defer></script></asp:Content>
