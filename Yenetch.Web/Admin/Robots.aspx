<%@ Page Title="robots.txt" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Robots.aspx.cs" Inherits="Yenetch.Web.Admin.RobotsPage" ValidateRequest="false" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>robots.txt</h1><p>Tell search engines and other crawlers which parts of the site they may visit. Served at <a href="/robots.txt" target="_blank" rel="noopener">/robots.txt</a>.</p></div>
    <div class="page-head__actions">
        <button class="btn btn--line" type="submit" name="reset" value="1" data-confirm="Replace your rules with the recommended ones?">Use recommended</button>
        <button class="btn btn--blue" type="submit" name="save" value="1">Save</button>
    </div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Quick settings</h2></div>
            <div class="stack" style="gap:10px">
                <label class="check"><input type="checkbox" name="test" value="1"<%= R.HideTestSites ? " checked" : "" %> /> Hide test copies from search engines <span class="muted small">(any address other than <%: Seo.Root %>, for example test.yenetch.com)</span></label>
                <label class="check"><input type="checkbox" name="ai" value="1"<%= R.BlockAi ? " checked" : "" %> /> Block AI training crawlers <span class="muted small">(GPTBot, CCBot, Google-Extended and others; Google and Bing search are not affected)</span></label>
                <label class="check"><input type="checkbox" name="sitemap" value="1"<%= R.Sitemap ? " checked" : "" %> /> Point crawlers to the sitemap</label>
                <label class="check"><input type="checkbox" name="all" value="1"<%= R.BlockAll ? " checked" : "" %> /> <b>Block all crawlers everywhere</b> <span class="muted small">(the site disappears from Google over time; use only while building)</span></label>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Rules</h2>
                <div class="seg"><label><input type="radio" name="mode" value="rules"<%= R.Mode != "custom" ? " checked" : "" %> /> Rule builder</label><label><input type="radio" name="mode" value="custom"<%= R.Mode == "custom" ? " checked" : "" %> /> Write it myself</label></div></div>
            <input type="hidden" name="count" value="<%= R.Groups.Count + 1 %>" />
            <% for (var i = 0; i <= R.Groups.Count; i++) { var g = i < R.Groups.Count ? R.Groups[i] : new RobotsGroup(); var isNew = i == R.Groups.Count; %>
            <%= isNew ? "<details class=\"items__card\" style=\"margin-top:12px\"><summary class=\"items__head\" style=\"cursor:pointer\"><b>+ Add rules for another crawler</b></summary>" : "<div class=\"items__card\" style=\"margin-top:12px\"><div class=\"items__head\"><b>" + H(string.IsNullOrWhiteSpace(g.Agents) ? "*" : g.Agents.Replace("\n", ", ")) + "</b></div>" %>
            <div class="form-grid" style="padding:12px 0 0">
                <div><label class="label">Crawlers (User-agent)</label><textarea class="field mono" name="g<%= i %>_agents" rows="2" placeholder="* for all, or Googlebot"><%: g.Agents %></textarea></div>
                <div><label class="label">Wait between visits (seconds)</label><input class="field" name="g<%= i %>_delay" value="<%= g.CrawlDelay > 0 ? g.CrawlDelay.ToString() : "" %>" inputmode="numeric" maxlength="3" placeholder="Usually empty" /><p class="fld__help">Google ignores this; Bing and Yandex respect it.</p></div>
                <div><label class="label">Allow</label><textarea class="field mono" name="g<%= i %>_allow" rows="4" placeholder="/"><%: g.Allow %></textarea><p class="fld__help">One path per line.</p></div>
                <div><label class="label">Disallow</label><textarea class="field mono" name="g<%= i %>_disallow" rows="4" placeholder="/admin"><%: g.Disallow %></textarea><p class="fld__help">One path per line. /private/ blocks everything under it; * matches anything.</p></div>
                <% if (!isNew) { %><div class="span-2"><label class="check"><input type="checkbox" name="g<%= i %>_del" value="1" /> Remove these rules</label></div><% } %>
            </div>
            <%= isNew ? "</details>" : "</div>" %>
            <% } %>
            <div style="margin-top:16px"><label class="label" for="extra">Extra sitemaps (optional)</label><textarea class="field mono" id="extra" name="extra" rows="2" placeholder="https://www.yenetch.com/other-sitemap.xml"><%: R.ExtraSitemaps %></textarea></div>
            <div style="margin-top:16px"><label class="label" for="custom">Your own robots.txt (used with "Write it myself")</label><textarea class="field field--code" id="custom" name="custom" rows="10" spellcheck="false"><%: R.Custom %></textarea></div>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>What crawlers see</h2></div>
            <p class="muted small" style="margin:0 0 8px">On <%: Seo.Root %></p>
            <pre class="mono small" style="white-space:pre-wrap;background:var(--line-2,#f2f2f5);padding:12px;border-radius:12px;margin:0"><%: Preview %></pre>
            <% if (R.HideTestSites) { %><p class="muted small" style="margin:10px 0 0">On test copies it blocks everything.</p><% } %>
        </section>
        <section class="card">
            <div class="card__head"><h2>Good to know</h2></div>
            <ul class="tips">
                <li>robots.txt asks politely. It does not hide a page; private pages also need a password, as the admin has.</li>
                <li>Do not block /assets/: Google needs the CSS and scripts to see the page properly.</li>
                <li>After a change, use the robots.txt report in Google Search Console to check it.</li>
            </ul>
        </section>
    </div>
</div>
</form>
</asp:Content>
