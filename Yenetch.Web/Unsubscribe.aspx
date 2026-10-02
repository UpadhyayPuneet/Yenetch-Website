<%@ Page Title="Newsletter preferences | Yenetch" MetaDescription="Manage your Yenetch newsletter subscription." Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Unsubscribe.aspx.cs" Inherits="Yenetch.Web.Unsubscribe" %>
<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server"><meta name="robots" content="noindex" /></asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
<form id="form1" runat="server">
<section class="phero"><div class="wrap wrap--text">
    <span class="kicker">Newsletter</span>
    <asp:PlaceHolder ID="AskPanel" runat="server">
        <h1>Unsubscribe?</h1>
        <p class="phero__lead">We will stop sending newsletters and automatic emails to <b><asp:Literal ID="EmailText" runat="server" /></b>. You can subscribe again at any time.</p>
        <div class="actions"><asp:Button ID="ConfirmButton" runat="server" Text="Unsubscribe" CssClass="btn btn--blue" OnClick="ConfirmButton_Click" /><a class="btn btn--line" href="/">Keep me subscribed</a></div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="DonePanel" runat="server" Visible="false">
        <h1>You are unsubscribed.</h1>
        <p class="phero__lead">You will not receive the newsletter any more. Changed your mind?</p>
        <div class="actions"><asp:Button ID="ResubscribeButton" runat="server" Text="Subscribe again" CssClass="btn btn--line" OnClick="ResubscribeButton_Click" /><a class="btn btn--blue" href="/">Back to the website</a></div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="BackPanel" runat="server" Visible="false">
        <h1>Welcome back.</h1>
        <p class="phero__lead">You are subscribed again.</p>
        <div class="actions"><a class="btn btn--blue" href="/">Back to the website</a></div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="MissingPanel" runat="server" Visible="false">
        <h1>This link has expired.</h1>
        <p class="phero__lead">We could not find that subscription. Email <a href="mailto:leads@yenetch.com">leads@yenetch.com</a> and we will remove you by hand.</p>
    </asp:PlaceHolder>
</div></section>
</form>
</asp:Content>
