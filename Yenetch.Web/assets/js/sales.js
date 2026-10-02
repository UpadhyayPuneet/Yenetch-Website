/* Yenetch sales tools: prices in the visitor's currency, the plan builder (/pricing), offer banners and popups
   (the free website audit popup and offers set in the admin), and the optional CAPTCHA on forms.
   Settings come from /api/site-data (window.YENETCH_CONFIG). Prices are always re-checked on the server. */
(function () {
  "use strict";
  var W = window, D = document;
  var C = W.YENETCH_CONFIG || {};
  var preview = function () { return D.body.classList.contains("is-preview"); };
  function $(s, r) { return (r || D).querySelector(s); }
  function $$(s, r) { return Array.prototype.slice.call((r || D).querySelectorAll(s)); }
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
  function store(k, v) { try { if (v === undefined) return localStorage.getItem(k); if (v === null) localStorage.removeItem(k); else localStorage.setItem(k, v); } catch (e) { return null; } }
  function track(n, l) { try { W.Yenetch && Yenetch.track && Yenetch.track(n, l); } catch (e) { } }
  var startedAt = Date.now();

  /* ================================================================ currency */
  var FX = C.fx || { base: "INR", currencies: [{ code: "INR", symbol: "₹", name: "Indian rupee", rate: 1 }], countries: {}, round: true };
  var byCode = {};
  (FX.currencies || []).forEach(function (c) { byCode[c.code] = c; });
  var current = "INR";

  function nice(v) {
    if (v <= 0) return 0;
    if (v < 10) return Math.ceil(v);
    if (v < 100) return Math.ceil(v / 5) * 5 - 1;
    if (v < 1000) return Math.ceil(v / 10) * 10 - 1;
    if (v < 10000) return Math.ceil(v / 50) * 50 - 1;
    var step = v < 100000 ? 500 : 5000;
    return Math.ceil(v / step) * step - 1;
  }
  function convert(inr, code) {
    var c = byCode[code];
    if (!c || code === "INR") return inr;
    var v = inr * c.rate;
    return FX.round ? nice(v) : Math.round(v * 100) / 100;
  }
  function format(v, code) {
    if (code === "INR" || !byCode[code]) return "₹" + Math.round(v).toLocaleString("en-IN");
    return byCode[code].symbol + v.toLocaleString("en-US", { minimumFractionDigits: v % 1 ? 2 : 0, maximumFractionDigits: 2 });
  }
  function paint() {
    $$("[data-inr]").forEach(function (el) {
      var inr = parseFloat(el.getAttribute("data-inr"));
      if (!isNaN(inr)) el.textContent = format(convert(inr, current), current);
    });
    $$("[data-currency-select]").forEach(function (s) { s.value = current; });
    $$(".cur-approx").forEach(function (n) { n.hidden = current === "INR"; });
  }
  function setCurrency(code, remember) {
    if (!byCode[code]) code = "INR";
    current = code;
    if (remember) store("yn_cur", code);
    paint();
    D.dispatchEvent(new CustomEvent("yn:currency", { detail: code }));
  }
  function initCurrency() {
    var selects = $$("[data-currency-select]");
    selects.forEach(function (s) {
      s.innerHTML = (FX.currencies || []).map(function (c) { return '<option value="' + esc(c.code) + '">' + esc(c.symbol.trim() + " " + c.code) + "</option>"; }).join("");
      s.addEventListener("change", function () { setCurrency(s.value, true); track("currency", s.value); });
      if ((FX.currencies || []).length < 2) s.closest("[data-currency-switch]").hidden = true;
    });
    var saved = store("yn_cur");
    if (saved && byCode[saved]) { setCurrency(saved); return; }
    if (!$("[data-inr]") && !$("[data-builder]")) return;
    // First visit: guess from the visitor's country, then their browser language.
    var guess = function () {
      var lang = (navigator.language || "").split("-")[1];
      var code = lang && FX.countries ? FX.countries[lang.toUpperCase()] : null;
      setCurrency(code && byCode[code] ? code : "INR");
    };
    var cached = null;
    try { cached = sessionStorage.getItem("yn_geo"); } catch (e) { }
    if (cached) { setCurrency(byCode[cached] ? cached : "INR"); return; }
    if (preview() || !W.fetch) return guess();
    fetch("/api/geo", { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
      try { sessionStorage.setItem("yn_geo", j.currency || ""); } catch (e) { }
      if (j.currency && byCode[j.currency]) setCurrency(j.currency); else guess();
    }).catch(guess);
  }
  W.Yenetch = W.Yenetch || {};
  Yenetch.currency = function () { return current; };

  /* ================================================================ CAPTCHA (Turnstile or reCAPTCHA v3, if switched on) */
  var cap = C.captcha, capLoading = null, widgets = [];
  function loadCaptcha() {
    if (!cap) return Promise.resolve(false);
    if (capLoading) return capLoading;
    capLoading = new Promise(function (done) {
      var s = D.createElement("script");
      s.src = cap.provider === "turnstile" ? "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit" : "https://www.google.com/recaptcha/api.js?render=" + encodeURIComponent(cap.siteKey);
      s.async = true; s.onload = function () { done(true); }; s.onerror = function () { done(false); };
      D.head.appendChild(s);
    });
    return capLoading;
  }
  Yenetch.captcha = {
    mount: function (el) {
      if (!cap || !el || el.getAttribute("data-mounted")) return;
      el.setAttribute("data-mounted", "1");
      loadCaptcha().then(function (ok) {
        if (!ok) return;
        if (cap.provider === "turnstile") {
          var wait = function () { if (W.turnstile) widgets.push({ el: el, id: W.turnstile.render(el, { sitekey: cap.siteKey, size: "flexible" }) }); else setTimeout(wait, 100); };
          wait();
          return;
        }
        // reCAPTCHA v3 is invisible: keep a fresh token (they last 2 minutes) in a hidden field, so any form posts it.
        var input = D.createElement("input"); input.type = "hidden"; input.name = "captcha"; el.appendChild(input);
        var refresh = function () {
          if (!W.grecaptcha) return setTimeout(refresh, 200);
          W.grecaptcha.ready(function () { W.grecaptcha.execute(cap.siteKey, { action: "form" }).then(function (t) { input.value = t; }); });
        };
        refresh(); setInterval(refresh, 100000);
      });
    },
    token: function (el, action) {
      if (!cap) return Promise.resolve("");
      return loadCaptcha().then(function () {
        if (cap.provider === "turnstile") {
          var w = widgets.filter(function (x) { return x.el === el; })[0];
          return w && W.turnstile ? W.turnstile.getResponse(w.id) || "" : "";
        }
        return new Promise(function (done) {
          if (!W.grecaptcha) return done("");
          W.grecaptcha.ready(function () { W.grecaptcha.execute(cap.siteKey, { action: action || "submit" }).then(done, function () { done(""); }); });
        });
      });
    },
    reset: function (el) { var w = widgets.filter(function (x) { return x.el === el; })[0]; if (w && W.turnstile) W.turnstile.reset(w.id); }
  };

  /* ================================================================ offer banners */
  function initBanners() {
    $$("[data-offer]").forEach(function (bar) {
      var id = bar.getAttribute("data-offer"), until = parseInt(store("yn_offer_x_" + id) || "0", 10);
      if (until > Date.now()) { bar.hidden = true; return; }
      var x = $("[data-offer-close]", bar);
      if (x) x.addEventListener("click", function () { bar.hidden = true; store("yn_offer_x_" + id, String(Date.now() + 3 * 864e5)); });
      var cta = $(".offer-bar__cta", bar);
      if (cta) cta.addEventListener("click", function () { track("offer_click", id); });
    });
  }

  /* ================================================================ popups */
  var popupOpen = false;
  function onPage(paths, services) {
    var path = location.pathname.replace(/\/$/, "") || "/";
    var hit = (paths || []).some(function (p) {
      p = String(p || "").trim(); if (!p) return false;
      if (p === "*") return true;
      if (p.slice(-1) === "*") { var base = p.slice(0, -1).replace(/\/$/, ""); return path === base || path.indexOf(base + "/") === 0; }
      return (p.replace(/\/$/, "") || "/") === path;
    });
    if (!hit && services && services.length && path.indexOf("/services/") === 0) hit = services.indexOf(path.slice(10)) > -1;
    return hit;
  }
  function dialog(inner, label, onClose) {
    if (popupOpen) return null;
    popupOpen = true;
    var last = D.activeElement;
    var wrap = D.createElement("div");
    wrap.className = "pop";
    wrap.innerHTML = '<div class="pop__card" role="dialog" aria-modal="true" aria-label="' + esc(label) + '" tabindex="-1"><button type="button" class="pop__x" aria-label="Close">×</button>' + inner + "</div>";
    D.body.appendChild(wrap);
    var card = $(".pop__card", wrap);
    requestAnimationFrame(function () { wrap.classList.add("is-on"); card.focus(); });
    var close = function () {
      wrap.classList.remove("is-on"); setTimeout(function () { wrap.remove(); }, 250);
      D.removeEventListener("keydown", key); popupOpen = false; if (last && last.focus) last.focus(); if (onClose) onClose();
    };
    var key = function (e) {
      if (e.key === "Escape") close();
      if (e.key === "Tab") {
        var f = $$("a[href],button,input,select,textarea", card).filter(function (x) { return !x.disabled && x.offsetParent !== null; });
        if (!f.length) return;
        if (e.shiftKey && D.activeElement === f[0]) { e.preventDefault(); f[f.length - 1].focus(); }
        else if (!e.shiftKey && D.activeElement === f[f.length - 1]) { e.preventDefault(); f[0].focus(); }
      }
    };
    D.addEventListener("keydown", key);
    $(".pop__x", wrap).addEventListener("click", close);
    wrap.addEventListener("click", function (e) { if (e.target === wrap) close(); });
    return { el: card, close: close };
  }
  // Popups never cover the cookie choice: they wait until the visitor has answered it.
  function afterCookieChoice(fn) {
    if (!D.querySelector(".cookie")) return fn();
    var tries = 0, t = setInterval(function () { if (!D.querySelector(".cookie") || ++tries > 150) { clearInterval(t); if (!D.querySelector(".cookie")) setTimeout(fn, 1500); } }, 2000);
  }
  function when(cfg, fn) {
    // Exit intent on desktop; on phones and tablets (no mouse) a delay instead.
    var fired = false, fire = function () { if (!fired && !popupOpen) { fired = true; afterCookieChoice(function () { if (!popupOpen) fn(); }); } };
    var touch = W.matchMedia && matchMedia("(hover: none)").matches;
    if (cfg.trigger === "scroll") {
      var onScroll = function () { var h = D.documentElement; if ((h.scrollTop + innerHeight) / h.scrollHeight * 100 >= (cfg.scroll || 60)) { removeEventListener("scroll", onScroll); fire(); } };
      addEventListener("scroll", onScroll, { passive: true });
    } else if (cfg.trigger === "delay" || touch) {
      setTimeout(fire, Math.max(5, cfg.delay || 25) * 1000);
    } else {
      D.addEventListener("mouseout", function (e) { if (!e.relatedTarget && e.clientY <= 0) fire(); });
      setTimeout(function () { /* never on the first seconds of a visit */ }, 0);
    }
  }
  function initAuditPopup() {
    var a = (C.popups || {}).audit;
    if (!a || preview()) return;
    if (onPage(a.exclude)) return;
    var until = parseInt(store("yn_pop_audit") || "0", 10);
    if (until > Date.now()) return;
    // Give people a moment with the page first.
    setTimeout(function () {
      when(a, function () {
        var d = dialog('<span class="pop__kicker">Free tool</span><h2>' + esc(a.title) + "</h2><p>" + esc(a.text) + "</p>" +
          '<form class="pop__form" novalidate><label class="sr-only" for="pop-url">Your website</label><input class="field" id="pop-url" name="url" placeholder="yourbusiness.com" inputmode="url" autocomplete="url" required maxlength="300">' +
          '<label class="sr-only" for="pop-email">Your email</label><input class="field" id="pop-email" name="email" type="email" placeholder="you@company.com" autocomplete="email" required maxlength="160">' +
          '<button class="btn btn--blue" type="submit">' + esc(a.button) + '</button><p class="pop__err" role="alert" hidden></p></form><p class="pop__small">Takes under a minute. No sign-up.</p>', "Free website audit");
        if (!d) return;
        store("yn_pop_audit", String(Date.now() + Math.max(1, a.days || 7) * 864e5));
        track("popup_open", "audit");
        $("form", d.el).addEventListener("submit", function (e) {
          e.preventDefault();
          var url = this.url.value.trim(), email = this.email.value.trim(), err = $(".pop__err", d.el);
          if (url.length < 4 || url.indexOf(".") < 0) { err.textContent = "Enter your website address."; err.hidden = false; this.url.focus(); return; }
          if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { err.textContent = "Enter your email for the report."; err.hidden = false; this.email.focus(); return; }
          track("popup_submit", "audit");
          location.href = "/website-audit?url=" + encodeURIComponent(url) + "&email=" + encodeURIComponent(email) + "#audit";
        });
      });
    }, 4000);
  }
  function initOfferPopups() {
    if (preview()) return;
    var offers = ((W.YENETCH_DATA || {}).offers || []).filter(function (o) { return o.popup && onPage(o.pages, o.services); });
    var o = offers.filter(function (x) { return parseInt(store("yn_pop_offer_" + x.id) || "0", 10) < Date.now(); })[0];
    if (!o) return;
    setTimeout(function () { afterCookieChoice(function () {
      if (popupOpen) return;
      var d = dialog((o.badge ? '<span class="pop__badge">' + esc(o.badge) + "</span>" : "") + "<h2>" + esc(o.title) + "</h2>" + (o.text ? "<p>" + esc(o.text) + "</p>" : "") +
        (o.endsLabel ? '<p class="pop__small">Ends ' + esc(o.endsLabel) + "</p>" : "") +
        (o.ctaUrl ? '<a class="btn btn--blue" href="' + esc(o.ctaUrl) + '">' + esc(o.ctaText || "See offer") + "</a>" : ""), "Offer");
      if (!d) return;
      store("yn_pop_offer_" + o.id, String(Date.now() + Math.max(1, (C.popups || {}).offerDays || 3) * 864e5));
      track("popup_open", "offer " + o.id);
    }); }, 9000);
  }

  /* ================================================================ plan builder (/pricing) */
  function initBuilder(root) {
    var dataEl = $("#builder-data", root), cat;
    try { cat = JSON.parse(dataEl.textContent); } catch (e) { return; }
    var plans = {}, addons = {};
    cat.plans.forEach(function (p) { plans[p.id] = p; });
    cat.addons.forEach(function (a) { addons[a.id] = a; });
    var state = { items: [], code: "" };
    try { var s = JSON.parse(store("yn_plan") || "null"); if (s && s.items) state = s; } catch (e) { }
    state.items = state.items.filter(function (i) { return i.kind === "addon" ? addons[i.id] : plans[i.id]; });
    var add = new URLSearchParams(location.search).get("add");
    if (add && plans[add]) addPlan(add, true);

    var lines = $("[data-pb-lines]", root), totals = $("[data-pb-totals]", root), offerEl = $("[data-pb-offer]", root), go = $("[data-pb-go]", root);
    var formWrap = $("[data-pb-form-wrap]"), form = $("[data-pb-form]"), done = $("[data-pb-done]"), errEl = $("[data-pb-error]");
    var codeForm = $("[data-pb-code]", root), last = null, timer = null, seq = 0;

    function save() { store("yn_plan", JSON.stringify(state)); }
    function has(kind, id) { return state.items.some(function (i) { return i.kind === kind && i.id === id; }); }
    function addPlan(id, quiet) {
      var p = plans[id]; if (!p) return;
      // One plan per service: choosing another plan of the same service replaces it.
      var replaced = state.items.filter(function (i) { return i.kind === "plan" && plans[i.id] && plans[i.id].service === p.service && i.id !== id; });
      state.items = state.items.filter(function (i) { return replaced.indexOf(i) < 0; });
      if (!has("plan", id)) state.items.push({ kind: "plan", id: id, qty: p.billing === "hourly" ? 20 : 1 });
      save(); if (!quiet) { refresh(); if (W.Yenetch.toast) Yenetch.toast((p.serviceName ? p.serviceName.split(" (")[0] + " " : "") + p.name + " added to your plan"); track("plan_add", id); }
    }
    function remove(kind, id) { state.items = state.items.filter(function (i) { return !(i.kind === kind && i.id === id); }); save(); refresh(); }
    function setQty(kind, id, qty) { state.items.forEach(function (i) { if (i.kind === kind && i.id === id) i.qty = qty; }); save(); refresh(); }

    D.addEventListener("click", function (e) {
      var b = e.target.closest("[data-add-plan]");
      if (b) { var id = b.getAttribute("data-add-plan"); if (has("plan", id)) remove("plan", id); else addPlan(id); return; }
      var r = e.target.closest("[data-pb-remove]");
      if (r) { remove(r.getAttribute("data-kind"), r.getAttribute("data-pb-remove")); return; }
      var ad = e.target.closest("[data-pb-addon]");
      if (ad) {
        var aid = ad.getAttribute("data-pb-addon"), a = addons[aid];
        if (has("addon", aid)) remove("addon", aid); else { state.items.push({ kind: "addon", id: aid, qty: Math.max(1, a.min || 1) }); save(); refresh(); track("addon_add", aid); }
      }
    });
    root.addEventListener("change", function (e) {
      var q = e.target.closest("[data-pb-qty]");
      if (!q) return;
      var v = Math.max(1, parseInt(q.value, 10) || 1);
      setQty(q.getAttribute("data-kind"), q.getAttribute("data-pb-qty"), v);
    });

    // Extras grouped by heading.
    var groups = {};
    cat.addons.forEach(function (a) { (groups[a.group] = groups[a.group] || []).push(a); });
    var extrasHtml = Object.keys(groups).map(function (g) {
      return '<div class="pb__group"><h3>' + esc(g) + '</h3><div class="pb__addons">' + groups[g].map(function (a) {
        return '<div class="pb__addon" data-addon-row="' + esc(a.id) + '"><div><b>' + esc(a.name) + "</b>" + (a.description ? "<p>" + esc(a.description) + "</p>" : "") +
          (cat.show && a.price ? '<span class="pb__unit"><span data-inr="' + a.price + '">₹' + a.price.toLocaleString("en-IN") + "</span> per " + esc(a.unit || "unit") + (a.billing === "monthly" ? " / month" : "") + "</span>" : "") + "</div>" +
          '<div class="pb__addon-ctl"><input class="field field--sm" type="number" min="' + (a.min || 1) + '"' + (a.max ? ' max="' + a.max + '"' : "") + ' aria-label="Quantity of ' + esc(a.name) + '" data-pb-qty="' + esc(a.id) + '" data-kind="addon" hidden>' +
          '<button type="button" class="btn btn--line btn--sm" data-pb-addon="' + esc(a.id) + '" aria-pressed="false">Add</button></div></div>';
      }).join("") + "</div></div>";
    }).join("");
    $("[data-pb-extras]", root).innerHTML = extrasHtml ? '<h3 class="pb__extras-title">Extras</h3>' + extrasHtml : "";

    function syncButtons() {
      $$("[data-add-plan]").forEach(function (b) {
        var on = has("plan", b.getAttribute("data-add-plan"));
        b.setAttribute("aria-pressed", on ? "true" : "false");
        b.textContent = on ? "Added ✓" : "Add to my plan";
        var card = b.closest(".plan"); if (card) card.classList.toggle("is-picked", on);
      });
      $$("[data-pb-addon]", root).forEach(function (b) {
        var id = b.getAttribute("data-pb-addon"), on = has("addon", id), it = state.items.filter(function (i) { return i.kind === "addon" && i.id === id; })[0];
        b.setAttribute("aria-pressed", on ? "true" : "false"); b.textContent = on ? "Remove" : "Add";
        var q = $('[data-pb-qty="' + id + '"]', root); q.hidden = !on; if (it) q.value = it.qty;
      });
      $("[data-pb-empty]", root).hidden = state.items.length > 0;
      go.disabled = state.items.length === 0;
    }

    function render(q) {
      last = q;
      if (!state.items.length) { lines.innerHTML = '<li class="pb__none">Nothing added yet.</li>'; totals.innerHTML = ""; offerEl.hidden = true; return; }
      var per = { monthly: "/mo", yearly: "/yr" };
      lines.innerHTML = q.lines.map(function (l) {
        var qty = l.unit ? (l.kind === "plan" ? '<input class="field field--xs" type="number" min="1" value="' + l.qty + '" data-pb-qty="' + esc(l.id) + '" data-kind="plan" aria-label="Hours"> ' + esc(l.unit) + "s" : " × " + l.qty + " " + esc(l.unit) + (l.qty === 1 ? "" : "s")) : "";
        return '<li><div><b>' + esc(l.kind === "plan" && l.service ? l.service.split(" (")[0] + " · " + l.name : l.name) + "</b>" + (qty ? "<small>" + qty + "</small>" : "") +
          (l.setup ? "<small>+ " + esc(l.setup) + " set-up</small>" : "") + "</div><span>" + (q.show ? (l.from ? "<small>from</small> " : "") + esc(l.amount) + (per[l.billing] || "") : "") +
          '</span><button type="button" class="pb__x" data-pb-remove="' + esc(l.id) + '" data-kind="' + l.kind + '" aria-label="Remove ' + esc(l.name) + '">×</button></li>';
      }).join("");
      if (q.offer) { offerEl.innerHTML = "<b>" + esc(q.offer) + "</b> " + esc(q.offerNote || ""); offerEl.hidden = false; offerEl.className = "pb__offer"; }
      else if (q.codeError) { offerEl.textContent = q.codeError; offerEl.hidden = false; offerEl.className = "pb__offer pb__offer--bad"; }
      else offerEl.hidden = true;
      if (!q.show) { totals.innerHTML = '<div class="pb__total"><dt>Estimate</dt><dd>We will send you prices</dd></div>'; return; }
      var t = "";
      if (q.discount) t += '<div class="pb__disc"><dt>Discount</dt><dd>−' + esc(q.discount) + "</dd></div>";
      t += '<div class="pb__total"><dt>First payment' + (q.hasFrom ? " (from)" : "") + "</dt><dd>" + esc(q.dueNow) + "</dd></div>";
      if (q.monthly) t += "<div><dt>Then monthly</dt><dd>" + esc(q.monthly) + "</dd></div>";
      if (q.yearly) t += "<div><dt>Yearly items</dt><dd>" + esc(q.yearly) + "</dd></div>";
      t += '<div class="pb__tax"><dt>' + esc(q.tax) + "</dt><dd>" + (q.currency === "INR" ? "+ " + q.taxPct + "% extra" : "if applicable") + "</dd></div>";
      totals.innerHTML = t;
    }

    function localQuote() {
      // Static preview only (no server): a simple sum in rupees.
      var q = { show: true, currency: "INR", lines: [], tax: cat.tax.name, taxPct: cat.tax.pct }, one = 0, mo = 0;
      state.items.forEach(function (i) {
        var p = i.kind === "plan" ? plans[i.id] : addons[i.id]; if (!p) return;
        var amt = p.price * i.qty, f = function (v) { return "₹" + Math.round(v).toLocaleString("en-IN"); };
        q.lines.push({ kind: i.kind, id: i.id, name: p.name, service: p.serviceName, qty: i.qty, unit: i.kind === "addon" ? p.unit : (p.billing === "hourly" ? "hour" : null), billing: p.billing, from: p.from, amount: f(amt), setup: p.setup ? f(p.setup) : null });
        if (p.billing === "monthly") mo += amt; else one += amt; one += p.setup || 0;
      });
      q.dueNow = "₹" + Math.round(one + mo).toLocaleString("en-IN"); q.monthly = mo ? "₹" + mo.toLocaleString("en-IN") : null;
      return q;
    }

    function refresh() {
      syncButtons();
      if (!state.items.length) { render({ lines: [] }); return; }
      if (preview() || !W.fetch) { render(localQuote()); return; }
      clearTimeout(timer);
      root.classList.add("is-busy");
      timer = setTimeout(function () {
        var my = ++seq;
        fetch("/api/quote", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({ mode: "price", items: state.items, currency: current, code: state.code }) })
          .then(function (r) { return r.json(); }).then(function (q) { if (my === seq) { root.classList.remove("is-busy"); render(q); } })
          .catch(function () { root.classList.remove("is-busy"); render(localQuote()); });
      }, 200);
    }

    codeForm.addEventListener("submit", function (e) { e.preventDefault(); state.code = codeForm.code.value.trim(); save(); refresh(); track("offer_code", state.code); });
    if (state.code) codeForm.code.value = state.code;
    D.addEventListener("yn:currency", refresh);

    go.addEventListener("click", function () {
      formWrap.hidden = false; form.hidden = false; done.hidden = true;
      Yenetch.captcha.mount($("[data-captcha]", form));
      formWrap.scrollIntoView({ behavior: "smooth", block: "start" });
      setTimeout(function () { form.name.focus({ preventScroll: true }); }, 400);
      track("quote_start", String(state.items.length));
    });
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      var f = form, fail = function (m, el) { errEl.textContent = m; errEl.hidden = false; if (el) el.focus(); };
      errEl.hidden = true;
      if (f.name.value.trim().length < 2) return fail("Enter your name.", f.name);
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.value.trim())) return fail("Enter a valid email address.", f.email);
      if (f.phone.value.replace(/\D/g, "").length < 8) return fail("Enter a phone number we can reach you on.", f.phone);
      var btn = $("button[type=submit]", f); btn.disabled = true; btn.textContent = "Sending…";
      var finish = function (msg) {
        f.hidden = true; done.hidden = false;
        if (msg) $("[data-pb-done-text]", done).textContent = msg;
        state = { items: [], code: "" }; save(); syncButtons(); render({ lines: [] });
        track("quote_sent", "");
      };
      if (preview() || !W.fetch) { finish("Design preview: on the live site your quote is emailed to you and saved in the CRM."); return; }
      Yenetch.captcha.token($("[data-captcha]", f), "quote").then(function (token) {
        return fetch("/api/quote", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify({
          mode: "submit", items: state.items, currency: current, code: state.code, name: f.name.value.trim(), email: f.email.value.trim(), phone: f.phone.value.trim(),
          company: f.company.value.trim(), notes: f.notes.value.trim(), website: f.website.value, t: startedAt, captcha: token, page: location.pathname }) });
      }).then(function (r) { return r.json().catch(function () { return { ok: false }; }); }).then(function (j) {
        if (j.ok) finish(j.total ? "We've emailed your estimate (" + j.total + " first payment). A specialist will call you within one working day." : null);
        else { fail(j.error || "We could not send this. Please try again or call us."); Yenetch.captcha.reset($("[data-captcha]", f)); }
      }).catch(function () { fail("We could not send this. Check your connection and try again."); })
        .then(function () { btn.disabled = false; btn.textContent = "Email me this quote"; });
    });

    if (add) setTimeout(function () { var b = $("#builder"); if (b) b.scrollIntoView({ block: "start" }); }, 300);
    refresh();
  }

  /* ================================================================ start */
  function start() {
    initCurrency();
    paint();
    initBanners();
    var b = $("[data-builder]"); if (b) initBuilder(b);
    $$("[data-captcha]").forEach(function (el) { Yenetch.captcha.mount(el); });
    initAuditPopup();
    initOfferPopups();
  }
  if (D.readyState === "loading") D.addEventListener("DOMContentLoaded", start); else start();
})();
