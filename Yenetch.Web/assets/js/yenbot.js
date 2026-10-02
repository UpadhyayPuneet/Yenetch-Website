/* Yenetch assistant: answers from the site knowledge base (yenetch.json) and captures leads conversationally.
   Leads POST as JSON to window.YENBOT_ENDPOINT (/api/lead). With no endpoint (static preview) they stay in the browser. */
(function () {
  "use strict";

  var CUBE = '<svg class="mark" viewBox="0 0 100 100" aria-hidden="true"><path class="m-o" d="M5.49,32.32C5.55,31.58 5.48,30.70 5.62,30.00C6.54,25.28 9.61,21.65 13.78,19.42C14.31,19.13 14.88,18.83 15.40,18.53L23.87,13.63L37.33,5.86C41.90,3.23 45.59,0.34 51.23,0.94C55.62,1.40 57.99,3.23 61.70,5.37L71.80,11.21L82.80,17.55C84.31,18.43 86.49,19.58 87.88,20.51C89.02,21.27 90.05,22.19 90.94,23.23C92.63,25.25 93.78,27.67 94.28,30.26C94.68,32.39 94.54,35.73 94.54,38.02L94.54,49.54L94.54,61.92C94.54,63.89 94.62,66.55 94.49,68.43C94.24,72.16 92.13,76.26 89.21,78.61C87.62,79.90 85.47,81.03 83.65,82.07L76.78,86.00L74.03,87.61C73.42,87.97 72.84,88.46 72.17,88.64C71.77,88.75 71.56,89.01 71.20,89.23L68.67,90.73L63.44,93.79C63.15,93.96 62.79,94.03 62.44,94.24C58.94,96.29 54.84,99.48 50.61,99.03C49.82,99.03 49.08,99.03 48.30,99.09C45.52,99.02 42.98,97.63 40.69,96.21C39.97,95.77 39.07,95.42 38.42,94.87C37.98,94.79 32.68,91.60 32.01,91.21C28.89,89.38 25.75,87.58 22.59,85.80C20.22,84.49 17.90,83.02 15.53,81.70C13.19,80.40 10.77,79.12 9.03,77.04C8.65,76.60 8.08,75.91 7.82,75.41C7.54,74.87 7.16,74.35 6.88,73.82C6.29,72.68 5.78,71.21 5.59,69.94C5.40,68.71 5.49,67.17 5.49,65.90L5.49,58.96L5.49,32.32zM54.81,10.36C54.03,9.97 53.03,9.31 52.25,9.05C49.90,8.28 47.22,8.76 45.22,10.20C44.84,10.43 44.47,10.71 44.09,10.92C43.90,11.02 43.34,11.03 43.23,11.16C42.35,12.13 41.26,12.25 40.18,12.89C39.81,13.11 39.80,13.36 39.48,13.61C38.13,14.05 37.03,15.08 35.76,15.72C35.35,15.93 34.85,15.90 34.47,16.20C32.38,17.59 30.34,19.06 28.00,20.05C27.39,20.30 26.84,20.63 26.26,20.94C25.58,21.25 25.25,21.78 24.65,22.14C23.73,22.68 22.79,23.19 21.83,23.67C21.33,23.92 20.78,24.16 20.31,24.45C19.97,24.67 19.71,25.00 19.37,25.23C19.04,25.45 18.61,25.59 18.25,25.77C17.76,26.01 16.19,26.89 15.84,27.21C14.29,28.65 13.69,29.76 13.18,31.81C12.87,33.07 13.04,37.23 13.04,38.77L13.04,53.90L13.04,63.40C13.04,64.76 13.02,66.39 13.05,67.73C13.08,68.84 14.12,71.00 14.69,71.93C14.87,72.23 16.06,72.98 16.45,73.21C18.86,74.65 21.31,76.07 23.75,77.46L32.30,82.40L41.37,87.63C43.13,88.64 44.89,89.66 46.65,90.68C47.19,90.99 48.24,91.27 48.85,91.40C51.69,91.99 54.33,90.03 56.67,88.68L64.57,84.11L75.85,77.60L80.66,74.82C82.28,73.89 83.83,73.18 85.10,71.76C86.75,69.23 86.69,68.72 86.70,65.77L86.69,61.80L86.69,48.38L86.70,36.88C86.70,34.05 87.15,31.04 85.34,28.68C84.20,27.19 82.55,26.41 80.97,25.50C79.74,24.78 78.51,24.08 77.28,23.38L61.04,13.99L56.91,11.61C56.27,11.24 55.37,10.80 54.81,10.36z"/><path class="m-t" d="M49.07,11.99C51.71,11.71 52.76,12.76 54.91,14.01L61.12,17.62L74.46,25.34C76.49,26.52 78.85,27.80 80.82,29.02L80.80,29.12C80.32,29.64 74.21,33.09 73.09,33.74L50.44,46.93L49.86,47.23L26.28,33.63L20.79,30.46C20.46,30.27 19.07,29.51 18.88,29.33L18.92,29.22C19.46,28.65 22.26,27.13 23.12,26.63L30.42,22.42L42.73,15.31C44.45,14.32 47.24,12.42 49.07,11.99z"/><path class="m-l" d="M16.33,33.28L38.10,45.86L44.19,49.39C44.92,49.80 46.89,50.88 47.50,51.35L47.48,75.47L47.49,82.77C47.49,84.11 47.56,85.91 47.43,87.19C45.65,86.28 43.90,85.22 42.16,84.26C41.42,83.86 40.65,83.38 39.90,83.02C39.80,82.29 39.83,81.10 39.84,80.33L39.84,76.09L39.84,62.65C39.84,60.55 39.94,57.59 39.81,55.61C39.20,55.36 37.77,54.51 37.13,54.15L32.21,51.35C30.60,50.43 16.40,42.46 16.15,42.13C16.21,40.16 16.11,38.15 16.15,36.17C16.16,35.64 16.06,33.58 16.33,33.28z"/><path class="m-r" d="M83.01,33.44C83.19,33.42 83.29,33.39 83.43,33.49C83.53,34.35 83.49,36.04 83.49,36.96C83.48,38.66 83.52,40.48 83.46,42.17C83.03,42.51 82.07,43.02 81.55,43.32L78.19,45.21L65.87,52.22L62.09,54.36C61.40,54.75 60.43,55.26 59.80,55.72C59.67,57.67 59.77,60.53 59.77,62.56L59.77,75.79C59.77,78.06 59.84,80.84 59.74,83.07C59.06,83.30 57.57,84.19 56.87,84.59L52.62,86.98C52.45,87.01 52.54,87.01 52.36,86.98C52.19,86.63 52.28,81.57 52.28,80.72L52.28,67.21L52.27,57.15C52.27,55.29 52.23,53.17 52.30,51.32C52.86,50.89 54.31,50.10 55.00,49.71L60.25,46.69L76.96,37.00L80.90,34.74C81.37,34.47 82.67,33.77 83.01,33.44z"/></svg>';
  var TIME = /\b(how long|how soon|how quick|how quickly|how fast|timeline|timeframe|time frame|time does|time will|take time|duration|how many (days|weeks|months)|when can|when will|turnaround|go live|delivery time)\b/;
  var COST = /\b(cost|costs|price|prices|pricing|how much|charges?|rates?|budget|fees?|package|packages)\b/;
  var BUY = /\b(price|pricing|cost|quote|quotation|estimate|proposal|budget|demo|hire|call me|contact|start|get started|consult|meeting|interested|need|want|looking for|require)\b/;
  var HELLO = /^(hi|hello|hey|hii|namaste|good (morning|afternoon|evening))\b/;
  var THANKS = /\b(thanks|thank you|thx|great|awesome|cool|ok|okay)\b/;
  var EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
  var PHONE = /^\+?[\d\s\-()]{8,16}$/;

  var S = { open: false, started: false, stage: "chat", lead: {}, topic: null, answered: 0, asked: false, last: null, history: [], chatKey: null };
  /* AI answers (when switched on in Admin > AI assistant): typed questions go to /api/chat; menu buttons stay instant.
     If the AI is off or cannot answer, the built-in answers below are used. */
  function aiOn() { return !!(window.YENETCH_CONFIG && window.YENETCH_CONFIG.chat) && !document.body.classList.contains("is-preview"); }
  function chatKey() {
    if (!S.chatKey) { var a = new Uint8Array(16); (window.crypto || window.msCrypto).getRandomValues(a); S.chatKey = Array.prototype.map.call(a, function (b) { return ("0" + b.toString(16)).slice(-2); }).join(""); }
    return S.chatKey;
  }
  function remember(role, text) { if (text) { S.history.push({ role: role, text: String(text).slice(0, 1500) }); if (S.history.length > 30) S.history = S.history.slice(-30); } }
  var ui = {};

  function data() { return Yenetch.data(); }
  /* Phone, email and WhatsApp come from the site data (edited in the admin); the values here are only a fallback. */
  function company() {
    var c = data().company || {};
    return { phone: c.phone || "", email: c.email || "", whatsapp: c.whatsapp || "" };
  }
  function norm(t) { return (" " + t.toLowerCase().replace(/[^a-z0-9.+@\s&-]/g, " ").replace(/\s+/g, " ") + " "); }
  function h(tag, cls, text) { var n = document.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; }

  /* ---------- UI ---------- */
  function build() {
    ui.launch = h("button", "yb-launch"); ui.launch.type = "button";
    ui.launch.innerHTML = '<span class="yb-av">' + CUBE + '</span><span class="yb-launch__dot"></span><span>Ask Yenetch</span>';
    ui.launch.setAttribute("aria-label", "Open the Yenetch assistant");
    ui.launch.addEventListener("click", function () { open(); });
    document.body.appendChild(ui.launch);

    ui.box = h("section", "yb"); ui.box.hidden = true;
    ui.box.setAttribute("role", "dialog"); ui.box.setAttribute("aria-label", "Yenetch assistant");
    ui.box.innerHTML =
      '<header class="yb__head"><span class="yb-av">' + CUBE + '</span><div><b>Yenetch Assistant</b><small>Typically replies instantly</small></div>' +
      '<button type="button" class="yb__close" aria-label="Close chat">×</button></header>' +
      '<div class="yb__log" aria-live="polite"></div><div class="yb__chips"></div>' +
      '<form class="yb__form" autocomplete="on"><label class="sr-only" for="yb-input">Type your message</label>' +
      '<input id="yb-input" name="yb-input" type="text" placeholder="Ask about services, pricing, results…" maxlength="400">' +
      '<button type="submit" aria-label="Send">➤</button></form>' +
      '<div class="yb__foot">Your details are only used to contact you about your enquiry.</div>';
    document.body.appendChild(ui.box);

    ui.log = ui.box.querySelector(".yb__log");
    ui.chips = ui.box.querySelector(".yb__chips");
    ui.input = ui.box.querySelector("input");
    ui.box.querySelector(".yb__close").addEventListener("click", close);
    ui.box.querySelector("form").addEventListener("submit", function (e) {
      e.preventDefault();
      var v = ui.input.value.trim(); if (!v) return;
      ui.input.value = ""; send(v, false);
    });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape" && S.open) close(); });
  }

  function scroll() { ui.log.scrollTop = ui.log.scrollHeight; }
  function me(text) { ui.log.appendChild(h("div", "yb-msg yb-msg--me", text)); scroll(); }
  function chips(list, viaAi) {
    ui.chips.innerHTML = "";
    (list || []).forEach(function (c) {
      var b = h("button", "yb-chip", c); b.type = "button";
      b.addEventListener("click", function () { send(c, !viaAi); });
      ui.chips.appendChild(b);
    });
  }

  // Queue bot output with a short typing pause so replies feel conversational.
  var queue = Promise.resolve();
  function bot(items, nextChips, viaAi) {
    items.forEach(function (it) { if (typeof it === "string") remember("assistant", it); });
    queue = queue.then(function () {
      return new Promise(function (done) {
        chips([]);
        var t = h("div", "yb-typing"); t.innerHTML = "<i></i><i></i><i></i>"; ui.log.appendChild(t); scroll();
        setTimeout(function () {
          t.remove();
          items.forEach(function (it) { ui.log.appendChild(typeof it === "string" ? h("div", "yb-msg yb-msg--bot", it) : it); });
          chips(nextChips, viaAi); scroll(); done();
        }, 420 + Math.min(900, (typeof items[0] === "string" ? items[0].length : 60) * 6));
      });
    });
  }

  function card(label, title, body, tags, metrics, route) {
    var c = h("div", "yb-card");
    c.appendChild(h("small", null, label)); c.appendChild(h("b", null, title));
    if (body) c.appendChild(h("p", null, body));
    if (metrics) {
      var m = h("div", "yb-metrics");
      metrics.forEach(function (x) { var s = h("span"); s.appendChild(h("b", null, x.value)); s.appendChild(document.createTextNode(x.label)); m.appendChild(s); });
      c.appendChild(m);
    }
    if (tags && tags.length) { var ul = h("ul"); tags.forEach(function (t) { ul.appendChild(h("li", null, t)); }); c.appendChild(ul); }
    if (route) { var a = h("a", "more", "View details"); a.href = Yenetch.url(route); c.appendChild(a); }
    return c;
  }

  /* ---------- knowledge search ---------- */
  function score(text, entry, fields) {
    var s = 0;
    (entry.keywords || []).forEach(function (k) { if (text.indexOf(" " + k + " ") > -1 || (k.length > 4 && text.indexOf(k) > -1)) s += k.split(" ").length * 3; });
    fields.forEach(function (f) {
      String(entry[f] || "").toLowerCase().split(/[^a-z0-9]+/).forEach(function (w) { if (w.length > 3 && text.indexOf(" " + w + " ") > -1) s += 1; });
    });
    return s;
  }

  function search(text, topicsOnly) {
    var d = data(), best = null;
    function consider(kind, list, fields) {
      (list || []).forEach(function (e) { var sc = score(text, e, fields); if (sc > 0 && (!best || sc > best.score)) best = { kind: kind, item: e, score: sc }; });
    }
    consider("service", d.services, ["name"]);
    consider("product", d.products, ["name", "category"]);
    consider("case", (d.caseStudies || []).map(function (c) { return Object.assign({ keywords: [c.client.toLowerCase(), c.industry.toLowerCase()] }, c); }), []);
    if (!topicsOnly) consider("faq", d.faqs, []);
    return best;
  }

  /* ---------- responses ---------- */
  var MENU = ["Grow leads & sales", "Build a website or app", "Explore products", "See client results", "Hire developers", "Talk to an expert"];

  function greet() {
    var offer = (data().offers || []).filter(function (o) { return o.chat; })[0];
    var lines = ["Hi, I'm the Yenetch assistant. I can explain our services, plans and prices, show results we've delivered for clients, or connect you with a specialist."];
    if (offer) lines.push("Running now: " + offer.title + (offer.endsLabel ? " (ends " + offer.endsLabel + ")" : "") + ".");
    lines.push("What brings you here today?");
    bot(lines, MENU);
  }

  function answerService(s) {
    S.topic = s.name; S.last = s;
    var related = (data().caseStudies || []).filter(function (c) { return c.services.indexOf(s.slug) > -1; })[0];
    var next = ["How long does it take?", "What does it cost?", related ? "Show a result" : "Get a free estimate", "Other services"];
    bot([s.summary, card("Service", s.name, null, s.includes, null, "/services/" + s.slug)], next);
  }

  function answerProduct(p) {
    S.topic = p.name; S.last = p;
    var cta = p.status === "Live" ? "Book a demo" : (p.status === "Early access" ? "Request early access" : "Join the waitlist");
    bot([p.name + " is our " + p.category.toLowerCase() + ". " + p.summary,
         card(p.status + " · " + p.category, p.name, p.price + (p.priceNote ? " (" + p.priceNote + ")" : ""), p.features, null, "/products/" + p.slug)],
        [cta, "How long to set up?", "Other products", "Talk to an expert"]);
  }

  /* Timeline and price questions are answered for the service or product being discussed:
     the one named in the question, else the last one shown. Without either, the general answer is used. */
  function answerAbout(kind, item) {
    S.topic = item.name; S.last = item;
    var isProduct = (data().products || []).indexOf(item) > -1;
    var text = kind === "time"
      ? (item.timeline || null)
      : (isProduct ? item.name + ": " + item.price + (item.priceNote ? ". " + item.priceNote : "") : item.pricing || null);
    if (!text) return false;
    var label = isProduct ? item.name : item.name.split(" (")[0];
    var other = kind === "time" ? "What does it cost?" : "How long does it take?";
    var related = !isProduct && (data().caseStudies || []).some(function (c) { return c.services.indexOf(item.slug) > -1; });
    bot([(kind === "time" ? "For " + label + ": " : (isProduct ? "" : label + " pricing: ")) + text], isProduct ? [other, "Book a demo", "Other products"] : [other, "Get a free estimate", related ? "Show a result" : "Other services"]);
    return true;
  }

  function answerCase(c) {
    S.topic = c.client; S.last = c;
    bot(["Here's what we did for " + c.client + ".", card(c.industry, c.title, c.solution, null, c.metrics, "/case-studies/" + c.slug)],
        ["Something similar for me", "Another result", "Explore services"]);
  }

  function showPillar(id) {
    var d = data(), p = d.pillars.filter(function (x) { return x.id === id; })[0];
    var list = d.services.filter(function (s) { return s.pillar === id; });
    bot([p.summary + " Pick one to learn more:"], list.map(function (s) { return s.name; }).slice(0, 8));
  }

  function listProducts() {
    var ps = data().products;
    bot(["We build our own products too. Two are live today and more are on the way:"].concat(
      ps.map(function (p) { return card(p.status, p.name, p.summary, null, null, "/products/" + p.slug); })), ps.map(function (p) { return p.name; }));
  }

  function nextResult() {
    var cs = data().caseStudies, i = cs.indexOf(S.last); answerCase(cs[(i + 1) % cs.length]);
  }

  /* ---------- lead capture ---------- */
  function startLead(ctx) {
    if (ctx) { S.lead.context = ctx; S.topic = ctx.topic || S.topic; }
    if (!S.open) open(true);
    S.asked = true;
    if (S.lead.name && S.lead.contact) { S.stage = "need"; askNeed(); return; }
    S.stage = "name";
    bot([(S.topic ? "Happy to get a " + S.topic + " specialist on this for you." : "Happy to connect you with a specialist.") +
         " They'll share a free, no-obligation plan. What should I call you?"], ["Maybe later"]);
    ui.input.setAttribute("autocomplete", "name");
  }

  function askNeed() {
    bot(["Last one: in a line, what do you want to achieve?", "For example: \"50 qualified leads a month for my clinic\" or \"a booking app for my cab business\"."], ["Skip this"]);
  }

  function leadStep(text, t) {
    if (/maybe later|not now|no thanks|skip for now|cancel/.test(t) && S.stage !== "need") {
      S.stage = "chat";
      bot(["No problem. I'm here whenever you're ready. What else can I help with?"], MENU); return;
    }
    if (S.stage === "name") {
      var name = text.replace(/^(i am|i'm|im|my name is|this is)\s+/i, "").trim().split(/\s+/).slice(0, 3).join(" ");
      if (name.length < 2 || /\d/.test(name)) { bot(["Sorry, I didn't catch that. What's your name?"], ["Maybe later"]); return; }
      S.lead.name = name.replace(/\b\w/g, function (c) { return c.toUpperCase(); });
      S.stage = "contact";
      bot(["Nice to meet you, " + S.lead.name.split(" ")[0] + ". What's the best phone number or email to reach you?"], ["Maybe later"]);
      ui.input.setAttribute("autocomplete", "tel");
      return;
    }
    if (S.stage === "contact") {
      var v = text.trim();
      if (!EMAIL.test(v) && !PHONE.test(v)) { bot(["That doesn't look like a phone number or email. Could you check it? For example 98765 43210 or name@company.com."], ["Maybe later"]); return; }
      S.lead.contact = v; S.stage = "need"; askNeed(); return;
    }
    if (S.stage === "need") {
      S.lead.need = /^skip/.test(t) ? "" : text;
      submitLead();
    }
  }

  function submitLead() {
    S.stage = "chat";
    var payload = {
      name: S.lead.name, contact: S.lead.contact, need: S.lead.need || "",
      topic: S.topic || "", context: S.lead.context || null,
      page: location.pathname, source: "chatbot", at: new Date().toISOString(), chat: S.chatKey || undefined
    };
    var first = S.lead.name.split(" ")[0];
    var ok = "Thanks, " + first + ". A specialist will reach you at " + S.lead.contact + " within one working day (Mon to Sat, 10am to 7pm IST).";
    var ep = window.YENBOT_ENDPOINT;
    if (ep && window.fetch) {
      fetch(ep, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) })
        .then(function (r) { if (!r.ok) throw new Error(r.status); bot([ok, "Anything else I can answer meanwhile?"], ["See client results", "Explore products"]); })
        .catch(function () { var co = company(); bot(["I couldn't send that just now. Please call " + co.phone + " or email " + co.email + " and mention your enquiry."], MENU); });
    } else {
      try { var all = JSON.parse(localStorage.getItem("yenetch-leads") || "[]"); all.push(payload); localStorage.setItem("yenetch-leads", JSON.stringify(all)); } catch (e) { /* storage unavailable */ }
      bot([ok, "(Design preview: this enquiry was kept in your browser. On the live site it goes straight to the sales inbox and CRM.)", "Anything else I can answer meanwhile?"], ["See client results", "Explore products"]);
    }
  }

  /* ---------- router ---------- */
  function askAi(text, t) {
    var typing = h("div", "yb-typing"); typing.innerHTML = "<i></i><i></i><i></i>";
    queue = queue.then(function () { chips([]); ui.log.appendChild(typing); scroll(); });
    var history = S.history.slice(-14);
    fetch(window.YENETCH_CONFIG.chat, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "same-origin",
      body: JSON.stringify({ key: chatKey(), messages: history, page: location.pathname, currency: (window.Yenetch && Yenetch.currency) ? Yenetch.currency() : "INR" }) })
      .then(function (r) { return r.json(); })
      .then(function (j) {
        queue = queue.then(function () { typing.remove(); });
        if (!j || j.fallback || !j.reply) { route(text, t); return; }
        var items = [j.reply];
        if (j.links && j.links.length) {
          var box = h("div", "yb-links");
          j.links.forEach(function (l) { var a = h("a", "more", l.label); a.href = Yenetch.url(l.url); box.appendChild(a); });
          items.push(box);
        }
        S.answered++;
        bot(items, (j.chips && j.chips.length ? j.chips : ["Talk to an expert"]).slice(0, 4), true);
        if (j.lead && !S.asked) { S.asked = true; bot(["I can have a specialist send you a tailored plan and estimate. Shall I set that up?"], ["Yes, please", "Not now"]); }
      })
      .catch(function () { queue = queue.then(function () { typing.remove(); }); route(text, t); });
  }
  function send(text, fromButton) {
    me(text);
    remember("user", text);
    var t = norm(text);
    if (S.stage !== "chat") { leadStep(text, t); return; }
    // Typed questions (and the AI's own suggestions) go to the AI; yes/no and lead buttons stay local.
    if (!fromButton && aiOn() && !/^ ?(yes|yes please|sure|not now|no thanks|maybe later|talk to an expert|book a call) ?$/.test(t)) { askAi(text, t); return; }
    route(text, t);
  }
  function route(text, t) {

    // Chip shortcuts first
    var d = data();
    if (/grow leads|more leads|get leads/.test(t)) return answerService(Yenetch.bySlug("services", "lead-generation"));
    if (/build a website or app/.test(t)) return bot(["Great. What are you planning to build?"], ["Business website", "Mobile app", "Custom software or CRM", "Online store"]);
    if (/business website/.test(t)) return answerService(Yenetch.bySlug("services", "web-development"));
    if (/custom software or crm/.test(t)) return answerService(Yenetch.bySlug("services", "crm-erp"));
    if (/explore products|other products|compare products/.test(t)) return listProducts();
    if (/see client results|another result/.test(t)) return S.last && S.last.client ? nextResult() : answerCase(d.caseStudies[0]);
    if (/show a result/.test(t) && S.last && S.last.slug) {
      var rel = d.caseStudies.filter(function (c) { return c.services.indexOf(S.last.slug) > -1; })[0];
      if (rel) return answerCase(rel);
    }
    if (/hire developers/.test(t)) return showPillar("talent");
    if (/explore services|other services|all services|what services|services do you/.test(t)) return bot(["We do marketing, development and ready-to-use products, with talent on hand when you need people. Which fits best?"],
      d.pillars.filter(function (p) { return p.id !== "talent"; }).map(function (p) { return p.name; }).concat(["Explore products"], d.pillars.filter(function (p) { return p.id === "talent"; }).map(function (p) { return p.name; })));
    for (var i = 0; i < d.pillars.length; i++) if (t.indexOf(d.pillars[i].name.toLowerCase()) > -1) return showPillar(d.pillars[i].id);
    if (/talk to an expert|get a free estimate|book a demo|request early access|join the waitlist|something similar for me|book a call/.test(t)) return startLead();

    if (HELLO.test(t.trim()) && t.trim().split(" ").length <= 3) return bot(["Hello! How can I help you today?"], MENU);

    var asksTime = TIME.test(t) || /how long to set up/.test(t), asksCost = COST.test(t);
    if (asksTime || asksCost) {
      var named = search(t, true);
      var about = named && (named.kind === "service" || named.kind === "product") ? named.item
                : (S.last && (d.services.indexOf(S.last) > -1 || d.products.indexOf(S.last) > -1) ? S.last : null);
      if (about && answerAbout(asksTime ? "time" : "cost", about)) { S.answered++; return; }
    }

    var exact = d.services.concat(d.products).filter(function (x) { return t.trim() === x.name.toLowerCase(); })[0];
    var hit = exact ? { kind: d.services.indexOf(exact) > -1 ? "service" : "product", item: exact } : search(t);

    if (hit) {
      S.answered++;
      if (hit.kind === "service") answerService(hit.item);
      else if (hit.kind === "product") answerProduct(hit.item);
      else if (hit.kind === "case") answerCase(hit.item);
      else { S.last = null; bot([hit.item.a], BUY.test(t) ? ["Get a free estimate", "Explore services"] : ["Explore services", "Talk to an expert"]); }
      // Offer a specialist once there is clear buying intent, or after a few useful answers.
      if (!S.asked && (BUY.test(t) || S.answered >= 3)) {
        S.asked = true;
        bot(["If it helps, I can have a specialist send you a tailored plan and estimate. Shall I set that up?"], ["Yes, please", "Not now"]);
      }
      return;
    }
    if (/^ ?(yes|yes please|sure|ok do it|yeah)/.test(t)) return startLead();
    if (/not now|no thanks/.test(t)) return bot(["Sure. Ask me anything else."], MENU);
    if (THANKS.test(t)) return bot(["You're welcome! Anything else I can help with?"], MENU);

    bot(["I'm not sure I understood that. I can help with services, products, pricing, timelines or past results. You can also speak to a person."], ["Explore services", "Explore products", "Talk to an expert"]);
  }

  /* ---------- open / close ---------- */
  function open(silent, firstMessage) {
    S.open = true; ui.box.hidden = false; ui.launch.hidden = true;
    if (window.Yenetch && Yenetch.track) Yenetch.track("chat_open", typeof firstMessage === "string" ? firstMessage : "");
    if (!S.started) { S.started = true; if (!silent && !firstMessage) greet(); }
    if (typeof firstMessage === "string" && firstMessage) send(firstMessage, true);
    setTimeout(function () { ui.input.focus({ preventScroll: true }); }, 50);
  }
  function close() { S.open = false; ui.box.hidden = true; ui.launch.hidden = false; ui.launch.focus(); }

  window.YenBot = {
    open: function (msg) { open(false, msg); },
    startLead: startLead
  };

  document.addEventListener("DOMContentLoaded", function () { if (data().services) build(); });
})();
