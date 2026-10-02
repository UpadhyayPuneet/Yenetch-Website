<%@ Page Title="Pricing & currency" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="PricingAdmin.aspx.cs" Inherits="Yenetch.Web.Admin.PricingAdminPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Yenetch.Data" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Pricing, currency &amp; popups</h1><p>Whether visitors see prices, the currencies they can choose, the free audit popup, and proposal defaults. Plans, extras and offers are edited in Website content.</p></div>
    <div class="page-head__actions"><a class="btn btn--line" href="/pricing" target="_blank" rel="noopener">View /pricing</a><button class="btn btn--blue" type="submit" name="act" value="save">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="kpis">
    <div class="kpi"><span>Prices on the website</span><b><%= Pricing.PublicPrices ? "Shown" : "Hidden" %></b><small><%= Pricing.PublicPrices ? "Visitors see prices" : "Only signed-in staff see them" %></small></div>
    <div class="kpi"><span>Plans</span><b><%= N(Pricing.Plans.Count) %></b><small><a href="/admin/content/plans">Edit plans &amp; prices</a></small></div>
    <div class="kpi"><span>Offers running</span><b><%= N(Offers.Active.Count) %></b><small><a href="/admin/content/offers">Edit offers</a></small></div>
    <div class="kpi"><span>Quotes, 30 days</span><b><%= N(Quotes30) %></b><small>from the plan builder</small></div>
</div>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Prices</h2><span class="badge <%= Pricing.PublicPrices ? "badge--won" : "badge--hot" %>"><%= Pricing.PublicPrices ? "Public" : "Hidden" %></span></div>
            <label class="check"><input type="checkbox" name="show" value="1"<%= Pricing.PublicPrices ? " checked" : "" %> /> Show prices to visitors (on /pricing, service pages, the plan builder and in the chatbot)</label>
            <p class="fld__help">The site ships with <b>example prices</b>. Check every plan in <a href="/admin/content/plans">Plans &amp; prices</a> and <a href="/admin/content/addons">Plan builder extras</a> before switching this on. While prices are hidden, visitors can still build a plan and ask for a quote.</p>
            <div class="form-grid" style="margin-top:12px">
                <div class="span-2"><label class="label" for="heading">Pricing page headline</label><input class="field" id="heading" name="heading" maxlength="120" value="<%: Pricing.Heading %>" /></div>
                <div class="span-2"><label class="label" for="intro">Pricing page introduction</label><textarea class="field" id="intro" name="intro" rows="2" maxlength="400"><%: Pricing.Intro %></textarea></div>
                <div class="span-2"><label class="label" for="note">Note under every estimate</label><textarea class="field" id="note" name="note" rows="2" maxlength="400"><%: Pricing.Note %></textarea></div>
                <div><label class="label" for="taxName">Tax name</label><input class="field" id="taxName" name="taxName" maxlength="20" value="<%: Pricing.TaxName %>" /></div>
                <div><label class="label" for="taxPct">Tax rate (%)</label><input class="field" id="taxPct" name="taxPct" inputmode="decimal" maxlength="5" value="<%= Pricing.TaxPct.ToString("0.##") %>" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Currencies</h2><span class="muted small"><%= Fx.UpdatedOn.HasValue ? "Rates updated " + Ago(Fx.UpdatedOn) : "Rates not downloaded yet" %></span></div>
            <p class="muted small" style="margin:0 0 12px">Prices are entered in rupees. Visitors see them in their own currency (guessed from their country, and they can change it). Rates come free from open.er-api.com once a day. Type a rate to fix it by hand.</p>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Show</th><th>Currency</th><th>Today's rate</th><th>Fixed rate (₹ for 1)</th></tr></thead>
                <tbody>
                <% foreach (var kv in Fx.Known) { if (kv.Key == "INR") continue; var rate = Fx.Rates.ContainsKey(kv.Key) ? Fx.Rates[kv.Key] : 0m; %><tr>
                    <td><input type="checkbox" name="cur" value="<%= kv.Key %>"<%= Fx.Enabled.Contains(kv.Key) ? " checked" : "" %> aria-label="Show <%= kv.Key %>" /></td>
                    <td><b><%= kv.Key %></b> <span class="muted small"><%: kv.Value[1] %></span></td>
                    <td class="small"><%= rate > 0 ? "1 " + kv.Key + " = ₹" + (1m / rate).ToString("0.##") + (Fx.IsManual(kv.Key) ? " (fixed)" : "") : "<span class=\"muted\">no rate yet</span>" %></td>
                    <td><input class="field" style="max-width:120px" name="manual_<%= kv.Key %>" inputmode="decimal" maxlength="10" value="<%: Settings.Get("fx.manual." + kv.Key) %>" placeholder="auto" /></td>
                </tr><% } %>
                </tbody>
            </table></div>
            <div class="form-grid" style="margin-top:12px">
                <div><label class="label" for="foreign">Currency for other countries</label><select class="field" id="foreign" name="foreign"><% foreach (var c in new[] { "USD", "EUR", "GBP" }) { %><option<%= Fx.ForeignDefault == c ? " selected" : "" %>><%= c %></option><% } %></select></div>
                <div><label class="label">&nbsp;</label><label class="check"><input type="checkbox" name="round" value="1"<%= Fx.Round ? " checked" : "" %> /> Round foreign prices (for example $199, not $197.43)</label></div>
            </div>
            <div class="form-actions"><button class="btn btn--line" type="submit" name="act" value="rates">Download today's rates now</button></div>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card" id="popup">
            <div class="card__head"><h2>Free website audit popup</h2><span class="badge <%= Audit.Enabled ? "badge--won" : "badge--open" %>"><%= Audit.Enabled ? "On" : "Off" %></span></div>
            <label class="check"><input type="checkbox" name="pop" value="1"<%= Audit.Enabled ? " checked" : "" %> /> Offer a free website audit in a popup</label>
            <div class="form-grid" style="margin-top:12px">
                <div><label class="label" for="popTrigger">Show it</label><select class="field" id="popTrigger" name="popTrigger">
                    <option value="exit"<%= Audit.Trigger == "exit" ? " selected" : "" %>>When leaving (desktop)</option>
                    <option value="delay"<%= Audit.Trigger == "delay" ? " selected" : "" %>>After some seconds</option>
                    <option value="scroll"<%= Audit.Trigger == "scroll" ? " selected" : "" %>>After scrolling</option></select>
                    <p class="fld__help">Phones have no "leaving" signal, so they use the delay.</p></div>
                <div><label class="label" for="popDelay">Delay (seconds)</label><input class="field" id="popDelay" name="popDelay" inputmode="numeric" maxlength="3" value="<%= Audit.Delay %>" /></div>
                <div><label class="label" for="popScroll">Scrolled (%)</label><input class="field" id="popScroll" name="popScroll" inputmode="numeric" maxlength="3" value="<%= Audit.Scroll %>" /></div>
                <div><label class="label" for="popDays">Show again after (days)</label><input class="field" id="popDays" name="popDays" inputmode="numeric" maxlength="3" value="<%= Audit.Days %>" /></div>
                <div class="span-2"><label class="label" for="popTitle">Headline</label><input class="field" id="popTitle" name="popTitle" maxlength="120" value="<%: Audit.Title %>" /></div>
                <div class="span-2"><label class="label" for="popText">Text</label><textarea class="field" id="popText" name="popText" rows="2" maxlength="300"><%: Audit.Text %></textarea></div>
                <div class="span-2"><label class="label" for="popButton">Button</label><input class="field" id="popButton" name="popButton" maxlength="60" value="<%: Audit.Button %>" /></div>
                <div class="span-2"><label class="label" for="popExclude">Never show on</label><textarea class="field" id="popExclude" name="popExclude" rows="3"><%: string.Join("\n", Audit.Exclude) %></textarea><p class="fld__help">One address per line; /blog/* covers every article.</p></div>
                <div class="span-2"><label class="label" for="offerDays">Offer popups: show again after (days)</label><input class="field" id="offerDays" name="offerDays" inputmode="numeric" maxlength="3" value="<%: Settings.Get("popup.offer.days") ?? "3" %>" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Proposal defaults</h2></div>
            <div class="form-grid">
                <div><label class="label" for="validDays">Valid for (days)</label><input class="field" id="validDays" name="validDays" inputmode="numeric" maxlength="3" value="<%= Yenetch.Crm.Proposals.DefaultValidDays %>" /></div>
                <div class="span-2"><label class="label" for="terms">Standard terms</label><textarea class="field" id="terms" name="terms" rows="7" maxlength="6000"><%: Yenetch.Crm.Proposals.DefaultTerms %></textarea><p class="fld__help">One term per line. Each new proposal starts with these; you can change them per proposal.</p></div>
            </div>
        </section>
    </div>
</div>
</form>
</asp:Content>
