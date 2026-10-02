"""Yenetch site generator.

One set of templates (tools/site/*.py), two outputs:
  1. preview/          static HTML for reviewing the design without .NET (every page, including all
                       21 service pages, 5 product pages, 8 case studies and 10 articles).
  2. Yenetch.Web/      Web Forms markup: Site.Master and every .aspx page. Lists and templated pages bind
                       to SiteContent (JSON today) and BlogStore (JSON or SQL) at runtime, so content changes
                       in yenetch.json, pages.json or the database need no rebuild.

    python3 tools/build.py            # both
    python3 tools/build.py preview    # preview only

Re-run after changing a template. Hand-written code-behind files (the templated pages, Contact) are never
overwritten; simple pages get a generated code-behind and designer file.
"""
import json, re, shutil, sys, pathlib
from urllib.parse import quote

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from sitegen import bind, ui, pages as pg  # noqa: E402
from sitegen import dock  # noqa: E402

ROOT, WEB, D, C = ui.ROOT, ui.WEB, ui.D, ui.C
esc, safe = bind.esc, ui.safe
FONTS = "https://fonts.googleapis.com/css2?family=Inter+Tight:wght@500;600;700&family=Inter:wght@400;500;600&display=swap"
VER = "10"

# (class, route, nav key, title, description, template)
STATIC = [
    ("Default", "/", "home", "Yenetch | Digital Marketing, Software Development & IT Talent", "Yenetch is a Gurugram and Jaipur based digital marketing, software development and IT resourcing company. 100+ clients since 2019. DPIIT-recognised.", pg.home),
    ("DigitalMarketing", "/digital-marketing", "marketing", "Digital Marketing Agency in Gurugram & Jaipur | Yenetch", "Performance marketing, SEO, social media, lead generation and brand management that report to revenue. See results from SLP Quest, Brkesiya and more.", pg.marketing),
    ("SoftwareDevelopment", "/software-development", "development", "Software Development Company in Gurugram & Jaipur | Yenetch", "Custom software, web and mobile apps, CRM, e-commerce and AI solutions built with .NET, React and Flutter. Dedicated teams and staff augmentation.", pg.development),
    ("TalentResourcing", "/talent-resourcing", "talent", "IT Staff Augmentation & Dedicated Teams in India | Yenetch", "Hire vetted developers, designers and marketers in days. Staff augmentation, dedicated teams and managed marketing squads from Gurugram and Jaipur.", pg.talent),
    ("Services", "/services", "", "Services | Digital Marketing, Software & IT Talent | Yenetch", "All 21 Yenetch services across digital marketing, software development and talent resourcing, delivered by one accountable team.", pg.services_index),
    ("Products", "/products", "products", "Products | Ecomm, Leads, Studio AI, Billing & Office | Yenetch", "Yenetch products: an online store platform, lead management, AI product photography, GST billing and office management software.", pg.products_index),
    ("CaseStudies", "/case-studies", "work", "Case Studies | Results for Hospitals, Universities & Brands | Yenetch", "Case studies from CabYaari, SLP Quest, South Asia Education, Brkesiya, Sahara Evols, JMCH, NUYU and RUHS, with the numbers that moved.", pg.cases_index),
    ("About", "/about", "company", "About Yenetch | Team, Values & Offices in Gurugram and Jaipur", "Meet Yenetch: 30+ marketers, engineers and designers in Gurugram and Jaipur, building growth and technology for 100+ clients since 2019.", pg.about),
    ("Careers", "/careers", "company", "Careers at Yenetch | Jobs in Gurugram & Jaipur", "Join 30+ developers, designers and marketers in Gurugram and Jaipur. See open roles and how we hire.", pg.careers),
    ("Contact", "/contact", "", "Contact Yenetch | Gurugram & Jaipur | +91 95873 65247", "Talk to Yenetch about marketing, software or dedicated teams. Call +91 95873 65247, WhatsApp, email hello@yenetch.com or visit our offices.", pg.contact),
    ("Book", "/book", "", "Book a Free Consultation Call | Yenetch", "Pick a time for a free 30-minute call with a Yenetch specialist in marketing, software or hiring. Confirmation and calendar invite by email.", pg.book_page),
    ("WebsiteAudit", "/website-audit", "", "Free Website Audit Tool: SEO, Speed & Mobile Check | Yenetch", "Free website audit in under a minute: check your SEO, page speed, mobile setup, security and social tags, with plain-English fixes and a score out of 100.", pg.audit_page),
    ("SolutionFinder", "/solution-finder", "", "Solution Finder | Find the Right Service in 4 Questions | Yenetch", "Answer four quick questions and get the marketing, software or talent services that fit your goal, stage and industry.", pg.finder_page),
    ("Blog", "/blog", "insights", "Insights | Marketing, Software & Growth Guides | Yenetch", "Practical guides on digital marketing, SEO, software, e-commerce and hiring from the Yenetch team.", pg.blog_index),
    ("Privacy", "/privacy", "", "Privacy Policy | Yenetch", "How Yenetch collects, uses and protects personal information shared through yenetch.com.", lambda: pg.legal("privacy")),
    ("Terms", "/terms", "", "Terms of Use | Yenetch", "Terms governing the use of the Yenetch website.", lambda: pg.legal("terms")),
    ("NotFound", "/404", "", "Page not found | Yenetch", "The page you were looking for could not be found.", pg.not_found),
]

# Templated pages: (class, preview items, route for item, template, title fn, description fn, nav key fn)
TEMPLATED = [
    ("ServiceDetail", D["services"], lambda s: "/services/" + s["slug"], pg.service_detail,
     lambda s: ui.COPY["services"].get(s["slug"], {}).get("seoTitle") or s["name"] + " | Yenetch",
     lambda s: ui.COPY["services"].get(s["slug"], {}).get("seoDescription") or s["summary"],
     lambda s: {"marketing": "marketing", "engineering": "development", "talent": "talent"}[s["pillar"]]),
    ("ProductDetail", D["products"], lambda p: "/products/" + p["slug"], pg.product_detail,
     lambda p: ui.COPY["products"].get(p["slug"], {}).get("seoTitle") or p["name"] + " | Yenetch",
     lambda p: ui.COPY["products"].get(p["slug"], {}).get("seoDescription") or p["summary"], lambda p: "products"),
    ("CaseStudyDetail", D["caseStudies"], lambda c: "/case-studies/" + c["slug"], pg.case_detail,
     lambda c: f'{c["client"]} Case Study | Yenetch', lambda c: c["title"] + ". " + c["challenge"][:90], lambda c: "work"),
    ("Landing", pg.LANDING_SEED, lambda l: "/" + l["slug"], pg.landing_page, lambda l: l["seoTitle"], lambda l: l["seoDescription"], lambda l: ""),
    ("BlogArticle", D["blog"], lambda b: "/blog/" + b["slug"], pg.blog_article,
     lambda b: b["title"] + " | Yenetch", lambda b: b["excerpt"], lambda b: "insights"),
]


# ------------------------------------------------------------------ preview
def build_preview():
    bind.MODE = "preview"
    out = ROOT / "preview"
    if out.exists():
        shutil.rmtree(out)
    (out / "assets").mkdir(parents=True)
    for sub in ("css", "js", "img"):
        shutil.copytree(WEB / "assets" / sub, out / "assets" / sub)
    (out / "assets/js/data.js").write_text(
        f"window.YENETCH_DATA = {safe(json.dumps(D, ensure_ascii=False))};\nwindow.YENBOT_ENDPOINT = null;\nwindow.YENETCH_PULSE = null;\n", encoding="utf-8")

    def write(route, key, title, desc, body):
        name = ui.L(route) if route != "/404" else "404.html"
        html = f'''<!doctype html>
<html lang="en-IN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>{esc(title)}</title><meta name="description" content="{esc(desc)}"><meta name="theme-color" content="#000000">
<link rel="icon" href="assets/img/favicon.svg" type="image/svg+xml">
<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin><link rel="stylesheet" href="{FONTS}">
<link rel="stylesheet" href="assets/css/site.css?v={VER}"></head>
<body class="is-preview">
<a class="sr-only" href="#main">Skip to content</a>
{ui.nav(key)}
<main id="main">{body}</main>
{ui.footer()}
{dock.chrome()}
<script src="assets/js/data.js"></script><script src="assets/js/site.js?v={VER}"></script><script src="assets/js/finder.js?v={VER}"></script><script src="assets/js/yenbot.js?v={VER}"></script><script src="assets/js/consent.js?v={VER}"></script><script src="assets/js/dock.js?v={VER}"></script><script src="assets/js/apply.js?v={VER}"></script><script src="assets/js/growth.js?v={VER}"></script>
</body></html>'''
        (out / name).write_text(html, encoding="utf-8")
        return name

    files = []
    for cls, route, key, title, desc, fn in STATIC:
        files.append(write(route, key, title, desc, fn()))
    for cls, items, route, fn, tfn, dfn, kfn in TEMPLATED:
        for it in items:
            files.append(write(route(it), kfn(it), tfn(it), dfn(it), fn(it)))
    print(f"preview: {len(files)} pages")
    return files


# ------------------------------------------------------------------ aspx
PAGE_HEAD = '''<%@ Page Title="{title}" MetaDescription="{desc}" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="{cls}.aspx.cs" Inherits="Yenetch.Web.{cls}" %>
<%@ MasterType VirtualPath="~/Site.Master" %>
<%@ Import Namespace="Yenetch.Data" %>
<%-- Generated by tools/build.py from tools/sitegen/pages.py. Edit the template and re-run the build. --%>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
{body}
</asp:Content>
'''

CONTROL_TYPES = {"form": "System.Web.UI.HtmlControls.HtmlForm", "asp:TextBox": "System.Web.UI.WebControls.TextBox",
                 "asp:DropDownList": "System.Web.UI.WebControls.DropDownList", "asp:Button": "System.Web.UI.WebControls.Button",
                 "asp:PlaceHolder": "System.Web.UI.WebControls.PlaceHolder", "asp:Repeater": "System.Web.UI.WebControls.Repeater",
                 "asp:Literal": "System.Web.UI.WebControls.Literal", "asp:FileUpload": "System.Web.UI.WebControls.FileUpload"}


def designer(cls, markup, base="Page"):
    fields = ""
    for tag, cid in re.findall(r'<(form|asp:\w+)\b[^>]*?\bID="(\w+)"', markup):
        fields += f'''
        /// <summary>{cid} control.</summary>
        protected global::{CONTROL_TYPES.get(tag, "System.Web.UI.Control")} {cid};
'''
    master = '''
        /// <summary>Master property (typed by the MasterType directive).</summary>
        public new global::Yenetch.Web.SiteMaster Master
        {
            get { return ((global::Yenetch.Web.SiteMaster)(base.Master)); }
        }
''' if base == "Page" else ""
    return f'''//------------------------------------------------------------------------------
// <auto-generated>
//     Generated by tools/build.py. Changes to this file will be lost when the build runs again.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Yenetch.Web
{{
    public partial class {cls}
    {{{fields}{master}    }}
}}
'''


SIMPLE_CODEBEHIND = '''using System;
using System.Web.UI;

namespace Yenetch.Web
{{
    /// <summary>{summary}
    /// Title and description are set in the @Page directive; Site.Master data-binds the page.</summary>
    public partial class {cls} : Page
    {{
    }}
}}
'''


def build_aspx():
    bind.MODE = "aspx"
    written = []
    for cls, route, key, title, desc, fn in STATIC:
        body = fn()
        tok = lambda t: t.replace(C["phone"], "{phone}").replace(C["email"], "{email}")  # expanded from the database by Site.Master
        markup = PAGE_HEAD.format(title=esc(tok(title)), desc=esc(tok(desc)), cls=cls, body=body)
        (WEB / f"{cls}.aspx").write_text(markup, encoding="utf-8-sig")
        (WEB / f"{cls}.aspx.designer.cs").write_text(designer(cls, markup), encoding="utf-8-sig")
        cb = WEB / f"{cls}.aspx.cs"
        if cls not in ("Contact", "Blog", "NotFound"):
            cb.write_text(SIMPLE_CODEBEHIND.format(cls=cls, summary=f"{title.split(' | ')[0]} ({route})."), encoding="utf-8-sig")
        written.append(cls)
    for cls, items, route, fn, *_ in TEMPLATED:
        body = fn()
        markup = PAGE_HEAD.format(title="Yenetch", desc="", cls=cls, body=body).replace(' MetaDescription=""', "")
        (WEB / f"{cls}.aspx").write_text(markup, encoding="utf-8-sig")
        (WEB / f"{cls}.aspx.designer.cs").write_text(designer(cls, markup), encoding="utf-8-sig")
        written.append(cls)
    build_master()
    pg.write_runtime_files()
    (WEB / "Site.Master.designer.cs").write_text(designer("SiteMaster", "", base="MasterPage"), encoding="utf-8-sig")
    print("aspx:", ", ".join(written), "+ Site.Master")


def build_master():
    (WEB / "Site.Master").write_text(f'''<%@ Master Language="C#" AutoEventWireup="true" CodeBehind="Site.Master.cs" Inherits="Yenetch.Web.SiteMaster" %>
<!DOCTYPE html>
<html lang="en-IN">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
    <%-- <title> and <meta name="description"> are rendered by ASP.NET from Page.Title and Page.MetaDescription. --%>
    <asp:PlaceHolder runat="server">
    <link rel="canonical" href="<%: CanonicalUrl %>" />
    <meta property="og:type" content="website" />
    <meta property="og:site_name" content="Yenetch" />
    <meta property="og:title" content="<%: Page.Title %>" />
    <meta property="og:description" content="<%: Page.MetaDescription %>" />
    <meta property="og:url" content="<%: CanonicalUrl %>" />
    <meta property="og:image" content="<%: OgImage %>" />
    <meta name="twitter:card" content="summary_large_image" />
    </asp:PlaceHolder>
    <meta name="theme-color" content="#000000" />
    <link rel="icon" href="/assets/img/favicon.svg" type="image/svg+xml" />
    <link rel="icon" href="/assets/img/favicon-32.png" sizes="32x32" type="image/png" />
    <link rel="apple-touch-icon" href="/assets/img/apple-touch-icon.png" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link rel="stylesheet" href="{FONTS}" />
    <link rel="stylesheet" href="/assets/css/site.css?v={VER}" />
    <asp:PlaceHolder runat="server"><script type="application/ld+json"><%= Yenetch.Data.Site.OrganizationLd %></script></asp:PlaceHolder>
    <asp:ContentPlaceHolder ID="HeadContent" runat="server" />
    <asp:PlaceHolder runat="server"><%= Yenetch.Data.Tracking.Head(Request.Url.AbsolutePath) %></asp:PlaceHolder>
</head>
<body>
    <%= Yenetch.Data.Tracking.BodyStart(Request.Url.AbsolutePath) %>
    <a class="sr-only" href="#main">Skip to content</a>
    {ui.nav("")}
    <main id="main">
        <asp:ContentPlaceHolder ID="MainContent" runat="server" />
    </main>
    {ui.footer()}
    {dock.chrome()}
    <script>window.YENETCH_DATA = <%= Yenetch.Data.SiteContent.RawJson %>; window.YENBOT_ENDPOINT = "/api/lead"; window.YENETCH_PULSE = "/api/pulse";</script>
    <script src="/assets/js/site.js?v={VER}" defer></script>
    <script src="/assets/js/finder.js?v={VER}" defer></script>
    <script src="/assets/js/yenbot.js?v={VER}" defer></script>
    <script src="/assets/js/consent.js?v={VER}" defer></script>
    <script src="/assets/js/dock.js?v={VER}" defer></script>
    <script src="/assets/js/apply.js?v={VER}" defer></script>
    <script src="/assets/js/growth.js?v={VER}" defer></script>
    <asp:ContentPlaceHolder ID="Scripts" runat="server" />
    <%= Yenetch.Data.Tracking.BodyEnd(Request.Url.AbsolutePath) %>
</body>
</html>
''', encoding="utf-8")


if __name__ == "__main__":
    build_preview()
    if len(sys.argv) < 2 or sys.argv[1] != "preview":
        build_aspx()
