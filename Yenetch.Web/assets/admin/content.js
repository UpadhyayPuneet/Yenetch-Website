/* Yenetch admin: content manager form. Draws the edit form for any website section from its field list
   (ContentSchema on the server), including repeating blocks, image uploads, rich text and lists, and posts the
   result back as one JSON value. Fields the form does not know about are kept as they were. No dependencies. */
(function () {
  "use strict";
  var $ = function (s, r) { return (r || document).querySelector(s); };
  var host = $("[data-cms-form]");
  if (!host) return;

  var schema = JSON.parse(host.getAttribute("data-schema") || "[]");
  var value = JSON.parse(host.getAttribute("data-value") || "{}");
  var choices = JSON.parse(host.getAttribute("data-choices") || "{}");
  var photos = JSON.parse(host.getAttribute("data-photos") || "{}");
  var page = $("[data-cms-page]"), out = $("[data-cms-data]");
  var editors = [], dirty = false, uid = 0;

  function el(tag, cls, html) { var e = document.createElement(tag); if (cls) e.className = cls; if (html != null) e.innerHTML = html; return e; }
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  function str(v) { return v == null ? "" : String(v); }
  function slugify(s) { return str(s).toLowerCase().normalize("NFKD").replace(/[̀-ͯ]/g, "").replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 90); }
  function grow(t) { t.style.height = "auto"; t.style.height = Math.min(t.scrollHeight + 2, 640) + "px"; }
  function photoSrc(v) { v = str(v).trim(); return !v ? "" : v.indexOf("/") >= 0 ? v : (photos[v] || ""); }

  /* Wraps a control with its label and help text. */
  function field(f, control, id) {
    var w = el("div", "fld fld--" + f.Type);
    var label = el("label", "label", esc(f.Label) + (f.Required ? ' <span class="req" aria-hidden="true">*</span>' : ""));
    if (id) label.setAttribute("for", id);
    w.appendChild(label);
    w.appendChild(control);
    if (f.Help) w.appendChild(el("p", "fld__help", esc(f.Help)));
    return w;
  }

  /* Renders fields for one object into container. Returns a getter that builds the object back,
     starting from the original so unknown keys survive. */
  function renderFields(fields, obj, container) {
    obj = obj || {};
    var getters = [];
    fields.forEach(function (f) {
      if (f.Type === "object" && f.Name.charAt(0) === "_") {
        var box = el("fieldset", "fgroup"); box.appendChild(el("legend", "", esc(f.Label)));
        var g = renderFields(f.Fields, obj, box);
        container.appendChild(box);
        getters.push(function (o) { var sub = g(); f.Fields.forEach(function (sf) { o[sf.Name] = sub[sf.Name]; }); });
        return;
      }
      var get = render(f, obj[f.Name], container);
      getters.push(function (o) { o[f.Name] = get(); });
    });
    return function () {
      var o = JSON.parse(JSON.stringify(obj));
      getters.forEach(function (g) { g(o); });
      return o;
    };
  }

  function render(f, v, container) {
    var id = "f" + (++uid), t = f.Type, input;
    switch (t) {
      case "textarea": case "lines": case "paragraphs": case "json":
        input = el("textarea", "field" + (t === "json" ? " field--code" : ""));
        input.id = id;
        input.rows = t === "json" ? 18 : 3;
        input.value = t === "lines" ? (v || []).join("\n") : t === "paragraphs" ? (v || []).join("\n\n") : str(v);
        if (t === "lines") input.placeholder = "One per line";
        input.addEventListener("input", function () { grow(input); });
        container.appendChild(field(f, input, id));
        setTimeout(function () { grow(input); }, 0);
        return function () {
          var s = input.value;
          if (t === "lines") return s.split(/\r?\n/).map(function (x) { return x.trim(); }).filter(Boolean);
          if (t === "paragraphs") return s.split(/\r?\n\s*\r?\n/).map(function (x) { return x.replace(/\s*\r?\n\s*/g, " ").trim(); }).filter(Boolean);
          return t === "json" ? s : s.trim();
        };

      case "number":
        input = el("input", "field"); input.type = "number"; input.id = id; input.value = v == null ? "" : v;
        container.appendChild(field(f, input, id));
        return function () { var n = parseFloat(input.value); return isNaN(n) ? 0 : n; };

      case "bool":
        var lab = el("label", "check"); input = el("input"); input.type = "checkbox"; input.checked = !!v;
        lab.appendChild(input); lab.appendChild(document.createTextNode(" " + f.Label));
        container.appendChild(lab);
        return function () { return input.checked; };

      case "select":
        input = el("select", "field"); input.id = id;
        var opts = f.Source ? (choices[f.Source] || []).map(function (c) { return [c.Key, c.Value]; }) : (f.Options || []).map(function (o) { return [o, o]; });
        input.appendChild(new Option("Choose…", ""));
        if (v && !opts.some(function (o) { return o[0] === v; })) opts.push([v, v]);
        opts.forEach(function (o) { input.appendChild(new Option(o[1] && o[1] !== o[0] ? o[1] : o[0], o[0])); });
        input.value = str(v);
        container.appendChild(field(f, input, id));
        return function () { return input.value; };

      case "multi":
        var box = el("div", "multi"), chosen = v || [];
        (choices[f.Source] || []).forEach(function (c) {
          var l = el("label", "check"), cb = el("input"); cb.type = "checkbox"; cb.value = c.Key; cb.checked = chosen.indexOf(c.Key) >= 0;
          l.appendChild(cb); l.appendChild(document.createTextNode(" " + (c.Value || c.Key))); box.appendChild(l);
        });
        container.appendChild(field(f, box));
        return function () { return Array.prototype.slice.call(box.querySelectorAll("input:checked")).map(function (c) { return c.value; }); };

      case "image":
        var wrap = el("div", "imgf"), fig = el("figure", "imgf__pic"), img = el("img"), empty = el("span", "imgf__empty", "No image");
        img.alt = ""; fig.appendChild(img); fig.appendChild(empty);
        var row = el("div", "imgf__row"), btn = el("button", "btn btn--line btn--sm", "Upload image"), file = el("input");
        btn.type = "button"; file.type = "file"; file.accept = "image/jpeg,image/png,image/webp,image/gif"; file.hidden = true;
        input = el("input", "field"); input.id = id; input.value = str(v); input.placeholder = "Image address or built-in photo name";
        row.appendChild(btn); row.appendChild(input); row.appendChild(file);
        wrap.appendChild(fig); wrap.appendChild(row);
        var show = function () { var s = photoSrc(input.value); img.hidden = !s; empty.hidden = !!s; if (s) img.src = s; };
        btn.addEventListener("click", function () { file.value = ""; file.click(); });
        file.addEventListener("change", function () {
          window.YenRte.upload(file.files[0], function (url) { input.value = url; show(); dirty = true; }, "content");
        });
        input.addEventListener("input", show);
        show();
        container.appendChild(field(f, wrap, id));
        return function () { return input.value.trim(); };

      case "html":
        var rte = el("div", "rte");
        rte.setAttribute("data-rte", "");
        rte.innerHTML = '<div class="rte__bar" role="toolbar" aria-label="Formatting">' +
          [["p", "Text"], ["h2", "H2"], ["h3", "H3"], null, ["bold", "<b>B</b>"], ["italic", "<i>I</i>"], ["link", "Link"], null, ["ul", "• List"], ["ol", "1. List"], ["quote", "Quote"], ["callout", "Note box"], ["table", "Table"], ["image", "Image"], null, ["clear", "Clear"], ["html", "HTML"]]
            .map(function (b) { return b ? '<button type="button" data-cmd="' + b[0] + '">' + b[1] + "</button>" : '<span class="rte__sep"></span>'; }).join("") +
          '</div><div class="rte__area prose-admin" contenteditable="true" data-rte-area="" role="textbox" aria-multiline="true" aria-label="' + esc(f.Label) + '"></div>' +
          '<textarea class="field rte__html" data-rte-source="" spellcheck="false" rows="20"></textarea>' +
          '<input type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden data-rte-file="">';
        $("[data-rte-source]", rte).value = str(v);
        container.appendChild(field(f, rte));
        var ed = window.YenRte.init(rte, function () { dirty = true; });
        editors.push(ed);
        return function () { ed.sync(); return $("[data-rte-source]", rte).value; };

      case "items":
        var list = el("div", "items"), add = el("button", "btn btn--line btn--sm items__add", "+ Add " + esc((f.ItemLabel || "item").toLowerCase()));
        add.type = "button";
        var rows = [];
        var addRow = function (data, focus) {
          var card = el("div", "items__card"), head = el("div", "items__head"), body = el("div", "items__body");
          head.innerHTML = '<span class="items__n"></span><span class="items__tools">' +
            '<button type="button" class="btn btn--ghost btn--sm" data-act="up" aria-label="Move up">↑</button>' +
            '<button type="button" class="btn btn--ghost btn--sm" data-act="down" aria-label="Move down">↓</button>' +
            '<button type="button" class="btn btn--ghost btn--sm" data-act="del" aria-label="Remove">Remove</button></span>';
          var row = { card: card, get: renderFields(f.Fields, data || {}, body) };
          card.appendChild(head); card.appendChild(body);
          head.addEventListener("click", function (e) {
            var b = e.target.closest("[data-act]"); if (!b) return;
            var i = rows.indexOf(row), a = b.getAttribute("data-act");
            if (a === "del") { if (!window.confirm("Remove this " + (f.ItemLabel || "item").toLowerCase() + "?")) return; rows.splice(i, 1); card.remove(); }
            else { var j = a === "up" ? i - 1 : i + 1; if (j < 0 || j >= rows.length) return; rows.splice(i, 1); rows.splice(j, 0, row); }
            rows.forEach(function (r) { list.appendChild(r.card); }); number(); dirty = true;
          });
          rows.push(row); list.appendChild(card); number();
          if (focus) { var first = body.querySelector("input, textarea, select"); if (first) first.focus(); }
        };
        var number = function () { rows.forEach(function (r, i) { r.card.querySelector(".items__n").textContent = (f.ItemLabel || "Item") + " " + (i + 1); }); };
        (v || []).forEach(function (d) { addRow(d); });
        add.addEventListener("click", function () { addRow({}, true); dirty = true; });
        var wrapItems = el("div"); wrapItems.appendChild(list); wrapItems.appendChild(add);
        container.appendChild(field(f, wrapItems));
        return function () { return rows.map(function (r) { return r.get(); }); };

      case "object":
        var fs = el("fieldset", "fgroup"); fs.appendChild(el("legend", "", esc(f.Label)));
        var g = renderFields(f.Fields, v || {}, fs);
        container.appendChild(fs);
        return function () { return g(); };

      default: // text, url, email
        input = el("input", "field"); input.id = id; input.value = str(v);
        input.type = t === "url" ? "url" : t === "email" ? "email" : "text";
        if (t === "url") input.placeholder = "https://";
        if (f.Name === "slug" || f.Name === "id") input.setAttribute("data-slug", "");
        if (f.Name === "name" || f.Name === "title") input.setAttribute("data-slug-source", "");
        container.appendChild(field(f, input, id));
        return function () { return input.value.trim(); };
    }
  }

  var collect = renderFields(schema, value, host);

  /* New items: the page address follows the name until it is edited. */
  var slug = $("[data-slug]", host), src = $("[data-slug-source]", host);
  if (slug && src) {
    var touched = !!slug.value;
    src.addEventListener("input", function () { if (!touched) slug.value = slugify(src.value); });
    slug.addEventListener("input", function () { touched = true; });
    slug.addEventListener("blur", function () { slug.value = slugify(slug.value); });
  }

  host.addEventListener("input", function () { dirty = true; });
  host.addEventListener("change", function () { dirty = true; });
  page.addEventListener("submit", function () { out.value = JSON.stringify(collect()); dirty = false; });
  window.addEventListener("beforeunload", function (e) { if (dirty) { e.preventDefault(); e.returnValue = ""; } });
})();
