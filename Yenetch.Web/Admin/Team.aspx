<%@ Page Title="Team" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Team.aspx.cs" Inherits="Yenetch.Web.Admin.Team" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>Team</h1><p>Who can sign in to the admin, and what they can see.</p></div>
</div>

<form id="form1" runat="server">
<div class="grid grid--main">
    <div class="stack">
        <section class="card card--flush">
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Name</th><th>Role</th><th class="right">Open leads</th><th class="hide-sm">Last sign-in</th><th class="right">Actions</th></tr></thead>
                <tbody>
                <% foreach (var u in Users) { %>
                <tr<%= u.IsActive ? "" : " class=\"muted\"" %>>
                    <td><div class="who"><span class="avatar<%= u.Id == Me.Id ? " avatar--blue" : "" %>"><%: u.Initials %></span><span><b><%: u.Name %></b><%= u.Id == Me.Id ? " <small class=\"muted\">(you)</small>" : "" %><span class="cell-sub"><%: u.Email %></span></span></div></td>
                    <td><%: u.Role %><% if (!u.IsActive) { %> <span class="badge badge--lost">Off</span><% } %></td>
                    <td class="right"><a href="/admin/leads?view=all&amp;owner=<%= u.Id %>"><%= N(u.OpenLeads) %></a></td>
                    <td class="hide-sm nowrap muted" title="<%: When(u.LastLoginOn) %>"><%: u.LastLoginOn.HasValue ? Ago(u.LastLoginOn) : "Never" %></td>
                    <td class="right"><a class="btn btn--line btn--sm" href="/admin/team?edit=<%= u.Id %>">Edit</a></td>
                </tr>
                <% } %>
                </tbody>
            </table></div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Roles</h2></div>
            <div class="table-wrap"><table class="table">
                <tbody>
                    <tr><td class="nowrap"><b>Admin</b></td><td>Everything, including the team, analytics and the newsletter.</td></tr>
                    <tr><td class="nowrap"><b>Manager</b></td><td>All leads, assigning, reports, analytics, visitors and the newsletter. Cannot manage the team.</td></tr>
                    <tr><td class="nowrap"><b>Sales</b></td><td>Their own leads and unassigned leads, follow-ups and the leads report. No analytics or newsletter.</td></tr>
                </tbody>
            </table></div>
        </section>
    </div>
    <div class="stack">
        <asp:Panel ID="EditPanel" runat="server" CssClass="card" Visible="false">
            <div class="card__head"><h2>Edit <%: Editing == null ? "" : Editing.Name %></h2><a href="/admin/team">Cancel</a></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="EditName">Name</label><asp:TextBox ID="EditName" runat="server" CssClass="field" MaxLength="120" /></div>
                <div><label class="label">Email</label><div class="muted"><%: Editing == null ? "" : Editing.Email %></div></div>
                <div><label class="label" for="EditRole">Role</label><asp:DropDownList ID="EditRole" runat="server" CssClass="field" /></div>
                <div><asp:CheckBox ID="EditActive" runat="server" CssClass="check" Text="Can sign in" /></div>
                <div><label class="label" for="EditPassword">New temporary password (optional)</label><asp:TextBox ID="EditPassword" runat="server" CssClass="field" MaxLength="100" autocomplete="new-password" />
                    <p class="muted small" style="margin-top:6px">At least 10 characters with letters and a number. Share it privately and ask them to change it under My account.</p></div>
                <div class="form-actions"><asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn btn--blue" OnClick="SaveButton_Click" /></div>
            </div>
        </asp:Panel>
        <asp:Panel ID="AddPanel" runat="server" CssClass="card">
            <div class="card__head"><h2>Add a teammate</h2></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="NewName">Name</label><asp:TextBox ID="NewName" runat="server" CssClass="field" MaxLength="120" /></div>
                <div><label class="label" for="NewEmail">Work email</label><asp:TextBox ID="NewEmail" runat="server" CssClass="field" MaxLength="160" inputmode="email" autocomplete="off" /></div>
                <div><label class="label" for="NewRole">Role</label><asp:DropDownList ID="NewRole" runat="server" CssClass="field" /></div>
                <div><label class="label" for="NewPassword">Temporary password</label><asp:TextBox ID="NewPassword" runat="server" CssClass="field" MaxLength="100" autocomplete="new-password" />
                    <p class="muted small" style="margin-top:6px">At least 10 characters with letters and a number. They can change it after signing in.</p></div>
                <div class="form-actions"><asp:Button ID="AddButton" runat="server" Text="Add teammate" CssClass="btn btn--blue" OnClick="AddButton_Click" /></div>
            </div>
        </asp:Panel>
    </div>
</div>
</form>
</asp:Content>
