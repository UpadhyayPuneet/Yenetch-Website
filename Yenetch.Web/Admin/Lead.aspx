<%@ Page Title="Lead" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Lead.aspx.cs" Inherits="Yenetch.Web.Admin.LeadPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/leads"><%= Icon("back") %>Leads</a>

<% if (IsNew) { %>
<div class="page-head"><div><h1>Add a lead</h1><p>For enquiries that came by phone, WhatsApp, email, referral or an event.</p></div></div>
<% } else { %>
<div class="lead-head">
    <div class="lead-head__who">
        <span class="avatar avatar--blue"><%: Initials(L.Name) %></span>
        <div>
            <h1><%: L.Name %></h1>
            <div class="lead-head__meta">
                <%= StatusBadge(L.Status) %><span class="prio prio--<%= L.Priority %>"><%: L.Priority %></span>
                <span class="muted small">· <%: L.LeadType %> · <%: L.Source %><%: string.IsNullOrEmpty(L.Channel) ? "" : " (" + L.Channel + ")" %> · received <%: When(L.CreatedOn) %></span>
            </div>
        </div>
    </div>
    <div class="quick">
        <% if (!string.IsNullOrEmpty(L.Phone)) { %><a class="btn btn--line" href="tel:<%= Att(L.Phone) %>"><%= Icon("phone") %>Call</a><% } %>
        <% if (L.WhatsAppUrl != null) { %><a class="btn btn--line" href="<%= Att(L.WhatsAppUrl) %>" target="_blank" rel="noopener"><%= Icon("whatsapp") %>WhatsApp</a><% } %>
        <% if (!string.IsNullOrEmpty(L.Email)) { %><a class="btn btn--line" href="mailto:<%= Att(L.Email) %>"><%= Icon("mail") %>Email</a><% } %>
        <% if (L.Status == "Won" && !string.IsNullOrEmpty(L.Email)) { %><button class="btn btn--line" type="submit" name="askReview" value="1" data-confirm="Email <%: L.Name %> a request for a Google review now?"><%= Icon("star") %>Ask for a review</button><% } %>
    </div>
</div>

<div class="pipeline" role="group" aria-label="Move to stage">
    <% foreach (var s in Stages) { %><button type="submit" name="setStatus" value="<%= s %>" class="<%= StageCss(s) %>"<%= s == L.Status ? " aria-current=\"step\"" : "" %>><%= s %></button><% } %>
</div>
<% } %>

<div class="grid grid--main">
    <div class="stack">
        <% if (!IsNew) { %>
        <section class="card">
            <div class="card__head"><h2>Log activity</h2></div>
            <div class="log-form">
                <div class="kind-pick" role="radiogroup" aria-label="Activity type">
                    <% foreach (var k in Lists.ActivityKinds) { %><input type="radio" name="kind" id="kind-<%= k %>" value="<%= k %>"<%= k == "Note" ? " checked" : "" %> /><label for="kind-<%= k %>"><%= k %></label><% } %>
                </div>
                <textarea class="field" name="note" rows="3" placeholder="What happened, what they said, what was agreed" aria-label="Notes"></textarea>
                <div class="log-form__row">
                    <label class="label" for="due" style="margin:0">Next follow-up</label>
                    <input class="field" type="datetime-local" id="due" name="due" />
                    <input class="field" type="text" name="dueNote" placeholder="Follow-up task, e.g. Send proposal" aria-label="Follow-up task" />
                    <button class="btn btn--blue" type="submit" name="log" value="1">Save</button>
                </div>
            </div>
        </section>

        <section class="card">
            <div class="card__head"><h2>Timeline</h2><span class="muted small"><%= Timeline.Count %> entries</span></div>
            <div class="timeline">
            <% foreach (var a in Timeline) { %>
                <div class="tl <%= a.IsTask ? (a.DoneOn.HasValue ? "tl--done" : "tl--task") : a.Kind == "System" || a.Kind == "Status" || a.Kind == "Assign" ? "tl--sys" : "" %>">
                    <span class="tl__icon"><%= a.IsTask && a.DoneOn.HasValue ? Icon("check") : ActivityIcon(a.IsTask ? "FollowUp" : a.Kind) %></span>
                    <div>
                        <div class="tl__top"><b><%: a.IsTask ? "Follow-up" : a.Kind %></b><span><%: a.UserName ?? "Website" %></span><span title="<%: When(a.CreatedOn) %>"><%: Ago(a.CreatedOn) %></span>
                            <% if (a.IsTask) { %><%= a.DoneOn.HasValue ? "<span class=\"due\">Done " + H(Ago(a.DoneOn)) + "</span>" : DueLabel(a.DueOn) %><% } %></div>
                        <% if (!string.IsNullOrEmpty(a.Body)) { %><div class="tl__body"><%: a.Body %></div><% } %>
                        <% if (a.IsTask && !a.DoneOn.HasValue) { %><div class="tl__actions"><button class="btn btn--line btn--sm" type="submit" name="done" value="<%= a.Id %>"><%= Icon("check") %>Mark done</button></div><% } %>
                    </div>
                </div>
            <% } %>
            </div>
        </section>
        <% } %>

        <section class="card">
            <div class="card__head"><h2><%= IsNew ? "Details" : "Edit details" %></h2></div>
            <asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
            <div class="form-grid">
                <div><label class="label" for="LName">Name</label><asp:TextBox ID="LName" runat="server" CssClass="field" MaxLength="120" /></div>
                <div><label class="label" for="LCompany">Company</label><asp:TextBox ID="LCompany" runat="server" CssClass="field" MaxLength="160" /></div>
                <div><label class="label" for="LPhone">Phone</label><asp:TextBox ID="LPhone" runat="server" CssClass="field" MaxLength="40" inputmode="tel" autocomplete="tel" /></div>
                <div><label class="label" for="LEmail">Email</label><asp:TextBox ID="LEmail" runat="server" CssClass="field" MaxLength="160" inputmode="email" autocomplete="email" /></div>
                <div><label class="label" for="LCity">City</label><asp:TextBox ID="LCity" runat="server" CssClass="field" MaxLength="80" /></div>
                <div><label class="label" for="LInterest">Interested in</label><asp:TextBox ID="LInterest" runat="server" CssClass="field" MaxLength="160" placeholder="Service or product" /></div>
                <div class="span-2"><label class="label" for="LNeed">Requirement</label><asp:TextBox ID="LNeed" runat="server" CssClass="field" TextMode="MultiLine" Rows="4" MaxLength="2000" /></div>
                <div><label class="label" for="LType">Type</label><asp:DropDownList ID="LType" runat="server" CssClass="field" /></div>
                <div><label class="label" for="LSource">Source</label><asp:DropDownList ID="LSource" runat="server" CssClass="field" /></div>
                <div><label class="label" for="LStatus">Status</label><asp:DropDownList ID="LStatus" runat="server" CssClass="field" /></div>
                <div><label class="label" for="LPriority">Priority</label><asp:DropDownList ID="LPriority" runat="server" CssClass="field" /></div>
                <div><label class="label" for="LValue">Expected value (₹)</label><asp:TextBox ID="LValue" runat="server" CssClass="field" MaxLength="14" inputmode="numeric" /></div>
                <div><label class="label" for="LOwner">Owner</label><asp:DropDownList ID="LOwner" runat="server" CssClass="field" /></div>
                <div class="span-2"><label class="label" for="LTags">Tags</label><asp:TextBox ID="LTags" runat="server" CssClass="field" MaxLength="300" placeholder="Comma separated, e.g. referral, enterprise" /></div>
                <div class="span-2"><label class="label" for="LLostReason">Reason lost (if lost)</label><asp:TextBox ID="LLostReason" runat="server" CssClass="field" MaxLength="300" /></div>
                <% if (IsNew) { %>
                <div><label class="label" for="NewDue">First follow-up</label><input class="field" type="datetime-local" id="NewDue" name="newDue" /></div>
                <div><label class="label" for="NewNote">First note</label><input class="field" type="text" id="NewNote" name="newNote" placeholder="e.g. Called in about a new website" /></div>
                <% } %>
            </div>
            <div class="form-actions">
                <asp:Button ID="SaveButton" runat="server" CssClass="btn btn--blue" Text="Save" OnClick="SaveButton_Click" />
                <% if (!IsNew && Me.IsAdmin) { %><button class="btn btn--danger" type="submit" name="delete" value="1" data-confirm="Delete this lead and its timeline? This cannot be undone."><%= Icon("trash") %>Delete lead</button><% } %>
            </div>
        </section>
    </div>

    <% if (!IsNew) { %>
    <div class="stack">
        <section class="card">
            <div class="card__head"><h2>Summary</h2></div>
            <dl class="facts">
                <div><dt>Phone</dt><b><%: L.Phone ?? "—" %></b></div>
                <div><dt>Email</dt><b><%: L.Email ?? "—" %></b></div>
                <div><dt>Owner</dt><b><%: L.AssignedName ?? "Unassigned" %></b></div>
                <div><dt>Next follow-up</dt><b><%= L.NextFollowUp.HasValue ? DueLabel(L.NextFollowUp) : "None scheduled" %></b></div>
                <div><dt>Value</dt><b><%: L.EstValue.HasValue ? Money(L.EstValue) : "—" %></b></div>
                <div><dt>Form page</dt><b><%: L.Page ?? "—" %></b></div>
                <div><dt>Last updated</dt><b><%: Ago(L.UpdatedOn) %></b></div>
            </dl>
        </section>
        <% if (Files.Count > 0) { %>
        <section class="card">
            <div class="card__head"><h2>Attached files</h2></div>
            <ul class="files">
            <% foreach (var f in Files) { %><li><a href="/Admin/Attachment.ashx?id=<%= f.Id %>" target="_blank" rel="noopener"><%= Icon("download") %><span><b><%: f.FileName %></b><small><%: f.SizeLabel %></small></span></a></li><% } %>
            </ul>
        </section>
        <% } %>

        <section class="card">
            <div class="card__head"><h2>Website journey</h2><% if (Visitor != null) { %><a href="/admin/visitors/<%= Visitor.Id %>">Full journey</a><% } %></div>
            <% if (Visitor == null) { %>
            <p class="muted small">No browsing history. Either the lead was added by hand, or the visitor did not allow analytics cookies.</p>
            <% } else { %>
            <dl class="facts">
                <div><dt>First came from</dt><b><%: Visitor.Channel %><%: string.IsNullOrEmpty(Visitor.Source) ? "" : " · " + Visitor.Source %></b></div>
                <div><dt>Landing page</dt><b><%: Visitor.Landing %></b></div>
                <div><dt>Visits</dt><b><%= Visitor.Sessions %> visits, <%= Visitor.Pageviews %> pages</b></div>
                <div><dt>Location</dt><b><%: string.IsNullOrEmpty(Visitor.Place) ? "Unknown" : Visitor.Place %></b></div>
                <div><dt>Device</dt><b><%: Visitor.Device %> · <%: Visitor.Os %> · <%: Visitor.Browser %></b></div>
                <div><dt>First seen</dt><b><%: When(Visitor.FirstSeen) %></b></div>
            </dl>
            <% if (PagesBefore.Count > 0) { %>
            <h3 style="margin:16px 0 8px">Pages viewed before enquiring</h3>
            <ol class="steps" style="padding:0">
                <% foreach (var p in PagesBefore) { %><li class="step"><time><%: Util.Time(p.At) %></time><i></i><span><b><%: p.Title ?? p.Path %></b><small><%: p.Path %></small></span><span class="step__stats"><%= p.Seconds.HasValue ? Dur(p.Seconds.Value) : "" %></span></li><% } %>
            </ol>
            <% } %>
            <% } %>
        </section>

        <% if (!string.IsNullOrEmpty(ContextText)) { %>
        <section class="card">
            <div class="card__head"><h2>Answers from the website</h2></div>
            <div class="context"><%: ContextText %></div>
        </section>
        <% } %>
    </div>
    <% } %>
</div>
</form>
</asp:Content>
