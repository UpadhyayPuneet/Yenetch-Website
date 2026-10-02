/* Yenetch admin: rich text editor (window.YenRte) used by the blog editor and the content manager, plus the blog
   post page: toolbar, clean paste, image uploads, HTML view, word count, web address from the title, Google preview and an
   unsaved-changes warning. No dependencies. */
(function () {
  "use strict";
  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };

  function esc(s) { return String(s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  /* ---- Uploads ---- */
  /* WebP copies (about a third smaller than JPG) made in this browser, because the server cannot write WebP.
     The server makes the resized JPG/PNG copies itself; if this browser cannot make WebP, the upload works the same. */
  var WIDTHS = [480, 960, 1600], MAX = 2000;
  function webpCopies(file) {
    if (!/^image\/(jpeg|png)$/.test(file.type) || !window.createImageBitmap || !document.createElement("canvas").toDataURL("image/webp").startsWith("data:image/webp")) return Promise.resolve([]);
    return createImageBitmap(file, { imageOrientation: "from-image" }).then(function (bmp) {
      var jobs = WIDTHS.filter(function (w) { return w < bmp.width; }).map(function (w) { return { name: "webp" + w, w: w }; });
      jobs.push({ name: "webpFull", w: Math.min(bmp.width, MAX) });
      return Promise.all(jobs.map(function (j) {
        var c = document.createElement("canvas"); c.width = j.w; c.height = Math.round(bmp.height * j.w / bmp.width);
        var g = c.getContext("2d"); g.imageSmoothingQuality = "high"; g.drawImage(bmp, 0, 0, c.width, c.height);
        return new Promise(function (ok) { c.toBlob(function (b) { ok(b ? { name: j.name, blob: b } : null); }, "image/webp", 0.8); });
      })).then(function (list) { return list.filter(Boolean); });
    }).catch(function () { return []; });
  }
  function upload(file, done, folder) {
    if (!file) return;
    if (file.size > 8 * 1024 * 1024) { window.alert("Images can be up to 8 MB."); return; }
    document.body.classList.add("is-uploading");
    webpCopies(file).then(function (copies) {
    var data = new FormData();
    data.append("file", file);
    copies.forEach(function (c) { data.append(c.name, c.blob, c.name + ".webp"); });
    return fetch("/Admin/Upload.ashx" + (folder ? "?to=" + folder : ""), { method: "POST", body: data, credentials: "same-origin", headers: { "X-Requested-With": "fetch" } })
      .then(function (r) { return r.json().catch(function () { return { error: "Upload failed (" + r.status + ")." }; }); })
      .then(function (j) { if (j.url) done(j.url); else window.alert(j.error || "Upload failed."); })
      .catch(function () { window.alert("Upload failed. Check your connection and try again."); })
    }).then(function () { document.body.classList.remove("is-uploading"); });
  }


  /* ---- Rich text editor: initRte(element with data-rte, onChange) ---- */
  function initRte(rte, onChange) {
    var area = $("[data-rte-area]", rte), source = $("[data-rte-source]", rte), fileInput = $("[data-rte-file]", rte);
    var htmlMode = false, saved = null;

    try { document.execCommand("defaultParagraphSeparator", false, "p"); } catch (e) {}
    area.innerHTML = source.value.trim() || "<p><br></p>";

    function sync() { if (!htmlMode) { normalize(); source.value = area.innerHTML; } }
    function markDirty() { count(); if (onChange) onChange(); }

    /* ---- Selection kept while the toolbar or a file dialog has focus ---- */
    function restore() {
      area.focus();
      if (!saved) return;
      var sel = window.getSelection();
      sel.removeAllRanges();
      sel.addRange(saved);
    }
    document.addEventListener("selectionchange", function () {
      var sel = window.getSelection();
      if (sel.rangeCount && area.contains(sel.anchorNode)) saved = sel.getRangeAt(0).cloneRange();
      state();
    });

    function exec(cmd, val) { restore(); document.execCommand(cmd, false, val); markDirty(); }
    function insert(html) { restore(); document.execCommand("insertHTML", false, html); markDirty(); }
    var BLOCK = /^(P|UL|OL|H2|H3|TABLE|DIV|ASIDE|BLOCKQUOTE|FIGURE)$/;

    /* Inserts a block (note box, table, image) after the paragraph the caret is in, never inside it. */
    function insertBlock(html) {
      restore();
      var tmp = document.createElement("div");
      tmp.innerHTML = html;
      var node = saved ? saved.startContainer : null, top = null;
      while (node && node !== area) { if (node.parentNode === area) top = node; node = node.parentNode; }
      var frag = document.createDocumentFragment(), first = tmp.firstChild, kids = Array.prototype.slice.call(tmp.childNodes);
      kids.forEach(function (k) { frag.appendChild(k); });
      var last = kids[kids.length - 1];
      if (top && top.nodeName === "P" && top.textContent.trim() === "" && !top.querySelector("img")) area.replaceChild(frag, top);
      else if (top) area.insertBefore(frag, top.nextSibling);
      else area.appendChild(frag);
      if (!last.nextSibling) { var p = document.createElement("p"); p.innerHTML = "<br>"; area.appendChild(p); }
      var target = first.querySelector && (first.querySelector("p, td, th") || first);
      var r = document.createRange(); r.selectNodeContents(target || first); r.collapse(false);
      var sel = window.getSelection(); sel.removeAllRanges(); sel.addRange(r); saved = r.cloneRange();
      markDirty();
    }

    /* Keeps the structure valid: no lists, headings or tables inside paragraphs, and no loose text at the top level. */
    function normalize() {
      Array.prototype.slice.call(area.childNodes).forEach(function (n) {
        if (n.nodeType === 3) {
          if (!n.nodeValue.trim()) { area.removeChild(n); return; }
          var p = document.createElement("p"); area.insertBefore(p, n); p.appendChild(n); return;
        }
        if (n.nodeType !== 1) { area.removeChild(n); return; }
        if (n.nodeName === "SPAN" || n.nodeName === "B" || n.nodeName === "STRONG" || n.nodeName === "EM" || n.nodeName === "A") {
          var w = document.createElement("p"); area.insertBefore(w, n); w.appendChild(n); return;
        }
        if (n.nodeName !== "P" || !Array.prototype.some.call(n.childNodes, function (c) { return c.nodeType === 1 && BLOCK.test(c.nodeName); })) return;
        var run = null;
        Array.prototype.slice.call(n.childNodes).forEach(function (c) {
          if (c.nodeType === 1 && BLOCK.test(c.nodeName)) { area.insertBefore(c, n); run = null; }
          else { if (!run) { run = document.createElement("p"); area.insertBefore(run, n); } run.appendChild(c); }
        });
        area.removeChild(n);
      });
      Array.prototype.slice.call(area.querySelectorAll("p")).forEach(function (p) {
        if (p.parentNode === area && !p.textContent.trim() && !p.querySelector("img") && p !== area.lastElementChild) area.removeChild(p);
      });
    }


    /* ---- Toolbar ---- */
    var commands = {
      p: function () { exec("formatBlock", "<p>"); },
      h2: function () { exec("formatBlock", "<h2>"); },
      h3: function () { exec("formatBlock", "<h3>"); },
      bold: function () { exec("bold"); },
      italic: function () { exec("italic"); },
      ul: function () { exec("insertUnorderedList"); },
      ol: function () { exec("insertOrderedList"); },
      quote: function () { exec("formatBlock", "<blockquote>"); },
      link: function () {
        var url = window.prompt("Link address (for example https://www.yenetch.com/contact)", "https://");
        if (!url || url === "https://") return;
        if (!/^(https?:\/\/|\/|mailto:|tel:|#)/i.test(url)) url = "https://" + url;
        if (saved && !saved.collapsed) exec("createLink", url);
        else insert('<a href="' + esc(url) + '">' + esc(url) + "</a>");
      },
      callout: function () { insertBlock('<aside class="callout"><strong>Good to know</strong><p>Write the note here.</p></aside><p><br></p>'); },
      table: function () {
        var cols = parseInt(window.prompt("How many columns?", "3"), 10) || 0, rows = parseInt(window.prompt("How many rows (not counting the heading row)?", "3"), 10) || 0;
        if (cols < 1 || rows < 1) return;
        cols = Math.min(cols, 8); rows = Math.min(rows, 40);
        var head = "", body = "", i, j;
        for (i = 0; i < cols; i++) head += "<th>Heading</th>";
        for (j = 0; j < rows; j++) { body += "<tr>"; for (i = 0; i < cols; i++) body += "<td>&nbsp;</td>"; body += "</tr>"; }
        insertBlock('<div class="table-wrap"><table><thead><tr>' + head + "</tr></thead><tbody>" + body + "</tbody></table></div><p><br></p>");
      },
      image: function () { fileInput.value = ""; fileInput.click(); },
      clear: function () { exec("removeFormat"); exec("formatBlock", "<p>"); },
      html: function (btn) {
        htmlMode = !htmlMode;
        if (htmlMode) { normalize(); source.value = pretty(area.innerHTML); }
        else { area.innerHTML = source.value.trim() || "<p><br></p>"; count(); }
        rte.classList.toggle("is-html", htmlMode);
        btn.setAttribute("aria-pressed", htmlMode ? "true" : "false");
        $$(".rte__bar button", rte).forEach(function (b) { if (b !== btn) b.disabled = htmlMode; });
        (htmlMode ? source : area).focus();
      }
    };

    $$(".rte__bar button", rte).forEach(function (b) {
      b.addEventListener("mousedown", function (e) { e.preventDefault(); });
      b.addEventListener("click", function () { var c = commands[b.getAttribute("data-cmd")]; if (c) c(b); });
    });

    function state() {
      if (htmlMode) return;
      ["bold", "italic"].forEach(function (c) {
        var b = $('[data-cmd="' + c + '"]', rte);
        var on = false; try { on = document.queryCommandState(c); } catch (e) {}
        if (b) b.classList.toggle("is-on", on);
      });
      var block = ""; try { block = (document.queryCommandValue("formatBlock") || "").toLowerCase(); } catch (e) {}
      ["p", "h2", "h3"].forEach(function (c) { var b = $('[data-cmd="' + c + '"]', rte); if (b) b.classList.toggle("is-on", block === c); });
    }

    area.addEventListener("keydown", function (e) {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") { e.preventDefault(); commands.link(); }
    });
    area.addEventListener("input", markDirty);
    source.addEventListener("input", markDirty);

    function pretty(html) {
      return html.replace(/\s*(<\/(p|h2|h3|ul|ol|li|blockquote|aside|table|thead|tbody|tr|div)>)\s*/gi, "$1\n")
                 .replace(/\s*(<(ul|ol|table|thead|tbody|tr|aside|div)[^>]*>)\s*/gi, "\n$1\n").replace(/\n{2,}/g, "\n").trim();
    }

    /* ---- Clean paste from Word, Google Docs and web pages ---- */
    var ALLOWED = { P: 1, H2: 1, H3: 1, STRONG: 1, EM: 1, A: 1, UL: 1, OL: 1, LI: 1, BLOCKQUOTE: 1, BR: 1, IMG: 1, TABLE: 1, THEAD: 1, TBODY: 1, TR: 1, TH: 1, TD: 1 };
    var RENAME = { B: "STRONG", I: "EM", H1: "H2", H4: "H3", H5: "H3", H6: "H3", DIV: "P" };

    function cleanNode(node, doc) {
      var out = doc.createDocumentFragment();
      Array.prototype.forEach.call(node.childNodes, function (n) {
        if (n.nodeType === 3) { out.appendChild(doc.createTextNode(n.nodeValue)); return; }
        if (n.nodeType !== 1) return;
        var tag = n.tagName, style = (n.getAttribute("style") || "").toLowerCase();
        if (/^(SCRIPT|STYLE|META|LINK|TITLE|O:P)$/.test(tag)) return;
        if (tag === "B" && /font-weight:\s*(normal|400)/.test(style)) tag = "SPAN"; // Google Docs wraps everything in <b style="font-weight:normal">
        if (tag === "SPAN" && /font-weight:\s*(bold|[6-9]00)/.test(style)) tag = "STRONG";
        else if (tag === "SPAN" && /font-style:\s*italic/.test(style)) tag = "EM";
        tag = RENAME[tag] || tag;
        var inner = cleanNode(n, doc);
        if (!ALLOWED[tag]) { out.appendChild(inner); return; }
        var el = doc.createElement(tag);
        if (tag === "A" && n.getAttribute("href")) el.setAttribute("href", n.getAttribute("href"));
        if (tag === "IMG") { if (!/^https?:|^\//.test(n.getAttribute("src") || "")) return; el.setAttribute("src", n.getAttribute("src")); el.setAttribute("alt", n.getAttribute("alt") || ""); }
        el.appendChild(inner);
        out.appendChild(el);
      });
      return out;
    }

    area.addEventListener("paste", function (e) {
      var cd = e.clipboardData;
      if (!cd) return;
      var html = cd.getData("text/html"), text = cd.getData("text/plain");
      e.preventDefault();
      if (html) {
        var doc = new DOMParser().parseFromString(html, "text/html");
        var box = document.createElement("div");
        box.appendChild(cleanNode(doc.body, document));
        document.execCommand("insertHTML", false, box.innerHTML.replace(/<p>\s*<\/p>/g, ""));
        normalize();
      } else if (text) {
        var parts = text.replace(/\r/g, "").split(/\n{2,}/);
        document.execCommand("insertHTML", false, parts.length > 1
          ? parts.map(function (p) { return "<p>" + esc(p).replace(/\n/g, "<br>") + "</p>"; }).join("")
          : esc(text).replace(/\n/g, "<br>"));
      }
      markDirty();
    });

    fileInput.addEventListener("change", function () {
      upload(fileInput.files[0], function (url) {
        var alt = window.prompt("Describe the image in a few words (helps Google and screen readers)", "") || "";
        insertBlock('<p><img src="' + esc(url) + '" alt="' + esc(alt) + '"></p>');
      });
    });

    function count() {
      if (!countEl) return;
      var text = htmlMode ? source.value.replace(/<[^>]+>/g, " ") : area.innerText;
      var words = (text.match(/[\wऀ-ॿ'’-]+/g) || []).length;
      countEl.textContent = words.toLocaleString("en-IN") + " words · about " + Math.max(1, Math.round(words / 220)) + " min read";
    }

    var countEl = $("[data-rte-count]", rte);
    count();
    return { sync: sync };
  }
  window.YenRte = { init: initRte, upload: upload };

  /* ---- Blog post page ---- */
  var form = $("[data-post-form]");
  if (!form || !$("[data-rte]", form)) return;
  var dirty = false;
  function markDirty() { dirty = true; }
  var editor = initRte($("[data-rte]", form), markDirty);
  var sync = editor.sync;
  var coverField = $("[data-cover-field]"), coverImg = $("[data-cover-img]"), coverFile = $("[data-cover-file]"), coverBtn = $("[data-cover-upload]");
  function showCover() {
    var v = coverField.value.trim();
    var ok = /^(https?:\/\/|\/)/.test(v);
    coverImg.hidden = !ok;
    if (ok) coverImg.src = v;
    $(".cover-pick__empty").hidden = ok;
  }
  if (coverField) {
    coverBtn.addEventListener("click", function () { coverFile.value = ""; coverFile.click(); });
    coverFile.addEventListener("change", function () { upload(coverFile.files[0], function (url) { coverField.value = url; showCover(); markDirty(); }); });
    coverField.addEventListener("input", function () { showCover(); markDirty(); });
  }

  $$("[data-count-for]").forEach(function (el) {
    var field = document.getElementById(el.getAttribute("data-count-for")), max = +el.getAttribute("data-max");
    function upd() { var n = field.value.length; el.textContent = n + " / " + max; el.style.color = n > max ? "var(--red)" : ""; }
    field.addEventListener("input", upd); upd();
  });

  var title = $("[data-title]"), slug = $("[data-slug]"), metaTitle = document.getElementById("MetaTitle"),
      metaDesc = document.getElementById("MetaDesc"), excerpt = document.getElementById("Excerpt");
  var slugTouched = !!slug.value || slug.readOnly;
  function slugify(s) { return s.toLowerCase().normalize("NFKD").replace(/[̀-ͯ]/g, "").replace(/[^a-z0-9ऀ-ॿ]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 90).replace(/-+$/, ""); }
  function serp() {
    $("[data-serp-slug]").textContent = slug.value || slugify(title.value) || "your-post";
    $("[data-serp-title]").textContent = ((metaTitle.value || title.value || "Your post title") + " | Yenetch").slice(0, 70);
    var d = metaDesc.value || excerpt.value || "Your summary shows here. Keep it under 160 characters so Google shows all of it.";
    $("[data-serp-desc]").textContent = d.length > 160 ? d.slice(0, 157) + "…" : d;
  }
  title.addEventListener("input", function () { if (!slugTouched) slug.value = slugify(title.value); serp(); markDirty(); });
  slug.addEventListener("input", function () { slugTouched = true; serp(); markDirty(); });
  slug.addEventListener("blur", function () { slug.value = slugify(slug.value); serp(); });
  [metaTitle, metaDesc, excerpt].forEach(function (f) { f.addEventListener("input", function () { serp(); markDirty(); }); });
  $$("input, select, textarea", form).forEach(function (f) { f.addEventListener("change", function () { dirty = true; }); });

  /* ---- Save ---- */
  form.addEventListener("submit", function () { sync(); dirty = false; });
  window.addEventListener("beforeunload", function (e) { if (dirty) { e.preventDefault(); e.returnValue = ""; } });

  serp();
})();
