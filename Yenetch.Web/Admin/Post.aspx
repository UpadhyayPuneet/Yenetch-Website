<%@ Page Title="Post" Language="C#" MasterPageFile="~/Admin/Admin.Master" AutoEventWireup="true" CodeBehind="Post.aspx.cs" Inherits="Yenetch.Web.Admin.PostPage" ValidateRequest="false" %>
<%@ Import Namespace="Yenetch.Crm" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
<form id="form1" runat="server" data-post-form="">
<a class="crumb" href="/admin/posts"><%= Icon("back") %>Blog</a>
<div class="page-head">
    <div><h1><%= IsNew ? (Original != null ? "Edit built-in article" : "New post") : "Edit post" %></h1>
        <p><%= StatusLine %></p></div>
    <div class="page-head__actions">
        <% if (!IsNew) { %><a class="btn btn--line" href="<%= Att(PreviewUrl) %>" target="_blank" rel="noopener"><%= Icon("external") %>Preview</a><% } %>
        <asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn btn--blue" OnClick="SaveButton_Click" />
    </div>
</div>
<asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
<div class="grid grid--main">
    <div class="stack" style="gap:16px">
        <section class="card">
            <label class="label" for="PostTitle">Title</label>
            <asp:TextBox ID="PostTitle" runat="server" CssClass="field field--title" MaxLength="200" placeholder="e.g. How much does a website cost in India?" data-title="" />
            <div class="rte" data-rte="">
                <div class="rte__bar" role="toolbar" aria-label="Formatting">
                    <button type="button" data-cmd="p" title="Paragraph">Text</button>
                    <button type="button" data-cmd="h2" title="Section heading (shows in the contents list)">H2</button>
                    <button type="button" data-cmd="h3" title="Sub-heading">H3</button>
                    <span class="rte__sep"></span>
                    <button type="button" data-cmd="bold" title="Bold (Ctrl+B)"><b>B</b></button>
                    <button type="button" data-cmd="italic" title="Italic (Ctrl+I)"><i>I</i></button>
                    <button type="button" data-cmd="link" title="Link (Ctrl+K)">Link</button>
                    <span class="rte__sep"></span>
                    <button type="button" data-cmd="ul" title="Bulleted list">• List</button>
                    <button type="button" data-cmd="ol" title="Numbered list">1. List</button>
                    <button type="button" data-cmd="quote" title="Quote">Quote</button>
                    <button type="button" data-cmd="callout" title="Highlighted note box">Note box</button>
                    <button type="button" data-cmd="table" title="Insert a table">Table</button>
                    <button type="button" data-cmd="image" title="Upload an image">Image</button>
                    <span class="rte__sep"></span>
                    <button type="button" data-cmd="clear" title="Remove formatting">Clear</button>
                    <button type="button" data-cmd="html" title="Edit the HTML" aria-pressed="false">HTML</button>
                </div>
                <div class="rte__area prose-admin" contenteditable="true" data-rte-area="" aria-label="Article body" role="textbox" aria-multiline="true"></div>
                <asp:TextBox ID="Body" runat="server" ValidateRequestMode="Disabled" CssClass="field rte__html" TextMode="MultiLine" Rows="24" spellcheck="false" data-rte-source="" />
                <div class="rte__foot"><span data-rte-count="">0 words</span><span>Tip: use H2 for each main section. For a FAQ, add an H2 called "Frequently asked questions" with H3 questions, each followed by its answer.</span></div>
                <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden="hidden" data-rte-file="" />
            </div>
        </section>
        <section class="card">
            <label class="label" for="Excerpt">Summary <span class="muted" data-count-for="Excerpt" data-max="300"></span></label>
            <asp:TextBox ID="Excerpt" runat="server" CssClass="field" TextMode="MultiLine" Rows="3" MaxLength="600" placeholder="One or two sentences shown on the blog list and in search results." />
        </section>
    </div>
    <div class="stack" style="gap:16px">
        <section class="card">
            <div class="card__head"><h2>Publishing</h2></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="StatusList">Status</label>
                    <asp:DropDownList ID="StatusList" runat="server" CssClass="field">
                        <asp:ListItem Value="Draft">Draft (only you can see it)</asp:ListItem>
                        <asp:ListItem Value="Published">Published</asp:ListItem>
                        <asp:ListItem Value="Hidden">Hidden (off the site)</asp:ListItem>
                    </asp:DropDownList></div>
                <div><label class="label" for="PublishOn">Publish date</label>
                    <input id="PublishOn" name="publishOn" type="datetime-local" class="field" value="<%= Att(PublishValue) %>" />
                    <p class="muted small" style="margin-top:6px">India time. Leave empty to use the moment you publish. A future date schedules the post.</p></div>
                <% if (!IsNew) { %><div><button class="btn btn--danger btn--sm" type="submit" name="delete" value="1" data-confirm="<%= Att(BuiltIn != null ? "Delete your edited copy? The original built-in article will show on the site again." : "Delete this post permanently?") %>"><%= Icon("trash") %><%= BuiltIn != null ? "Delete my edits" : "Delete post" %></button></div><% } %>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Cover image</h2></div>
            <figure class="cover-pick" data-cover="">
                <img alt="" src="<%= Att(CoverSrc) %>" data-cover-img=""<%= string.IsNullOrEmpty(CoverSrc) ? " hidden=\"hidden\"" : "" %> />
                <span class="cover-pick__empty"<%= string.IsNullOrEmpty(CoverSrc) ? "" : " hidden=\"hidden\"" %>>No cover image yet</span>
            </figure>
            <div class="log-form__row" style="margin-top:10px">
                <button type="button" class="btn btn--line btn--sm" data-cover-upload=""><%= Icon("download") %>Upload image</button>
                <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden="hidden" data-cover-file="" />
            </div>
            <label class="label" for="Cover" style="margin-top:12px">or image address</label>
            <asp:TextBox ID="Cover" runat="server" CssClass="field" MaxLength="400" placeholder="/uploads/blog/... or https://..." data-cover-field="" />
            <p class="muted small" style="margin-top:6px">Wide images work best, about 1600 × 900.</p>
        </section>
        <section class="card">
            <div class="card__head"><h2>Details</h2></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="Category">Category</label>
                    <asp:TextBox ID="Category" runat="server" CssClass="field" MaxLength="80" list="cats" autocomplete="off" />
                    <datalist id="cats"><% foreach (var c in Categories) { %><option value="<%= Att(c) %>"></option><% } %></datalist></div>
                <div><label class="label" for="Author">Author</label><asp:TextBox ID="Author" runat="server" CssClass="field" MaxLength="120" placeholder="Team Yenetch" list="author-list" />
                    <datalist id="author-list"><% foreach (var a in Yenetch.Data.Authors.All) { %><option value="<%: a.Name %>"></option><% } %></datalist>
                    <p class="fld__help">Pick a <a href="/admin/content/authors">blog author</a> so the article links to their profile.</p></div>
                <div><label class="label" for="Tags">Tags</label><asp:TextBox ID="Tags" runat="server" CssClass="field" MaxLength="400" placeholder="seo, google, local business" /></div>
            </div>
        </section>
        <section class="card">
            <div class="card__head"><h2>Search engines</h2></div>
            <div class="stack" style="gap:12px">
                <div><label class="label" for="Slug">Web address</label>
                    <div class="slug-field"><span>/blog/</span><asp:TextBox ID="Slug" runat="server" CssClass="field" MaxLength="90" data-slug="" /></div>
                    <% if (Original != null || (BuiltIn != null)) { %><p class="muted small" style="margin-top:6px">Built-in articles keep their address.</p><% } %></div>
                <div><label class="label" for="MetaTitle">Google title <span class="muted" data-count-for="MetaTitle" data-max="60"></span></label><asp:TextBox ID="MetaTitle" runat="server" CssClass="field" MaxLength="200" placeholder="Leave empty to use the title" /></div>
                <div><label class="label" for="MetaDesc">Google description <span class="muted" data-count-for="MetaDesc" data-max="160"></span></label><asp:TextBox ID="MetaDesc" runat="server" CssClass="field" TextMode="MultiLine" Rows="3" MaxLength="320" placeholder="Leave empty to use the summary" /></div>
                <div class="serp" aria-label="How it may look on Google">
                    <span class="serp__url">yenetch.com › blog › <span data-serp-slug=""></span></span>
                    <b class="serp__title" data-serp-title=""></b>
                    <span class="serp__desc" data-serp-desc=""></span>
                </div>
            </div>
        </section>
    </div>
</div>
</form>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server"><script src="/assets/admin/editor.js?v=3" defer></script></asp:Content>
