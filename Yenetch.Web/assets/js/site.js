/* Yenetch site helpers: navigation, chat triggers, scroll effects, count-up, filters, share and newsletter. */
(function () {
  "use strict";

  window.Yenetch = window.Yenetch || {};

  // Content model: inlined by Site.Master (window.YENETCH_DATA) so there is no extra request.
  Yenetch.data = function () { return window.YENETCH_DATA || {}; };
  Yenetch.bySlug = function (list, slug) {
    return (Yenetch.data()[list] || []).filter(function (x) { return x.slug === slug; })[0] || null;
  };

  // Site routes. The static preview uses flat files (/services/seo -> services-seo.html).
  Yenetch.url = function (route) {
    if (!document.body.classList.contains("is-preview")) return route;
    var p = route.split("#"), path = p[0].replace(/^\/|\/$/g, "");
    return (path ? path.replace(/\//g, "-") : "index") + ".html" + (p[1] ? "#" + p[1] : "");
  };

  // Leads from forms outside the chatbot (preview contact form, newsletter).
  Yenetch.sendLead = function (lead) {
    lead.page = location.pathname;
    if (window.YENBOT_ENDPOINT) {
      return fetch(window.YENBOT_ENDPOINT, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(lead) })
        .then(function (r) { return r.ok; }).catch(function () { return false; });
    }
    try { var k = "yenetch.leads", all = JSON.parse(localStorage.getItem(k) || "[]"); all.push(lead); localStorage.setItem(k, JSON.stringify(all)); } catch (e) { }
    return Promise.resolve(true);
  };

  Yenetch.toast = function (text) {
    var t = document.createElement("div");
    t.className = "toast"; t.setAttribute("role", "status"); t.textContent = text;
    document.body.appendChild(t);
    setTimeout(function () { t.remove(); }, 3600);
  };

  var reduce = window.matchMedia && matchMedia("(prefers-reduced-motion: reduce)").matches;

  document.addEventListener("DOMContentLoaded", function () {
    // Mobile menu
    var btn = document.querySelector(".nav__menu"), links = document.getElementById("nav-links");
    if (btn && links) {
      btn.addEventListener("click", function () {
        var open = links.classList.toggle("is-open");
        btn.setAttribute("aria-expanded", open ? "true" : "false");
        document.body.style.overflow = open ? "hidden" : "";
      });
      links.addEventListener("click", function (e) {
        var sub = e.target.closest(".nav__sub");
        if (sub) {
          // Mobile: the arrow opens this section's pages and closes any other open section.
          var item = sub.parentNode, open = !item.classList.contains("is-sub");
          Array.prototype.forEach.call(links.querySelectorAll(".has-mega.is-sub"), function (el) { el.classList.remove("is-sub"); el.querySelector(".nav__sub").setAttribute("aria-expanded", "false"); });
          item.classList.toggle("is-sub", open); sub.setAttribute("aria-expanded", open ? "true" : "false");
          return;
        }
        if (e.target.closest("a")) { links.classList.remove("is-open"); btn.setAttribute("aria-expanded", "false"); document.body.style.overflow = ""; }
      });
    }

    // Any element with data-chat opens the assistant; the attribute value is sent as the first message.
    document.addEventListener("click", function (e) {
      var el = e.target.closest("[data-chat]");
      if (!el || !window.YenBot) return;
      e.preventDefault();
      YenBot.open(el.getAttribute("data-chat") || "");
    });

    // Hero mark follows the pointer a little.
    var cube = document.querySelector(".ybox3d");
    if (cube && !reduce && matchMedia("(pointer: fine)").matches) {
      window.addEventListener("pointermove", function (e) {
        var x = e.clientX / innerWidth - .5, y = e.clientY / innerHeight - .5;
        cube.style.setProperty("--ry", (x * 22).toFixed(2) + "deg");
        cube.style.setProperty("--rx", (-y * 14).toFixed(2) + "deg");
      }, { passive: true });
    }

    // Count-up: numbers render final values; they only animate once they scroll into view.
    if (!reduce && "IntersectionObserver" in window) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (en) {
          if (!en.isIntersecting) return;
          io.unobserve(en.target);
          var el = en.target, m = el.textContent.match(/^([^\d]*)([\d.,]+)(.*)$/);
          if (!m) return;
          var end = parseFloat(m[2].replace(/,/g, "")), dec = (m[2].split(".")[1] || "").length, t0 = null;
          function step(ts) {
            if (!t0) t0 = ts;
            var p = Math.min(1, (ts - t0) / 1400), v = end * (1 - Math.pow(1 - p, 3));
            el.textContent = m[1] + (dec ? v.toFixed(dec) : Math.round(v).toLocaleString("en-IN")) + m[3];
            if (p < 1) requestAnimationFrame(step);
          }
          requestAnimationFrame(step);
        });
      }, { threshold: .6 });
      document.querySelectorAll("[data-count]").forEach(function (n) {
        var r = n.getBoundingClientRect();
        if (r.top > innerHeight) io.observe(n); // leave numbers already on screen untouched
      });
    }

    // Statement: words light up in reading order as the paragraph scrolls through the viewport.
    document.querySelectorAll("[data-words]").forEach(function (el) {
      var words = el.textContent.trim().split(/\s+/);
      el.innerHTML = words.map(function (w) { return '<span class="w">' + w.replace(/</g, "&lt;") + "</span>"; }).join(" ");
      if (reduce) return;
      var spans = el.querySelectorAll(".w");
      function paint() {
        var r = el.getBoundingClientRect(), vh = innerHeight;
        var p = Math.min(1, Math.max(0, (vh * .85 - r.top) / (r.height + vh * .35)));
        var lit = Math.round(p * spans.length);
        for (var i = 0; i < spans.length; i++) spans[i].classList.toggle("is-dim", i >= lit);
      }
      paint();
      addEventListener("scroll", paint, { passive: true });
    });

    // Sub-navigation and table of contents: highlight the section in view.
    [".subnav__links", ".toc"].forEach(function (sel) {
      var box = document.querySelector(sel);
      if (!box || !("IntersectionObserver" in window)) return;
      var links = {}, current = null;
      box.querySelectorAll('a[href^="#"]').forEach(function (a) { links[a.getAttribute("href").slice(1)] = a; });
      var spy = new IntersectionObserver(function (entries) {
        entries.forEach(function (en) {
          if (!en.isIntersecting) return;
          if (current) current.classList.remove("is-active");
          current = links[en.target.id]; if (current) current.classList.add("is-active");
        });
      }, { rootMargin: "-30% 0px -60% 0px" });
      Object.keys(links).forEach(function (id) { var t = document.getElementById(id); if (t) spy.observe(t); });
    });

    // Case study filter chips.
    var grid = document.querySelector("[data-filter-grid]");
    document.querySelectorAll("[data-filter]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        var f = chip.getAttribute("data-filter");
        document.querySelectorAll("[data-filter]").forEach(function (c) { c.classList.toggle("is-on", c === chip); });
        if (grid) grid.querySelectorAll("[data-industry]").forEach(function (t) {
          t.classList.toggle("is-hidden", f !== "*" && t.getAttribute("data-industry") !== f);
        });
      });
    });

    // Copy link buttons on articles.
    document.querySelectorAll("[data-copy-link]").forEach(function (b) {
      b.addEventListener("click", function () {
        var done = function () { Yenetch.toast("Link copied"); };
        if (navigator.clipboard) navigator.clipboard.writeText(location.href).then(done, done); else done();
      });
    });

    // Newsletter and the preview's contact form send leads through the same endpoint as the chatbot.
    document.querySelectorAll("[data-newsletter]").forEach(function (f) {
      f.addEventListener("submit", function (e) {
        e.preventDefault();
        var email = f.querySelector("input").value.trim();
        Yenetch.sendLead({ name: "Newsletter subscriber", contact: email, need: "Newsletter", topic: "Newsletter", source: "newsletter" })
          .then(function () { f.reset(); Yenetch.toast("Subscribed. The next note lands in your inbox."); });
      });
    });
    document.querySelectorAll("[data-lead-form]").forEach(function (f) {
      f.addEventListener("submit", function (e) {
        e.preventDefault();
        var v = function (n) { return (f.elements[n] && f.elements[n].value || "").trim(); };
        Yenetch.sendLead({ name: v("name"), contact: v("contact"), topic: v("topic"), need: v("need"), source: "contact-form" }).then(function () {
          f.innerHTML = '<div class="cform__done"><h2>Thank you.</h2><p>A specialist will reach you within one working day.</p></div>';
        });
      });
    });
  });
})();
