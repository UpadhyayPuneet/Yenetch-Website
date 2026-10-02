<%@ Page Title="Booking" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="BookingDetail.aspx.cs" Inherits="Yenetch.Web.Admin.BookingDetailPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/bookings"><%= Icon("back") %>Bookings</a>
<div class="page-head">
    <div><h1><%: B.Name %></h1><p><%: B.WhenText %> · <%: B.Mode %></p></div>
    <div class="page-head__actions"><% if (B.LeadId.HasValue) { %><a class="btn btn--blue" href="/admin/leads/<%= B.LeadId %>">Open lead</a><% } %></div>
</div>
<div class="grid grid--main">
    <section class="card">
        <div class="card__head"><h2>Details</h2><span class="badge <%= Css(B.Status) %>"><%: B.Status %></span></div>
        <dl class="facts">
            <div><dt>When</dt><b><%: B.WhenText %> (<%= (int)(B.EndOn - B.StartOn).TotalMinutes %> minutes)</b></div>
            <div><dt>Email</dt><b><a href="mailto:<%: B.Email %>"><%: B.Email %></a></b></div>
            <% if (!string.IsNullOrEmpty(B.Phone)) { %><div><dt>Phone</dt><b><a href="tel:<%: B.Phone %>"><%: B.Phone %></a></b></div><% } %>
            <% if (!string.IsNullOrEmpty(B.Company)) { %><div><dt>Company</dt><b><%: B.Company %></b></div><% } %>
            <div><dt>Topic</dt><b><%: B.Topic %></b></div>
            <div><dt>How</dt><b><%: B.Mode %></b></div>
            <% if (!string.IsNullOrEmpty(B.Notes)) { %><div><dt>Their note</dt><b style="white-space:pre-line"><%: B.Notes %></b></div><% } %>
            <div><dt>Booked</dt><b><%: Util.When(B.CreatedOn) %></b></div>
        </dl>
    </section>
    <section class="card">
        <div class="card__head"><h2>Update</h2></div>
        <div class="stack" style="gap:12px">
            <div><label class="label" for="status">Status</label><select class="field" id="status" name="status"><% foreach (var s in Bookings.Statuses) { %><option<%= s == B.Status ? " selected" : "" %>><%= s %></option><% } %></select>
                <p class="fld__help">Cancelling emails the visitor with a link to book again and frees the slot.</p></div>
            <div><label class="label" for="notes">Notes for the team</label><textarea class="field" id="notes" name="notes" rows="5"><%: B.AdminNotes %></textarea></div>
            <div class="form-actions"><button class="btn btn--blue" type="submit" name="save" value="1">Save</button></div>
        </div>
    </section>
</div>
</form>
</asp:Content>
