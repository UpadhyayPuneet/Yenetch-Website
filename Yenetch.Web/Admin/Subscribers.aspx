<%@ Page Title="Subscribers" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Subscribers.aspx.cs" Inherits="Yenetch.Web.Admin.Subscribers" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Newsletter subscribers</h1><p><%= N(ActiveCount) %> active · <%= N(NewThisMonth) %> joined in the last 30 days.</p></div>
    <div class="page-head__actions">
        <a class="btn btn--line" href="/Admin/Export.ashx?what=subscribers"><%= Icon("download") %>Export CSV</a>
        <a class="btn btn--blue" href="/admin/campaigns/new"><%= Icon("send") %>Write a newsletter</a>
    </div>
</div>

<form id="form1" runat="server">
<div class="grid grid--main">
    <div class="stack">
        <nav class="chips" aria-label="Status" style="margin:0">
            <a class="chip<%= Q("status") == "" ? " is-on" : "" %>" href="<%= Att(With("status", null)) %>">All <b><%= N(AllCount) %></b></a>
            <a class="chip<%= Q("status") == "Active" ? " is-on" : "" %>" href="<%= Att(With("status", "Active")) %>">Active <b><%= N(ActiveCount) %></b></a>
            <a class="chip<%= Q("status") == "Unsubscribed" ? " is-on" : "" %>" href="<%= Att(With("status", "Unsubscribed")) %>">Unsubscribed <b><%= N(AllCount - ActiveCount) %></b></a>
        </nav>
        <section class="card card--flush">
            <div class="filters" style="padding:14px 14px 0;margin:0">
                <input class="field search" type="search" name="q" form="search" value="<%= Att(Q("q")) %>" placeholder="Search email or name" aria-label="Search subscribers" />
                <button class="btn btn--line" type="submit" form="search">Search</button>
            </div>
            <% if (Rows.Count == 0) { %><div class="empty"><b>No subscribers yet</b>Signups from the website footer and blog land here.</div><% } else { %>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Email</th><th class="hide-sm">Source</th><th>Status</th><th>Joined</th><th class="right">Actions</th></tr></thead>
                <tbody>
                <% foreach (var s in Rows) { %>
                <tr>
                    <td><b><%: s.Email %></b><span class="cell-sub"><%: s.Name %></span></td>
                    <td class="hide-sm"><%: s.Source %></td>
                    <td><span class="badge <%= s.Status == "Active" ? "badge--won" : "badge--lost" %>"><%: s.Status %></span></td>
                    <td class="nowrap muted" title="<%: When(s.CreatedOn) %>"><%: Date(s.CreatedOn) %></td>
                    <td class="right nowrap">
                        <% if (s.Status == "Active") { %><button class="btn btn--line btn--sm" type="submit" name="unsub" value="<%= s.Id %>">Unsubscribe</button><% } else { %><button class="btn btn--line btn--sm" type="submit" name="resub" value="<%= s.Id %>" data-confirm="Only resubscribe people who asked to be added back. Continue?">Resubscribe</button><% } %>
                        <button class="btn btn--ghost btn--sm" type="submit" name="del" value="<%= s.Id %>" data-confirm="Delete <%: s.Email %> permanently?" aria-label="Delete"><%= Icon("trash") %></button>
                    </td>
                </tr>
                <% } %>
                </tbody>
            </table></div>
            <%= Pager(Total, PageSize) %>
            <% } %>
        </section>
    </div>
    <div class="stack">
        <section class="card">
            <div class="card__head"><h2>Add a subscriber</h2></div>
            <div class="stack" style="gap:10px">
                <div><label class="label" for="NewEmail">Email</label><asp:TextBox ID="NewEmail" runat="server" CssClass="field" MaxLength="160" inputmode="email" /></div>
                <div><label class="label" for="NewName">Name (optional)</label><asp:TextBox ID="NewName" runat="server" CssClass="field" MaxLength="120" /></div>
                <div><asp:Button ID="AddButton" runat="server" Text="Add" CssClass="btn" OnClick="AddButton_Click" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Import</h2></div>
            <p class="muted small" style="margin-bottom:10px">Paste emails or a CSV. Only add people who agreed to receive the newsletter.</p>
            <asp:TextBox ID="ImportText" runat="server" CssClass="field" TextMode="MultiLine" Rows="5" placeholder="one@example.com, two@example.com" />
            <div class="form-actions" style="margin-top:10px"><asp:Button ID="ImportButton" runat="server" Text="Import" CssClass="btn btn--line" OnClick="ImportButton_Click" /></div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Where they signed up</h2></div>
            <%= Bars(Sources, "No signups yet.") %>
        </section>
    </div>
</div>
</form>
<form id="search" method="get" action="/admin/subscribers"><% if (Q("status") != "") { %><input type="hidden" name="status" value="<%= Att(Q("status")) %>" /><% } %></form>
</asp:Content>
