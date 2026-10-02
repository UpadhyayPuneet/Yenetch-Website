<%@ Page Title="My account" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Account.aspx.cs" Inherits="Yenetch.Web.Admin.Account" %>
<%@ Import Namespace="Yenetch.Crm" %>
<%@ Import Namespace="System.Linq" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<div class="page-head">
    <div><h1>My account</h1><p><%: Me.Email %> · <%: Me.Role %></p></div>
</div>

<form id="form1" runat="server">
<div class="grid grid--2">
    <section class="card">
        <div class="card__head"><h2>Your name</h2></div>
        <div class="stack" style="gap:12px">
            <div><label class="label" for="MyName">Name shown to the team</label><asp:TextBox ID="MyName" runat="server" CssClass="field" MaxLength="120" /></div>
            <div class="form-actions"><asp:Button ID="NameButton" runat="server" Text="Save name" CssClass="btn" OnClick="NameButton_Click" /></div>
        </div>
    </section>
    <section class="card">
        <div class="card__head"><h2>Change password</h2></div>
        <div class="stack" style="gap:12px">
            <div><label class="label" for="CurrentPassword">Current password</label><asp:TextBox ID="CurrentPassword" runat="server" CssClass="field" TextMode="Password" autocomplete="current-password" /></div>
            <div><label class="label" for="NewPassword">New password</label><asp:TextBox ID="NewPassword" runat="server" CssClass="field" TextMode="Password" autocomplete="new-password" /></div>
            <div><label class="label" for="ConfirmPassword">New password again</label><asp:TextBox ID="ConfirmPassword" runat="server" CssClass="field" TextMode="Password" autocomplete="new-password" /></div>
            <p class="muted small">At least 10 characters with letters and a number.</p>
            <div class="form-actions"><asp:Button ID="PasswordButton" runat="server" Text="Change password" CssClass="btn btn--blue" OnClick="PasswordButton_Click" /></div>
        </div>
    </section>
</div>

<section class="card" id="two-step" style="margin-top:16px">
    <div class="card__head"><h2>Two-step sign-in</h2>
        <% if (TfMethod == "app") { %><span class="badge badge--won">On · authenticator app</span><% } else if (TfMethod == "email") { %><span class="badge badge--won">On · email codes</span><% } else { %><span class="badge badge--open">Off</span><% } %></div>
    <% if (TfError != null) { %><div class="form-error" role="alert"><%: TfError %></div><% } %>

    <% if (NewCodes != null) { %>
    <div class="tf-codes">
        <p><b>Save your backup codes now.</b> Each one works once if you lose your phone. They will not be shown again.</p>
        <ol class="tf-codes__list mono"><% foreach (var c in NewCodes) { %><li><%: c %></li><% } %></ol>
        <div class="form-actions"><button class="btn" type="button" data-copy="<%: string.Join("\n", NewCodes) %>">Copy codes</button><button class="btn" type="button" onclick="window.print()">Print</button></div>
    </div>
    <% } else if (SetupSecret != null) { %>
    <div class="tf-setup">
        <div class="tf-setup__qr" data-qr="<%: SetupUri %>" aria-label="QR code for your authenticator app"></div>
        <div class="stack" style="gap:12px">
            <ol class="tips tips--num">
                <li>Install a free authenticator app: Google Authenticator, Microsoft Authenticator or Authy.</li>
                <li>In the app, tap <b>+</b> and scan this QR code. Can't scan? Enter this key: <code class="mono tf-key"><%: Grouped(SetupSecret) %></code></li>
                <li>Type the 6-digit code the app shows.</li>
            </ol>
            <input type="hidden" name="tf_secret" value="<%: ProtectedSecret %>" />
            <div><label class="label" for="tf_code">Code from the app</label><input class="field otp" id="tf_code" name="tf_code" inputmode="numeric" autocomplete="one-time-code" maxlength="7" placeholder="123456" /></div>
            <div class="form-actions"><button class="btn btn--blue" type="submit" name="tf" value="confirm">Turn on</button><a class="btn" href="/admin/account#two-step">Cancel</a></div>
        </div>
    </div>
    <% } else if (TfMethod == null) { %>
    <p class="muted">After your password, you also enter a 6-digit code from your phone. Even if someone learns your password, they cannot sign in.<%= TwoFactor.RequiredForAll ? " Your admin requires a second step, so until you set up an app we email you a code." : "" %></p>
    <div class="form-actions"><button class="btn btn--blue" type="submit" name="tf" value="start">Set up authenticator app</button><button class="btn" type="submit" name="tf" value="email">Use email codes instead</button></div>
    <p class="muted small">An authenticator app is free, works without internet and is the most secure option. Email codes are simpler but only as safe as your mailbox.</p>
    <% } else { %>
    <p class="muted"><%= TfMethod == "app" ? "You enter a code from your authenticator app after your password. You have <b>" + BackupLeft + "</b> backup code" + (BackupLeft == 1 ? "" : "s") + " left." : "We email you a 6-digit code after your password." %></p>
    <div class="form-grid" style="max-width:640px">
        <div><label class="label" for="tf_pw">Your password, to make changes</label><input class="field" type="password" id="tf_pw" name="tf_pw" autocomplete="current-password" /></div>
    </div>
    <div class="form-actions">
        <% if (TfMethod == "app") { %><button class="btn" type="submit" name="tf" value="codes">New backup codes</button><% } else { %><button class="btn btn--blue" type="submit" name="tf" value="start">Switch to authenticator app</button><% } %>
        <button class="btn btn--danger" type="submit" name="tf" value="off"><%= TwoFactor.RequiredForAll && TfMethod == "app" ? "Switch to email codes" : "Turn off" %></button>
    </div>
    <% } %>

    <% if (DeviceList.Count > 0) { %>
    <h3 class="sub-h">Browsers you signed in from</h3>
    <table class="table table--compact"><thead><tr><th>Browser</th><th>Place</th><th>Last used</th><th></th></tr></thead><tbody>
    <% foreach (var d in DeviceList) { %><tr><td><%: d.Name %></td><td><%: d.Place ?? d.Ip %></td><td><%: Util.When(d.LastOn) %></td><td><%= d.TrustedUntil.HasValue && d.TrustedUntil.Value > DateTime.UtcNow ? "<span class=\"badge\">Trusted</span>" : "" %></td></tr><% } %>
    </tbody></table>
    <% if (DeviceList.Any(d => d.TrustedUntil.HasValue && d.TrustedUntil.Value > DateTime.UtcNow)) { %><div class="form-actions"><button class="btn" type="submit" name="tf" value="forget">Ask for a code on every browser again</button></div><% } %>
    <% } %>
</section>
</form>
<script src="/assets/admin/qrcode.js"></script>
</asp:Content>
