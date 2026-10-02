<%@ Page Title="Lead scoring" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Scoring.aspx.cs" Inherits="Yenetch.Web.Admin.ScoringPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>Lead scoring</h1><p>Every lead gets a score from 0 to 100 so the team calls the hottest leads first. Choose how many points each signal is worth. Scores update when something happens and every night.</p></div>
    <div class="page-head__actions"><button class="btn btn--line" type="submit" name="act" value="recalc">Re-score open leads now</button><button class="btn btn--blue" type="submit" name="act" value="save">Save</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="kpis">
    <div class="kpi"><span>Hot (<%= LeadScoring.HotAt %>+)</span><b><%= N(Hot) %></b><small>open leads</small></div>
    <div class="kpi"><span>Warm (<%= LeadScoring.WarmAt %>–<%= LeadScoring.HotAt - 1 %>)</span><b><%= N(Warm) %></b><small>open leads</small></div>
    <div class="kpi"><span>Cold (under <%= LeadScoring.WarmAt %>)</span><b><%= N(Cold) %></b><small>open leads</small></div>
    <div class="kpi"><span>Average score</span><b><%= Avg %></b><small>open leads</small></div>
</div>
<div class="grid grid--main">
    <section class="card card--flush">
        <div class="card__head"><h2>Points per signal</h2><span class="muted small">Negative numbers take points away</span></div>
        <div class="table-wrap"><table class="table">
            <thead><tr><th>Signal</th><th style="width:120px">Points</th></tr></thead>
            <tbody><% foreach (var s in LeadScoring.Signals) { %><tr>
                <td><label for="w_<%= s[0] %>"><%: s[1] %></label><% if (s[2] != W[s[0]].ToString()) { %><span class="cell-sub">Default <%= s[2] %></span><% } %></td>
                <td><input class="field" style="max-width:90px" id="w_<%= s[0] %>" name="w_<%= s[0] %>" inputmode="numeric" maxlength="4" value="<%= W[s[0]] %>" /></td>
            </tr><% } %></tbody>
        </table></div>
    </section>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Hot, warm and cold</h2></div>
            <div class="form-grid">
                <div><label class="label" for="hot">Hot from</label><input class="field" id="hot" name="hot" inputmode="numeric" maxlength="3" value="<%= LeadScoring.HotAt %>" /></div>
                <div><label class="label" for="warm">Warm from</label><input class="field" id="warm" name="warm" inputmode="numeric" maxlength="3" value="<%= LeadScoring.WarmAt %>" /></div>
                <div class="span-2"><label class="check"><input type="checkbox" name="auto" value="1"<%= LeadScoring.AutoPriority ? " checked" : "" %> /> Set each open lead's priority (Hot, Warm, Cold) from its score</label>
                    <p class="fld__help">A priority someone sets by hand on a lead is kept and no longer changed by the score.</p></div>
            </div>
            <div class="form-actions"><button class="btn btn--line" type="submit" name="act" value="defaults" data-confirm="Put every signal back to its default points?">Restore defaults</button></div>
        </section>
        <section class="card">
            <div class="card__head"><h2>How it works</h2></div>
            <ul class="tips">
                <li>Website behaviour counts for visitors who accepted analytics cookies.</li>
                <li>Won leads score 100; lost and junk leads score 0.</li>
                <li>Open a lead to see exactly which signals gave it its score.</li>
                <li>Sort the lead list by score to call the best leads first.</li>
            </ul>
        </section>
    </div>
</div>
</form>
</asp:Content>
