<%@ Page Title="AI assistant" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="AiAdmin.aspx.cs" Inherits="Yenetch.Web.Admin.AiAdminPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server">
<div class="page-head">
    <div><h1>AI assistant</h1><p>The chat on the website answers typed questions with AI, using only your website content, plans, prices and offers. Add one or more API keys: if a key reaches its limit or runs out of credit, the next one answers.</p></div>
    <div class="page-head__actions"><button class="btn btn--blue" type="submit" name="act" value="settings">Save settings</button></div>
</div>
<% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
<div class="kpis">
    <div class="kpi"><span>Status</span><b><%= !Ai.Enabled ? "Off" : Keys.Any(k => k.Ready) ? "On" : "Paused" %></b><small><%= !Ai.Enabled ? (Keys.Count == 0 ? "Add an API key to switch it on" : "Switched off below") : Keys.Any(k => k.Ready) ? "Typed questions are answered by AI" : "Every key is resting; built-in answers meanwhile" %></small></div>
    <div class="kpi"><span>Answers today</span><b><%= N(Keys.Sum(k => k.UsedToday)) %></b><small>of <%= N(Ai.DailyCap) %> allowed a day</small></div>
    <div class="kpi"><span>Keys ready</span><b><%= Keys.Count(k => k.Ready) %> / <%= Keys.Count %></b><small><%= Keys.Any(k => k.Resting) ? Keys.Count(k => k.Resting) + " resting" : "none resting" %></small></div>
    <div class="kpi"><span>Conversations, 30 days</span><b><%= N(Chats30) %></b><small><%= N(ChatLeads30) %> became leads</small></div>
</div>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card card--flush">
            <div class="card__head"><h2>API keys</h2><span class="muted small">Tried from top to bottom</span></div>
            <% if (Keys.Count == 0) { %><div class="empty"><b>No keys yet</b>Add an Anthropic API key below. Without a key the chat uses its built-in answers.</div><% } else { %>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>Key</th><th>Model</th><th>Status</th><th>Today</th><th>All time</th><th></th></tr></thead>
                <tbody>
                <% for (var i = 0; i < Keys.Count; i++) { var k = Keys[i]; %><tr>
                    <td><b class="cell-main"><%: k.Label %></b><span class="cell-sub mono"><%: k.KeyHint %></span></td>
                    <td class="small"><%: ModelName(k.Model) %></td>
                    <td><%= StatusOf(k) %><% if (!string.IsNullOrEmpty(k.LastError)) { %><span class="cell-sub" title="<%: k.LastError %>"><%: Util.Cut(k.LastError, 70) %><%= k.LastErrorOn.HasValue ? " · " + Ago(k.LastErrorOn) : "" %></span><% } %></td>
                    <td class="nowrap"><%= N(k.UsedToday) %><%= k.DailyLimit > 0 ? " / " + N(k.DailyLimit) : "" %></td>
                    <td class="nowrap small"><%= N(k.Requests) %> answers<span class="cell-sub"><%= N((k.InputTokens + k.OutputTokens) / 1000.0) %>k tokens</span></td>
                    <td class="nowrap">
                        <button class="btn btn--line btn--sm" type="submit" name="test" value="<%= k.Id %>">Test</button>
                        <% if (k.Resting) { %><button class="btn btn--line btn--sm" type="submit" name="wake" value="<%= k.Id %>">Use now</button><% } %>
                        <% if (i > 0) { %><button class="btn btn--line btn--sm" type="submit" name="up" value="<%= k.Id %>" title="Move up" aria-label="Move up">↑</button><% } %>
                        <a class="btn btn--line btn--sm" href="?edit=<%= k.Id %>#key">Edit</a>
                    </td>
                </tr><% } %>
                </tbody>
            </table></div>
            <% } %>
        </section>
        <section class="card" id="key">
            <div class="card__head"><h2><%= Editing != null ? "Edit key: " + H(Editing.Label) : "Add a key" %></h2></div>
            <input type="hidden" name="keyId" value="<%= Editing != null ? Editing.Id : 0 %>" />
            <div class="form-grid">
                <div><label class="label" for="label">Name</label><input class="field" id="label" name="label" maxlength="80" value="<%: Editing != null ? Editing.Label : "" %>" placeholder="Main key" /></div>
                <div><label class="label" for="model">Model</label><select class="field" id="model" name="model">
                    <% foreach (var m in Ai.Models) { %><option value="<%= m[0] %>"<%= (Editing != null ? Editing.Model : Ai.DefaultModel) == m[0] ? " selected" : "" %>><%: m[1] %></option><% } %></select></div>
                <div class="span-2"><label class="label" for="apikey">Anthropic API key</label><input class="field mono" id="apikey" name="apikey" type="password" autocomplete="new-password" placeholder="<%= Editing != null ? "Saved (" + H(Editing.KeyHint) + "). Type to replace" : "sk-ant-..." %>" />
                    <p class="fld__help">Create one at console.anthropic.com under API keys. It is stored encrypted and never shown again. Set a monthly spend limit in the Anthropic console too.</p></div>
                <div><label class="label" for="limit">Daily limit (answers)</label><input class="field" id="limit" name="limit" inputmode="numeric" maxlength="6" value="<%= Editing != null ? Editing.DailyLimit.ToString() : "0" %>" />
                    <p class="fld__help">0 for no limit. When reached, the next key answers until midnight.</p></div>
                <% if (Editing != null) { %><div><label class="label">&nbsp;</label><label class="check"><input type="checkbox" name="active" value="1"<%= Editing.IsActive ? " checked" : "" %> /> Use this key</label></div><% } %>
            </div>
            <div class="form-actions">
                <button class="btn btn--blue" type="submit" name="act" value="key"><%= Editing != null ? "Save key" : "Add key" %></button>
                <% if (Editing != null) { %><a class="btn btn--line" href="/admin/ai">Cancel</a><button class="btn btn--line btn--danger" type="submit" name="del" value="<%= Editing.Id %>" data-confirm="Delete this key?">Delete key</button><% } %>
            </div>
        </section>
        <section class="card card--flush">
            <div class="card__head"><h2>Recent conversations</h2></div>
            <% if (Chats.Count == 0) { %><div class="empty"><b>No AI conversations yet</b>They appear here once visitors ask the assistant questions.</div><% } else { %>
            <div class="table-wrap"><table class="table">
                <thead><tr><th>When</th><th>First question</th><th>Messages</th><th>Lead</th></tr></thead>
                <tbody>
                <% foreach (var c in Chats) { %><tr>
                    <td class="nowrap small"><%: Ago(c.DateN("LastOn")) %><span class="cell-sub"><%: c.Str("Page") %></span></td>
                    <td><details><summary><%: Util.Cut(FirstQuestion(c.Str("Transcript")), 90) %></summary><div class="chatlog"><%= Transcript(c.Str("Transcript")) %></div></details></td>
                    <td><%= c.Int("Messages") %></td>
                    <td><% if (c.IntN("LeadId").HasValue) { %><a href="/admin/leads/<%= c.IntN("LeadId") %>">Open lead</a><% } else { %><span class="muted">–</span><% } %></td>
                </tr><% } %>
                </tbody>
            </table></div>
            <% } %>
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Settings</h2></div>
            <label class="check"><input type="checkbox" name="enabled" value="1"<%= Ai.SwitchedOn ? " checked" : "" %> /> Answer typed questions with AI</label>
            <div class="form-grid" style="margin-top:12px">
                <div><label class="label" for="perVisitor">Questions per visitor per hour</label><input class="field" id="perVisitor" name="perVisitor" inputmode="numeric" maxlength="4" value="<%= Ai.PerVisitorPerHour %>" /></div>
                <div><label class="label" for="dailyCap">Answers per day (all keys)</label><input class="field" id="dailyCap" name="dailyCap" inputmode="numeric" maxlength="6" value="<%= Ai.DailyCap %>" /></div>
                <div class="span-2"><label class="label" for="extra">Extra instructions (optional)</label><textarea class="field" id="extra" name="extra" rows="5" maxlength="3000" placeholder="For example: Always suggest the free website audit to people asking about SEO."><%: Ai.ExtraInstructions %></textarea>
                    <p class="fld__help">The assistant already knows your services, plans, prices, offers, products, case studies and FAQs from Website content, and only talks about Yenetch.</p></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>How switching works</h2></div>
            <ul class="tips">
                <li><b>Rate limited:</b> the key rests for the time Anthropic asks (usually a minute).</li>
                <li><b>Out of credit:</b> rests 6 hours. Top up, then press <b>Use now</b>.</li>
                <li><b>Key rejected or model not available:</b> rests 24 hours. Check or replace the key.</li>
                <li><b>Busy or no connection:</b> rests 30 seconds.</li>
                <li>When no key can answer, the chat quietly uses its built-in answers, so visitors always get a reply.</li>
            </ul>
        </section>
    </div>
</div>
</form>
</asp:Content>
