<%@ Page Title="Security & backups" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Security.aspx.cs" Inherits="Yenetch.Web.Admin.SecurityPage" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Security &amp; backups</h1><p>Two-step sign-in, sign-in alerts and nightly backups of the website data.</p></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Sign-in protection</h2></div>
            <div class="stack" style="gap:10px">
                <label class="check check--top"><input type="checkbox" name="require2fa" value="1"<%= TwoFactor.RequiredForAll ? " checked" : "" %> /> <span><b>Require two-step sign-in for everyone</b><br /><span class="muted small">People without an authenticator app get a code by email until they set one up in My account.</span></span></label>
                <label class="check check--top"><input type="checkbox" name="alerts" value="1"<%= TwoFactor.Alerts ? " checked" : "" %> /> <span><b>Email a sign-in alert for a new browser</b><br /><span class="muted small">Each person is told when their account signs in from a browser it has not used before.</span></span></label>
                <label class="check check--top"><input type="checkbox" name="trust" value="1"<%= TwoFactor.AllowTrust ? " checked" : "" %> /> <span><b>Allow "Trust this browser for 30 days"</b><br /><span class="muted small">Skips the code on a person's own computer. Turn off for the strictest setting.</span></span></label>
                <div class="form-actions"><button class="btn btn--blue" type="submit" name="act" value="security">Save</button></div>
            </div>
        </section>

        <section class="card card--flush">
            <div class="card__head"><h2>Team sign-in status</h2></div>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Person</th><th>Role</th><th>Two-step</th><th>Last sign-in</th><th></th></tr></thead>
                <tbody>
                <% foreach (var u in People) { %>
                <tr>
                    <td><span class="cell-main"><%: u.Str("Name") %></span><span class="cell-sub"><%: u.Str("Email") %></span></td>
                    <td><%: u.Str("Role") %><%= u.Bool("IsActive") ? "" : " <span class=\"badge\">Inactive</span>" %></td>
                    <td><%= Badge(u) %></td>
                    <td><%: Util.When(u.DateN("LastLoginOn")) %><span class="cell-sub"><%: u.Str("LastLoginIp") %></span></td>
                    <td><% if (!string.IsNullOrEmpty(u.Str("TwoFactor"))) { %><button class="btn btn--sm" type="submit" name="reset" value="<%= u.Int("Id") %>" data-confirm="Turn off two-step sign-in for <%: u.Str("Name") %>? Use this when someone lost their phone and their backup codes. They can set it up again in My account.">Reset two-step</button><% } %></td>
                </tr>
                <% } %>
                </tbody>
            </table></div>
        </section>

        <section class="card">
            <div class="card__head"><h2>Backups</h2><% if (Backups.Running) { %><span class="badge badge--new">Backing up now…</span><% } %></div>
            <% if (!string.IsNullOrEmpty(Backups.LastError)) { %><div class="form-error" role="alert">The last backup failed: <%: Backups.LastError %></div><% } %>
            <div class="form-grid">
                <div><label class="check"><input type="checkbox" name="b_on" value="1"<%= Backups.Enabled ? " checked" : "" %> /> Back up every night</label></div>
                <div><label class="check"><input type="checkbox" name="b_files" value="1"<%= Backups.IncludeFiles ? " checked" : "" %> /> Include uploaded images, CVs and attachments</label></div>
                <div><label class="label" for="b_hour">Time (IST)</label><select class="field" id="b_hour" name="b_hour"><% for (var h = 0; h < 24; h++) { %><option value="<%= h %>"<%= h == Backups.Hour ? " selected" : "" %>><%= new DateTime(2000, 1, 1, h, 0, 0).ToString("h tt") %></option><% } %></select><p class="fld__help">A quiet hour, such as 2 AM.</p></div>
                <div><label class="label" for="b_keep">Keep the newest</label><select class="field" id="b_keep" name="b_keep"><% foreach (var k in new[] { 3, 7, 14, 30 }) { %><option value="<%= k %>"<%= k == Backups.Keep ? " selected" : "" %>><%= k %> backups</option><% } %></select><p class="fld__help">Older ones are deleted to save disk space.</p></div>
            </div>
            <div class="form-actions"><button class="btn btn--blue" type="submit" name="act" value="backup">Save</button><button class="btn" type="submit" name="act" value="now">Back up now</button></div>
            <% if (Files.Count > 0) { %>
            <table class="table" style="margin-top:12px"><thead><tr><th>Backup</th><th>Size</th><th></th></tr></thead><tbody>
            <% foreach (var f in Files) { %><tr><td><%: Util.When(f.CreatedOn) %><span class="cell-sub mono"><%: f.Name %></span></td><td><%: Backups.Size(f.Size) %></td>
                <td style="text-align:right;white-space:nowrap"><a class="btn btn--sm" href="/admin/backup?f=<%: f.Name %>"><%= Icon("download") %>Download</a> <button class="btn btn--sm btn--danger" type="submit" name="del" value="<%: f.Name %>" data-confirm="Delete this backup?">Delete</button></td></tr><% } %>
            </tbody></table>
            <% } else { %><p class="muted small" style="margin-top:12px">No backups yet. The first one runs tonight, or click Back up now.</p><% } %>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card" id="captcha">
            <div class="card__head"><h2>Spam protection (CAPTCHA)</h2><span class="badge <%= Guard.CaptchaOn ? "badge--won" : "badge--open" %>"><%= Guard.CaptchaOn ? "On" : "Off" %></span></div>
            <p class="muted small" style="margin:0 0 12px">Forms already block bots with a hidden trap field, a minimum fill time and a limit per visitor. A CAPTCHA adds a check on the contact form, booking, website audit, job applications and the plan builder.
                <b>Cloudflare Turnstile</b> is recommended: free, private, and usually invisible to people.</p>
            <div class="form-grid">
                <div class="span-2"><label class="label" for="capProvider">Provider</label><select class="field" id="capProvider" name="capProvider">
                    <option value=""<%= Guard.CaptchaProvider == "" ? " selected" : "" %>>Off</option>
                    <option value="turnstile"<%= Guard.CaptchaProvider == "turnstile" ? " selected" : "" %>>Cloudflare Turnstile (recommended)</option>
                    <option value="recaptcha"<%= Guard.CaptchaProvider == "recaptcha" ? " selected" : "" %>>Google reCAPTCHA v3</option></select></div>
                <div class="span-2"><label class="label" for="capSite">Site key</label><input class="field mono" id="capSite" name="capSite" maxlength="200" value="<%: Guard.CaptchaSiteKey %>" /></div>
                <div class="span-2"><label class="label" for="capSecret">Secret key</label><input class="field mono" id="capSecret" name="capSecret" type="password" autocomplete="new-password" maxlength="200" placeholder="<%= HasCaptchaSecret ? "Saved. Type to replace" : "" %>" /></div>
            </div>
            <p class="fld__help">Turnstile: Cloudflare dashboard &gt; Turnstile &gt; Add site (free, no Cloudflare hosting needed). reCAPTCHA: google.com/recaptcha/admin, choose v3. Add your domain in either.</p>
            <div class="form-actions"><button class="btn btn--line" type="submit" name="act" value="captcha">Save spam protection</button></div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Good habits</h2></div>
            <ul class="tips">
                <li>Ask everyone to turn on two-step sign-in with an authenticator app (free: Google or Microsoft Authenticator).</li>
                <li>Download a backup now and then and keep it somewhere safe, away from the web server.</li>
                <li>Keep your host's own SQL Server backups switched on in Plesk as well. They restore fastest.</li>
                <li>Turn off people who leave in <a href="/admin/team">Team</a>. Their sign-in stops at once.</li>
                <li>Use HTTPS on the live site. The site then tells browsers to always use HTTPS.</li>
            </ul>
        </section>
        <section class="card">
            <div class="card__head"><h2>Already protected</h2></div>
            <ul class="tips">
                <li>Passwords are stored as salted PBKDF2 hashes, never as text.</li>
                <li>Five wrong passwords or codes lock an account for 15 minutes.</li>
                <li>Uploaded CVs, attachments and backups are never served directly to the web.</li>
                <li>Admin pages are hidden from search engines and protected against cross-site form posts.</li>
                <li>Database queries use parameters, so typed text can never run as SQL.</li>
                <li>Text people type is encoded before it is shown; blog and page HTML from the editors is cleaned of scripts.</li>
                <li>Email addresses and phone numbers on the website are hidden from address-harvesting bots.</li>
                <li>Forms and the chat are rate limited per visitor; API keys and passwords are stored encrypted.</li>
            </ul>
        </section>
    </div>
</div>
</form>
</asp:Content>
