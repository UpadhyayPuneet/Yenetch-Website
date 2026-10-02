/* Yenetch growth tools: call booking (/book) and the free website audit (/website-audit).
   Each part runs only on its own page. Endpoints: /api/slots, /api/book, /api/audit.
   In the static preview (body.is-preview) there is no server, so both show sample data. */
(function () {
  "use strict";
  var preview = function () { return document.body.classList.contains("is-preview"); };
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  function track(n, l) { try { window.Yenetch && Yenetch.track && Yenetch.track(n, l); } catch (e) { } }
  function vid() { var m = document.cookie.match(/(?:^|; )yn_vid=([a-f0-9]{32})/); return m ? m[1] : ""; }

  /* ================================================================ booking */
  function initBooking(root) {
    var daysEl = root.querySelector("[data-bk-days]"), timesEl = root.querySelector("[data-bk-times]"), form = root.querySelector("[data-bk-form]");
    var stepTime = root.querySelector('[data-bk-step="time"]'), done = root.querySelector("[data-bk-done]"), closed = root.querySelector("[data-bk-closed]");
    var err = root.querySelector("[data-bk-error]"), data = null, day = null, time = null;
    var localTz = ""; try { localTz = Intl.DateTimeFormat().resolvedOptions().timeZone || ""; } catch (e) { }
    var differentTz = localTz && localTz !== "Asia/Kolkata" && localTz !== "Asia/Calcutta";
    var tzNote = root.querySelector("[data-bk-tz]");
    if (tzNote) tzNote.textContent = "Times are India time (IST)" + (differentTz ? ", with your local time below" : "");

    function istDate(d, t) { return new Date(d + "T" + t + ":00+05:30"); }
    function dayLabel(d) { var x = istDate(d, "12:00"); return { wd: x.toLocaleDateString("en-IN", { weekday: "short", timeZone: "Asia/Kolkata" }), dm: x.toLocaleDateString("en-IN", { day: "numeric", month: "short", timeZone: "Asia/Kolkata" }), long: x.toLocaleDateString("en-IN", { weekday: "long", day: "numeric", month: "long", timeZone: "Asia/Kolkata" }) }; }
    function timeLabel(d, t) { return istDate(d, t).toLocaleTimeString("en-IN", { hour: "numeric", minute: "2-digit", timeZone: "Asia/Kolkata" }); }
    function localLabel(d, t) { return istDate(d, t).toLocaleTimeString([], { hour: "numeric", minute: "2-digit", timeZoneName: "short" }); }

    function sample() {
      var days = {}, now = new Date();
      for (var i = 1, n = 0; n < 12 && i < 30; i++) {
        var x = new Date(now.getTime() + i * 864e5), wd = x.getDay();
        if (wd === 0) continue;
        var ds = x.toISOString().slice(0, 10), slots = [];
        ["10:00", "10:45", "11:30", "12:15", "14:00", "14:45", "15:30", "16:15", "17:00"].forEach(function (t, k) { if ((k + i) % 3) slots.push(t); });
        days[ds] = wd === 6 ? slots.slice(0, 4) : slots; n++;
      }
      return { enabled: true, minutes: 30, days: days, modes: ["Google Meet video call", "Phone call", "Visit our office"], topics: ["Performance Marketing", "SEO", "Website & Web Application Development", "Something else"] };
    }

    function load() {
      if (preview()) return render(sample());
      fetch("/api/slots", { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(render)
        .catch(function () { daysEl.innerHTML = '<p class="bk__loading">We could not load the open times. Please call or WhatsApp us.</p>'; });
    }

    function render(d) {
      data = d;
      var keys = Object.keys(d.days || {});
      if (!d.enabled || !keys.length) { root.querySelector('[data-bk-step="day"]').hidden = true; closed.hidden = false; if (d.enabled) closed.querySelector("h2").textContent = "No open times in the next few weeks."; return; }
      daysEl.innerHTML = keys.map(function (k) {
        var l = dayLabel(k);
        return '<button type="button" class="bk__day" role="option" aria-selected="false" data-day="' + k + '"><small>' + esc(l.wd) + '</small><b>' + esc(l.dm) + '</b><span>' + d.days[k].length + ' open</span></button>';
      }).join("");
      form.querySelector("[data-bk-topics]").innerHTML = '<option value="">Choose a topic</option>' + (d.topics || []).map(function (t) { return "<option>" + esc(t) + "</option>"; }).join("");
      form.querySelector("[data-bk-modes]").innerHTML = (d.modes || []).map(function (m, i) {
        return '<label class="bk__mode"><input type="radio" name="mode" value="' + esc(m) + '"' + (i === 0 ? " checked" : "") + '><span>' + esc(m) + "</span></label>";
      }).join("");
      var pre = new URLSearchParams(location.search).get("topic");
      if (pre) { var sel = form.querySelector("[data-bk-topics]"); var hit = Array.prototype.filter.call(sel.options, function (o) { return o.value && o.value.toLowerCase().indexOf(pre.toLowerCase()) >= 0; })[0]; if (hit) sel.value = hit.value; }
    }

    daysEl.addEventListener("click", function (e) {
      var b = e.target.closest("[data-day]"); if (!b) return;
      day = b.getAttribute("data-day"); time = null;
      Array.prototype.forEach.call(daysEl.querySelectorAll("[data-day]"), function (x) { x.setAttribute("aria-selected", x === b ? "true" : "false"); });
      root.querySelector("[data-bk-dayname]").textContent = dayLabel(day).long;
      timesEl.innerHTML = data.days[day].map(function (t) {
        return '<button type="button" class="bk__time" role="option" aria-selected="false" data-time="' + t + '"><b>' + esc(timeLabel(day, t)) + "</b>" + (differentTz ? "<small>" + esc(localLabel(day, t)) + "</small>" : "") + "</button>";
      }).join("");
      stepTime.hidden = false; form.hidden = true;
      stepTime.scrollIntoView({ behavior: "smooth", block: "nearest" });
    });

    timesEl.addEventListener("click", function (e) {
      var b = e.target.closest("[data-time]"); if (!b) return;
      time = b.getAttribute("data-time");
      Array.prototype.forEach.call(timesEl.querySelectorAll("[data-time]"), function (x) { x.setAttribute("aria-selected", x === b ? "true" : "false"); });
      root.querySelector("[data-bk-chosen]").textContent = dayLabel(day).long + ", " + timeLabel(day, time) + " IST (" + data.minutes + " min)";
      form.hidden = false;
      form.scrollIntoView({ behavior: "smooth", block: "start" });
      setTimeout(function () { var f = form.querySelector("#bk-name"); if (f && !f.value) f.focus({ preventScroll: true }); }, 350);
    });

    form.querySelector("[data-bk-back]").addEventListener("click", function () { form.hidden = true; stepTime.scrollIntoView({ behavior: "smooth", block: "start" }); });

    function fail(msg, el) { err.textContent = msg; err.hidden = !msg; if (el) el.focus(); }

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      var f = form.elements;
      if (f.name.value.trim().length < 2) return fail("Enter your name.", f.name);
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.value.trim())) return fail("Enter a valid email address.", f.email);
      if (f.phone.value.replace(/\D/g, "").length < 8) return fail("Enter a phone number we can reach you on.", f.phone);
      fail("");
      var btn = form.querySelector("[type=submit]");
      var finish = function (when) {
        form.hidden = true; stepTime.hidden = true; root.querySelector('[data-bk-step="day"]').hidden = true; done.hidden = false;
        root.querySelector("[data-bk-done-text]").textContent = "See you on " + when + ". We've emailed the details and a calendar invite to " + f.email.value.trim() + ".";
        done.scrollIntoView({ behavior: "smooth", block: "center" }); track("booking", day + " " + time);
      };
      if (preview()) return finish(dayLabel(day).long + ", " + timeLabel(day, time) + " IST");
      var fd = new FormData(form); fd.append("date", day); fd.append("time", time);
      btn.disabled = true; btn.textContent = "Booking…";
      fetch("/api/book", { method: "POST", body: fd, credentials: "same-origin" })
        .then(function (r) { return r.json().catch(function () { return { ok: false }; }); })
        .then(function (j) {
          if (j && j.ok) return finish(j.when);
          fail((j && j.error) || "Something went wrong. Please try again, or call us.");
          if (j && /taken/.test(j.error || "")) load();
        })
        .catch(function () { fail("We could not book this. Check your connection and try again."); })
        .then(function () { btn.disabled = false; btn.textContent = "Confirm booking"; });
    });

    load();
  }

  /* ================================================================ website audit */
  var CAT_NAMES = { seo: "SEO", speed: "Speed", mobile: "Mobile", security: "Security", social: "Social sharing" };

  function initAudit(root) {
    var form = root.querySelector("[data-au-form]"), busy = root.querySelector("[data-au-busy]"), out = root.querySelector("[data-au-result]"), err = root.querySelector("[data-au-error]");
    function fail(msg, el) { err.textContent = msg; err.hidden = !msg; if (el) el.focus(); }

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      var f = form.elements, url = f.url.value.trim();
      if (!/^(https?:\/\/)?[a-z0-9-]+(\.[a-z0-9-]+)+([\/?#].*)?$/i.test(url)) return fail("Enter your website address, for example yourbusiness.com.", f.url);
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.value.trim())) return fail("Enter your email so we can send the report.", f.email);
      fail("");
      var btn = root.querySelector("[data-au-btn]");
      btn.disabled = true; busy.hidden = false; out.hidden = true;
      var show = function (j) { busy.hidden = true; btn.disabled = false; render(j, f.email.value.trim()); };
      if (preview()) { setTimeout(function () { show(sampleAudit(url)); }, 900); return; }
      var fd = new FormData(form); fd.append("vid", vid());
      fetch("/api/audit", { method: "POST", body: fd, credentials: "same-origin" })
        .then(function (r) { return r.json().catch(function () { return { ok: false }; }); })
        .then(function (j) {
          if (j && j.ok) return show(j);
          busy.hidden = true; btn.disabled = false; fail((j && j.error) || "We could not check that website. Please try again.");
        })
        .catch(function () { busy.hidden = true; btn.disabled = false; fail("We could not reach the audit service. Check your connection and try again."); });
    });

    function grade(s) { return s >= 85 ? "good" : s >= 60 ? "ok" : "poor"; }
    function gaugeSvg(score) {
      var c = 2 * Math.PI * 52, off = c * (1 - score / 100);
      return '<svg viewBox="0 0 120 120" class="au__gauge au__gauge--' + grade(score) + '" role="img" aria-label="Score ' + score + ' out of 100"><circle cx="60" cy="60" r="52" class="au__track"/><circle cx="60" cy="60" r="52" class="au__arc" stroke-dasharray="' + c.toFixed(1) + '" stroke-dashoffset="' + off.toFixed(1) + '"/><text x="60" y="58" text-anchor="middle" class="au__num">' + score + '</text><text x="60" y="78" text-anchor="middle" class="au__of">out of 100</text></svg>';
    }

    function render(j, email) {
      var fails = j.checks.filter(function (c) { return c.status === "fail"; }), warns = j.checks.filter(function (c) { return c.status === "warn"; }), pass = j.checks.filter(function (c) { return c.status === "pass"; });
      var item = function (c) { return '<li class="au__check au__check--' + c.status + '"><span class="au__dot" aria-hidden="true"></span><div><b>' + esc(c.title) + '</b><small class="au__cat">' + esc(CAT_NAMES[c.cat] || c.cat) + "</small>" + (c.detail ? "<p>" + esc(c.detail) + "</p>" : "") + (c.fix && c.status !== "pass" ? '<p class="au__fix"><b>Fix:</b> ' + esc(c.fix) + "</p>" : "") + "</div></li>"; };
      var cats = j.categories.map(function (c) { return '<div class="au__bar au__bar--' + grade(c.score) + '"><span>' + esc(c.name) + '</span><i style="--w:' + c.score + '%"></i><b>' + c.score + "</b></div>"; }).join("");
      var stats = j.stats ? '<dl class="au__stats"><div><dt>Response time</dt><dd>' + esc(j.stats.time) + '</dd></div><div><dt>Page size</dt><dd>' + esc(j.stats.size) + '</dd></div><div><dt>Scripts</dt><dd>' + esc(j.stats.scripts) + '</dd></div><div><dt>Images</dt><dd>' + esc(j.stats.images) + "</dd></div></dl>" : "";
      out.innerHTML =
        '<div class="au__summary"><div class="au__score">' + gaugeSvg(j.score) + '</div><div><span class="kicker">Audit for ' + esc(j.host) + '</span><h2>' + esc(j.headline) + '</h2><p>' + fails.length + " issue" + (fails.length === 1 ? "" : "s") + " to fix, " + warns.length + " improvement" + (warns.length === 1 ? "" : "s") + " and " + pass.length + ' checks passed.' + (email ? " A copy is on its way to " + esc(email) + "." : "") + '</p>' + stats + '</div><div class="au__cats">' + cats + "</div></div>" +
        (fails.length ? '<h3 class="au__h">Fix these first</h3><ul class="au__list">' + fails.map(item).join("") + "</ul>" : "") +
        (warns.length ? '<h3 class="au__h">Worth improving</h3><ul class="au__list">' + warns.map(item).join("") + "</ul>" : "") +
        (pass.length ? '<details class="au__passed"><summary>' + pass.length + ' checks passed</summary><ul class="au__list">' + pass.map(item).join("") + "</ul></details>" : "") +
        '<div class="au__cta"><div><h3>Want these fixed?</h3><p>Book a free 30-minute call. A specialist will go through your report and tell you what will move your rankings and leads most.</p></div><a class="btn btn--blue" href="' + (preview() ? "book.html" : "/book?topic=SEO") + '">Book a free call</a></div>' +
        (preview() ? '<p class="cform__note">Preview: this is a sample result. The live site checks the real website.</p>' : "");
      out.hidden = false;
      out.scrollIntoView({ behavior: "smooth", block: "start" });
      track("website_audit", j.host + " " + j.score);
    }

    function sampleAudit(url) {
      var host = url.replace(/^https?:\/\//, "").split("/")[0];
      return { ok: true, host: host, score: 72, headline: "Good start, with a few things holding you back.", stats: { time: "0.9 s", size: "1.8 MB", scripts: "14", images: "22" },
        categories: [{ name: "SEO", score: 70 }, { name: "Speed", score: 58 }, { name: "Mobile", score: 100 }, { name: "Security", score: 75 }, { name: "Social sharing", score: 50 }],
        checks: [
          { cat: "seo", status: "fail", title: "No meta description", detail: "Google shows a random snippet of your page instead of a summary you chose.", fix: "Write a 140 to 160 character description that says what you offer and where." },
          { cat: "speed", status: "warn", title: "The page is heavy (1.8 MB)", detail: "Big pages load slowly on mobile data.", fix: "Compress images and serve them as WebP." },
          { cat: "social", status: "warn", title: "No social sharing image", detail: "Links shared on WhatsApp and LinkedIn show without a picture.", fix: "Add an og:image tag with a 1200 x 630 image." },
          { cat: "mobile", status: "pass", title: "Set up for mobile screens" }, { cat: "security", status: "pass", title: "Served over HTTPS" }, { cat: "seo", status: "pass", title: "Has a page title" }] };
    }
  }

  function start() {
    var b = document.querySelector("[data-book]"); if (b) initBooking(b);
    var a = document.querySelector("[data-audit]"); if (a) initAudit(a);
  }
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start); else start();
})();
