/* Proposal page (/proposal/{token}): accept with a typed name, or decline with an optional reason. */
(function () {
  "use strict";
  var box = document.querySelector("[data-proposal]");
  if (!box) return;
  var token = box.getAttribute("data-proposal");
  var accept = box.querySelector("[data-accept]"), decline = box.querySelector("[data-decline]"), done = box.querySelector("[data-done]");
  if (!accept) return;
  var err = accept.querySelector("[data-error]");
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  function post(body) {
    body.token = token;
    return fetch("/api/proposal", { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin", body: JSON.stringify(body) })
      .then(function (r) { return r.json().catch(function () { return { ok: false }; }); });
  }
  function finish(html) { accept.hidden = true; decline.hidden = true; done.innerHTML = html; done.hidden = false; done.scrollIntoView({ behavior: "smooth", block: "center" }); }

  accept.addEventListener("submit", function (e) {
    e.preventDefault();
    var name = accept.name.value.trim();
    err.hidden = true;
    if (name.length < 2) { err.textContent = "Type your full name to sign."; err.hidden = false; accept.name.focus(); return; }
    if (!accept.agree.checked) { err.textContent = "Tick the box to confirm you accept."; err.hidden = false; return; }
    var btn = accept.querySelector("button[type=submit]"); btn.disabled = true; btn.textContent = "Accepting…";
    post({ action: "accept", name: name, agree: "1" }).then(function (j) {
      if (!j.ok) { err.textContent = j.error || "Something went wrong. Please try again."; err.hidden = false; btn.disabled = false; btn.textContent = "Accept proposal"; return; }
      finish("<h2>Thank you, " + esc(name.split(" ")[0]) + "!</h2><p>The proposal is accepted and a confirmation is on its way to your email. Your project lead will be in touch within one working day.</p>" +
        (j.payUrl ? '<a class="btn btn--blue" href="' + esc(j.payUrl) + '" rel="noopener">Pay securely now</a> ' : "") +
        (j.portal ? '<a class="btn btn--line" href="' + esc(j.portal) + '" rel="noopener">Open client portal</a>' : ""));
    }).catch(function () { err.textContent = "We could not reach the server. Check your connection and try again."; err.hidden = false; btn.disabled = false; btn.textContent = "Accept proposal"; });
  });
  box.querySelector("[data-decline-open]").addEventListener("click", function () { accept.hidden = true; decline.hidden = false; decline.reason.focus(); });
  box.querySelector("[data-decline-cancel]").addEventListener("click", function () { decline.hidden = true; accept.hidden = false; });
  decline.addEventListener("submit", function (e) {
    e.preventDefault();
    post({ action: "decline", reason: decline.reason.value.trim() }).then(function (j) {
      if (j.ok) finish("<h2>Thanks for letting us know.</h2><p>We have let the team know. If anything changes, we would be glad to send an updated proposal.</p>");
    });
  });
})();
