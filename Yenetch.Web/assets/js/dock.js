/* Yenetch mobile dock, contact sheet and back-to-top rocket.
   Markup comes from tools/sitegen/dock.py. The dock reuses the site's own hooks: the centre button drives the
   existing mobile menu (.nav__menu in site.js) and every [data-chat] element opens the assistant (site.js -> YenBot). */
(function () {
  "use strict";

  var reduce = window.matchMedia && matchMedia("(prefers-reduced-motion: reduce)").matches;
  var root = document.documentElement;

  function focusables(box) {
    return Array.prototype.filter.call(
      box.querySelectorAll('a[href], button:not([disabled]), input, select, textarea, [tabindex]:not([tabindex="-1"])'),
      function (el) { return el.offsetParent !== null || el === document.activeElement; });
  }

  document.addEventListener("DOMContentLoaded", function () {
    var dock = document.querySelector(".dock");
    var navBtn = document.querySelector(".nav__menu");
    var links = document.getElementById("nav-links");
    var sheet = document.getElementById("csheet");

    function menuOpen() { return !!(links && links.classList.contains("is-open")); }
    function closeMenu() { if (menuOpen() && navBtn) navBtn.click(); }

    /* ---------- contact sheet ---------- */
    var opener = null, closeTimer = null;
    function sheetOpen() { return !!(sheet && !sheet.hidden && sheet.classList.contains("is-open")); }

    function openSheet(from) {
      if (!sheet) return;
      closeMenu();
      clearTimeout(closeTimer);
      opener = from || null;
      sheet.hidden = false;
      void sheet.offsetWidth; // start the transition from the closed state
      sheet.classList.add("is-open");
      root.classList.add("dock-sheet-open");
      document.body.style.overflow = "hidden";
      if (opener) opener.setAttribute("aria-expanded", "true");
      var first = sheet.querySelector(".ctile2");
      if (first) first.focus({ preventScroll: true });
    }

    function closeSheet(restoreFocus) {
      if (!sheet || sheet.hidden) return;
      sheet.classList.remove("is-open");
      root.classList.remove("dock-sheet-open");
      if (!menuOpen()) document.body.style.overflow = "";
      if (opener) opener.setAttribute("aria-expanded", "false");
      closeTimer = setTimeout(function () { sheet.hidden = true; }, reduce ? 0 : 320);
      if (restoreFocus !== false && opener) opener.focus({ preventScroll: true });
    }

    if (sheet) {
      sheet.addEventListener("click", function (e) {
        if (e.target.closest("[data-csheet-close]")) { closeSheet(); return; }
        // Tiles: leaving the page or opening the assistant closes the sheet first.
        var tile = e.target.closest(".ctile2");
        if (tile) closeSheet(false);
      });
      sheet.addEventListener("keydown", function (e) {
        if (e.key === "Escape") { e.preventDefault(); closeSheet(); return; }
        if (e.key !== "Tab") return;
        var f = focusables(sheet.querySelector(".csheet__panel"));
        if (!f.length) return;
        var first = f[0], last = f[f.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
      });
    }

    /* ---------- dock ---------- */
    if (dock) {
      var menuBtn = dock.querySelector(".dock__menu");
      var contactBtn = dock.querySelector('[data-dock="contact"]');
      var askBtn = dock.querySelector('[data-dock="ask"]');

      // Active slot for the current section (works for site routes and the preview's flat files).
      var path = location.pathname.toLowerCase(), key = null;
      if (/(^|\/)contact(\.html|\.aspx)?\/?$/.test(path)) key = "contact";
      else if (/(^|\/)(services|digital-marketing|software-development|talent-resourcing)([\/.-]|$)/.test(path)) key = "services";
      else if (path === "/" || /\/(index\.html|default\.aspx)$/.test(path)) key = "home";
      var cur = key && dock.querySelector('[data-dock="' + key + '"]');
      if (cur) { cur.classList.add("is-active"); if (cur.tagName === "A") cur.setAttribute("aria-current", "page"); }

      // Centre button: reuse the header's mobile menu toggle and mirror its state.
      if (menuBtn && navBtn) {
        menuBtn.addEventListener("click", function () {
          closeSheet(false);
          navBtn.click();
        });
        var sync = function () {
          var open = navBtn.getAttribute("aria-expanded") === "true";
          menuBtn.setAttribute("aria-expanded", open ? "true" : "false");
          menuBtn.setAttribute("aria-label", open ? "Close menu" : "Open menu");
          root.classList.toggle("dock-menu-open", open);
        };
        new MutationObserver(sync).observe(navBtn, { attributes: true, attributeFilter: ["aria-expanded"] });
        sync();
      } else if (menuBtn) {
        menuBtn.hidden = true;
      }

      if (contactBtn) contactBtn.addEventListener("click", function () {
        if (sheetOpen()) closeSheet(); else openSheet(contactBtn);
      });

      // Ask: close the menu first; site.js's [data-chat] handler then opens the assistant.
      if (askBtn) askBtn.addEventListener("click", function () { closeMenu(); closeSheet(false); });

      // The floating chat launcher is hidden while the dock shows, so return focus to the dock when chat closes.
      var chat = document.querySelector(".yb"), launch = document.querySelector(".yb-launch");
      if (chat && askBtn) {
        new MutationObserver(function () {
          if (chat.hidden && launch && launch.offsetParent === null && getComputedStyle(dock).display !== "none") askBtn.focus({ preventScroll: true });
        }).observe(chat, { attributes: true, attributeFilter: ["hidden"] });
      }
    }

    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && dock && menuOpen() && !sheetOpen() && getComputedStyle(dock).display !== "none") {
        closeMenu();
        var mb = dock.querySelector(".dock__menu"); if (mb) mb.focus({ preventScroll: true });
      }
    });

    /* ---------- rocket: back to top ---------- */
    var rk = document.querySelector("[data-rocket]");
    if (!rk) return;
    var shown = false, busy = false, ticking = false;

    function update() {
      ticking = false;
      var y = window.scrollY || window.pageYOffset, max = document.documentElement.scrollHeight - innerHeight;
      rk.style.setProperty("--p", max > 0 ? Math.min(1, y / max).toFixed(3) : "0");
      if (busy) return;
      var show = y > innerHeight * 0.9;
      if (show !== shown) { shown = show; rk.classList.toggle("is-shown", show); }
    }
    addEventListener("scroll", function () { if (!ticking) { ticking = true; requestAnimationFrame(update); } }, { passive: true });
    addEventListener("resize", update, { passive: true });
    update();

    function particles(n, cls, spread, dyMin, dyMax, sMin, sMax) {
      for (var i = 0; i < n; i++) {
        var s = document.createElement("span");
        s.className = cls;
        s.style.setProperty("--x", ((Math.random() * 2 - 1) * spread).toFixed(1) + "px");
        s.style.setProperty("--y", (dyMin + Math.random() * (dyMax - dyMin)).toFixed(1) + "px");
        s.style.setProperty("--s", (sMin + Math.random() * (sMax - sMin)).toFixed(2));
        s.style.setProperty("--d", (Math.random() * 0.22).toFixed(2) + "s");
        s.style.setProperty("--t", (0.8 + Math.random() * 0.6).toFixed(2) + "s");
        rk.appendChild(s);
        (function (el) { setTimeout(function () { el.remove(); }, 2000); })(s);
      }
    }

    function landFocus() {
      var logo = document.querySelector(".nav .logo");
      if (logo && rk.contains(document.activeElement)) logo.focus({ preventScroll: true });
    }

    rk.addEventListener("click", function () {
      if (busy) return;
      if (reduce) { window.scrollTo(0, 0); landFocus(); return; }
      busy = true;
      rk.classList.add("is-ignite");
      particles(9, "rk__puff", 34, 4, 18, 1.2, 2.2);
      particles(7, "rk__spark", 22, 14, 38, 1, 1);
      setTimeout(function () {
        rk.classList.add("is-launch");
        particles(10, "rk__puff", 54, 6, 26, 1.8, 3.4);
        window.scrollTo({ top: 0, behavior: "smooth" });
      }, 520);
      setTimeout(function () { rk.classList.remove("is-shown"); shown = false; landFocus(); }, 1500);
      setTimeout(function () {
        rk.classList.add("is-reset");
        rk.classList.remove("is-ignite", "is-launch");
        void rk.offsetWidth;
        rk.classList.remove("is-reset");
        busy = false;
        update();
      }, 2100);
    });
  });
})();
