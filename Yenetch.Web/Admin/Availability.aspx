<%@ Page Title="Availability" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Availability.aspx.cs" Inherits="Yenetch.Web.Admin.AvailabilityPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/bookings"><%= Icon("back") %>Bookings</a>
<div class="page-head">
    <div><h1>Availability</h1><p>When visitors can book a call on <a href="/book" target="_blank" rel="noopener">/book</a>. All times are India time (IST).</p></div>
    <div class="page-head__actions"><button class="btn btn--blue" type="submit" name="save" value="1">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Booking</h2><label class="check"><input type="checkbox" name="enabled" value="1"<%= S.Enabled ? " checked" : "" %> /> Online booking is on</label></div>
            <div class="form-grid">
                <div><label class="label" for="slot">Call length</label><select class="field" id="slot" name="slot"><% foreach (var m in new[] { 15, 20, 30, 45, 60, 90 }) { %><option value="<%= m %>"<%= m == S.SlotMinutes ? " selected" : "" %>><%= m %> minutes</option><% } %></select></div>
                <div><label class="label" for="buffer">Break between calls</label><select class="field" id="buffer" name="buffer"><% foreach (var m in new[] { 0, 5, 10, 15, 30, 60 }) { %><option value="<%= m %>"<%= m == S.BufferMinutes ? " selected" : "" %>><%= m == 0 ? "No break" : m + " minutes" %></option><% } %></select></div>
                <div><label class="label" for="notice">Minimum notice</label><select class="field" id="notice" name="notice"><% foreach (var h in new[] { 0, 1, 2, 4, 12, 24, 48 }) { %><option value="<%= h %>"<%= h == S.MinNoticeHours ? " selected" : "" %>><%= h == 0 ? "None" : h + (h == 1 ? " hour" : " hours") %></option><% } %></select><p class="fld__help">How soon someone can book from now.</p></div>
                <div><label class="label" for="ahead">Book up to</label><select class="field" id="ahead" name="ahead"><% foreach (var d in new[] { 7, 14, 21, 30, 45, 60, 90 }) { %><option value="<%= d %>"<%= d == S.DaysAhead ? " selected" : "" %>><%= d %> days ahead</option><% } %></select></div>
                <div><label class="label" for="max">Most calls in a day</label><input class="field" id="max" name="max" inputmode="numeric" maxlength="2" value="<%= S.MaxPerDay %>" /><p class="fld__help">0 means no limit.</p></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Weekly hours</h2></div>
            <p class="muted small" style="margin:0 0 12px">For a lunch break, write two ranges: 10:00-13:00, 14:00-18:30.</p>
            <div class="week">
            <% for (var i = 1; i <= 7; i++) { var d = i % 7; var w = S.Week[d]; %>
                <div class="week__day"><label class="check"><input type="checkbox" name="on<%= d %>" value="1"<%= w.On ? " checked" : "" %> /> <b><%= DayNames[d] %></b></label>
                    <input class="field" name="hours<%= d %>" value="<%: w.Hours %>" placeholder="10:00-18:00" aria-label="<%= DayNames[d] %> hours" /></div>
            <% } %>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Days off</h2></div>
            <textarea class="field mono" name="closed" rows="5" placeholder="2026-10-20&#10;2026-12-24 to 2026-12-31"><%: S.Closed %></textarea>
            <p class="fld__help">One per line: a date (2026-10-20) or a range (2026-12-24 to 2026-12-31). Holidays, team offsites, leave.</p>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Right now</h2></div>
            <p style="margin:0"><b><%= OpenDays %></b> days with <b><%= OpenSlots %></b> open times in the next <%= S.DaysAhead %> days.</p>
            <% if (NextSlot != null) { %><p class="muted small" style="margin:6px 0 0">Next open time: <%: NextSlot %> IST.</p><% } %>
        </section>
        <section class="card">
            <div class="card__head"><h2>Call details</h2></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="modes">Ways to meet</label><textarea class="field" id="modes" name="modes" rows="3"><%: S.Modes %></textarea><p class="fld__help">One per line. The visitor picks one.</p></div>
                <div><label class="label" for="link">Meeting link (optional)</label><input class="field" id="link" name="link" value="<%: S.MeetingLink %>" placeholder="https://meet.google.com/..." /><p class="fld__help">Added to the confirmation and calendar invite for video calls.</p></div>
                <div><label class="label" for="notify">Send booking alerts to</label><input class="field" id="notify" name="notify" value="<%: S.NotifyTo %>" placeholder="Empty: the lead inbox in Email settings" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Booking page text</h2></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="heading">Heading</label><input class="field" id="heading" name="heading" maxlength="120" value="<%: S.Heading %>" /></div>
                <div><label class="label" for="intro">Introduction</label><textarea class="field" id="intro" name="intro" rows="4" maxlength="600"><%: S.Intro %></textarea></div>
            </div>
        </section>
    </div>
</div>
</form>
</asp:Content>
