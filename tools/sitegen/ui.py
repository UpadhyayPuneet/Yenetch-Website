"""Shared building blocks: links, photos, icons, navigation, footer, mockups and common sections."""
import json, pathlib
from datetime import date
from . import bind
from .bind import P, X, esc, each, fmt, when, when_any

ROOT = pathlib.Path(__file__).resolve().parent.parent.parent
WEB = ROOT / "Yenetch.Web"
D = json.loads((WEB / "assets/data/yenetch.json").read_text(encoding="utf-8"))
PH = json.loads((WEB / "assets/img/stock/photos.json").read_text(encoding="utf-8"))
FACES = json.loads((ROOT / "tools/logo_faces.json").read_text())
SOCIAL_ICONS = json.loads((ROOT / "tools/sitegen/social_icons.json").read_text())
_pages = WEB / "assets/data/pages.json"
COPY = json.loads(_pages.read_text(encoding="utf-8")) if _pages.exists() else {"services": {}, "products": {}, "talent": {}}

SVC = {s["slug"]: s for s in D["services"]}
CASE = {c["slug"]: c for c in D["caseStudies"]}
PROD = {p["slug"]: p for p in D["products"]}
PILLAR = {p["id"]: p for p in D["pillars"]}
C = D["company"]
PHONE_TEL = "tel:+919587365247"
safe = lambda s: s.replace("</", "<\\/")


# ------------------------------------------------------------------ company details (database-driven on the site)
# In the preview these return the values from yenetch.json. In aspx they bind to Yenetch.Data.Site, which reads the
# CMS database, so phone, email, WhatsApp and similar details change from the admin without a rebuild.
def co(field):
    """Escaped company field (camelCase JSON name, e.g. "phone", "email", "recognition")."""
    if bind.MODE == "aspx":
        return f"<%: Yenetch.Data.Site.Company.{field[:1].upper() + field[1:]} %>"
    return esc(C[field])


def tel_href():
    return "<%: Yenetch.Data.Site.PhoneTel %>" if bind.MODE == "aspx" else PHONE_TEL


def wa_href(text="Hi Yenetch"):
    from urllib.parse import quote
    if bind.MODE == "aspx":
        return f'<%: Yenetch.Data.Site.WhatsApp("{text}") %>'
    return f'{C["whatsapp"]}?text={quote(text)}'


def mail_href(subject=None):
    from urllib.parse import quote
    if bind.MODE == "aspx":
        return f'<%: Yenetch.Data.Site.MailTo({chr(34) + subject + chr(34) if subject else "null"}) %>'
    return f'mailto:{C["email"]}' + (f"?subject={quote(subject)}" if subject else "")


# ------------------------------------------------------------------ links
def L(route):
    """Site route in aspx; a flat file name in the preview (/services/seo -> services-seo.html)."""
    if bind.MODE == "aspx" or route.startswith(("http", "tel:", "mailto:", "#")):
        return route
    path, _, frag = route.partition("#")
    if path.startswith("/blog/category/"):
        path = "/blog"
    name = "index" if path in ("", "/") else path.strip("/").replace("/", "-")
    return f"{name}.html" + ("#" + frag if frag else "")


def href(v):
    """Link attribute value for a bound URL (P holds a route, X a C# expression)."""
    if isinstance(v, P):
        return esc(L(v.v))
    return str(v)


# ------------------------------------------------------------------ photos
def img(key, cls="", sizes="(max-width: 900px) 100vw, 50vw", eager=False, alt=None):
    """<img> for a stock key. key: str (static), P (preview value) or X (C# expression)."""
    load = ' fetchpriority="high"' if eager else ' loading="lazy" decoding="async"'
    c = f' class="{cls}"' if cls else ""
    if isinstance(key, P):
        key = key.v
    if isinstance(key, str):
        if bind.MODE == "preview":
            p = PH.get(key)
            if not p:
                return f'<span class="ph-missing{(" " + cls) if cls else ""}"></span>'
            return f'<img{c} src="assets/img/stock/{p["file"]}" alt="{esc(alt or p["alt"])}"{load}>'
        expr, item = f'"{key}"', False
    else:
        expr, item = key.expr, key.item
    s = lambda f: str(X(f"Photos.{f}({expr})", item))
    a = esc(alt) if alt else s("Alt")
    return f'<img{c} src="{s("Src")}" srcset="{s("SrcSet")}" sizes="{sizes}" alt="{a}"{load}>'


def media(key, cls="media", ratio=None, **kw):
    style = f' style="aspect-ratio:{ratio}"' if ratio else ""
    return f'<figure class="{cls}"{style}>{img(key, **kw)}</figure>'


# ------------------------------------------------------------------ icons and marks
def mark(cls="mark"):
    f = FACES
    return (f'<svg class="{cls}" viewBox="0 0 100 100" aria-hidden="true"><path class="m-o" d="{f["ring"]}"/>'
            f'<path class="m-t" d="{f["top"]}"/><path class="m-l" d="{f["left"]}"/><path class="m-r" d="{f["right"]}"/></svg>')


GENERIC_ICON = "M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z"


def social_icon(sid):
    """Brand icon. sid: a fixed id (str), or a bound id (P / X) from the social links list."""
    if isinstance(sid, X):
        return f'<svg viewBox="0 0 24 24" aria-hidden="true"><path d="{X(f"Yenetch.Data.Site.SocialIconPath({sid.expr})", sid.item)}"/></svg>'
    if isinstance(sid, P):
        sid = sid.v
    return f'<svg viewBox="0 0 24 24" aria-hidden="true"><path d="{SOCIAL_ICONS.get(sid, GENERIC_ICON)}"/></svg>'


def social_links(cls="social"):
    items = each((C["social"], "Yenetch.Data.Site.Social"),
                 lambda s, i: f'<a href="{s.url}" target="_blank" rel="noopener" aria-label="Yenetch on {s.name}">{social_icon(s.id)}</a>',
                 "Yenetch.Models.SocialLink")
    return f'<div class="{cls}">{items}</div>'


def social_icons_cs():
    """C# source for Yenetch.Web/Code/Data/SocialIcons.g.cs (the aspx side of social_icon)."""
    rows = "".join(f'            {{ "{k}", "{v}" }},\n' for k, v in SOCIAL_ICONS.items())
    return f'''//------------------------------------------------------------------------------
// <auto-generated>
//     Generated by tools/build.py from tools/sitegen/social_icons.json. Changes to this file will be lost when the build runs again.
// </auto-generated>
//------------------------------------------------------------------------------
using System.Collections.Generic;

namespace Yenetch.Data
{{
    /// <summary>SVG paths (24x24 viewBox) for social network icons, keyed by the social link id. Use Site.SocialIconPath(id).</summary>
    public static class SocialIcons
    {{
        public static readonly Dictionary<string, string> Paths = new Dictionary<string, string>
        {{
{rows}        }};
    }}
}}
'''


# ------------------------------------------------------------------ bound-list helpers (preview value or C# expression)
def ix_cls(i, idxs, cls):
    """cls when the loop index is one of idxs. i is an int, a P index (preview) or an X index (Container.ItemIndex)."""
    if isinstance(i, P):
        i = i.v
    if isinstance(i, int):
        return cls if i in idxs else ""
    if not idxs:
        return ""
    test = " || ".join(f"{i.expr} == {n}" for n in idxs)
    return f'<%# ({test}) ? "{cls}" : "" %>'


def ix_in(i, idxs, html_):
    """html_ only for the listed loop indexes."""
    if isinstance(i, (P, int)):
        return html_ if (i.v if isinstance(i, P) else i) in idxs else ""
    if not idxs:
        return ""
    return when(i, html_, cs=lambda e: "(" + " || ".join(f"{e} == {n}" for n in idxs) + ")")


def num2(i):
    """Two-digit, one-based position of a loop index: 01, 02, ..."""
    return fmt(i, lambda v: f"{v+1:02d}", lambda e: f'({e} + 1).ToString("00")')


def top(v, n):
    """First n items of a bound string list."""
    if isinstance(v, P):
        return P((v.v or [])[:n])
    return X(f"Yenetch.Data.Site.Top({v.expr}, {n})", v.item)


def joined(v, sep=", "):
    """Escaped, joined string list."""
    return fmt(v, lambda l: sep.join(l or []), lambda e: f'Yenetch.Data.Site.Join({e}, "{sep}")')


def svc_list(pillar_id):
    """(preview list, C# expr) for the services of one pillar, in data order."""
    return ([prep_service(s) for s in D["services"] if s["pillar"] == pillar_id], f'Yenetch.Data.Site.ServicesFor("{pillar_id}")')


def prod_list():
    return ([prep_product(p) for p in D["products"]], "Yenetch.Data.Site.Products")


def stat_value(keyword, fallback):
    """A company stat value by label keyword (e.g. "served" -> "100+"), so headline numbers follow the admin."""
    if bind.MODE == "aspx":
        return f'<%: Yenetch.Data.Site.StatValue("{keyword}", "{fallback}") %>'
    s = next((x for x in C.get("stats", []) if keyword.lower() in (x.get("label") or "").lower()), None)
    return esc(s["value"] if s and s.get("value") else fallback)


def count_word(n_py, cs):
    """Number in words ("Twenty-one") for copy that counts content."""
    if bind.MODE == "aspx":
        return f"<%: Yenetch.Data.Site.CountWord({cs}) %>"
    ones = "zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen".split()
    tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"]
    w = str(n_py) if n_py >= 100 else ones[n_py] if n_py < 20 else tens[n_py // 10] + ("" if n_py % 10 == 0 else "-" + ones[n_py % 10])
    return w[:1].upper() + w[1:]


def count(n_py, cs):
    return f"<%: {cs} %>" if bind.MODE == "aspx" else str(n_py)


ICONS = {
    "arrow": '<path d="M5 12h14M13 6l6 6-6 6"/>',
    "calendar": '<rect x="3" y="4.5" width="18" height="16.5" rx="3"/><path d="M8 2.5v4M16 2.5v4M3 10h18M8 14h2M14 14h2M8 17.5h2"/>',
    "gauge": '<path d="M4.5 18a9 9 0 1 1 15 0"/><path d="m12 13 4-5"/><circle cx="12" cy="13" r="1.5"/>',
    "phone": '<path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2"/>',
    "mail": '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/>',
    "pin": '<path d="M12 21s-7-6.2-7-11a7 7 0 0 1 14 0c0 4.8-7 11-7 11z"/><circle cx="12" cy="10" r="2.5"/>',
    "chat": '<path d="M4 5h16v11H9l-5 4z"/>',
    "check": '<path d="m5 12 5 5 9-10"/>',
    "link": '<path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1"/>',
    "clock": '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
}


def icon(name, cls="ico"):
    return f'<svg class="{cls}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{ICONS[name]}</svg>'


# ------------------------------------------------------------------ navigation
NAV = [("Marketing", "/digital-marketing", "marketing", "marketing"),
       ("Development", "/software-development", "development", "engineering"),
       ("Products", "/products", "products", None),
       ("Talent", "/talent-resourcing", "talent", "talent"),
       ("Work", "/case-studies", "work", None),
       ("Company", "/about", "company", None),
       ("Insights", "/blog", "insights", None)]


def _cur(key, active):
    if bind.MODE == "aspx":
        return f' <%= Current("{key}") %>'
    return ' aria-current="page"' if key == active else ""


def mega(pillar_id):
    p = P(PILLAR[pillar_id]) if bind.MODE == "preview" else X(f'Yenetch.Data.Site.Pillar("{pillar_id}")')
    lis = each(svc_list(pillar_id), lambda s, i: f'<li><a href="{href(s.url)}"><b>{s.name}</b><span>{s.summary}</span></a></li>', "Yenetch.Models.Service")
    return f'''<div class="mega"><div class="wrap mega__grid">
      <div class="mega__intro"><span class="kicker">{p.name}</span><p>{p.summary}</p><a class="more" href="{href(p.url)}">Explore {p.name}</a></div>
      <ul class="mega__list">{lis}</ul></div></div>'''


def mega_products():
    lis = each(prod_list(), lambda p, i: f'<li><a href="{href(p.url)}"><b>{p.name} <em class="pill {p.statusCss}">{p.status}</em></b><span>{p.summary}</span></a></li>', "Yenetch.Models.Product")
    return f'''<div class="mega"><div class="wrap mega__grid">
      <div class="mega__intro"><span class="kicker">Products</span><p>Ready-to-use software from the engineers who build custom systems for our clients.</p><a class="more" href="{L("/products")}">All products</a></div>
      <ul class="mega__list">{lis}</ul></div></div>'''


def pill_css(status):
    return {"Live": "pill--live", "Early access": "pill--new"}.get(status, "pill--soon")


def nav(active=""):
    links = ""
    for text, route, key, pillar in NAV:
        panel = mega(pillar) if pillar else (mega_products() if key == "products" else "")
        cls = "nav__item has-mega" if panel else "nav__item"
        sub = (f'<button class="nav__sub" type="button" aria-expanded="false" aria-label="Show {text} pages">'
               '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg></button>') if panel else ""
        links += f'<div class="{cls}"><a class="nav__link" href="{L(route)}"{_cur(key, active)}>{text}</a>{sub}{panel}</div>'
    return f'''<header class="nav" id="nav"><div class="wrap nav__bar">
  <a class="logo" href="{L("/")}" aria-label="Yenetch home">{mark()}<span>yenetch</span></a>
  <nav class="nav__links" id="nav-links" aria-label="Main">{links}
    <a class="nav__mobile-cta btn btn--blue" href="{L("/contact")}">Let's talk</a></nav>
  <div class="nav__cta"><a class="nav__phone" href="{tel_href()}" aria-label="Call Yenetch">{icon("phone")}</a><a class="btn btn--black btn--sm" href="{L("/contact")}">Let's talk</a></div>
  <button class="nav__menu" type="button" aria-controls="nav-links" aria-expanded="false" aria-label="Menu"><i></i><i></i></button>
</div></header>'''


def footer():
    def col(title, items):
        return f'<div class="footer__col"><h4>{esc(title)}</h4><ul>' + "".join(f'<li><a href="{L(h)}">{esc(t)}</a></li>' for t, h in items) + "</ul></div>"
    short = lambda v: fmt(v, lambda n: n.split(" (")[0].replace(" & Paid Media", "").replace(" & Community", "").replace("Website & Web Application Development", "Web Development"),
                          lambda e: f"Yenetch.Data.Site.ShortName({e})")
    svc_col = lambda pid: each(svc_list(pid), lambda s, i: f'<li><a href="{href(s.url)}">{short(s.name)}</a></li>', "Yenetch.Models.Service")
    mk, dv, tl = svc_col("marketing"), svc_col("engineering"), svc_col("talent")
    offices = each((C["offices"], "Yenetch.Data.Site.Offices"), lambda o, i: f'<div><b>{o.city}</b><span>{o.label}</span><a href="{o.map}" target="_blank" rel="noopener">{o.address}</a></div>', "Yenetch.Models.Office")
    return f'''<footer class="footer"><div class="wrap">
  <div class="footer__top">
    <div class="footer__brand"><a class="logo logo--lg" href="{L("/")}">{mark()}<span>yenetch</span></a>
      <p>Digital marketing, software development and ready-to-use products for businesses that take growth seriously.</p>
      {social_links()}</div>
    <div class="footer__contact">
      <a class="footer__big-link" href="{mail_href()}">{co("email")}</a>
      <a class="footer__big-link" href="{tel_href()}">{co("phone")}</a>
      <div class="footer__offices">{offices}</div></div>
  </div>
  <div class="footer__grid">
    <div class="footer__col"><h4>Digital Marketing</h4><ul>{mk}</ul></div>
    <div class="footer__col"><h4>Software Development</h4><ul>{dv}</ul></div>
    <div class="footer__col"><h4>Products</h4><ul>{each(prod_list(), lambda p, i: f'<li><a href="{href(p.url)}">{p.name}</a></li>', "Yenetch.Models.Product")}</ul>
      <h4 class="footer__sub">Talent &amp; Resourcing</h4><ul>{tl}</ul></div>
    {col("Company", [("About us", "/about"), ("Team", "/about#team"), ("Case studies", "/case-studies"), ("Careers", "/careers"), ("Insights", "/blog"), ("Solution finder", "/solution-finder"), ("Free website audit", "/website-audit"), ("Book a call", "/book"), ("Contact", "/contact")])}
  </div>
  <div class="footer__word" aria-hidden="true">{mark()}<span>yenetch</span></div>
  <div class="footer__base"><span>© {"<%: System.DateTime.Now.Year %>" if bind.MODE == "aspx" else date.today().year} Yenetch. All rights reserved. {co("recognition")}.</span>
    <nav aria-label="Legal"><a href="{L("/privacy")}">Privacy</a><a href="{L("/terms")}">Terms</a><a href="#cookie-settings" data-cookie-settings>Cookie settings</a><a href="{L("/sitemap.xml") if bind.MODE == "aspx" else "#"}">Sitemap</a></nav></div>
</div></footer>'''


# ------------------------------------------------------------------ page chrome pieces
def h1_lines(lines):
    return "".join(f'<span class="ln"><span>{l}</span></span>' for l in lines)


def crumbs(items):
    """items: [(name, route or None)] - name may be bound markup."""
    out = []
    for n, r in items:
        out.append(f'<a href="{L(r) if isinstance(r, str) else href(r)}">{n}</a>' if r else f'<span aria-current="page">{n}</span>')
    return '<nav class="crumbs" aria-label="Breadcrumb">' + "<i>/</i>".join(out) + "</nav>"


def breadcrumb_ld(items):
    return '<script type="application/ld+json">' + json.dumps({"@context": "https://schema.org", "@type": "BreadcrumbList", "itemListElement": [
        {"@type": "ListItem", "position": i + 1, "name": n, "item": "https://www.yenetch.com" + u} for i, (n, u) in enumerate(items)]}) + "</script>"


def head(kicker, title, text="", cls="", tag="h2"):
    t = f"<p>{text}</p>" if text else ""
    return f'<div class="head {cls}"><span class="kicker">{kicker}</span><{tag}>{title}</{tag}>{t}</div>'


def subnav(title, links, cta_label="Get a quote", cta_chat=None):
    """Apple-style local navigation that sticks under the main bar."""
    a = "".join(f'<a href="#{i}">{esc(t)}</a>' for t, i in links)
    btn = f'<button class="btn btn--blue btn--xs" type="button" data-chat="{cta_chat}">{cta_label}</button>' if cta_chat else f'<a class="btn btn--blue btn--xs" href="{L("/contact")}">{cta_label}</a>'
    return f'<div class="subnav"><div class="wrap subnav__bar"><b class="subnav__title">{title}</b><nav class="subnav__links" aria-label="On this page">{a}</nav>{btn}</div></div>'


def cta(title="Ready when you are.", text="Get a free 20-minute consultation and a written plan within two working days.", chat="Talk to an expert"):
    return f'''<section class="sec--tight"><div class="wrap"><div class="cta dark fx-scale">
  <span class="cta__mark">{mark()}</span>
  <h2>{title}</h2><p>{esc(text) if isinstance(text, str) else text}</p>
  <div class="actions"><a class="btn btn--blue" href="{L("/book")}">Book a free call</a><button class="btn btn--line" type="button" data-chat="{chat}">Get my free plan</button><a class="btn btn--line" href="{tel_href()}">{icon("phone")} {co("phone")}</a></div>
  <div class="cta__alt"><a href="{wa_href()}" target="_blank" rel="noopener">{social_icon("whatsapp")} Chat on WhatsApp</a><a href="{mail_href()}">{icon("mail")} {co("email")}</a></div>
</div></div></section>'''


def faq_static(items, title="Questions we hear every week."):
    rows = "".join(f'<details><summary>{esc(q)}</summary><p>{esc(a)}</p></details>' for q, a in items)
    ld = json.dumps({"@context": "https://schema.org", "@type": "FAQPage", "mainEntity": [
        {"@type": "Question", "name": q, "acceptedAnswer": {"@type": "Answer", "text": a}} for q, a in items]}, ensure_ascii=False)
    return f'''<section class="sec" id="faq"><div class="wrap faq-wrap"><div class="head fx-up"><span class="kicker">FAQ</span><h2>{title}</h2></div>
<div class="faq">{rows}</div></div></section><script type="application/ld+json">{safe(ld)}</script>'''


def faq_bound(src, ld, title="Questions, answered."):
    """FAQ from bound data. ld: JSON-LD for the preview, or an X for aspx."""
    rows = each(src, lambda f, i: f'<details><summary>{f.q}</summary><p>{f.a}</p></details>', "Yenetch.Models.Faq")
    script = f'<script type="application/ld+json">{safe(ld) if isinstance(ld, str) else ld.raw}</script>'
    return f'''<section class="sec" id="faq"><div class="wrap faq-wrap"><div class="head fx-up"><span class="kicker">FAQ</span><h2>{title}</h2></div>
<div class="faq">{rows}</div></div></section>{script}'''


def faq_ld(items):
    return json.dumps({"@context": "https://schema.org", "@type": "FAQPage", "mainEntity": [
        {"@type": "Question", "name": f["q"], "acceptedAnswer": {"@type": "Answer", "text": f["a"]}} for f in items]}, ensure_ascii=False)


# ------------------------------------------------------------------ generated visuals (HTML/SVG illustrations)
def chart_svg(points, w=340, h=120):
    xs = [i * (w - 20) / (len(points) - 1) + 10 for i in range(len(points))]
    hi = max(points) * 1.15
    ys = [h - 18 - p / hi * (h - 30) for p in points]
    line = " ".join(f"{x:.1f},{y:.1f}" for x, y in zip(xs, ys))
    area = f"10,{h-18} " + line + f" {xs[-1]:.1f},{h-18}"
    grid = "".join(f'<line x1="10" x2="{w-10}" y1="{y:.1f}" y2="{y:.1f}"/>' for y in (h - 18, (h - 18) * .66, (h - 18) * .33))
    months = ["Apr", "May", "Jun", "Jul", "Aug", "Sep"]
    labels = "".join(f'<text class="lbl" x="{xs[int(i*(len(xs)-1)/5)]:.0f}" y="{h-4}" text-anchor="middle">{m}</text>' for i, m in enumerate(months))
    return f'''<svg viewBox="0 0 {w} {h}" role="img" aria-label="Leads trend, sample data"><defs><linearGradient id="gBlue" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0066FF" stop-opacity=".22"/><stop offset="1" stop-color="#0066FF" stop-opacity="0"/></linearGradient></defs>
<g class="grid">{grid}</g><polygon class="area" points="{area}"/><polyline class="line" points="{line}"/><circle class="dot" cx="{xs[-1]:.1f}" cy="{ys[-1]:.1f}" r="4"/>{labels}</svg>'''


def win(bar, body, dark=False, label=""):
    return f'<div class="win{" win--dark" if dark else ""}" role="img" aria-label="{esc(label or bar)}"><div class="win__bar"><i></i><i></i><i></i><span>{bar}</span></div><div class="win__body">{body}</div></div>'


def vis_dashboard(dark=False):
    body = f'''<div class="kpis"><div class="kpi"><small>Qualified leads</small><b>1,284</b><em>▲ 32%</em></div><div class="kpi"><small>Cost per lead</small><b>₹142</b><em>▼ 18%</em></div><div class="kpi"><small>ROAS</small><b>4.6x</b><em>▲ 0.8</em></div></div>
  <div class="chart">{chart_svg([210, 260, 240, 330, 390, 470, 455, 560, 640, 700, 780, 880])}</div>
  <div class="bars"><div><span>Meta Ads</span><s style="width:86%"></s><span>46%</span></div><div><span>Google Ads</span><s style="width:62%"></s><span>31%</span></div><div><span>SEO</span><s style="width:28%"></s><span>15%</span></div><div><span>LinkedIn</span><s style="width:14%"></s><span>8%</span></div></div>'''
    return win("Growth dashboard · sample data", body, dark, "Sample marketing dashboard")


def vis_dev():
    code = ('<span class="c">// Auto-assign the nearest driver</span>\n'
            '<span class="k">public async</span> <span class="t">Task</span>&lt;<span class="t">Trip</span>&gt; Dispatch(<span class="t">Booking</span> b)\n{\n'
            '    <span class="k">var</span> driver = <span class="k">await</span> _fleet.Nearest(b.Pickup, radiusKm: <span class="s">5</span>);\n'
            '    <span class="k">var</span> trip = <span class="t">Trip</span>.Start(b, driver);\n'
            '    <span class="k">await</span> _notify.WhatsApp(b.Customer, <span class="s">"Driver on the way"</span>);\n'
            '    <span class="k">return await</span> _trips.Save(trip);\n}')
    return f'''<div class="devshot">{win("DispatchService.cs", f'<div class="code">{code}</div>', True, "Code editor")}
<div class="phone" aria-hidden="true"><div class="phone__screen"><div class="phone__map"></div><div class="phone__sheet"><b>Driver arriving in 4 min</b><span>Sedan · RJ14 CA 2041</span><div class="phone__btn">Track ride</div></div></div></div></div>'''


def vis_serp():
    rows = "".join(f'<div class="serp__r{" is-us" if i == 0 else ""}"><small>{u}</small><b>{t}</b><p>{d}</p></div>' for i, (u, t, d) in enumerate([
        ("yourbrand.in › services", "Best physiotherapy clinic in Jaipur | YourBrand", "Book a same-day appointment. 4.9★ from 1,200+ patients. Open 7 days."),
        ("directory.example › jaipur", "Top 10 clinics in Jaipur", "Compare ratings, timings and fees for clinics near you."),
        ("healthsite.example", "What to expect at your first visit", "A guide to assessments, sessions and recovery plans.")]))
    body = f'<div class="serp"><div class="serp__q">physiotherapy clinic near me{icon("arrow")}</div><div class="serp__map"><i></i><i></i><i></i><span>Map pack · #1</span></div>{rows}</div>'
    return win("Search results · illustration", body, label="Search results with the client ranked first")


def vis_social():
    tiles = "".join(f'<i class="t{n}"></i>' for n in range(1, 7))
    return f'''<div class="social-vis"><div class="phone phone--static" aria-hidden="true"><div class="phone__screen"><div class="ig__head"><i></i><b>yourbrand</b><span>Follow</span></div>
<div class="ig__stats"><span><b>248</b>posts</span><span><b>38.2k</b>followers</span><span><b>+12%</b>reach</span></div><div class="ig__grid">{tiles}</div></div></div>
<div class="float-card float-card--a"><small>Engagement rate</small><b>6.8%</b><em>▲ 2.1 pts</em></div><div class="float-card float-card--b"><small>Leads from DMs</small><b>312</b></div></div>'''


def vis_reviews():
    stars = "★★★★★"
    rev = "".join(f'<div class="rv"><div><b>{n}</b><span class="stars">{stars[:s]}<em>{stars[s:]}</em></span></div><p>{t}</p>{"<small>Owner replied within 2 hours</small>" if r else ""}</div>' for n, s, t, r in [
        ("Meera K.", 5, "Staff were quick and kind. Booked online in a minute.", True), ("Arjun P.", 4, "Great service, parking could be better.", True), ("Sana R.", 5, "Exactly what we needed. Recommended.", False)])
    body = f'<div class="reviews"><div class="reviews__score"><b>4.8</b><span class="stars">★★★★★</span><small>Google rating · 640 reviews</small></div>{rev}</div>'
    return win("Reputation monitor · illustration", body, label="Review dashboard")


def vis_crm():
    body = '<div class="ui-kanban"><div><small>New · 24</small><p class="hot">Aarav M. · Website</p><p>Priya S. · Meta</p><p>Dev R. · Google</p></div><div><small>Contacted · 12</small><p>Rohan K. · Google</p><p class="hot">Neha J. · WhatsApp</p></div><div><small>Won · 7</small><p>Kabir T. · Referral</p><p>Isha V. · Event</p></div></div>'
    return win("Yenetch Leads · Pipeline", body, label="Lead pipeline board")


def vis_store():
    body = '<div class="ui-store"><div><i></i><b>Linen kurta</b>₹2,499</div><div><i></i><b>Leather tote</b>₹3,850</div><div><i></i><b>Silk scarf</b>₹1,299</div></div><div class="kpis"><div class="kpi"><small>Orders today</small><b>86</b></div><div class="kpi"><small>Revenue</small><b>₹1.9L</b></div><div class="kpi"><small>Fees</small><b>₹0</b></div></div>'
    return win("store.yourbrand.in", body, label="Online store admin")


def vis_design():
    body = '''<div class="figma"><aside><i></i><i></i><i></i><i></i></aside><div class="figma__canvas"><div class="fr fr--a"><span>Home</span><i></i><i></i><b></b></div><div class="fr fr--b"><span>Checkout</span><i></i><b></b></div><div class="fr fr--c"><span>Success</span><em></em></div><div class="cursor"><svg viewBox="0 0 12 16"><path d="M0 0v13l3.5-3.2L6 16l2-.8-2.4-6H10z"/></svg><span>Design lead</span></div></div></div>'''
    return win("Checkout flow v3 · Design file", body, label="Interface design file")


def vis_ai():
    body = '''<div class="ai"><div class="ai__msg ai__msg--me">Which products sold best in Jaipur last month?</div>
<div class="ai__msg"><b>Top 3 in Jaipur, August</b><div class="ai__bars"><p><span>Linen kurta</span><s style="width:92%"></s></p><p><span>Silk scarf</span><s style="width:64%"></s></p><p><span>Leather tote</span><s style="width:41%"></s></p></div><small>From 1,284 orders · updated 2 min ago</small></div>
<div class="ai__input">Ask about your data…<i></i></div></div>'''
    return win("AI assistant · your data", body, True, "AI assistant answering a sales question")


def vis_cloud():
    svc = "".join(f'<p><i class="{c}"></i><span>{n}</span><em>{u}</em></p>' for n, u, c in [("API gateway", "99.99%", "ok"), ("Web app", "99.98%", "ok"), ("Database", "99.99%", "ok"), ("Background jobs", "Deploying", "busy")])
    bars = "".join(f'<s style="height:{h}%"></s>' for h in [40, 52, 48, 60, 58, 72, 66, 80, 62, 70, 76, 68, 84, 74, 70, 88])
    body = f'<div class="cloud"><div class="cloud__status">{svc}</div><div class="cloud__chart"><small>Requests per minute</small><div>{bars}</div></div></div>'
    return win("Infrastructure · Production", body, True, "Cloud monitoring dashboard")


def vis_talent():
    people = [("Senior .NET Developer", "7 yrs · C#, Azure, SQL Server", "Available in 5 days", "a"), ("Performance Marketer", "5 yrs · Google, Meta, GA4", "Available now", "b"), ("Flutter Developer", "4 yrs · Flutter, Firebase", "Available in 7 days", "c")]
    cards = "".join(f'<div class="tc tc--{k}"><i></i><div><b>{r}</b><span>{s}</span></div><em>{a}</em></div>' for r, s, a, k in people)
    return f'<div class="talent-vis">{cards}<div class="float-card float-card--c"><small>Shortlist sent</small><b>48 hours</b></div></div>'


def vis_product(slug):
    if slug == "ecomm":
        return vis_store()
    if slug == "leads":
        return vis_crm()
    if slug == "studio-ai":
        return win("Studio AI · Background: Warm studio", '<div class="ui-photo"><figure><figcaption>Phone photo</figcaption></figure><figure><figcaption>Studio AI</figcaption></figure></div>', label="Before and after product photo")
    if slug == "billing-pos":
        return win("Billing · Counter 1", '<div class="ui-receipt"><div class="grid"><span>Chai</span><span>Coffee</span><span>Sandwich</span><span>Wrap</span><span>Cookie</span><span>Juice</span></div><div class="bill"><p><span>Coffee x2</span><span>₹240</span></p><p><span>Sandwich</span><span>₹180</span></p><p><span>GST 5%</span><span>₹21</span></p><p><span>Total</span><span>₹441</span></p></div></div>', label="Billing counter screen")
    return win("Yenetch Office · Attendance", '<div class="ui-hr"><p><i></i><span>Ananya · Design</span><em>In</em></p><p><i></i><span>Vikram · Engineering</span><em>In</em></p><p><i></i><span>Sara · Marketing</span><em class="away">Leave</em></p><p><i></i><span>Kabir · Sales</span><em>In</em></p></div>', label="Attendance screen")


VISUALS = {"dashboard": vis_dashboard, "serp": vis_serp, "social": vis_social, "reviews": vis_reviews, "crm": vis_crm, "dev": vis_dev,
           "store": vis_store, "design": vis_design, "ai": vis_ai, "cloud": vis_cloud, "talent": vis_talent}


def _known(keys):
    return "new[] { " + ", ".join(f'"{k}"' for k in keys) + " }"


def visual_switch(v, photo=None, photo_cls="phero__photo"):
    """Service illustration by key. Preview: the one visual. aspx: every visual, each shown only when it matches;
    a service with an unknown (or empty) key shows its photo instead."""
    if isinstance(v, P):
        return VISUALS.get(v.v, vis_dashboard)()
    out = "".join(when(v, fn(), cs=lambda e, k=k: f'{e} == "{k}"') for k, fn in VISUALS.items())
    if photo is not None:
        out += when(v, f'<figure class="{photo_cls}">{img(photo, "", "(max-width: 960px) 100vw, 45vw")}</figure>',
                    cs=lambda e: f"System.Array.IndexOf({_known(VISUALS)}, {e}) < 0")
    return out


PRODUCT_VISUALS = ("ecomm", "leads", "studio-ai", "billing-pos", "office")


def product_switch(v, photo=None, photo_cls="phero__photo"):
    """Product illustration by slug; in aspx a product without a built-in illustration shows its photo."""
    if isinstance(v, P):
        return vis_product(v.v)
    out = "".join(when(v, vis_product(k), cs=lambda e, k=k: f'{e} == "{k}"') for k in PRODUCT_VISUALS)
    if photo is not None:
        out += when(v, f'<figure class="{photo_cls}">{img(photo, "", "(max-width: 960px) 100vw, 45vw")}</figure>',
                    cs=lambda e: f"System.Array.IndexOf({_known(PRODUCT_VISUALS)}, {e}) < 0")
    return out


def cube_stage():
    f = FACES
    svg = (
        '<svg class="ybox" viewBox="-12 -16 124 132">'
        '<defs>'
        '<linearGradient id="yb-ring" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset=".55" stop-color="#c9c9cf"/><stop offset="1" stop-color="#6e6e76"/></linearGradient>'
        '<linearGradient id="yb-bar" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#f4f4f6"/><stop offset="1" stop-color="#8e8e96"/></linearGradient>'
        '<linearGradient id="yb-top" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#7fb0ff"/><stop offset=".5" stop-color="#0066ff"/><stop offset="1" stop-color="#0043b8"/></linearGradient>'
        '<linearGradient id="yb-sheen" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff" stop-opacity="0"/><stop offset=".5" stop-color="#fff" stop-opacity=".85"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></linearGradient>'
        '<radialGradient id="yb-glow"><stop offset="0" stop-color="#0066ff" stop-opacity=".55"/><stop offset="1" stop-color="#0066ff" stop-opacity="0"/></radialGradient>'
        '<filter id="yb-blur" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="6"/></filter>'
        f'<clipPath id="yb-body"><path d="{f["ring"]}"/><path d="{f["left"]}"/><path d="{f["right"]}"/></clipPath>'
        '</defs>'
        '<ellipse class="ybox__glow" cx="50" cy="104" rx="46" ry="9" fill="url(#yb-glow)"/>'
        f'<path class="ybox__halo" d="{f["top"]}" fill="#0066ff" filter="url(#yb-blur)"/>'
        f'<path class="ybox__ring" d="{f["ring"]}" fill="url(#yb-ring)"/>'
        f'<path class="ybox__bar ybox__bar--l" d="{f["left"]}" fill="url(#yb-bar)"/>'
        f'<path class="ybox__bar ybox__bar--r" d="{f["right"]}" fill="url(#yb-bar)"/>'
        '<g clip-path="url(#yb-body)"><g transform="rotate(20 50 50)"><rect class="ybox__sheen" x="-50" y="-20" width="28" height="150" fill="url(#yb-sheen)"/></g></g>'
        f'<path class="ybox__lid" d="{f["top"]}" fill="url(#yb-top)"/>'
        '</svg>')
    return ('<div class="hero__stage" aria-hidden="true"><div class="ybox3d">' + svg + '</div>'
            '<div class="hero__orbit"><span>Marketing</span><span>Development</span><span>Products</span></div></div>')


# ------------------------------------------------------------------ shared sections
def case_card(c, i, blue=False):
    """Stacking case-study card. c is a P or X case study."""
    nums = each(c.metrics, lambda m, j: f'<div><b class="num">{m.value}</b><span>{m.label}</span></div>', "Yenetch.Models.Metric")
    link = when(c.url, f'<a href="{c.url}" target="_blank" rel="noopener">Visit {c.urlLabel}</a>')
    return f'''<article class="scard{" scard--blue" if blue else ""}" style="--i:{i if isinstance(i, int) else attr_ix(i)}">
  <div class="scard__body"><div class="scard__meta"><span>{c.client}</span><span>{c.industry}</span></div>
    <h3>{c.title}</h3>
    <dl><div><dt>The challenge</dt><dd>{c.challenge}</dd></div><div><dt>What we did</dt><dd>{c.solution}</dd></div></dl>
    <div class="scard__foot"><a class="more" href="{href(c.pageUrl)}">Read the case study</a>{link}</div></div>
  <div class="scard__side">{img(c.photo, "scard__photo", "(max-width: 860px) 100vw, 40vw")}<div class="scard__nums">{nums}</div></div></article>'''


def attr_ix(ix):
    return bind.attr_raw(ix, lambda v: v, lambda e: e)


def stack_static(slugs, blue_idx=(1,)):
    if bind.MODE == "preview":
        return '<div class="stack">' + "".join(case_card(P(prep_case(CASE[s])), i, i in blue_idx) for i, s in enumerate(slugs)) + "</div>"
    arr = ", ".join(f'"{s}"' for s in slugs)
    return '<div class="stack">' + each(X(f"SiteContent.Cases({arr})"), lambda c, i: case_card(c, i), "Yenetch.Models.CaseStudy") + "</div>"


def case_tile(c):
    """Grid card with photo for the case study index and related work."""
    return f'''<a class="ctile fx-up" href="{href(c.pageUrl)}" data-industry="{c.industry}">
  <figure class="ctile__media">{img(c.photo, "", "(max-width: 700px) 100vw, 33vw")}<figcaption><b class="num">{c.headline.value}</b><span>{c.headline.label}</span></figcaption></figure>
  <div class="ctile__body"><div class="ctile__meta"><span>{c.client}</span><span>{c.industry}</span></div><h3>{c.title}</h3><span class="more">Read the case study</span></div></a>'''


def service_tile(s, i, cls="tile"):
    num = fmt(i, lambda v: f"{v+1:02d}", lambda e: f'({e} + 1).ToString("00")')
    return f'<a class="{cls}" href="{href(s.url)}"><span class="tile__ix num">{num}</span><h3>{s.name}</h3><p>{s.summary}</p><span class="more">Learn more</span></a>'


def bento(pillar, dark_idx=(0,), blue_idx=(), wide_idx=(0, 1)):
    def tile(s, i):
        cls = "tile" + ix_cls(i, wide_idx, " tile--wide") + ix_cls(i, dark_idx, " tile--dark") + ix_cls(i, blue_idx, " tile--blue")
        tags = each(top(s.includes, 4), lambda x, j: f"<li>{x}</li>")
        photo = ix_in(i, wide_idx, f'<figure class="tile__photo">{img(s.photo, "", "(max-width: 1000px) 100vw, 50vw")}</figure>')
        num = fmt(i, lambda v: f"{v+1:02d}", lambda e: f'({e} + 1).ToString("00")')
        return f'''<a class="{cls}" href="{href(s.url)}">{photo}<span class="tile__ix num">{num}</span><h3>{s.name}</h3><p>{s.summary}</p><ul>{tags}</ul><span class="more">Learn more</span></a>'''
    return f'<div class="bento fx-stagger">{each(svc_list(pillar), tile, "Yenetch.Models.Service")}</div>'


def finder_section(tag="h2"):
    return f'''<section class="sec paper" id="finder"><div class="wrap finder">
  <div class="head fx-up" style="margin:0"><span class="kicker">Solution finder</span><{tag}>Tell us the goal. We'll map the route.</{tag}>
    <p>Four quick questions. You get the right services, products and a proof point from a client like you. No sign-up, no spam.</p>
    <div class="actions"><button class="btn btn--line" type="button" data-chat="Talk to an expert">Rather talk to a person?</button></div></div>
  <div class="finder__panel fx-right" id="finder-panel" aria-live="polite"><noscript>Enable JavaScript to use the solution finder, or <a href="{L("/contact")}">contact us</a>.</noscript></div>
</div></section>'''


def post_card(b, cls="post"):
    return f'''<a class="{cls}" href="{href(b.url)}"><figure class="post__cover">{img(b.coverImage, "", "(max-width: 900px) 100vw, 33vw")}</figure>
  <div class="post__body"><div class="post__meta"><b>{b.category}</b><span>{b.readMinutes} min read</span></div><h3>{b.title}</h3><p>{b.excerpt}</p><time datetime="{b.date}">{b.displayDate}</time></div></a>'''


def posts_section(title="Field notes from people doing the work.", n=3):
    src = ([prep_post(b) for b in sorted(D["blog"], key=lambda b: b["date"], reverse=True)[:n]], f"Yenetch.Data.BlogStore.Repository.Latest({n})")
    items = each(src, lambda b, i: post_card(b), "Yenetch.Models.BlogPost")
    return f'''<section class="sec paper" id="insights"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Insights</span><h2>{title}</h2></div><a class="btn btn--line" href="{L("/blog")}">All articles</a></div>
  <div class="posts fx-stagger">{items}</div></div></section>'''


def quotes_section():
    src = ([dict(t, initial=(t.get("name") or "")[:1]) for t in D["testimonials"]], "Yenetch.Data.Site.Testimonials")
    def face(t):
        if bind.MODE != "aspx":
            return f"<i>{t.initial}</i>"
        # Reviewer photo when one is set in the admin, otherwise the initial.
        return ("<asp:PlaceHolder runat=\"server\" Visible='<%# Item.Photo != null && Item.Photo.Trim() != \"\" %>'>"
                '<img src="<%#: Yenetch.Data.Site.PhotoUrl(Item.Photo) %>" alt="" width="40" height="40" loading="lazy" decoding="async"></asp:PlaceHolder>'
                "<asp:PlaceHolder runat=\"server\" Visible='<%# Item.Photo == null || Item.Photo.Trim() == \"\" %>'>"
                f"<i>{t.initial}</i></asp:PlaceHolder>")
    q = each(src, lambda t, i: f'<figure class="quote fx-up"><blockquote>“{t.text}”</blockquote><figcaption>{face(t)}<span><b>{t.name}</b>{t.company}</span></figcaption></figure>', "Yenetch.Models.Testimonial")
    return f'''<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Client voices</span><h2>In their words.</h2></div>
  <div class="quotes">{q}</div></div></section>'''


def logos_marquee(text="Trusted by growing brands, hospitals, universities and the High Court of Delhi"):
    def chip(n):
        if bind.MODE != "aspx":
            return f"<span>{n}</span>"
        # Logo when one is uploaded in the admin (fitted to one size in CSS), otherwise the name.
        return ("<asp:PlaceHolder runat=\"server\" Visible='<%# Yenetch.Data.Site.ClientLogo(Item) != \"\" %>'>"
                '<span class="logos__img"><img src="<%#: Yenetch.Data.Site.ClientLogo(Item) %>" alt="<%#: Item %>" loading="lazy" decoding="async"></span></asp:PlaceHolder>'
                "<asp:PlaceHolder runat=\"server\" Visible='<%# Yenetch.Data.Site.ClientLogo(Item) == \"\" %>'>"
                f"<span>{n}</span></asp:PlaceHolder>")
    names = each((D["clients"], "Yenetch.Data.Site.Clients"), lambda n, i: chip(n))
    return f'<section class="logos" aria-label="Clients"><p>{text}</p><div class="logos__track">{names}{names}</div></section>'


def leader_card(l):
    photo = img(l["photo"], "", "240px") if l.get("photo") else f'<span class="leader__mono">{mark()}</span>'
    title = esc(l["name"] or l["role"])
    sub = esc(l["role"] if l["name"] else l["remit"])
    remit = f'<p class="leader__remit">{esc(l["remit"])}</p>' if l["name"] else ""
    li = f'<a class="leader__in" href="{esc(l["linkedin"])}" target="_blank" rel="noopener" aria-label="LinkedIn">{social_icon("linkedin")}</a>' if l.get("linkedin") else ""
    return f'<article class="leader fx-up"><figure class="leader__photo">{photo}</figure><div><h3>{title}</h3><p>{sub}</p>{remit}</div>{li}</article>'


def team_section(full=True):
    """Leadership and squads. Leader names and photos come from yenetch.json (team.leadership)."""
    t = D["team"]
    if bind.MODE == "aspx":
        leaders = each(X("SiteContent.Current.Team.Leadership"), lambda l, i: f'''<article class="leader fx-up"><figure class="leader__photo">{when(l.hasPhoto, img(l.photo, "", "240px"), cs=lambda e: e)}{when(l.hasPhoto, f'<span class="leader__mono">{mark()}</span>', cs=lambda e: "!" + e)}</figure><div><h3>{l.title}</h3><p>{l.subtitle}</p>{when(l.hasName, f'<p class="leader__remit">{l.remit}</p>', cs=lambda e: e)}</div>{when(l.linkedin, f'<a class="leader__in" href="{l.linkedin}" target="_blank" rel="noopener" aria-label="LinkedIn">{social_icon("linkedin")}</a>')}</article>''', "Yenetch.Models.Leader")
    else:
        leaders = "".join(leader_card(l) for l in t["leadership"])
    squads = each((t["squads"], "Yenetch.Data.Site.Squads"), lambda s, i: f'<li class="squad fx-up"><b class="num">{num2(i)}</b><h3>{s.name}</h3><p>{s.text}</p><ul>{each(s.roles, lambda r, j: f"<li>{r}</li>")}</ul></li>', "Yenetch.Models.Squad")
    return f'''<section class="sec" id="team"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Team</span><h2>The people who answer for your results.</h2>
    <p>Thirty-plus specialists across Gurugram and Jaipur, organised in squads so every client gets senior attention and a bench behind it.</p></div>
    <a class="btn btn--line" href="{L("/careers")}">Join the team</a></div>
  <div class="leaders">{leaders}</div>
  <ul class="squads">{squads}</ul>
</div></section>'''


def mosaic(keys, cls="mosaic"):
    return f'<div class="{cls}">' + "".join(f'<figure class="mosaic__i mosaic__i--{i+1}">{img(k, "", "(max-width: 700px) 50vw, 25vw")}</figure>' for i, k in enumerate(keys)) + "</div>"


def photo_rows(row_a, row_b):
    """Two rows of photos that slide in opposite directions as the page scrolls."""
    r = lambda keys: "".join(f'<figure>{img(k, "", "320px")}</figure>' for k in keys)
    return f'<div class="prow" aria-hidden="true"><div class="prow__track prow__track--a">{r(row_a)}</div><div class="prow__track prow__track--b">{r(row_b)}</div></div>'


def industries_section():
    tiles = each((D["industries"], "Yenetch.Data.Site.Industries"), lambda x, i: f'''<article class="ind fx-up"><figure>{img(x.photo, "", "(max-width: 700px) 50vw, 25vw")}</figure><div class="ind__txt"><h3>{x.name}</h3><p>{joined(x.clients)}</p></div></article>''', "Yenetch.Models.Industry")
    return f'''<section class="sec" id="industries"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Industries</span><h2>Deep in the sectors we serve.</h2><p>Playbooks, compliance know-how and benchmarks from years of work in each industry.</p></div>
  <div class="inds">{tiles}</div></div></section>'''


# ------------------------------------------------------------------ preview data prep (computed fields the C# model exposes)
def prep_case(c):
    c = dict(c)
    c["pageUrl"] = "/case-studies/" + c["slug"]
    c["urlLabel"] = (c.get("url") or "").replace("https://", "").replace("http://", "").replace("www.", "").rstrip("/")
    c["headline"] = c["metrics"][0] if c.get("metrics") else {}
    c["hasQuote"] = bool(c.get("quote"))
    return c


def prep_post(b):
    b = dict(b)
    b["url"] = "/blog/" + b["slug"]
    b["displayDate"] = date.fromisoformat(b["date"]).strftime("%-d %b %Y")
    return b


def prep_service(s):
    s = dict(s)
    s["url"] = "/services/" + s["slug"]
    return s


def prep_product(p):
    p = dict(p)
    p["url"] = "/products/" + p["slug"]
    p["statusCss"] = pill_css(p["status"])
    p["shortName"] = p["name"].replace("Yenetch ", "")
    return p
