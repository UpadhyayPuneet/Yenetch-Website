/* Proposal editor: line items with live totals, adding plans and extras from the catalogue, recipient fields with
   suggestions (team, client, people emailed before) and copying the client link. */
(function () {
  "use strict";
  var lines = document.querySelector("[data-lines]");
  if (!lines) return;
  var form = lines.closest("form");
  function $(s, r) { return (r || document).querySelector(s); }
  function $$(s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); }
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  var tbody = $("[data-lines]"), hidden = $("[data-items-json]"), curSel = $("[data-currency]");
  var rates = {};
  try { rates = JSON.parse($("[data-rates]").getAttribute("data-rates")); } catch (e) { }

  function num(v, d) { var n = parseFloat(String(v).replace(/,/g, "")); return isNaN(n) ? d : n; }
  function money(v) {
    var c = curSel.value;
    var s = c === "INR" ? "₹" + Math.round(v).toLocaleString("en-IN") : c + " " + v.toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    return s;
  }

  function row(it) {
    var tr = document.createElement("tr");
    tr.innerHTML =
      '<td><input class="field" data-f="name" maxlength="200" placeholder="What you will deliver" value="' + esc(it.name) + '">' +
      '<textarea class="field field--note" data-f="description" rows="1" maxlength="1000" placeholder="Details (optional)">' + esc(it.description || "") + "</textarea></td>" +
      '<td><input class="field" data-f="qty" inputmode="decimal" value="' + esc(it.qty == null ? 1 : it.qty) + '" aria-label="Quantity"></td>' +
      '<td><input class="field" data-f="unit" maxlength="30" placeholder="—" value="' + esc(it.unit || "") + '" aria-label="Unit"></td>' +
      '<td><input class="field" data-f="price" inputmode="decimal" value="' + esc(it.price == null ? "" : it.price) + '" aria-label="Price"></td>' +
      '<td><select class="field" data-f="billing" aria-label="Billed"><option value="one-time">One-time</option><option value="monthly">Monthly</option><option value="yearly">Yearly</option></select></td>' +
      '<td class="nowrap" data-amount></td><td><button type="button" class="btn btn--line btn--sm" data-del aria-label="Remove line">×</button></td>';
    $("[data-f=billing]", tr).value = it.billing || "one-time";
    tbody.appendChild(tr);
    return tr;
  }

  function read() {
    return $$("tr", tbody).map(function (tr) {
      var g = function (f) { return $("[data-f=" + f + "]", tr).value; };
      return { name: g("name").trim(), description: g("description").trim(), qty: num(g("qty"), 1), unit: g("unit").trim(), price: num(g("price"), 0), billing: g("billing") };
    });
  }

  function calc() {
    var items = read(), one = 0, mo = 0, yr = 0;
    $$("tr", tbody).forEach(function (tr, i) {
      var it = items[i], a = Math.round(it.qty * it.price * 100) / 100;
      $("[data-amount]", tr).textContent = it.name || it.price ? money(a) + (it.billing === "monthly" ? "/mo" : it.billing === "yearly" ? "/yr" : "") : "";
      if (!it.name) return;
      if (it.billing === "monthly") mo += a; else if (it.billing === "yearly") yr += a; else one += a;
    });
    var disc = num($("#disc").value, 0), tax = num($("#taxPct").value, 0), dep = num($("#dep").value, 0);
    var sub = one + mo + yr, d = sub * disc / 100, t = (sub - d) * tax / 100, total = sub - d + t;
    var due = dep > 0 && dep < 100 ? total * dep / 100 : total;
    var html = "<div><dt>One-time</dt><dd>" + money(one) + "</dd></div>";
    if (mo) html += "<div><dt>Monthly</dt><dd>" + money(mo) + "</dd></div>";
    if (yr) html += "<div><dt>Yearly</dt><dd>" + money(yr) + "</dd></div>";
    if (d) html += "<div><dt>Discount (" + disc + "%)</dt><dd>−" + money(d) + "</dd></div>";
    if (t) html += "<div><dt>" + esc($("#taxName").value || "Tax") + " (" + tax + "%)</dt><dd>" + money(t) + "</dd></div>";
    html += '<div class="pe-totals__big"><dt>First payment</dt><dd>' + money(total) + "</dd></div>";
    if (due !== total) html += "<div><dt>Due on acceptance (" + dep + "%)</dt><dd>" + money(due) + "</dd></div>";
    if (mo) html += "<div><dt>Then each month</dt><dd>" + money(mo * (1 - disc / 100) * (1 + tax / 100)) + "</dd></div>";
    $("[data-totals]").innerHTML = html;
    hidden.value = JSON.stringify(items.filter(function (i) { return i.name; }));
  }

  var start = [];
  try { start = JSON.parse(hidden.value || "[]"); } catch (e) { }
  if (!start.length) start = [{ name: "", qty: 1, price: "", billing: "one-time" }];
  start.forEach(row);

  form.addEventListener("input", function (e) { if (e.target.closest("[data-lines]") || e.target.hasAttribute("data-calc") || e.target.id === "taxName") calc(); });
  form.addEventListener("change", calc);
  tbody.addEventListener("click", function (e) { var b = e.target.closest("[data-del]"); if (b) { b.closest("tr").remove(); if (!$("tr", tbody)) row({ qty: 1, billing: "one-time" }); calc(); } });
  $("[data-add-line]").addEventListener("click", function () { var tr = row({ qty: 1, billing: "one-time" }); $("[data-f=name]", tr).focus(); calc(); });
  $("[data-catalog]").addEventListener("change", function () {
    var v = this.value; this.value = ""; if (!v) return;
    var it; try { it = JSON.parse(v); } catch (e) { return; }
    var c = curSel.value, rate = c === "INR" ? 1 : rates[c];
    var conv = function (p) { return rate ? Math.round(p * rate * 100) / 100 : p; };
    if (c !== "INR" && !rate) alert("No exchange rate for " + c + " yet. The rupee price was added: change it by hand.");
    // Replace an empty first line instead of adding below it.
    var first = $("tr", tbody);
    if (first && !$("[data-f=name]", first).value && $$("tr", tbody).length === 1) first.remove();
    row({ name: it.name, description: it.description, qty: it.qty, unit: it.unit, price: conv(it.price), billing: it.billing });
    if (it.setup) row({ name: it.name.replace(/ plan$/, "") + ": one-time set-up", qty: 1, price: conv(it.setup), billing: "one-time" });
    calc();
  });
  form.addEventListener("submit", calc);
  calc();

  /* ---- copy link ---- */
  $$("[data-copy]").forEach(function (b) {
    b.addEventListener("click", function () {
      var t = b.getAttribute("data-copy");
      (navigator.clipboard ? navigator.clipboard.writeText(t) : Promise.reject()).then(function () { b.textContent = "Link copied"; }, function () { prompt("Copy the client link:", t); });
    });
  });

  /* ---- recipients: chips with suggestions ---- */
  var box = $("[data-suggest]");
  if (!box) return;
  var suggest = [];
  try { suggest = JSON.parse(box.getAttribute("data-suggest")); } catch (e) { }
  var valid = function (s) { return /^[^\s@,;<>]+@[^\s@,;<>]+\.[^\s@,;<>]{2,}$/.test(s); };
  $$("[data-recipients]", box).forEach(function (input) {
    var wrap = document.createElement("div"); wrap.className = "rcpt";
    var list = document.createElement("div"); list.className = "rcpt__chips";
    var typer = document.createElement("input"); typer.className = "rcpt__input"; typer.type = "text"; typer.setAttribute("autocomplete", "off");
    typer.setAttribute("aria-label", (input.labels && input.labels[0] ? input.labels[0].textContent : "Recipients") + ": type an address");
    var menu = document.createElement("ul"); menu.className = "rcpt__menu"; menu.hidden = true; menu.setAttribute("role", "listbox");
    input.type = "hidden";
    input.parentNode.insertBefore(wrap, input);
    wrap.appendChild(list); list.appendChild(typer); wrap.appendChild(menu); wrap.appendChild(input);
    var values = input.value.split(/[,;\s]+/).filter(Boolean), active = -1;
    function sync() { input.value = values.join(", "); }
    function draw() {
      $$(".rcpt__chip", list).forEach(function (c) { c.remove(); });
      values.forEach(function (v, i) {
        var c = document.createElement("span"); c.className = "rcpt__chip" + (valid(v) ? "" : " is-bad");
        c.innerHTML = esc(v) + '<button type="button" aria-label="Remove ' + esc(v) + '">×</button>';
        c.querySelector("button").addEventListener("click", function () { values.splice(i, 1); draw(); sync(); typer.focus(); });
        list.insertBefore(c, typer);
      });
    }
    function add(v) { v = v.trim().replace(/[,;]$/, ""); if (v && values.indexOf(v) < 0) values.push(v); typer.value = ""; draw(); sync(); hide(); }
    function hide() { menu.hidden = true; active = -1; }
    function show() {
      var q = typer.value.trim().toLowerCase();
      var hits = suggest.filter(function (s) { return values.indexOf(s.email) < 0 && (!q || s.email.toLowerCase().indexOf(q) > -1 || s.label.toLowerCase().indexOf(q) > -1); }).slice(0, 8);
      if (!hits.length) { hide(); return; }
      menu.innerHTML = hits.map(function (s, i) { return '<li role="option" data-email="' + esc(s.email) + '"' + (i === active ? ' class="is-on" aria-selected="true"' : "") + "><b>" + esc(s.label) + "</b><span>" + esc(s.email) + "</span></li>"; }).join("");
      menu.hidden = false;
    }
    typer.addEventListener("input", function () { active = -1; if (/[,;]$/.test(typer.value)) add(typer.value); else show(); });
    typer.addEventListener("focus", show);
    typer.addEventListener("blur", function () { setTimeout(function () { if (typer.value.trim()) add(typer.value); hide(); }, 150); });
    typer.addEventListener("keydown", function (e) {
      var items = $$("li", menu);
      if (e.key === "ArrowDown" && items.length) { e.preventDefault(); active = Math.min(items.length - 1, active + 1); show(); }
      else if (e.key === "ArrowUp" && items.length) { e.preventDefault(); active = Math.max(0, active - 1); show(); }
      else if (e.key === "Enter" || e.key === "Tab" && typer.value.trim()) {
        if (e.key === "Enter") e.preventDefault();
        // Enter picks the highlighted suggestion, or the first one when what was typed is not a full address yet.
        var pick = active >= 0 ? items[active] : (!valid(typer.value.trim()) ? items[0] : null);
        if (pick) add(pick.getAttribute("data-email")); else if (typer.value.trim()) add(typer.value);
      } else if (e.key === "Backspace" && !typer.value && values.length) { values.pop(); draw(); sync(); }
      else if (e.key === "Escape") hide();
    });
    menu.addEventListener("mousedown", function (e) { var li = e.target.closest("li"); if (li) { e.preventDefault(); add(li.getAttribute("data-email")); typer.focus(); } });
    wrap.addEventListener("click", function (e) { if (e.target === wrap || e.target === list) typer.focus(); });
    draw();
  });
})();
