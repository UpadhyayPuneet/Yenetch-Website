<%@ Page Title="Tracking & scripts" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Tracking.aspx.cs" Inherits="Yenetch.Web.Admin.TrackingPage" ValidateRequest="false" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Tracking &amp; scripts</h1><p>Connect Google Analytics, Google Ads, Tag Manager, Meta, LinkedIn and Clarity, verify the site with Google and Bing, and add any other script.</p></div>
    <div class="page-head__actions"><button class="btn btn--blue" type="submit" name="save" value="1">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Google and ad platforms</h2></div>
            <div class="form-grid">
                <div><label class="label" for="ga4">Google Analytics 4 ID</label><input class="field mono" id="ga4" name="ga4" value="<%: T.Ga4 %>" placeholder="G-XXXXXXXXXX" maxlength="30" /><p class="fld__help">Runs after the visitor accepts analytics cookies.</p></div>
                <div><label class="label" for="gtm">Google Tag Manager ID</label><input class="field mono" id="gtm" name="gtm" value="<%: T.Gtm %>" placeholder="GTM-XXXXXXX" maxlength="20" /><p class="fld__help">Runs after analytics consent. Use either this or the IDs here, not both for the same tag.</p></div>
                <div><label class="label" for="ads">Google Ads ID</label><input class="field mono" id="ads" name="ads" value="<%: T.GoogleAds %>" placeholder="AW-123456789" maxlength="20" /><p class="fld__help">Conversion tracking and remarketing. Runs after marketing consent.</p></div>
                <div><label class="label" for="meta">Meta Pixel ID</label><input class="field mono" id="meta" name="meta" value="<%: T.MetaPixel %>" placeholder="123456789012345" maxlength="20" inputmode="numeric" /><p class="fld__help">Facebook and Instagram ads. Runs after marketing consent.</p></div>
                <div><label class="label" for="li">LinkedIn Partner ID</label><input class="field mono" id="li" name="li" value="<%: T.LinkedIn %>" placeholder="1234567" maxlength="12" inputmode="numeric" /><p class="fld__help">LinkedIn Insight Tag. Runs after marketing consent.</p></div>
                <div><label class="label" for="clarity">Microsoft Clarity ID</label><input class="field mono" id="clarity" name="clarity" value="<%: T.Clarity %>" placeholder="abcd1234ef" maxlength="16" /><p class="fld__help">Free heatmaps and session recordings. Runs after analytics consent.</p></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Site verification</h2></div>
            <div class="form-grid">
                <div><label class="label" for="gv">Google Search Console</label><input class="field mono" id="gv" name="gv" value="<%: T.GoogleVerify %>" maxlength="100" placeholder="Content value only" /><p class="fld__help">From the HTML tag method: paste only the part inside content="...".</p></div>
                <div><label class="label" for="bv">Bing Webmaster Tools</label><input class="field mono" id="bv" name="bv" value="<%: T.BingVerify %>" maxlength="64" placeholder="Content value only" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Custom scripts</h2><span class="muted small"><%= T.Scripts.Count %> added</span></div>
            <p class="muted small" style="margin:0 0 12px">For any other tool: chat widgets, heatmaps, conversion pixels. Paste the code the tool gives you.</p>
            <input type="hidden" name="count" value="<%= T.Scripts.Count + 1 %>" />
            <% for (var i = 0; i <= T.Scripts.Count; i++) { var s = i < T.Scripts.Count ? T.Scripts[i] : new CustomScript { Placement = "body-end", Category = "marketing", Enabled = true }; var isNew = i == T.Scripts.Count; %>
            <% if (isNew) { %><details class="items__card" style="margin-top:12px"<%= T.Scripts.Count == 0 ? " open" : "" %>><summary class="items__head" style="cursor:pointer"><b>+ Add a script</b></summary><% } else { %><div class="items__card" style="margin-top:12px"><div class="items__head"><b><%: s.Name %></b><span class="badge <%= s.Enabled ? "badge--won" : "badge--open" %>"><%= s.Enabled ? "On" : "Off" %></span></div><% } %>
            <div class="form-grid" style="padding:12px 0 0">
                <div><label class="label">Name</label><input class="field" name="s<%= i %>_name" value="<%: s.Name %>" maxlength="80" placeholder="For example Hotjar" /></div>
                <div><label class="label">Where on the page</label><select class="field" name="s<%= i %>_place"><% foreach (var p in PlaceNames) { %><option value="<%= p[0] %>"<%= s.Placement == p[0] ? " selected" : "" %>><%= p[1] %></option><% } %></select></div>
                <div><label class="label">Runs</label><select class="field" name="s<%= i %>_cat">
                    <option value="necessary"<%= s.Category == "necessary" ? " selected" : "" %>>Always (essential)</option>
                    <option value="analytics"<%= s.Category == "analytics" ? " selected" : "" %>>After analytics consent</option>
                    <option value="marketing"<%= s.Category == "marketing" ? " selected" : "" %>>After marketing consent</option></select></div>
                <div><label class="label">Only on these pages (optional)</label><textarea class="field" name="s<%= i %>_pages" rows="2" placeholder="/contact&#10;/services/*"><%: s.Pages %></textarea></div>
                <div class="span-2"><label class="label">Code</label><textarea class="field field--code" name="s<%= i %>_code" rows="6" spellcheck="false" placeholder="&lt;script&gt;...&lt;/script&gt;"><%: s.Code %></textarea></div>
                <div class="span-2"><label class="check"><input type="checkbox" name="s<%= i %>_on" value="1"<%= s.Enabled ? " checked" : "" %> /> Turned on</label>
                <% if (!isNew) { %><label class="check" style="margin-left:16px"><input type="checkbox" name="s<%= i %>_del" value="1" /> Remove this script</label><% } %></div>
            </div>
            <%= isNew ? "</details>" : "</div>" %>
            <% } %>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>How consent works</h2></div>
            <ul class="tips">
                <li><b>Essential</b> scripts run for everyone.</li>
                <li><b>Analytics</b> scripts run only after a visitor accepts analytics cookies in the cookie banner.</li>
                <li><b>Marketing</b> scripts run only after a visitor accepts marketing cookies. The banner shows this choice once a marketing tag is added.</li>
                <li>Scripts never run in the admin, so a broken script cannot lock you out.</li>
            </ul>
        </section>
        <section class="card">
            <div class="card__head"><h2>Related</h2></div>
            <ul class="tips">
                <li><a href="/admin/robots">robots.txt rules</a> decide what search engines may crawl.</li>
                <li><a href="/sitemap.xml" target="_blank" rel="noopener">sitemap.xml</a> lists every page automatically. Submit it in Google Search Console.</li>
            </ul>
        </section>
    </div>
</div>
</form>
</asp:Content>
