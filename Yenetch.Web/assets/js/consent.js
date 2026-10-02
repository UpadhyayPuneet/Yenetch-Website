/* Cookie consent and first-party analytics.
   - Shows the cookie banner until the visitor chooses. The choice is kept in the yn_consent cookie for 6 months.
   - Always counts pageviews anonymously (no cookies, no IP stored).
   - With analytics consent, sets yn_vid (visitor, 13 months) and yn_sid (session, 30 minutes idle) so visits join into a journey.
   Hits go to window.YENETCH_PULSE (/api/pulse). With no endpoint (static preview) nothing is sent.
   Other scripts can record events with Yenetch.track(name, label).
   Tags added in the admin (Google, Meta, LinkedIn, custom scripts) arrive as <template data-consent="analytics|marketing"> and
   start only after the visitor accepts that kind of cookie. */
(function () {
  "use strict";

  var C = "yn_consent", V = "yn_vid", S = "yn_sid";
  var endpoint = window.YENETCH_PULSE || null;
  var W = window.Yenetch = window.Yenetch || {};

  function cookie(name) {
    var m = document.cookie.match("(?:^|; )" + name + "=([^;]*)");
    return m ? decodeURIComponent(m[1]) : null;
  }
  function setCookie(name, value, seconds) {
    document.cookie = name + "=" + encodeURIComponent(value) + "; path=/; max-age=" + seconds + "; SameSite=Lax" + (location.protocol === "https:" ? "; Secure" : "");
  }
  function newId() {
    var b = new Uint8Array(16);
    (window.crypto || window.msCrypto).getRandomValues(b);
    return Array.prototype.map.call(b, function (x) { return ("0" + x.toString(16)).slice(-2); }).join("");
  }

  function consented() { return (cookie(C) || "").indexOf("a1") >= 0; }
  function marketingOk() { return (cookie(C) || "").indexOf("m1") >= 0; }
  function hasTags(kind) { return !!document.querySelector('template[data-consent="' + kind + '"]'); }

  // Runs the gated tags the visitor has agreed to. Scripts are re-created so the browser executes them.
  function activate() {
    Array.prototype.forEach.call(document.querySelectorAll("template[data-consent]"), function (t) {
      var kind = t.getAttribute("data-consent");
      if (t.hasAttribute("data-on") || !(kind === "analytics" ? consented() : kind === "marketing" ? marketingOk() : true)) return;
      t.setAttribute("data-on", "");
      var frag = t.content.cloneNode(true);
      Array.prototype.forEach.call(frag.querySelectorAll("script"), function (old) {
        var s = document.createElement("script");
        Array.prototype.forEach.call(old.attributes, function (a) { s.setAttribute(a.name, a.value); });
        s.text = old.text;
        old.parentNode.replaceChild(s, old);
      });
      t.parentNode.insertBefore(frag, t.nextSibling);
    });
  }

  // Visitor and session ids exist only with consent. The session cookie is refreshed on every hit.
  function ids() {
    if (!consented()) return false;
    if (!/^[a-f0-9]{32}$/.test(cookie(V) || "")) setCookie(V, newId(), 395 * 86400);
    else setCookie(V, cookie(V), 395 * 86400);
    setCookie(S, /^[a-f0-9]{32}$/.test(cookie(S) || "") ? cookie(S) : newId(), 1800);
    return true;
  }

  function send(hit) {
    if (!endpoint) return;
    hit.c = ids() ? 1 : 0;
    var body = JSON.stringify(hit);
    try {
      if (navigator.sendBeacon && navigator.sendBeacon(endpoint, new Blob([body], { type: "text/plain" }))) return;
    } catch (e) { }
    try { fetch(endpoint, { method: "POST", body: body, keepalive: true, headers: { "Content-Type": "text/plain" } }); } catch (e) { }
  }

  // ---- Pageview, time on page and scroll depth ----------------------------------------------------------

  var pageId = newId(), visibleMs = 0, visibleSince = document.visibilityState === "visible" ? Date.now() : 0, maxScroll = 0;

  function pageview() {
    var tz = "";
    try { tz = Intl.DateTimeFormat().resolvedOptions().timeZone || ""; } catch (e) { }
    send({ k: "pv", id: pageId, p: location.pathname, q: location.search, t: document.title, r: document.referrer,
           sw: screen.width, sh: screen.height, l: navigator.language || "", tz: tz });
  }

  function scrollDepth() {
    var h = document.documentElement.scrollHeight - innerHeight;
    var pct = h <= 0 ? 100 : Math.round(scrollY * 100 / h);
    if (pct > maxScroll) maxScroll = Math.min(100, pct);
  }

  function flush() {
    if (visibleSince) { visibleMs += Date.now() - visibleSince; visibleSince = 0; }
    send({ k: "end", id: pageId, d: Math.round(visibleMs / 1000), sc: maxScroll });
  }

  document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === "hidden") flush();
    else visibleSince = Date.now();
  });
  addEventListener("pagehide", flush);
  addEventListener("scroll", scrollDepth, { passive: true });

  // ---- Events -------------------------------------------------------------------------------------------

  W.track = function (name, label) { send({ k: "ev", n: String(name).slice(0, 60), lb: label ? String(label).slice(0, 200) : "", p: location.pathname }); };

  document.addEventListener("click", function (e) {
    var a = e.target.closest ? e.target.closest("a, button, [data-track]") : null;
    if (!a) return;
    var tagged = a.getAttribute("data-track");
    if (tagged) return W.track(tagged, a.getAttribute("data-track-label") || text(a));
    var href = a.getAttribute("href") || "";
    if (/^tel:/i.test(href)) return W.track("call", href.slice(4));
    if (/^mailto:/i.test(href)) return W.track("email", href.slice(7).split("?")[0]);
    if (/wa\.me|whatsapp\.com/i.test(href)) return W.track("whatsapp", location.pathname);
    if (/^https?:\/\//i.test(href) && a.host && a.host !== location.host) return W.track("outbound", a.host);
    if (/\/contact|contact\.html/.test(href) || /btn--blue/.test(a.className)) return W.track("cta_click", text(a));
  }, true);

  function text(el) { return (el.getAttribute("aria-label") || el.textContent || "").replace(/\s+/g, " ").trim().slice(0, 80); }

  // ---- Banner -------------------------------------------------------------------------------------------

  function choose(analytics, marketing) {
    var had = cookie(C);
    setCookie(C, (analytics ? "a1" : "a0") + (marketing ? "m1" : "m0"), 180 * 86400);
    if (!analytics) { setCookie(V, "", 0); setCookie(S, "", 0); }
    closeBanner();
    // Accepting on the first page upgrades that page's anonymous view to a tracked one.
    if (analytics && !had) pageview();
    activate();
  }

  var banner;
  function openBanner(showPrefs) {
    if (banner) { banner.remove(); banner = null; }
    banner = document.createElement("div");
    banner.className = "cookie";
    banner.setAttribute("role", "dialog");
    banner.setAttribute("aria-labelledby", "cookie-title");
    var privacy = (W.url ? W.url("/privacy") : "/privacy") + "#cookies";
    banner.innerHTML =
      '<div class="cookie__body"><h2 id="cookie-title">Your privacy</h2>' +
      '<p>We use essential cookies to run this site. With your permission, we also use analytics cookies to learn which pages help people, so we can improve them. We never sell your data. <a href="' + privacy + '">Cookie policy</a></p>' +
      '<div class="cookie__prefs"' + (showPrefs ? "" : " hidden") + '>' +
        '<label class="cookie__opt"><span><b>Essential</b><small>Security, your cookie choice and forms. Always on.</small></span><input type="checkbox" checked disabled></label>' +
        '<label class="cookie__opt"><span><b>Analytics</b><small>Pages visited, time on page, device and approximate location, so we can improve the site.</small></span><input type="checkbox" data-cookie-analytics' + (consented() ? " checked" : "") + '></label>' +
        (hasTags("marketing") ? '<label class="cookie__opt"><span><b>Marketing</b><small>Lets ad platforms such as Google, Meta and LinkedIn measure our ads and show you relevant ones.</small></span><input type="checkbox" data-cookie-marketing' + (marketingOk() ? " checked" : "") + '></label>' : "") +
      '</div></div>' +
      '<div class="cookie__actions">' +
        (showPrefs ? '<button type="button" class="btn btn--line" data-cookie="save">Save choices</button>'
                   : '<button type="button" class="btn btn--line" data-cookie="prefs">Settings</button>') +
        '<button type="button" class="btn btn--line" data-cookie="necessary">Necessary only</button>' +
        '<button type="button" class="btn btn--blue" data-cookie="all">Accept all</button>' +
      '</div>';
    document.body.appendChild(banner);
    requestAnimationFrame(function () { banner.classList.add("is-in"); });
    banner.addEventListener("click", function (e) {
      var b = e.target.closest("[data-cookie]");
      if (!b) return;
      var act = b.getAttribute("data-cookie");
      var m = banner.querySelector("[data-cookie-marketing]");
      if (act === "all") choose(true, true);
      else if (act === "necessary") choose(false, false);
      else if (act === "save") choose(banner.querySelector("[data-cookie-analytics]").checked, !!(m && m.checked));
      else if (act === "prefs") openBanner(true);
    });
  }
  function closeBanner() {
    if (!banner) return;
    var b = banner; banner = null;
    b.classList.remove("is-in");
    setTimeout(function () { b.remove(); }, 400);
  }
  W.cookieSettings = function () { openBanner(true); };

  function start() {
    pageview();
    activate();
    if (!cookie(C)) openBanner(false);
    document.addEventListener("click", function (e) {
      var l = e.target.closest && e.target.closest("[data-cookie-settings]");
      if (l) { e.preventDefault(); openBanner(true); }
    });
  }
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start); else start();
})();
