/* Yenetch careers: job application popup. Any [data-apply] link opens it (the value pre-selects the role; empty =
   general application). Posts multipart to /api/apply. Without JavaScript the links still open email. */
(function () {
  "use strict";
  var ENDPOINT = "/api/apply";
  var dlg, form, jobSel;

  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }

  function roles() {
    var seen = {}, out = [];
    Array.prototype.forEach.call(document.querySelectorAll("[data-apply]"), function (a) {
      var t = a.getAttribute("data-apply");
      if (t && !seen[t]) { seen[t] = 1; out.push({ title: t, team: a.getAttribute("data-team") || "" }); }
    });
    return out;
  }

  function row(label, control, hint) {
    return '<div class="cform__row">' + label + control + (hint ? '<small class="cform__hint">' + hint + "</small>" : "") + "</div>";
  }

  function build() {
    var jobs = roles();
    dlg = document.createElement("dialog");
    dlg.className = "apl";
    dlg.setAttribute("aria-labelledby", "apl-title");
    dlg.innerHTML =
      '<div class="apl__panel"><div class="apl__head"><div><span class="kicker">Careers</span><h2 id="apl-title">Apply to Yenetch</h2></div>' +
      '<button class="apl__x" type="button" aria-label="Close" data-apl-close>&times;</button></div>' +
      '<form class="cform apl__form" novalidate>' +
      '<div class="apl__grid">' +
      row('<label for="apl-name">Full name *</label>', '<input class="field" id="apl-name" name="name" required maxlength="120" autocomplete="name">') +
      row('<label for="apl-email">Email *</label>', '<input class="field" id="apl-email" name="email" type="email" required maxlength="160" autocomplete="email">') +
      row('<label for="apl-phone">Phone *</label>', '<input class="field" id="apl-phone" name="phone" type="tel" required maxlength="40" autocomplete="tel">') +
      row('<label for="apl-job">Role *</label>', '<select class="field" id="apl-job" name="job" required>' +
        jobs.map(function (j) { return '<option value="' + esc(j.title) + '" data-team="' + esc(j.team) + '">' + esc(j.title) + "</option>"; }).join("") +
        '<option value="General application" data-team="General">General application</option></select>') +
      row('<label for="apl-exp">Total experience (years) *</label>', '<input class="field" id="apl-exp" name="experience" type="number" min="0" max="50" step="0.5" inputmode="decimal" required>', "Use 0 if you are a fresher") +
      row('<label for="apl-city">Current city</label>', '<input class="field" id="apl-city" name="city" maxlength="80" autocomplete="address-level2">') +
      row('<label for="apl-notice">Notice period</label>', '<select class="field" id="apl-notice" name="notice"><option value="">Choose…</option><option>Immediate</option><option>15 days</option><option>30 days</option><option>60 days</option><option>90 days</option></select>') +
      row('<label for="apl-skills">Key skills</label>', '<input class="field" id="apl-skills" name="skills" maxlength="400" placeholder="For example React, Node, SQL">') +
      "</div>" +
      row('<label for="apl-cv">CV (PDF or Word, up to 5 MB)</label>', '<input class="field apl__file" id="apl-cv" name="cv" type="file" accept=".pdf,.doc,.docx">') +
      row('<label for="apl-link">Or a link to your CV</label>', '<input class="field" id="apl-link" name="resumeUrl" type="url" maxlength="400" placeholder="Google Drive, Dropbox or website link">', "Attach a file or add a link, one is required") +
      row('<label for="apl-port">LinkedIn or portfolio</label>', '<input class="field" id="apl-port" name="portfolio" type="url" maxlength="400" placeholder="https://">') +
      row('<label for="apl-note">Anything else we should know?</label>', '<textarea class="field" id="apl-note" name="note" rows="3" maxlength="2000"></textarea>') +
      '<input type="text" name="website" tabindex="-1" autocomplete="off" aria-hidden="true" style="position:absolute;left:-9999px">' +
      '<input type="hidden" name="team" id="apl-team">' +
      '<p class="apl__error" role="alert" hidden></p>' +
      '<button class="btn btn--blue" type="submit">Send application</button>' +
      '<p class="cform__note">We use your details only to consider you for roles at Yenetch. See our <a href="/privacy">privacy policy</a>.</p>' +
      "</form>" +
      '<div class="cform__done apl__done" hidden><h3>Application sent.</h3><p>Thank you. We read every application and will contact you within a week if your profile is a fit. A confirmation is on its way to your inbox.</p><button class="btn btn--line" type="button" data-apl-close>Close</button></div>' +
      "</div>";
    document.body.appendChild(dlg);
    form = dlg.querySelector("form");
    jobSel = dlg.querySelector("#apl-job");

    dlg.addEventListener("click", function (e) {
      if (e.target === dlg || e.target.closest("[data-apl-close]")) close();
    });
    dlg.addEventListener("close", function () { document.documentElement.classList.remove("apl-open"); });
    form.addEventListener("submit", submit);
  }

  function open(role) {
    if (!dlg) build();
    form.hidden = false;
    dlg.querySelector(".apl__done").hidden = true;
    error("");
    jobSel.value = role || "General application";
    if (!jobSel.value) jobSel.value = "General application";
    document.documentElement.classList.add("apl-open");
    if (dlg.showModal) dlg.showModal(); else dlg.setAttribute("open", "");
    setTimeout(function () { var f = dlg.querySelector("#apl-name"); if (f) f.focus(); }, 30);
  }

  function close() { if (dlg.close) dlg.close(); else dlg.removeAttribute("open"); document.documentElement.classList.remove("apl-open"); }

  function error(msg) { var p = dlg.querySelector(".apl__error"); p.textContent = msg; p.hidden = !msg; }

  function submit(e) {
    e.preventDefault();
    var f = form.elements, file = f.cv.files[0];
    if (f.name.value.trim().length < 2) return fail(f.name, "Enter your full name.");
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.value.trim())) return fail(f.email, "Enter a valid email address.");
    if (f.phone.value.replace(/\D/g, "").length < 8) return fail(f.phone, "Enter a phone number we can call.");
    if (f.experience.value === "" || isNaN(parseFloat(f.experience.value))) return fail(f.experience, "Enter your total experience in years (0 for freshers).");
    if (!file && !f.resumeUrl.value.trim()) return fail(f.cv, "Attach your CV or add a link to it.");
    if (file && !/\.(pdf|docx?)$/i.test(file.name)) return fail(f.cv, "Attach the CV as a PDF or Word file.");
    if (file && file.size > 5 * 1024 * 1024) return fail(f.cv, "The CV is larger than 5 MB. Add a link instead.");
    var opt = jobSel.options[jobSel.selectedIndex];
    f.team.value = opt ? opt.getAttribute("data-team") || "" : "";
    error("");

    var btn = form.querySelector("[type=submit]");
    if (document.body.classList.contains("is-preview")) { done(); return; } // static preview has no server
    btn.disabled = true; btn.textContent = "Sending…";
    fetch(ENDPOINT, { method: "POST", body: new FormData(form), credentials: "same-origin" })
      .then(function (r) { return r.json().catch(function () { return { ok: false }; }); })
      .then(function (j) {
        if (j && j.ok) done();
        else error((j && j.error) || "Something went wrong. Please try again, or email your CV to us.");
      })
      .catch(function () { error("We could not send this. Check your connection and try again."); })
      .then(function () { btn.disabled = false; btn.textContent = "Send application"; });
  }

  function fail(el, msg) { error(msg); if (el && el.focus) el.focus(); }

  function done() {
    form.reset();
    form.hidden = true;
    dlg.querySelector(".apl__done").hidden = false;
    var b = dlg.querySelector(".apl__done .btn"); if (b) b.focus();
  }

  document.addEventListener("click", function (e) {
    var a = e.target.closest("[data-apply]");
    if (!a) return;
    e.preventDefault();
    open(a.getAttribute("data-apply"));
  });
})();
