<%@ Page Title="Your booked call | Yenetch" MetaDescription="Manage your call with Yenetch." Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BookCancel.aspx.cs" Inherits="Yenetch.Web.BookCancel" %>
<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server"><meta name="robots" content="noindex" /></asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
<section class="phero"><div class="wrap wrap--text">
    <span class="kicker">Your call</span>
    <% if (B == null) { %>
        <h1>This link has expired.</h1>
        <p class="phero__lead">We could not find that booking. Call or WhatsApp us and we will sort it out, or pick a new time.</p>
        <div class="actions"><a class="btn btn--blue" href="/book">Book a call</a><a class="btn btn--line" href="/contact">Contact us</a></div>
    <% } else if (Done) { %>
        <h1>Your call is cancelled.</h1>
        <p class="phero__lead">We have let the team know. Would another time work better?</p>
        <div class="actions"><a class="btn btn--blue" href="/book?topic=<%: HttpUtility.UrlEncode(B.Topic ?? "") %>">Pick another time</a><a class="btn btn--line" href="/">Back to the website</a></div>
    <% } else if (B.Status == "Confirmed" && B.StartOn > DateTime.UtcNow) { %>
        <h1>See you <%: Yenetch.Crm.Util.Ist(B.StartOn).ToString("dddd d MMMM", Yenetch.Crm.Util.India) %>.</h1>
        <p class="phero__lead"><b><%: B.WhenText %></b><br /><%: B.Mode %><%: string.IsNullOrEmpty(B.Topic) ? "" : " · " + B.Topic %></p>
        <form method="post" class="actions">
            <input type="hidden" name="t" value="<%: B.Token %>" />
            <a class="btn btn--blue" href="/">Keep my call</a>
            <button class="btn btn--line" type="submit" name="cancel" value="1">Cancel this call</button>
        </form>
        <p class="cform__note" style="margin-top:16px">Need a different time? Cancel this one and pick a new slot. It takes a minute.</p>
    <% } else { %>
        <h1>This call is <%: B.Status == "Confirmed" ? "over" : B.Status.ToLowerInvariant() %>.</h1>
        <p class="phero__lead"><%: B.WhenText %></p>
        <div class="actions"><a class="btn btn--blue" href="/book">Book a new call</a><a class="btn btn--line" href="/contact">Contact us</a></div>
    <% } %>
</div></section>
</asp:Content>
