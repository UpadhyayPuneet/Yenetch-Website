<%@ Page Title="Application" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Application.aspx.cs" Inherits="Yenetch.Web.Admin.ApplicationPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<a class="crumb" href="/admin/applications"><%= Icon("back") %>Applications</a>
<div class="page-head">
    <div><h1><%: A.Name %></h1><p><%: A.Job %><%: string.IsNullOrEmpty(A.Category) ? "" : " · " + A.Category %> · applied <%: Ago(A.CreatedOn) %> · <span class="badge badge--<%= Yenetch.Web.Admin.ApplicationsPage.Css(A.Status) %>"><%: A.Status %></span></p></div>
    <div class="page-head__actions">
        <% if (!string.IsNullOrEmpty(A.Phone)) { %><a class="btn btn--line" href="tel:<%: A.Phone %>"><%= Icon("phone") %>Call</a><a class="btn btn--line" href="<%: WhatsApp %>" target="_blank" rel="noopener"><%= Icon("whatsapp") %>WhatsApp</a><% } %>
        <% if (A.HasFile) { %><a class="btn btn--blue" href="/Admin/Resume.ashx?id=<%= A.Id %>" target="_blank" rel="noopener"><%= Icon("note") %>Open CV</a><% } %>
    </div>
</div>
<asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>

<div class="grid grid--main">
    <div class="stack">
        <section class="card">
            <div class="card__head"><h2>Candidate</h2></div>
            <dl class="facts">
                <div><dt>Email</dt><b><a href="mailto:<%: A.Email %>"><%: A.Email %></a></b></div>
                <div><dt>Phone</dt><b><%: A.Phone ?? "—" %></b></div>
                <div><dt>Experience</dt><b><%: A.ExperienceLabel %></b></div>
                <div><dt>City</dt><b><%: A.City ?? "—" %></b></div>
                <div><dt>Notice period</dt><b><%: A.Notice ?? "—" %></b></div>
                <div><dt>Key skills</dt><b><%: A.Skills ?? "—" %></b></div>
                <div><dt>CV</dt><b><% if (A.HasFile) { %><a href="/Admin/Resume.ashx?id=<%= A.Id %>" target="_blank" rel="noopener"><%: A.FileName %></a> <span class="muted">(<%: A.SizeLabel %>)</span> · <a href="/Admin/Resume.ashx?id=<%= A.Id %>&amp;download=1">Download</a><% } else { %>—<% } %></b></div>
                <div><dt>CV link</dt><b><% if (!string.IsNullOrEmpty(A.ResumeUrl)) { %><a href="<%: A.ResumeUrl %>" target="_blank" rel="noopener nofollow"><%: Yenetch.Crm.Util.Cut(A.ResumeUrl, 60) %></a><% } else { %>—<% } %></b></div>
                <div><dt>LinkedIn / portfolio</dt><b><% if (!string.IsNullOrEmpty(A.Portfolio)) { %><a href="<%: A.Portfolio %>" target="_blank" rel="noopener nofollow"><%: Yenetch.Crm.Util.Cut(A.Portfolio, 60) %></a><% } else { %>—<% } %></b></div>
                <div><dt>Applied</dt><b><%: When(A.CreatedOn) %></b></div>
            </dl>
            <% if (!string.IsNullOrWhiteSpace(A.Note)) { %><div class="card__note"><b>Their note</b><p><%= H(A.Note).Replace("\n", "<br>") %></p></div><% } %>
        </section>

        <section class="card">
            <div class="card__head"><h2>Email <%: A.Name.Split(' ')[0] %></h2></div>
            <p class="muted small" style="margin-bottom:10px">Sent from your company address with the Yenetch layout. Replies come to you. Start from a template:</p>
            <div class="chips" style="margin:0 0 12px" data-mail-templates>
                <button class="chip" type="button" data-t="shortlist">Shortlisted</button>
                <button class="chip" type="button" data-t="interview">Invite to interview</button>
                <button class="chip" type="button" data-t="reject">Not a fit</button>
            </div>
            <div class="stack" style="gap:10px">
                <div><label class="label" for="Subject">Subject</label><asp:TextBox ID="Subject" runat="server" CssClass="field" MaxLength="200" /></div>
                <div><label class="label" for="Message">Message</label><asp:TextBox ID="Message" runat="server" CssClass="field" TextMode="MultiLine" Rows="9" /></div>
                <div class="form-actions"><asp:Button ID="SendButton" runat="server" Text="Send email" CssClass="btn btn--blue" OnClick="SendButton_Click" /></div>
            </div>
        </section>
    </div>

    <div class="stack">
        <section class="card">
            <div class="card__head"><h2>Review</h2></div>
            <div class="stack" style="gap:10px">
                <div><label class="label" for="StatusList">Status</label><asp:DropDownList ID="StatusList" runat="server" CssClass="field" /></div>
                <div><label class="label" for="RatingList">Rating</label><asp:DropDownList ID="RatingList" runat="server" CssClass="field" /></div>
                <div><label class="label" for="Notes">Team notes (only visible here)</label><asp:TextBox ID="Notes" runat="server" CssClass="field" TextMode="MultiLine" Rows="8" /></div>
                <div class="form-actions"><asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn" OnClick="SaveButton_Click" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Delete</h2></div>
            <p class="muted small" style="margin-bottom:10px">Removes the application and the CV file. Use this when a candidate asks you to delete their data.</p>
            <asp:Button ID="DeleteButton" runat="server" Text="Delete application" CssClass="btn btn--line" OnClick="DeleteButton_Click" data-confirm="Delete this application and its CV permanently?" />
        </section>
    </div>
</div>
</form>
<script>
(function () {
  var first = <%= Js(A.Name.Split(' ')[0]) %>, job = <%= Js(A.Job) %>, co = <%= Js(Company) %>;
  var t = {
    shortlist: ["Your application for " + job + " at " + co, "Hi " + first + ",\n\nThank you for applying for the " + job + " role. We liked your profile and have shortlisted you for the next step.\n\nWe will be in touch shortly with details. In the meantime, feel free to reply with any questions.\n\nBest regards,\nTeam " + co],
    interview: ["Interview invitation: " + job + " at " + co, "Hi " + first + ",\n\nWe would like to invite you to an interview for the " + job + " role.\n\nPlease reply with two or three times that suit you over the next few days, and let us know whether you prefer a video call or meeting at our office.\n\nBest regards,\nTeam " + co],
    reject: ["Your application for " + job + " at " + co, "Hi " + first + ",\n\nThank you for your interest in " + co + " and for the time you put into your application for the " + job + " role.\n\nAfter careful review, we have decided not to move forward at this time. We will keep your details on file and reach out if a suitable role opens up.\n\nWe wish you all the best.\n\nTeam " + co]
  };
  document.querySelector("[data-mail-templates]").addEventListener("click", function (e) {
    var b = e.target.closest("[data-t]"); if (!b) return;
    document.getElementById("Subject").value = t[b.getAttribute("data-t")][0];
    document.getElementById("Message").value = t[b.getAttribute("data-t")][1];
    document.getElementById("Message").focus();
  });
})();
</script>
</asp:Content>
