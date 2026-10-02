/* Service finder: four quick questions, then a recommended plan built from the rules in yenetch.json. */
(function () {
  "use strict";

  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  function Finder(root) {
    this.root = root;
    this.cfg = Yenetch.data().finder;
    this.answers = {};
    this.step = 0;
    this.render();
  }

  Finder.prototype.render = function () {
    var steps = this.cfg.steps, self = this;
    this.root.innerHTML = "";

    var bar = el("div", "finder__progress");
    for (var i = 0; i < steps.length; i++) {
      var seg = el("i"); if (i < this.step || this.step >= steps.length) seg.className = "is-done"; bar.appendChild(seg);
    }
    this.root.appendChild(bar);

    if (this.step >= steps.length) { this.renderResult(); return; }

    var s = steps[this.step];
    var meta = el("div", "finder__meta");
    meta.appendChild(el("span", null, "Question " + (this.step + 1) + " of " + steps.length));
    meta.appendChild(el("span", null, "About 30 seconds"));
    this.root.appendChild(meta);

    var q = el("h3", "finder__q", s.question); q.id = "finder-q"; q.tabIndex = -1;
    this.root.appendChild(q);

    var opts = el("div", "finder__options"); opts.setAttribute("role", "group"); opts.setAttribute("aria-labelledby", "finder-q");
    s.options.forEach(function (o) {
      var b = el("button", "opt", o.label); b.type = "button";
      b.setAttribute("aria-pressed", self.answers[s.id] === o.id ? "true" : "false");
      b.addEventListener("click", function () {
        self.answers[s.id] = o.id; self.step++; self.render();
        var h = self.root.querySelector("#finder-q, .result h3"); if (h) h.focus({ preventScroll: true });
      });
      opts.appendChild(b);
    });
    this.root.appendChild(opts);

    var nav = el("div", "finder__nav");
    var back = el("button", "finder__back", "← Back"); back.type = "button"; back.disabled = this.step === 0;
    back.addEventListener("click", function () { self.step--; self.render(); });
    nav.appendChild(back);
    this.root.appendChild(nav);
  };

  Finder.prototype.plan = function () {
    var r = this.cfg.rules, a = this.answers;
    var g = r.goal[a.goal], ind = r.industry[a.industry] || {}, st = r.stage[a.stage] || {};
    var product = g.product;
    if (!product && ind.product && (a.goal === "sell-online" || a.goal === "brand")) product = ind.product;
    if (a.goal === "sell-online" && a.industry === "retail") product = "ecomm";
    return {
      primary: Yenetch.bySlug("services", g.primary),
      support: g.support.map(function (s) { return Yenetch.bySlug("services", s); }),
      product: product ? Yenetch.bySlug("products", product) : null,
      extra: (a.industry === "retail" && product !== "studio-ai") ? Yenetch.bySlug("products", "studio-ai") : null,
      proof: Yenetch.bySlug("caseStudies", ind.case),
      note: st.note,
      urgent: a.timeline === "now"
    };
  };

  Finder.prototype.renderResult = function () {
    var p = this.plan(), self = this, pillars = {};
    if (Yenetch.track) Yenetch.track("finder_done", p.primary.name);
    (Yenetch.data().pillars || []).forEach(function (x) { pillars[x.id] = x.short; });

    var wrap = el("div", "result");

    var prim = el("div", "result__primary");
    prim.appendChild(el("small", null, "Your best starting point"));
    var h = el("h3", null, p.primary.name); h.tabIndex = -1; prim.appendChild(h);
    prim.appendChild(el("p", null, p.primary.summary));
    wrap.appendChild(prim);

    var list = el("ul", "result__list");
    p.support.forEach(function (s) {
      var li = el("li"); li.appendChild(el("b", null, s.name)); li.appendChild(el("span", null, pillars[s.pillar] || "")); list.appendChild(li);
    });
    [p.product, p.extra].forEach(function (pr) {
      if (!pr) return;
      var li = el("li"); li.appendChild(el("b", null, pr.name + " · " + pr.category)); li.appendChild(el("span", null, pr.status)); list.appendChild(li);
    });
    wrap.appendChild(list);

    if (p.proof) {
      var m = p.proof.metrics[0];
      var proof = el("p", "result__proof");
      proof.appendChild(document.createTextNode("Proof it works: we delivered "));
      proof.appendChild(el("b", null, m.value + " " + m.label));
      proof.appendChild(document.createTextNode(" for " + p.proof.client + ". " + (p.note || "")));
      wrap.appendChild(proof);
    }

    var act = el("div", "result__actions");
    var go = el("button", "btn btn--blue", p.urgent ? "Book a free call on this plan" : "Send me this plan"); go.type = "button";
    go.addEventListener("click", function () {
      if (window.YenBot) YenBot.startLead({ topic: p.primary.name, plan: [p.primary.slug].concat(p.support.map(function (s) { return s.slug; })), answers: self.answers });
    });
    var again = el("button", "btn btn--line", "Start over"); again.type = "button";
    again.addEventListener("click", function () { self.answers = {}; self.step = 0; self.render(); });
    act.appendChild(go); act.appendChild(again);
    wrap.appendChild(act);

    this.root.appendChild(wrap);
  };

  document.addEventListener("DOMContentLoaded", function () {
    var root = document.getElementById("finder-panel");
    if (root && Yenetch.data().finder) new Finder(root);
  });
})();
