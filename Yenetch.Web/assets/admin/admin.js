/* Yenetch admin: charts, table rows, confirmations, filters, mobile menu and the campaign preview. No dependencies. */
(function () {
  "use strict";

  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };
  var NS = "http://www.w3.org/2000/svg";

  function svg(tag, attrs, parent) {
    var el = document.createElementNS(NS, tag);
    for (var k in attrs) el.setAttribute(k, attrs[k]);
    if (parent) parent.appendChild(el);
    return el;
  }
  function fmt(n) { return Math.round(n).toLocaleString("en-IN"); }
  function niceMax(v) {
    if (v <= 4) return 4;
    var p = Math.pow(10, Math.floor(Math.log10(v))), m = v / p;
    return (m <= 1 ? 1 : m <= 2 ? 2 : m <= 2.5 ? 2.5 : m <= 5 ? 5 : 10) * p;
  }
  function dayLabel(s) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(s)) return s;
    var d = new Date(s + "T00:00:00");
    return d.toLocaleDateString("en-IN", { day: "numeric", month: "short" });
  }

  /* ---- Charts: one series at a time (area for time series, bars for categories) with a hover tooltip ---- */
  function drawChart(box, seriesIndex) {
    var cfg = JSON.parse(box.getAttribute("data-chart"));
    var s = cfg.series[seriesIndex || 0];
    var labels = cfg.labels, vals = s.values, n = vals.length;
    box.innerHTML = "";
    var W = box.clientWidth || 600, H = box.clientHeight || 240, L = 40, R = 8, T = 10, B = 26;
    var max = niceMax(Math.max.apply(null, vals.concat([0])));
    var root = svg("svg", { viewBox: "0 0 " + W + " " + H, role: "img", "aria-label": s.name + " chart" }, box);
    var defs = svg("defs", {}, root), g = svg("linearGradient", { id: "chart-fill", x1: 0, x2: 0, y1: 0, y2: 1 }, defs);
    svg("stop", { offset: "0%", "stop-color": "#0066FF", "stop-opacity": ".18" }, g);
    svg("stop", { offset: "100%", "stop-color": "#0066FF", "stop-opacity": "0" }, g);
    var x = function (i) { return cfg.type === "bar" ? L + (i + .5) * (W - L - R) / n : L + (n <= 1 ? 0 : i * (W - L - R) / (n - 1)); };
    var y = function (v) { return T + (H - T - B) * (1 - v / max); };

    for (var t = 0; t <= 4; t++) {
      var v = max * t / 4, yy = y(v);
      svg("line", { x1: L, x2: W - R, y1: yy, y2: yy, "class": "grid-line" }, root);
      var tx = svg("text", { x: L - 8, y: yy + 4, "text-anchor": "end", "class": "axis" }, root); tx.textContent = fmt(v);
    }
    var every = Math.max(1, Math.ceil(n / Math.max(2, Math.floor((W - L) / 70))));
    labels.forEach(function (lb, i) {
      if (i % every && i !== n - 1) return;
      if (i === n - 1 && i % every && (n - 1) % every < every / 2) return;
      var tx = svg("text", { x: x(i), y: H - 6, "text-anchor": "middle", "class": "axis" }, root); tx.textContent = dayLabel(lb);
    });

    if (cfg.type === "bar") {
      var bw = Math.max(2, Math.min(36, (W - L - R) / n - 4));
      vals.forEach(function (v, i) {
        var h = Math.max(v > 0 ? 2 : 0, (H - T - B) - (y(v) - T));
        svg("rect", { x: x(i) - bw / 2, y: y(v), width: bw, height: h, rx: Math.min(4, bw / 3), "class": "bar-mark" }, root);
      });
    } else {
      var d = vals.map(function (v, i) { return (i ? "L" : "M") + x(i).toFixed(1) + " " + y(v).toFixed(1); }).join(" ");
      svg("path", { d: d + " L" + x(n - 1) + " " + y(0) + " L" + x(0) + " " + y(0) + " Z", "class": "area" }, root);
      svg("path", { d: d, "class": "line" }, root);
    }

    var cross = svg("line", { y1: T, y2: H - B, "class": "cross", visibility: "hidden" }, root);
    var dot = svg("circle", { r: 4.5, "class": "dot", visibility: "hidden" }, root);
    var tip = document.createElement("div"); tip.className = "tip"; box.appendChild(tip);
    root.addEventListener("mousemove", function (e) {
      var r = root.getBoundingClientRect(), px = (e.clientX - r.left) * W / r.width;
      var i = cfg.type === "bar" ? Math.floor((px - L) / ((W - L - R) / n)) : Math.round((px - L) / ((W - L - R) / Math.max(1, n - 1)));
      i = Math.max(0, Math.min(n - 1, i));
      cross.setAttribute("x1", x(i)); cross.setAttribute("x2", x(i)); cross.setAttribute("visibility", "visible");
      if (cfg.type !== "bar") { dot.setAttribute("cx", x(i)); dot.setAttribute("cy", y(vals[i])); dot.setAttribute("visibility", "visible"); }
      tip.innerHTML = "<b>" + fmt(vals[i]) + "</b>" + s.name + " · " + dayLabel(labels[i]);
      tip.style.left = (x(i) * r.width / W) + "px"; tip.style.top = (y(vals[i]) * r.height / H) + "px";
      tip.classList.add("is-on");
    });
    root.addEventListener("mouseleave", function () { cross.setAttribute("visibility", "hidden"); dot.setAttribute("visibility", "hidden"); tip.classList.remove("is-on"); });
  }

  function charts() {
    $$("[data-chart]").forEach(function (box) {
      box._series = box._series || 0;
      drawChart(box, box._series);
    });
    $$("[data-chart-tabs]").forEach(function (tabs) {
      var box = document.getElementById(tabs.getAttribute("data-chart-tabs"));
      $$("button", tabs).forEach(function (b, i) {
        b.addEventListener("click", function () {
          $$("button", tabs).forEach(function (x) { x.classList.remove("is-on"); });
          b.classList.add("is-on"); box._series = i; drawChart(box, i);
        });
      });
    });
    var t;
    addEventListener("resize", function () { clearTimeout(t); t = setTimeout(function () { $$("[data-chart]").forEach(function (b) { drawChart(b, b._series); }); }, 150); });
  }

  /* ---- Rows that open a record ---- */
  document.addEventListener("click", function (e) {
    var tr = e.target.closest("tr[data-href]");
    if (!tr || e.target.closest("a, button, input, select, label, textarea")) return;
    if (e.metaKey || e.ctrlKey) window.open(tr.getAttribute("data-href")); else location.href = tr.getAttribute("data-href");
  });

  /* ---- Confirm before destructive or outward actions ---- */
  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-confirm]");
    if (b && !confirm(b.getAttribute("data-confirm"))) { e.preventDefault(); e.stopImmediatePropagation(); }
  }, true);

  /* ---- Filters submit on change ---- */
  $$("form[data-autosubmit] select, form[data-autosubmit] input[type=date]").forEach(function (el) {
    el.addEventListener("change", function () { el.form.submit(); });
  });

  /* ---- Select all rows for bulk actions ---- */
  $$("[data-check-all]").forEach(function (all) {
    all.addEventListener("change", function () {
      $$("input[type=checkbox][name='" + all.getAttribute("data-check-all") + "']").forEach(function (c) { c.checked = all.checked; });
    });
  });

  /* ---- Mobile menu ---- */
  var menu = $("[data-menu]");
  if (menu) menu.addEventListener("click", function () { $(".shell").classList.toggle("is-menu"); });
  document.addEventListener("click", function (e) {
    var shell = $(".shell.is-menu");
    if (shell && !e.target.closest(".side") && !e.target.closest("[data-menu]")) shell.classList.remove("is-menu");
  });

  /* ---- Campaign live preview ---- */
  var body = $("[data-preview-source]"), frame = $("[data-preview]");
  if (body && frame) {
    var shellHtml = frame.getAttribute("data-shell") || "{{content}}";
    var render = function () {
      var subj = $("[data-preview-subject]"), pre = $("[data-preview-preheader]");
      frame.srcdoc = shellHtml.replace("{{content}}", body.value.replace(/\{\{name\}\}/g, "Priya")).replace("{{preheader}}", pre ? pre.value : "");
      if (subj) document.title = (subj.value || "New campaign") + " · Yenetch Admin";
    };
    var timer;
    body.addEventListener("input", function () { clearTimeout(timer); timer = setTimeout(render, 250); });
    render();
    $$("[data-snippet]").forEach(function (b) {
      b.addEventListener("click", function () {
        var s = b.getAttribute("data-snippet"), i = body.selectionStart || body.value.length;
        body.value = body.value.slice(0, i) + s + body.value.slice(body.selectionEnd || i);
        body.focus(); body.selectionStart = body.selectionEnd = i + s.length; render();
      });
    });
  }

  /* ---- Pages that watch a running job refresh themselves ---- */
  var every = document.body.getAttribute("data-refresh");
  if (every) setTimeout(function () { location.reload(); }, parseInt(every, 10) * 1000);

  /* ---- Two-step sign-in: QR code for the authenticator app (assets/admin/qrcode.js, MIT) ---- */
  $$("[data-qr]").forEach(function (box) {
    if (!window.qrcode) return;
    var q = window.qrcode(0, "M");
    q.addData(box.getAttribute("data-qr"));
    q.make();
    box.innerHTML = q.createSvgTag({ cellSize: 5, margin: 2, scalable: true });
  });

  /* ---- Copy buttons ---- */
  $$("[data-copy]").forEach(function (b) {
    b.addEventListener("click", function () {
      var text = b.getAttribute("data-copy").replace(/\\n/g, "\n"), label = b.textContent;
      var done = function () { b.textContent = "Copied"; setTimeout(function () { b.textContent = label; }, 1600); };
      if (navigator.clipboard) navigator.clipboard.writeText(text).then(done); else { var t = document.createElement("textarea"); t.value = text; document.body.appendChild(t); t.select(); document.execCommand("copy"); t.remove(); done(); }
    });
  });

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", charts); else charts();
})();
