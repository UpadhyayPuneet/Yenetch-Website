"""Page templates. Each function returns the page body; static pages take no arguments,
templated pages (service, product, case study, article) take their bound objects."""
import json, re
from datetime import date
from . import bind
from .bind import P, X, esc, each, fmt, when, when_any, page
from .ui import *  # noqa: F403  (building blocks)
from . import ui


# =================================================================== home
def home():
    # Headline numbers follow the company stats in the admin (matched by label keyword); the labels here are page copy.
    stats = "".join(f'<div class="stat fx-up"><b data-count>{stat_value(k, v)}</b><span>{l}</span></div>' for k, v, l in
                    [("served", "100+", "clients across India and Europe since 2019"), ("satisfaction", "98%", "client satisfaction on project close"),
                     ("retention", "89%", "of clients stay with us year after year"), ("specialist", "30+", "in-house marketers, engineers and designers")])
    note = lambda p: when(p.priceNote, f'<small>{fmt(p.priceNote, lambda v: v.split(".")[0], lambda e: f"Yenetch.Data.Site.FirstSentence({e})")}</small>')
    pcards = each(prod_list(), lambda p, i: f'''<article class="pcard" id="{p.slug}"><div class="pcard__top"><span class="pcard__cat">{p.category}</span><span class="pill {p.statusCss}">{p.status}</span></div>
  <h3>{p.name}</h3><p>{p.summary}</p><p class="pcard__price">{p.price}{note(p)}</p>
  <a class="more" href="{href(p.url)}">Explore {p.shortName}</a>
  {product_switch(p.slug, p.photo, "vis-photo")}</article>''', "Yenetch.Models.Product")
    steps = each((D["process"], "Yenetch.Data.Site.Process"), lambda s, i: f'<li class="fx-up"><h3>{s.name}</h3><p>{s.text}</p></li>', "Yenetch.Models.ProcessStep")
    mtags = "".join(f"<li>{t}</li>" for t in ["Performance ads", "SEO", "Social", "Funnels", "Brand & ORM", "Influencers"])
    dtags = "".join(f"<li>{t}</li>" for t in ["Web apps", "Mobile apps", "CRM & ERP", "E-commerce", "AI", "Cloud"])
    squads = each((D["team"]["squads"], "Yenetch.Data.Site.Squads"), lambda s, i: f'<li><b>{s.name}</b><span>{joined(s.roles)}</span></li>', "Yenetch.Models.Squad")
    offices_n = count(len(C["offices"]), "Yenetch.Data.Site.Offices.Count")
    cities = fmt(P(" & ".join(o["city"] for o in C["offices"])) if bind.MODE == "preview" else X("Yenetch.Data.Site.OfficeCities"), lambda v: v, lambda e: e)
    return f'''
<section class="hero dark" id="top"><div class="wrap">
  <div>
    <p class="hero__eyebrow"><b>DPIIT-recognised</b> Digital marketing, software &amp; products since 2019</p>
    <h1>{h1_lines(["We think", '<span class="outside">outside</span> the box.', "You grow."])}</h1>
    <p class="hero__lead">Yenetch brings you customers, builds the software that serves them, and ships ready-to-use products you can launch this week. One accountable team.</p>
    <div class="actions"><a class="btn btn--blue" href="#finder">Find your solution</a><a class="btn btn--line" href="#work">See the results</a></div>
    <div class="hero__proof"><div><b class="num">{stat_value("served", "100+")}</b><span>clients</span></div><div><b class="num">{stat_value("satisfaction", "98%")}</b><span>satisfaction</span></div><div><b class="num">{stat_value("specialist", "30+")}</b><span>specialists</span></div><div><b>{offices_n}</b><span>offices, {cities}</span></div></div>
  </div>
  {cube_stage()}
</div></section>

{logos_marquee()}

<section class="sec"><div class="wrap">
  <p class="statement" data-words>Marketing that brings demand. Software that turns demand into revenue. Products that work from day one. Planned and measured by one team.</p>
</div></section>

<section class="sec" style="padding-top:0" id="practices"><div class="wrap">
  <div class="verticals">
    <a class="vcard vcard--dark fx-left" href="{L("/digital-marketing")}">
      <span class="kicker">Digital Marketing</span><h3>Marketing that reports to revenue.</h3>
      <p>Paid media, SEO, social and funnels, run by specialists and judged on qualified leads and sales.</p>
      <ul class="vcard__tags">{mtags}</ul><span class="more">Explore Digital Marketing</span>
      <div class="vcard__visual">{vis_dashboard()}</div></a>
    <a class="vcard vcard--light fx-right" href="{L("/software-development")}">
      <span class="kicker">Software Development</span><h3>Software engineered to run your business.</h3>
      <p>Websites, apps, CRMs and AI tools built for speed, security and the way your team actually works.</p>
      <ul class="vcard__tags">{dtags}</ul><span class="more">Explore Software Development</span>
      <div class="vcard__visual">{vis_dev()}</div></a>
  </div>
</div></section>

<section class="dark" id="products"><div class="rail"><div class="rail__sticky">
  <div class="wrap"><div class="head head--row" style="margin-bottom:40px"><div class="head"><span class="kicker">Products</span><h2>We build our own software, too.</h2>
    <p>Ready-to-use platforms from the same engineers who build custom systems for our clients.</p></div><a class="btn btn--line" href="{L("/products")}">All products</a></div></div>
  <div class="rail__track">{pcards}</div>
</div></div></section>

<section class="sec dark sec--photo"><div class="wrap">
  <div class="head fx-up"><span class="kicker">By the numbers</span><h2>Taken seriously since day one.</h2></div>
  <div class="stats">{stats}</div>
</div></section>

<section class="sec" id="work"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Case studies</span><h2>Named clients. Measured results.</h2>
    <p>Every story shows the problem, what we built and the number that moved. Where the work is public, we link to it.</p></div>
    <a class="btn btn--line" href="{L("/case-studies")}">All {count(len(D["caseStudies"]), "Yenetch.Data.Site.CaseStudies.Count")} case studies</a></div>
  {stack_static(["cabyaari-crm", "slp-quest-event-funnel", "south-asia-education", "brkesiya-exhibition"])}
</div></section>

{industries_section()}


{finder_section()}

<section class="sec" id="process"><div class="wrap">
  <div class="head fx-up"><span class="kicker">How we work</span><h2>A process you can see into.</h2><p>You always know what is happening, who owns it and what ships next.</p></div>
  <ol class="engine">{steps}</ol>
</div></section>

<section class="sec" id="talent"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Also from Yenetch</span><h2>Need people, not a project?</h2></div>
  <a class="vband fx-up" href="{L("/talent-resourcing")}">
    <div><span class="kicker">Talent &amp; Resourcing</span><h3>Vetted engineers and marketers in your team within days.</h3>
      <p>Staff augmentation, dedicated teams and managed marketing squads, backed by our bench and our delivery process.</p><span class="more">Explore Talent &amp; Resourcing</span></div>
    <div class="vband__visual">{vis_talent()}</div></a>
</div></section>

<section class="sec dark team-teaser" id="people"><div class="wrap">
  <div class="split split--wide">
    <div class="split__text fx-left"><span class="kicker">The team</span><h2>Thirty people who treat your business like their own.</h2>
      <p class="lead">Marketers, engineers and designers across Gurugram and Jaipur, organised in squads with a senior lead for every account.</p>
      <ul class="squad-list">{squads}</ul>
      <div class="actions"><a class="btn btn--white" href="{L("/about#team")}">Meet the team</a><a class="btn btn--line" href="{L("/careers")}">Careers</a></div></div>
    <div class="fx-right">{mosaic(["office-team", "culture-workshop", "whiteboard", "culture-celebrate"])}</div>
  </div>
</div></section>

{quotes_section()}
{_aspx_only('<asp:PlaceHolder runat="server"><%= Yenetch.Crm.Reviews.WidgetHtml() %></asp:PlaceHolder>')}
{posts_section()}
{cta("Let's build what's next.")}
'''


# =================================================================== verticals
def marketing():
    engine = [("Attract", "Be found by people already searching and seen by people who should be.", "SEO · Paid media · Social"),
              ("Capture", "Turn visits into enquiries with pages built for one action.", "Landing pages · Funnels · Lead magnets"),
              ("Nurture", "Follow up in seconds on WhatsApp, email and calls.", "Automation · Content · CRM routing"),
              ("Convert", "Give sales teams warm, qualified conversations.", "Yenetch Leads · Scoring · Retargeting"),
              ("Retain", "Keep customers coming back and talking about you.", "ORM · Community · Email")]
    eng = "".join(f'<li class="fx-up"><h3>{a}</h3><p>{b}</p><small>{c}</small></li>' for a, b, c in engine)
    channels = "".join(f"<span>{c}</span>" for c in ["Google Search", "YouTube", "Meta", "Instagram", "LinkedIn", "WhatsApp", "Google Business Profile", "Email", "Influencers", "Outdoor & print"])
    faqs = [("How much does digital marketing cost with Yenetch?", "Retainers are scoped to your goals and channels, and ad spend is billed separately at actuals. You get a written plan with fees and expected lead volumes before you commit."),
            ("How soon will we see results from ads and SEO?", "Paid campaigns usually produce leads within the first two weeks. SEO compounds over three to six months, starting with quick technical and local wins."),
            ("Do you guarantee leads or rankings?", "No honest agency can guarantee rankings. We commit to targets for cost per lead and lead quality, report weekly and change course quickly when numbers slip."),
            ("Will we own our ad accounts and data?", "Yes. Ad accounts, pixels, analytics and creative stay in your name. We work inside them with the access you grant."),
            ("Which industries do you work with?", "Education, healthcare and fitness, retail and D2C fashion, mobility, real estate, B2B software and public institutions, across India and Europe.")]
    return f'''
<section class="phero dark"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), ("Digital Marketing", None)])}
    <span class="kicker">Digital Marketing</span>
    <h1>{h1_lines(["Marketing that", 'reports to <span class="blue">revenue.</span>'])}</h1>
    <p class="phero__lead">We plan, run and optimise the campaigns that bring you qualified leads and sales, and we show you exactly what every rupee returned.</p>
    <div class="actions"><button class="btn btn--blue" type="button" data-chat="Get a free marketing audit">Get a free marketing audit</button><a class="btn btn--line" href="#results">See results</a></div></div>
  <div class="phero__visual fx-right">{vis_dashboard(dark=True)}</div>
</div></section>
{subnav("Digital Marketing", [("Services", "services"), ("Approach", "approach"), ("Results", "results"), ("FAQ", "faq")], "Free audit", "Get a free marketing audit")}

<section class="sec--tight"><div class="wrap"><div class="chips fx-up" aria-label="Channels we run">{channels}</div></div></section>

<section class="sec" style="padding-top:0"><div class="wrap split">
  <div class="split__text fx-left"><span class="kicker">Why Yenetch</span><h2>Specialists for each channel, one plan for all of them.</h2>
    <p class="lead">Most businesses hire an ads freelancer, an SEO agency and a social media team, and then wonder why the numbers don't add up. We run every channel from one plan and one dashboard, so budget moves to whatever is working this week.</p>
    <ul class="checks"><li>A named strategist and channel specialists on every account</li><li>Weekly reporting on leads, cost per lead and revenue</li><li>Landing pages and tracking built in-house, not outsourced</li></ul></div>
  <div class="fx-right">{mosaic(["mk-analytics", "mk-content", "mk-social", "mk-brand"], "mosaic mosaic--tight")}</div>
</div></section>

<section class="sec dark" id="approach"><div class="wrap">
  <div class="head fx-up"><span class="kicker">The growth engine</span><h2>Five stages. One system. No leaks.</h2><p>Most marketing fails between stages. We plan the whole journey, so an ad click becomes a customer, and a customer becomes a referral.</p></div>
  <ol class="engine">{eng}</ol>
</div></section>

<section class="sec" id="services"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Services</span><h2>Every channel, run by a specialist.</h2><p>Pick one service or let us combine them into a growth plan. Each has its own team, playbook and page.</p></div>
  {bento("marketing", dark_idx=(0,), blue_idx=(3,), wide_idx=(0, 1))}
</div></section>

<section class="sec paper"><div class="wrap split">
  <div class="split__text fx-left"><span class="kicker">Reporting</span><h2>You see what we see.</h2>
    <ul class="checks"><li>Live dashboard with leads, cost per lead and revenue by channel</li><li>Weekly summary in plain language, with next steps</li><li>Monthly strategy review with your marketing lead</li><li>Every lead tracked from first click to closed sale</li></ul></div>
  <div class="fx-right">{vis_dashboard()}</div>
</div></section>

<section class="sec" id="results"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Results</span><h2>Campaigns measured in outcomes.</h2></div>
  {stack_static(["slp-quest-event-funnel", "brkesiya-exhibition", "nuyu-health-club", "sahara-evols", "south-asia-education"], blue_idx=(0,))}
</div></section>

{finder_section()}
{faq_static(faqs)}
{cta("Let's fill your pipeline.", "Get a free audit of your ads, SEO and funnel, with three fixes you can act on this month.", "Get a free marketing audit")}
{breadcrumb_ld([("Home", "/"), ("Digital Marketing", "/digital-marketing")])}
'''


def development():
    build = [("Discover", "Workshops, user journeys and a fixed-scope estimate.", "Week 1"),
             ("Design", "UX flows, clickable prototypes and architecture.", "Weeks 2–3"),
             ("Build", "Two-week sprints with a working demo every time.", "Sprints"),
             ("Launch", "QA, security checks, go-live and team training.", "Go-live"),
             ("Evolve", "Monitoring, SLAs and a roadmap for what's next.", "Ongoing")]
    eng = "".join(f'<li class="fx-up"><h3>{a}</h3><p>{b}</p><small>{c}</small></li>' for a, b, c in build)
    stack_chips = "".join(f"<span>{t}</span>" for t in ["ASP.NET & .NET Core", "C#", "Node.js", "Python", "PHP", "React", "Next.js", "Angular", "Flutter", "React Native", "SQL Server", "MySQL", "MongoDB", "AWS", "Azure", "OpenAI & LLMs"])
    t = (lambda k: P(SVC.get(k, {}))) if bind.MODE == "preview" else (lambda k: X(f'Yenetch.Data.Site.Service("{k}")'))
    li = lambda k: each(top(t(k).includes, 4), lambda x, i: f"<li>{x}</li>")
    models = f'''<div class="models">
  <div class="model fx-up"><span class="kicker">Project</span><h3>Fixed scope</h3><p>A defined product with a fixed price and timeline.</p><ul><li>Written scope and milestones</li><li>Demo every two weeks</li><li>Free support window after launch</li></ul><a class="more" href="{L("/services/custom-software")}">Custom software</a></div>
  <div class="model model--pick fx-up"><span class="kicker">Most chosen</span><h3>Dedicated team</h3><p>{t("dedicated-teams").summary}</p><ul>{li("dedicated-teams")}</ul><a class="more" href="{L("/services/dedicated-teams")}">Dedicated teams</a></div>
  <div class="model fx-up"><span class="kicker">Resourcing</span><h3>Staff augmentation</h3><p>{t("staff-augmentation").summary}</p><ul>{li("staff-augmentation")}</ul><a class="more" href="{L("/services/staff-augmentation")}">Hire developers</a></div>
</div>'''
    faqs = [("How much does custom software or an app cost?", "It depends on features, integrations and platforms. After a free discovery call we send a fixed quote or a monthly team cost, whichever fits your project."),
            ("How long does it take to build?", "A business website takes 3 to 6 weeks, a web or mobile app 8 to 16 weeks, and an enterprise platform is delivered in phases, with something live early."),
            ("Who owns the source code?", "You do. Code, designs and data are transferred to you, and we sign an NDA before any discussion if you need one."),
            ("Can you work with our existing system or team?", "Yes. We modernise legacy applications, integrate with your tools, and our developers can join your team through staff augmentation."),
            ("Do you provide support after launch?", "Every project includes a free support window, and annual maintenance plans cover updates, security patches, monitoring and priority fixes.")]
    return f'''
<section class="phero dark"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), ("Software Development", None)])}
    <span class="kicker">Software Development</span>
    <h1>{h1_lines(["Software engineered", 'to run your <span class="blue">business.</span>'])}</h1>
    <p class="phero__lead">Websites, apps, CRMs and AI tools designed around how your team works, built to be fast and secure, and supported long after launch.</p>
    <div class="actions"><button class="btn btn--blue" type="button" data-chat="Get a project estimate">Get a project estimate</button><a class="btn btn--line" href="#engagement">Hire developers</a></div></div>
  <div class="phero__visual fx-right" style="padding-bottom:30px">{vis_dev()}</div>
</div></section>
{subnav("Software Development", [("Services", "services"), ("Process", "process"), ("Results", "results"), ("Engagement", "engagement"), ("FAQ", "faq")], "Get an estimate", "Get a project estimate")}

<section class="sec--tight"><div class="wrap"><div class="chips fx-up" aria-label="Technologies">{stack_chips}</div></div></section>

<section class="sec" id="services" style="padding-top:0"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Services</span><h2>From first screen to full platform.</h2><p>{count_word(len(svc_list("engineering")[0]), 'Yenetch.Data.Site.ServicesFor("engineering").Count')} engineering services, one delivery standard. Each has a dedicated page with scope, process and examples.</p></div>
  {bento("engineering", dark_idx=(1,), blue_idx=(4,), wide_idx=(0, 1))}
</div></section>

<section class="sec paper"><div class="wrap split">
  <div class="fx-left">{mosaic(["dev-code", "dev-ux", "dev-mobile", "dev-cloud"], "mosaic mosaic--tight")}</div>
  <div class="split__text fx-right"><span class="kicker">Engineering standards</span><h2>Built to last, not just to launch.</h2>
    <ul class="checks"><li>Code reviews on every change and automated tests on core flows</li><li>Security basics by default: OWASP checks, encrypted secrets, least-privilege access</li><li>Performance budgets, so pages stay fast on Indian mobile networks</li><li>Documentation and handover, so you are never locked in</li></ul></div>
</div></section>

<section class="sec dark" id="process"><div class="wrap">
  <div class="head fx-up"><span class="kicker">How we build</span><h2>Predictable delivery, sprint after sprint.</h2><p>A working demo every two weeks, so you see progress in software, not status reports.</p></div>
  <ol class="engine">{eng}</ol>
</div></section>

<section class="sec" id="results"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Results</span><h2>Systems that changed how clients operate.</h2></div>
  {stack_static(["cabyaari-crm", "south-asia-education", "jmch-website-cms"], blue_idx=(1,))}
</div></section>

<section class="sec paper" id="engagement"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Ways to work with us</span><h2>Build a project, or build your team.</h2><p>Choose a fixed-scope project, a dedicated team, or developers who join your team on contract.</p></div>
  {models}
</div></section>

{faq_static(faqs)}
{cta("Let's build it properly.", "Share your idea or problem. You'll get an approach, timeline and estimate within two working days.", "Get a project estimate")}
{breadcrumb_ld([("Home", "/"), ("Software Development", "/software-development")])}
'''


def talent():
    t = COPY.get("talent", {})
    src = lambda k: (t.get(k, []), f"Yenetch.Data.Site.Talent.{k[:1].upper() + k[1:]}")
    roles = each(src("roles"), lambda r, i: f'<li class="role fx-up"><h3>{r.name}</h3><ul>{each(r.skills, lambda x, j: f"<li>{x}</li>")}</ul></li>', "Yenetch.Models.RoleFamily")
    models = each(src("models"), lambda m, i: f'<article class="model{ix_cls(i, (1,), " model--pick")} fx-up"><span class="kicker">{m.timeline}</span><h3>{m.name}</h3><p>{m.text}</p><p class="model__best"><b>Best for</b> {m.bestFor}</p></article>', "Yenetch.Models.EngagementModel")
    steps = each(src("steps"), lambda x, i: f'<li class="fx-up"><h3>{x.name}</h3><p>{x.text}</p></li>', "Yenetch.Models.ProcessStep")
    guar = each(src("guarantees"), lambda g, i: f'<div class="gcard fx-up">{icon("check")}<h3>{g.title}</h3><p>{g.text}</p></div>', "Yenetch.Models.TitleText")
    intro = each(src("intro"), lambda x, i: f'<p class="lead">{x}</p>')
    if bind.MODE == "preview":
        faq_block = faq_bound(src("faqs"), faq_ld(t["faqs"]), "Questions we hear every week.") if t.get("faqs") else ""
    else:
        faq_block = when_any(X("Yenetch.Data.Site.Talent.Faqs"), faq_bound(src("faqs"), X("Yenetch.Data.Seo.FaqPage(Yenetch.Data.Site.Talent.Faqs)"), "Questions we hear every week."))
    return f'''
<section class="phero dark"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), ("Talent & Resourcing", None)])}
    <span class="kicker">Talent &amp; Resourcing</span>
    <h1>{h1_lines(["Your team,", 'ready in <span class="blue">days.</span>'])}</h1>
    <p class="phero__lead">Staff augmentation, dedicated teams and managed marketing squads. Vetted specialists join your team on contract, with our delivery process and bench behind them.</p>
    <div class="actions"><button class="btn btn--blue" type="button" data-chat="Hire developers">Request profiles</button><a class="btn btn--line" href="#models">Engagement models</a></div></div>
  <div class="phero__visual fx-right">{vis_talent()}</div>
</div></section>
{subnav("Talent & Resourcing", [("Roles", "roles"), ("Models", "models"), ("How it works", "how"), ("FAQ", "faq")], "Request profiles", "Hire developers")}

<section class="sec"><div class="wrap split">
  <div class="split__text fx-left"><span class="kicker">Why teams choose us</span><h2>Specialists who have shipped real client work.</h2>{intro}</div>
  <div class="fx-right">{mosaic(["talent-team", "remote-engineer", "hiring-interview", "culture-workshop"], "mosaic mosaic--tight")}</div>
</div></section>

<section class="sec paper" id="roles"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Roles we staff</span><h2>Engineering, design and marketing talent on demand.</h2></div>
  <ul class="roles">{roles}</ul>
</div></section>

<section class="sec" id="models"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Engagement models</span><h2>Choose how you want to work.</h2></div>
  <div class="models models--4">{models}</div>
</div></section>

<section class="sec dark" id="how"><div class="wrap">
  <div class="head fx-up"><span class="kicker">How it works</span><h2>From brief to productive in five steps.</h2></div>
  <ol class="engine">{steps}</ol>
</div></section>

<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Our commitments</span><h2>Low risk, by design.</h2></div>
  <div class="gcards">{guar}</div>
</div></section>

<section class="sec paper"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Services</span><h2>Talent services.</h2></div>
  {bento("talent", dark_idx=(0,), wide_idx=(0, 1))}
</div></section>

{faq_block}
{cta("Tell us who you need.", "Share the role, skills and start date. You'll get matched profiles within 48 hours.", "Hire developers")}
{breadcrumb_ld([("Home", "/"), ("Talent & Resourcing", "/talent-resourcing")])}
'''


# =================================================================== service detail (templated)
def service_detail(s=None):
    """s: the service dict in the preview. In aspx the code-behind exposes Svc, Copy, Pillar, Related, Cases, FaqLd, ServiceLd, CrumbLd."""
    if bind.MODE == "preview":
        svc, copy, pil = P(prep_service(s)), P(COPY["services"].get(s["slug"], {})), P(PILLAR[s["pillar"]])
        related = ([prep_service(x) for x in D["services"] if x["pillar"] == s["pillar"] and x["slug"] != s["slug"]], None)
        cases = [prep_case(c) for c in D["caseStudies"] if s["slug"] in c["services"]]
        cases_v = P(cases)
        faqld = faq_ld(COPY["services"].get(s["slug"], {}).get("faqs", []))
        ld = json.dumps({"@context": "https://schema.org", "@type": "Service", "name": s["name"], "description": s["summary"], "areaServed": "IN", "provider": {"@type": "Organization", "name": "Yenetch"}})
        crumb = breadcrumb_ld([("Home", "/"), (PILLAR[s["pillar"]]["name"], PILLAR[s["pillar"]]["url"]), (s["name"], "/services/" + s["slug"])])
    else:
        svc, copy, pil = X("Svc"), X("Copy"), X("Pillar")
        related = (None, "Related")
        cases_v = X("Cases")
        faqld, ld, crumb = X("FaqLd"), "<%= ServiceLd %>", '<script type="application/ld+json"><%= CrumbLd %></script>'
        cases = None
    ld_tag = f'<script type="application/ld+json">{ld}</script>'
    num = lambda i: fmt(i, lambda v: f"{v+1:02d}", lambda e: f'({e} + 1).ToString("00")')
    intro = each(copy.intro, lambda p, i: f"<p>{p}</p>")
    outcomes = each(copy.outcomes, lambda o, i: f'<div class="ocard fx-up"><b class="num">{num(i)}</b><h3>{o.title}</h3><p>{o.text}</p></div>', "Yenetch.Models.TitleText")
    deliver = each(copy.deliverables, lambda d, i: f'<div class="dcard">{icon("check")}<h3>{d.title}</h3><p>{d.text}</p></div>', "Yenetch.Models.TitleText")
    process = each(copy.process, lambda p, i: f'<li class="fx-up"><h3>{p.name}</h3><p>{p.text}</p></li>', "Yenetch.Models.ProcessStep")
    tools = each(copy.tools, lambda t, i: f"<span>{t}</span>")
    engage = each(copy.engagement, lambda m, i: f'<div class="model fx-up"><h3>{m.name}</h3><p>{m.text}</p></div>', "Yenetch.Models.ProcessStep")
    ideal = each(copy.idealFor, lambda x, i: f"<li>{x}</li>")
    includes = each(svc.includes, lambda x, i: f"<li>{x}</li>")
    rel = each(related, lambda r, i: f'<a class="rtile" href="{href(r.url)}"><h3>{r.name}</h3><p>{r.summary}</p><span class="more">Learn more</span></a>', "Yenetch.Models.Service")
    case_list = (each(cases_v, lambda c, i: case_card(c, i), "Yenetch.Models.CaseStudy"))
    cases_block = when_any(cases_v, f'''<section class="sec" id="results"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Results</span><h2>Proof from this service.</h2></div>
  <div class="stack">{case_list}</div></div></section>''')
    return f'''
<section class="phero dark phero--svc"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), (pil.name, pil.url), (svc.name, None)])}
    <span class="kicker">{svc.name}</span>
    <h1 class="h1-svc">{copy.headline}</h1>
    <p class="phero__lead">{svc.summary}</p>
    <div class="actions"><button class="btn btn--blue" type="button" data-chat="{svc.name}">Get a free proposal</button><a class="btn btn--line" href="#included">What's included</a></div></div>
  <div class="phero__visual fx-right">{visual_switch(svc.visual, svc.photo)}</div>
</div></section>
{subnav(str(svc.name), [("Overview", "overview"), ("What's included", "included"), ("Process", "process"), ("Pricing", "pricing"), ("FAQ", "faq")], "Get a proposal", str(svc.name))}

<section class="sec" id="overview"><div class="wrap split">
  <div class="split__text fx-left"><span class="kicker">Overview</span><h2>How we approach it.</h2><div class="prose-lite">{intro}</div>
    <ul class="checks">{includes}</ul></div>
  <figure class="split__media fx-zoom">{img(svc.photo, "", "(max-width: 900px) 100vw, 45vw")}</figure>
</div></section>

<section class="sec dark"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Outcomes</span><h2>What changes for your business.</h2></div>
  <div class="ocards">{outcomes}</div>
</div></section>

<section class="sec" id="included"><div class="wrap">
  <div class="head fx-up"><span class="kicker">What's included</span><h2>Handled end to end by specialists.</h2></div>
  <div class="dcards fx-stagger">{deliver}</div>
</div></section>

<section class="sec paper" id="process"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Process</span><h2>Four steps, no surprises.</h2></div>
  <ol class="engine engine--4">{process}</ol>
  <div class="tools fx-up"><span class="kicker">Tools and platforms</span><div class="chips chips--sm">{tools}</div></div>
</div></section>

<section class="sec" id="pricing"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Pricing and engagement</span><h2>Pay for outcomes you can plan around.</h2><p>Every engagement starts with a free consultation and a written proposal with scope, timeline and fees.</p></div>
  {service_plans(s["slug"] if s else None)}
  <div class="pricing">
    <div class="models models--auto">{engage}</div>
    <aside class="ideal fx-up"><h3>A good fit if you are</h3><ul class="checks">{ideal}</ul>
      <button class="btn btn--blue" type="button" data-chat="{svc.name}">Get a proposal</button></aside>
  </div>
</div></section>

{cases_block}

<section class="sec paper"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Related</span><h2>Often combined with.</h2></div><a class="btn btn--line" href="{href(pil.url)}">All {pil.name} services</a></div>
  <div class="rtiles">{rel}</div>
</div></section>

{faq_bound(copy.faqs, faqld)}
{cta("Let's talk about your goals.", "Get a free consultation and a written proposal within two working days.", str(svc.name))}
{ld_tag}{crumb}
'''


# =================================================================== products
def products_index():
    dark = lambda i: " pbig--dark" if isinstance(i, P) and i.v % 2 == 0 else "" if isinstance(i, P) else f'<%# {i.expr} % 2 == 0 ? " pbig--dark" : "" %>'
    cards = each(prod_list(), lambda p, i: f'''<a class="pbig{dark(i)} fx-up" href="{href(p.url)}">
  <div class="pbig__txt"><div class="pcard__top"><span class="pcard__cat">{p.category}</span><span class="pill {p.statusCss}">{p.status}</span></div>
    <h2>{p.name}</h2><p>{p.summary}</p><p class="pcard__price">{p.price}</p><span class="more">Explore {p.shortName}</span></div>
  <div class="pbig__vis">{product_switch(p.slug, p.photo, "vis-photo")}</div></a>''', "Yenetch.Models.Product")
    return f'''
<section class="phero"><div class="wrap">
  {crumbs([("Home", "/"), ("Products", None)])}
  <span class="kicker">Products</span>
  <h1>{h1_lines(["Software we built,", 'ready for your <span class="blue">business.</span>'])}</h1>
  <p class="phero__lead">Store, sales, billing, product photography and office tools, built by the engineers who build custom systems for our clients.</p>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap pbigs">{cards}</div></section>
{cta("Not sure which fits?", "Tell us how your business runs today and we'll recommend a product, a custom build or both.", "Which product fits me?")}
'''


def product_detail(p=None):
    if bind.MODE == "preview":
        prod, copy = P(prep_product(p)), P(COPY["products"].get(p["slug"], {}))
        others = ([prep_product(x) for x in D["products"] if x["slug"] != p["slug"]], None)
        faqld = faq_ld(COPY["products"].get(p["slug"], {}).get("faqs", []))
        ld = json.dumps({"@context": "https://schema.org", "@type": "SoftwareApplication", "name": p["name"], "description": p["summary"], "applicationCategory": "BusinessApplication", "operatingSystem": "Web"})
        crumb = breadcrumb_ld([("Home", "/"), ("Products", "/products"), (p["name"], "/products/" + p["slug"])])
    else:
        prod, copy = X("Prod"), X("Copy")
        others = (None, "Others")
        faqld, ld, crumb = X("FaqLd"), "<%= ProductLd %>", '<script type="application/ld+json"><%= CrumbLd %></script>'
    num = lambda i: fmt(i, lambda v: f"{v+1:02d}", lambda e: f'({e} + 1).ToString("00")')
    intro = each(copy.intro, lambda x, i: f"<p>{x}</p>")
    hl = each(copy.highlights, lambda h, i: f'<div class="dcard"><b class="num">{num(i)}</b><h3>{h.title}</h3><p>{h.text}</p></div>', "Yenetch.Models.TitleText")
    how = each(copy.howItWorks, lambda h, i: f'<li class="fx-up"><h3>{h.name}</h3><p>{h.text}</p></li>', "Yenetch.Models.ProcessStep")
    feats = each(prod.features, lambda f, i: f"<span>{f}</span>")
    who = each(copy.whoFor, lambda w, i: f"<li>{w}</li>")
    oth = each(others, lambda o, i: f'<a class="rtile" href="{href(o.url)}"><span class="pill {o.statusCss}">{o.status}</span><h3>{o.name}</h3><p>{o.summary}</p><span class="more">Explore</span></a>', "Yenetch.Models.Product")
    return f'''
<section class="phero phero--prod"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), ("Products", "/products"), (prod.name, None)])}
    <div class="phero__tags"><span class="kicker">{prod.category}</span><span class="pill {prod.statusCss}">{prod.status}</span></div>
    <h1 class="h1-svc">{prod.name}</h1>
    <p class="phero__sub">{copy.headline}</p>
    <p class="phero__lead">{prod.summary}</p>
    <p class="phero__price"><b>{prod.price}</b> {prod.priceNote}</p>
    <div class="actions"><button class="btn btn--blue" type="button" data-chat="{prod.name}">Book a demo</button><a class="btn btn--line" href="#features">See features</a></div></div>
  <div class="phero__visual fx-right">{product_switch(prod.slug, prod.photo)}</div>
</div></section>
{subnav(str(prod.name), [("Overview", "overview"), ("Features", "features"), ("How it works", "how"), ("FAQ", "faq")], "Book a demo", str(prod.name))}

<section class="sec" id="overview"><div class="wrap split">
  <div class="split__text fx-left"><span class="kicker">Overview</span><h2>Built for how Indian businesses run.</h2><div class="prose-lite">{intro}</div>
    <h3 class="split__sub">Made for</h3><ul class="checks">{who}</ul></div>
  <figure class="split__media fx-zoom">{img(prod.photo, "", "(max-width: 900px) 100vw, 45vw")}</figure>
</div></section>

<section class="sec paper" id="features"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Highlights</span><h2>Everything you need, nothing you don't.</h2></div>
  <div class="dcards fx-stagger">{hl}</div>
  <div class="tools fx-up"><span class="kicker">Included</span><div class="chips chips--sm">{feats}</div></div>
</div></section>

<section class="sec dark" id="how"><div class="wrap">
  <div class="head fx-up"><span class="kicker">How it works</span><h2>Up and running without the IT project.</h2></div>
  <ol class="engine engine--3">{how}</ol>
</div></section>

<section class="sec"><div class="wrap"><div class="pricecard fx-scale">
  <div><span class="kicker">Pricing</span><h2>{prod.price}</h2><p>{prod.priceNote}</p></div>
  <div class="actions"><button class="btn btn--blue" type="button" data-chat="{prod.name}">Book a demo</button><a class="btn btn--line" href="{L("/contact")}">Talk to sales</a></div>
</div></div></section>

{faq_bound(copy.faqs, faqld)}

<section class="sec paper"><div class="wrap">
  <div class="head fx-up"><span class="kicker">More from Yenetch</span><h2>Other products.</h2></div>
  <div class="rtiles">{oth}</div>
</div></section>
{cta("See it on your own data.", "Book a 20-minute demo and we'll set it up around one real workflow from your business.", str(prod.name))}
<script type="application/ld+json">{ld}</script>{crumb}
'''


# =================================================================== services index
def services_index():
    # The three practices (pillars) are the site's structure; their names, text and services come from the data.
    blocks = ""
    for i, pid in enumerate([x["id"] for x in D["pillars"]]):
        p = P(PILLAR[pid]) if bind.MODE == "preview" else X(f'Yenetch.Data.Site.Pillar("{pid}")')
        blocks += f'''<section class="sec{" paper" if i == 1 else ""}" id="{pid}"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">{p.short}</span><h2>{p.name}</h2><p>{p.summary}</p></div><a class="btn btn--line" href="{href(p.url)}">Explore {p.name}</a></div>
  {bento(pid, dark_idx=(0,), blue_idx=((3,) if pid == "marketing" else (4,) if pid == "engineering" else ()), wide_idx=(0, 1))}
</div></section>'''
    prods = each(prod_list(), lambda p, i: f'''<a class="rtile" href="{href(p.url)}"><span class="pill {p.statusCss}">{p.status}</span><h3>{p.name}</h3><p>{p.summary}</p><span class="more">Explore</span></a>''', "Yenetch.Models.Product")
    prod_block = f'''<section class="sec dark" id="products"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Products</span><h2>Ready-to-use software</h2><p>Platforms from the same engineers who build our custom systems, live in days instead of months.</p></div><a class="btn btn--line" href="{L("/products")}">All products</a></div>
  <div class="rtiles rtiles--3">{prods}</div>
</div></section>'''
    j = blocks.rfind("<section", 0, blocks.find('id="talent"'))
    blocks = blocks[:j] + prod_block + blocks[j:]
    return f'''
<section class="phero dark"><div class="wrap">
  {crumbs([("Home", "/"), ("Services", None)])}
  <span class="kicker">Services</span>
  <h1>{h1_lines([count_word(len(D["services"]), "Yenetch.Data.Site.Services.Count") + " services.", 'One <span class="blue">accountable</span> team.'])}</h1>
  <p class="phero__lead">Marketing, development and ready-to-use products that plug into each other, with talent on hand when you need people. One team, one result.</p>
  <div class="actions"><a class="btn btn--blue" href="{L("/solution-finder")}">Find the right service</a></div>
</div></section>
{blocks}
{cta()}
'''


# =================================================================== work
def cases_index():
    inds = (sorted({c["industry"] for c in D["caseStudies"]}), "Yenetch.Data.Site.CaseIndustries")
    chips = '<button class="chip is-on" type="button" data-filter="*">All work</button>' + each(inds, lambda x, i: f'<button class="chip" type="button" data-filter="{x}">{x}</button>')
    tiles = each(([prep_case(c) for c in D["caseStudies"]], "SiteContent.Current.CaseStudies"), lambda c, i: case_tile(c), "Yenetch.Models.CaseStudy")
    return f'''
<section class="phero"><div class="wrap">
  {crumbs([("Home", "/"), ("Case studies", None)])}
  <span class="kicker">Case studies</span>
  <h1>{h1_lines(["Named clients.", 'Measured <span class="blue">results.</span>'])}</h1>
  <p class="phero__lead">Hospitals, universities, retailers, startups and public institutions. Every story shows the problem, what we did and the number that moved.</p>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap">
  <div class="chips-filter" role="toolbar" aria-label="Filter by industry">{chips}</div>
  <div class="ctiles" data-filter-grid>{tiles}</div>
</div></section>
{logos_marquee("Also trusted by")}
{quotes_section()}
{cta("Your story could be next.", "Tell us the number you want to move. We'll tell you honestly how we'd move it.")}
'''


def case_detail(c=None):
    if bind.MODE == "preview":
        case = P(prep_case(c))
        svcs = ([prep_service(SVC[s]) for s in c["services"] if s in SVC], None)
        more = ([prep_case(x) for x in D["caseStudies"] if x["slug"] != c["slug"]][:3], None)
        crumb = breadcrumb_ld([("Home", "/"), ("Case studies", "/case-studies"), (c["client"], "/case-studies/" + c["slug"])])
    else:
        case = X("Case")
        svcs, more = (None, "SiteContent.ServiceList(Case.Services)"), (None, "More")
        crumb = '<script type="application/ld+json"><%= CrumbLd %></script>'
    nums = each(case.metrics, lambda m, i: f'<div class="fx-up"><b class="num" data-count>{m.value}</b><span>{m.label}</span></div>', "Yenetch.Models.Metric")
    sv = each(svcs, lambda s, i: f'<a href="{href(s.url)}">{s.name}</a>', "Yenetch.Models.Service")
    quote = when(case.hasQuote, f'''<section class="sec"><div class="wrap"><figure class="bigquote fx-up"><blockquote>“{case.quote.text}”</blockquote><figcaption><b>{case.quote.name}</b> {case.quote.role}</figcaption></figure></div></section>''', cs=lambda e: e)
    visit = when(case.url, f'<a class="btn btn--line" href="{case.url}" target="_blank" rel="noopener">Visit {case.urlLabel}</a>')
    moreh = each(more, lambda m, i: case_tile(m), "Yenetch.Models.CaseStudy")
    return f'''
<section class="phero dark"><div class="wrap phero__grid phero__grid--case">
  <div>{crumbs([("Home", "/"), ("Case studies", "/case-studies"), (case.client, None)])}
    <div class="phero__tags"><span class="tag">{case.client}</span><span class="tag">{case.industry}</span></div>
    <h1 class="h1-svc">{case.title}</h1>
    <div class="actions">{visit}<button class="btn btn--blue" type="button" data-chat="Talk to an expert">Get results like this</button></div></div>
  <figure class="phero__photo fx-right">{img(case.photo, "", "(max-width: 960px) 100vw, 45vw", eager=True)}</figure>
</div></section>
<section class="metrics-band"><div class="wrap metrics">{nums}</div></section>

<section class="sec"><div class="wrap story">
  <div class="story__row fx-up"><h2>The challenge</h2><p>{case.challenge}</p></div>
  <div class="story__row fx-up"><h2>What we did</h2><p>{case.solution}</p></div>
  <div class="story__row fx-up"><h2>Services</h2><div class="story__links">{sv}</div></div>
</div></section>
{quote}
<section class="sec paper"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">More work</span><h2>More results.</h2></div><a class="btn btn--line" href="{L("/case-studies")}">All case studies</a></div>
  <div class="ctiles">{moreh}</div>
</div></section>
{cta("Let's write your success story.", "Tell us the number you want to move and we'll show you how.")}
{crumb}
'''


# =================================================================== company
def about():
    stats = each((C["stats"][:4], "System.Linq.Enumerable.Take(Yenetch.Data.Site.Stats, 4)"), lambda s, i: f'<div class="stat fx-up"><b data-count>{s.value}</b><span>{s.label}</span></div>', "Yenetch.Models.Stat")
    values = values_list()
    offices = each((C["offices"], "Yenetch.Data.Site.Offices"), lambda o, i: f'''<article class="office fx-up"><figure>{img(o.photo, "", "(max-width: 800px) 100vw, 50vw")}</figure>
  <div><span class="kicker">{o.label}</span><h3>{o.city}</h3><p>{o.address}</p><a class="more" href="{o.map}" target="_blank" rel="noopener">Get directions</a></div></article>''', "Yenetch.Models.Office")
    return f'''
<section class="phero dark phero--about"><div class="wrap">
  {crumbs([("Home", "/"), ("About", None)])}
  <span class="kicker">About Yenetch</span>
  <h1>{h1_lines(["Your desires,", 'our <span class="blue">innovation.</span>'])}</h1>
  <p class="phero__lead">Since 2019 we have helped more than a hundred businesses, hospitals, universities and institutions grow with marketing, software and people who care about the result.</p>
</div>
{photo_rows(["office-team", "culture-workshop", "whiteboard", "culture-celebrate", "office-meeting", "dev-ux"], ["talent-team", "culture-casual", "mk-content", "hiring-interview", "office-workspace", "remote-engineer"])}
</section>

<section class="sec"><div class="wrap split">
  <div class="split__text fx-left"><span class="kicker">Our story</span><h2>Built by practitioners, for businesses that need results.</h2>
    <p class="lead">Yenetch started in 2019 with a simple belief: marketing and software should be planned together. A campaign is only as good as the website it lands on, and software only pays off when people use it.</p>
    <p class="lead">Today more than thirty marketers, engineers and designers work from Gurugram and Jaipur for clients across India and Europe, from the High Court of Delhi and university hospitals to fashion labels and mobility startups. We are recognised by DPIIT under Startup India.</p></div>
  <div class="fx-right"><div class="mv">
    <div class="mv__card"><span class="kicker">Mission</span><p>Give every business access to the marketing, software and talent that used to be reserved for large companies.</p></div>
    <div class="mv__card mv__card--dark"><span class="kicker">Vision</span><p>Be the most trusted growth and technology partner for ambitious Indian businesses.</p></div></div></div>
</div></section>

<section class="sec dark"><div class="wrap"><div class="stats">{stats}</div></div></section>

<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">What we value</span><h2>Four promises we keep on every project.</h2></div>
  <div class="values values--light">{values}</div>
</div></section>

{team_section()}

<section class="sec paper" id="offices"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Offices</span><h2>Two cities, one team.</h2><p>Strategy and client services in Gurugram, delivery and engineering in Jaipur. Visits by appointment.</p></div>
  <div class="offices">{offices}</div>
</div></section>

<section class="sec"><div class="wrap follow fx-up">
  <div><span class="kicker">Follow Yenetch</span><h2>Ideas, launches and life at work.</h2></div>
  {social_links("social social--lg")}
</div></section>
{cta("Work with a team that cares.", "Start with a free consultation. No pitch decks, just a clear plan.")}
'''


def values_list():
    """Company values, numbered 01, 02, ..."""
    return each((C["values"], "Yenetch.Data.Site.Values"), lambda v, i: f'<div class="value fx-up"><b>{fmt(i, lambda n: f"0{n+1}", lambda e: f"{chr(34)}0{chr(34)} + ({e} + 1)")}</b><h3>{v.name}</h3><p>{v.text}</p></div>', "Yenetch.Models.CompanyValue")


def careers():
    cr = D["careers"]
    perks = each((cr["perks"], "Yenetch.Data.Site.Perks"), lambda p, i: f'<div class="gcard fx-up">{icon("check")}<h3>{p.title}</h3><p>{p.text}</p></div>', "Yenetch.Models.TitleText")
    apply = lambda r: fmt(r.title, lambda t: f'mailto:{C["email"]}?subject=Application: {t}', lambda e: f'Yenetch.Data.Site.MailTo("Application: " + {e})')
    roles = each((cr["roles"], "Yenetch.Data.Site.Jobs"), lambda r, i: f'''<li class="job"><div><h3>{r.title}</h3><p>{r.team} · {r.location} · {r.type}</p></div><a class="btn btn--line btn--sm" href="{apply(r)}" data-apply="{r.title}" data-team="{r.team}">Apply</a></li>''', "Yenetch.Models.Job")
    steps = each((cr["steps"], "Yenetch.Data.Site.HiringSteps"), lambda s, i: f'<li class="fx-up"><h3>{s.name}</h3><p>{s.text}</p></li>', "Yenetch.Models.ProcessStep")
    values = values_list()
    return f'''
<section class="phero dark"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), ("Careers", None)])}
    <span class="kicker">Careers</span>
    <h1>{h1_lines(["Do the best work", 'of your <span class="blue">career.</span>'])}</h1>
    <p class="phero__lead">Join marketers, engineers and designers in Gurugram and Jaipur who ship real work for real clients, and learn from each other every week.</p>
    <div class="actions"><a class="btn btn--blue" href="#roles">See open roles</a></div></div>
  <div class="fx-right">{mosaic(["culture-celebrate", "culture-workshop", "culture-casual", "whiteboard"], "mosaic mosaic--tight")}</div>
</div></section>

<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Why Yenetch</span><h2>A place to grow fast.</h2></div>
  <div class="gcards">{perks}</div>
</div></section>

<section class="sec dark"><div class="wrap">
  <div class="head fx-up"><span class="kicker">How we work</span><h2>What we expect from each other.</h2></div>
  <div class="values">{values}</div>
</div></section>

<section class="sec" id="roles"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Open roles</span><h2>Current openings.</h2></div><a class="btn btn--line" href="{mail_href("General application") if bind.MODE == "aspx" else "mailto:" + C["email"] + "?subject=General application"}" data-apply="">Send a general application</a></div>
  <ul class="jobs">{roles}</ul>
</div></section>

<section class="sec paper"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Hiring process</span><h2>Four steps, answered within a week.</h2></div>
  <ol class="engine engine--4">{steps}</ol>
</div></section>
{cta("Don't see your role?", "We are always glad to meet good people. Send your CV or portfolio with a note on what you'd love to work on.")}
{_aspx_only('<asp:PlaceHolder runat="server"><%= Yenetch.Data.Seo.JobPostingsTag() %></asp:PlaceHolder>')}
'''


def contact():
    offices = each((C["offices"], "Yenetch.Data.Site.Offices"), lambda o, i: f'''<article class="office office--sm"><figure>{img(o.photo, "", "(max-width: 800px) 100vw, 30vw")}</figure><div><span class="kicker">{o.label}</span><h3>{o.city}</h3><p>{o.address}</p><a class="more" href="{o.map}" target="_blank" rel="noopener">Get directions</a></div></article>''', "Yenetch.Models.Office")
    options = [s["name"] for s in D["services"]] + [p["name"] for p in D["products"]] + ["Something else"]
    accept = ".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.jpg,.jpeg,.png,.webp,.txt"
    hint = '<small class="cform__hint" id="AttachmentHint">PDF, Word, Excel, PowerPoint or image, up to 5 MB</small>'
    if bind.MODE == "aspx":
        # Topics (every service and product) bind on the first request only, so a postback keeps the visitor's choice.
        # /contact?service={slug} pre-selects that service.
        topics = (" DataSource='<%# IsPostBack ? null : Yenetch.Data.Site.ContactTopics %>'"
                  " SelectedValue='<%# IsPostBack ? Interest.SelectedValue : Yenetch.Data.Site.ContactTopic(Request.QueryString[\"service\"]) %>'")
        form = f'''<form id="ContactForm" runat="server" class="cform">
    <asp:PlaceHolder ID="FormFields" runat="server">
      <div class="cform__row"><label for="Name">Your name</label><asp:TextBox ID="Name" runat="server" CssClass="field" MaxLength="120" autocomplete="name" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="Name" ErrorMessage="Please enter your name." Display="Dynamic" CssClass="err" /></div>
      <div class="cform__row"><label for="ContactInfo">Phone or email</label><asp:TextBox ID="ContactInfo" runat="server" CssClass="field" MaxLength="160" autocomplete="email" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="ContactInfo" ErrorMessage="Please enter a phone number or email." Display="Dynamic" CssClass="err" /></div>
      <div class="cform__row"><label for="Interest">I'm interested in</label><asp:DropDownList ID="Interest" runat="server" CssClass="field"{topics} /></div>
      <div class="cform__row"><label for="Need">What do you want to achieve?</label><asp:TextBox ID="Need" runat="server" CssClass="field" TextMode="MultiLine" Rows="5" MaxLength="1000" /></div>
      <div class="cform__row"><label for="Attachment">Attach a brief (optional)</label><asp:FileUpload ID="Attachment" runat="server" CssClass="field" accept="{accept}" aria-describedby="AttachmentHint" />{hint}</div>
      <input type="text" name="website" tabindex="-1" autocomplete="off" aria-hidden="true" style="position:absolute;left:-9999px">
      <div data-captcha></div>
      <asp:Literal ID="FormError" runat="server" Visible="false" />
      <asp:Button ID="SendButton" runat="server" Text="Send enquiry" CssClass="btn btn--blue" OnClick="SendButton_Click" />
      <p class="cform__note">We reply within one working day. Your details are used only to respond to you. See our <a href="/privacy">privacy policy</a>.</p>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="ThankYou" runat="server" Visible="false"><div class="cform__done">{icon("check")}<h2>Thank you.</h2><p>A specialist will reach you within one working day.</p></div></asp:PlaceHolder>
  </form>'''
    else:
        opts = "".join(f"<option>{esc(o)}</option>" for o in options)
        form = f'''<form class="cform" data-lead-form>
      <div class="cform__row"><label for="Name">Your name</label><input id="Name" name="name" class="field" required maxlength="120" autocomplete="name"></div>
      <div class="cform__row"><label for="ContactInfo">Phone or email</label><input id="ContactInfo" name="contact" class="field" required maxlength="160" autocomplete="email"></div>
      <div class="cform__row"><label for="Interest">I'm interested in</label><select id="Interest" name="topic" class="field">{opts}</select></div>
      <div class="cform__row"><label for="Need">What do you want to achieve?</label><textarea id="Need" name="need" class="field" rows="5" maxlength="1000"></textarea></div>
      <div class="cform__row"><label for="Attachment">Attach a brief (optional)</label><input type="file" id="Attachment" name="attachment" class="field" accept="{accept}" aria-describedby="AttachmentHint">{hint}</div>
      <button class="btn btn--blue" type="submit">Send enquiry</button>
      <p class="cform__note">We reply within one working day. Your details are used only to respond to you. See our <a href="{L("/privacy")}">privacy policy</a>.</p>
  </form>'''
    return f'''
<section class="phero"><div class="wrap">
  {crumbs([("Home", "/"), ("Contact", None)])}
  <span class="kicker">Contact</span>
  <h1>{h1_lines(["Let's talk about", 'your <span class="blue">goals.</span>'])}</h1>
  <p class="phero__lead">Share a few details and a specialist will reply within one working day. Prefer to talk now? We're a call or a WhatsApp away.</p>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap contact">
  <div class="contact__form">{form}</div>
  <aside class="contact__side">
    <a class="ccard" href="{tel_href()}">{icon("phone")}<span><small>Call</small><b>{co("phone")}</b></span></a>
    <a class="ccard" href="{wa_href()}" target="_blank" rel="noopener">{social_icon("whatsapp")}<span><small>WhatsApp</small><b>Chat with us</b></span></a>
    <a class="ccard" href="{mail_href()}">{icon("mail")}<span><small>Email</small><b>{co("email")}</b></span></a>
    <a class="ccard" href="{L("/book")}">{icon("calendar")}<span><small>Free 30-minute call</small><b>Book a time</b></span></a>
    <button class="ccard" type="button" data-chat="Talk to an expert">{icon("chat")}<span><small>Instant answers</small><b>Ask our assistant</b></span></button>
    <div class="ccard ccard--social"><small>Follow us</small>{social_links()}</div>
  </aside>
</div></section>
<section class="sec paper"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Offices</span><h2>Visit us.</h2></div>
  <div class="offices">{offices}</div>
</div></section>
{_aspx_only('<asp:PlaceHolder runat="server"><%= Yenetch.Crm.Reviews.WidgetHtml() %></asp:PlaceHolder><asp:PlaceHolder runat="server"><script type="application/ld+json"><%= Yenetch.Data.Seo.LocalBusinesses() %></script></asp:PlaceHolder>')}
'''


def finder_page():
    return f'''
<section class="phero dark"><div class="wrap">
  {crumbs([("Home", "/"), ("Solution finder", None)])}
  <span class="kicker">Solution finder</span>
  <h1>{h1_lines(["The right service,", 'in four <span class="blue">questions.</span>'])}</h1>
  <p class="phero__lead">Answer four questions about your goal, stage and industry. You'll get the services and products that fit, and a proof point from a client like you.</p>
</div></section>
{finder_section("h2")}
<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Or browse</span><h2>Three practices.</h2></div>
  <div class="rtiles rtiles--3">{each((D["pillars"], "Yenetch.Data.Site.Pillars"), lambda p, i: f'<a class="rtile rtile--photo" href="{href(p.url)}"><figure>{img(p.photo, "", "(max-width: 900px) 100vw, 33vw")}</figure><h3>{p.name}</h3><p>{p.summary}</p><span class="more">Explore</span></a>', "Yenetch.Models.Pillar")}</div>
</div></section>
{cta()}
'''


# =================================================================== blog
def blog_index():
    posts = [prep_post(b) for b in sorted(D["blog"], key=lambda b: b["date"], reverse=True)]
    cats = sorted({b["category"] for b in D["blog"]})
    if bind.MODE == "preview":
        chips = '<a class="chip is-on" href="blog.html">All</a>' + "".join(f'<a class="chip" href="blog.html#{esc(c)}" data-cat="{esc(c)}">{esc(c)}</a>' for c in cats)
        featured, rest, pager = P(posts[0]), (posts[1:], None), ""
    else:
        chips = '<a class="chip<%= string.IsNullOrEmpty(Category) ? " is-on" : "" %>" href="/blog">All</a>' + \
            each(X("Categories"), lambda c, i: f'<a class="chip" href="/blog/category/<%#: Yenetch.Models.BlogPost.Slugify(Item) %>"><%#: Item %></a>')
        featured, rest, pager = X("Featured"), (None, "Posts"), "<%= PagerHtml %>"
    feat = f'''<a class="feature fx-up" href="{href(featured.url)}"><figure>{img(featured.coverImage, "", "(max-width: 900px) 100vw, 60vw", eager=True)}</figure>
  <div class="feature__body"><div class="post__meta"><b>{featured.category}</b><span>{featured.readMinutes} min read</span></div><h2>{featured.title}</h2><p>{featured.excerpt}</p><time datetime="{featured.date}">{featured.displayDate}</time><span class="more">Read article</span></div></a>'''
    if bind.MODE == "aspx":
        feat = f"<asp:PlaceHolder runat=\"server\" Visible='<%# Featured != null %>'>{feat}</asp:PlaceHolder>"
    grid = each(rest, lambda b, i: post_card(b), "Yenetch.Models.BlogPost")
    return f'''
<section class="phero"><div class="wrap">
  {crumbs([("Home", "/"), ("Insights", None)])}
  <span class="kicker">Insights</span>
  <h1>{h1_lines(["Field notes from", 'people doing the <span class="blue">work.</span>'])}</h1>
  <p class="phero__lead">Practical guides on marketing, software, e-commerce and hiring, written by the specialists who run these projects every day.</p>
  <div class="chips-filter" aria-label="Categories">{chips}</div>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap">
  {feat}
  <div class="posts posts--grid fx-stagger">{grid}</div>
  {pager}
</div></section>
{newsletter()}
'''


def newsletter():
    return f'''<section class="sec--tight"><div class="wrap"><div class="news fx-up">
  <div><span class="kicker">Newsletter</span><h2>One useful note a month.</h2><p>Marketing and software ideas you can use, never spam. Unsubscribe any time.</p></div>
  <form class="news__form" data-newsletter><label class="sr-only" for="news-email">Email</label><input id="news-email" class="field" type="email" required placeholder="you@company.com" autocomplete="email"><button class="btn btn--blue" type="submit">Subscribe</button></form>
</div></div></section>'''


def _toc_from_html(body):
    return [{"id": m.group(1), "text": re.sub("<[^>]+>", "", m.group(2))} for m in re.finditer(r'<h2[^>]*\sid="([^"]+)"[^>]*>(.*?)</h2>', body, re.S)]


def _faq_from_html(body):
    i = body.find("Frequently asked questions")
    if i < 0:
        return []
    return [{"q": re.sub("<[^>]+>", "", q).strip(), "a": re.sub("<[^>]+>", "", a).strip()} for q, a in re.findall(r"<h3[^>]*>(.*?)</h3>\s*<p[^>]*>(.*?)</p>", body[i:], re.S)]


def blog_article(b=None):
    if bind.MODE == "preview":
        body_html = (WEB / f"assets/data/blog/{b['slug']}.html").read_text(encoding="utf-8")
        post = P(prep_post(b))
        toc = (_toc_from_html(body_html), None)
        body = re.sub(r'href="(/[^"#]*)(#[^"]*)?"', lambda m: f'href="{L(m.group(1) + (m.group(2) or ""))}"', body_html)
        related = ([prep_post(x) for x in sorted(D["blog"], key=lambda x: (x["category"] != b["category"], x["date"]), reverse=False) if x["slug"] != b["slug"]][:3], None)
        url = f"https://www.yenetch.com/blog/{b['slug']}"
        from urllib.parse import quote
        share_u, share_t = quote(url, safe=""), quote(b["title"], safe="")
        ld = json.dumps({"@context": "https://schema.org", "@type": "BlogPosting", "headline": b["title"], "description": b["excerpt"], "datePublished": b["date"], "author": {"@type": "Organization", "name": "Yenetch"}, "publisher": {"@type": "Organization", "name": "Yenetch"}}, ensure_ascii=False)
        fl = _faq_from_html(body_html)
        faqld = faq_ld(fl) if fl else "{}"
        crumb = breadcrumb_ld([("Home", "/"), ("Insights", "/blog"), (b["title"], "/blog/" + b["slug"])])
    else:
        post = X("Post")
        toc, body, related = (None, "Toc"), "<%= Post.BodyHtml %>", (None, "Related")
        share_u, share_t = "<%: ShareUrl %>", "<%: ShareText %>"
        ld, faqld = "<%= ArticleLd %>", "<%= FaqLd %>"
        crumb = '<script type="application/ld+json"><%= CrumbLd %></script>'
    toc_items = each(toc, lambda t, i: f'<li><a href="#{t.id}">{t.text}</a></li>', "Yenetch.Models.TocItem")
    if bind.MODE == "aspx":
        m = '@"' + mark().replace('"', '""') + '"'
        byline = f"<%= Yenetch.Data.Authors.Byline(Post.Author, {m}) %>"
        author_box = f"<%= Yenetch.Data.Authors.Box(Post.Author, {m}) %>"
    else:
        byline = f'<span class="byline__av">{mark()}</span><span><b>{post.author}</b>'
        author_box = f'<div class="author"><span class="byline__av">{mark()}</span><div><b>Written by the Yenetch team</b><p>Our articles are written and reviewed by the marketers, engineers and designers who run these projects for clients every day.</p></div></div>'
    share = f'''<div class="share" aria-label="Share this article">
  <a href="https://www.linkedin.com/sharing/share-offsite/?url={share_u}" target="_blank" rel="noopener" aria-label="Share on LinkedIn">{social_icon("linkedin")}</a>
  <a href="https://x.com/intent/post?url={share_u}&amp;text={share_t}" target="_blank" rel="noopener" aria-label="Share on X">{social_icon("x")}</a>
  <a href="https://www.facebook.com/sharer/sharer.php?u={share_u}" target="_blank" rel="noopener" aria-label="Share on Facebook">{social_icon("facebook")}</a>
  <a href="https://wa.me/?text={share_t}%20{share_u}" target="_blank" rel="noopener" aria-label="Share on WhatsApp">{social_icon("whatsapp")}</a>
  <button type="button" data-copy-link aria-label="Copy link">{icon("link")}</button></div>'''
    rel = each(related, lambda r, i: post_card(r), "Yenetch.Models.BlogPost")
    return f'''
<article class="article">
<header class="article__head"><div class="wrap wrap--text">
  {crumbs([("Home", "/"), ("Insights", "/blog"), (post.category, None)])}
  <div class="post__meta"><b>{post.category}</b><span>{post.readMinutes} min read</span></div>
  <h1>{post.title}</h1>
  <p class="article__lead">{post.excerpt}</p>
  <div class="article__byline">{byline}<time datetime="{post.date}">{post.displayDate}</time></span>{share}</div>
</div></header>
<figure class="article__cover"><div class="wrap">{img(post.coverImage, "", "(max-width: 1100px) 100vw, 1100px", eager=True)}</div></figure>
<div class="wrap article__grid">
  <aside class="toc"><nav aria-label="In this article"><b>In this article</b><ol>{toc_items}</ol></nav></aside>
  <div class="prose">{body}
    {author_box}
    <div class="article__share"><span>Share this article</span>{share}</div>
  </div>
</div>
</article>
<section class="sec paper"><div class="wrap">
  <div class="head head--row fx-up"><div class="head"><span class="kicker">Keep reading</span><h2>Related articles.</h2></div><a class="btn btn--line" href="{L("/blog")}">All articles</a></div>
  <div class="posts">{rel}</div>
</div></section>
{newsletter()}
{cta("Want this done for you?", "Our specialists can take this from idea to results. Start with a free consultation.")}
<script type="application/ld+json">{ld}</script><script type="application/ld+json">{faqld}</script>{crumb}
'''


# =================================================================== legal and system pages
def legal_body(kind):
    """Body HTML of the privacy policy or terms, with the current company details. Written to App_Data/seed/{kind}.html
    by the build (the database is filled from it on first run); the site renders the database copy."""
    if kind == "privacy":
        body = f'''<p>This policy explains what personal information Yenetch ("we", "us") collects through www.yenetch.com, why we collect it and how we protect it. It applies to visitors, prospective clients and job applicants.</p>
<h2 id="what-we-collect">What we collect</h2><ul><li>Details you give us through the contact form, chatbot, solution finder, newsletter or email: your name, phone number, email address, company and what you need.</li><li>Technical data such as IP address, device, browser, approximate location and pages visited, collected through server logs and, if you allow them, analytics cookies (see Cookies below).</li><li>Applications you send for a role, including your CV and portfolio.</li></ul>
<h2 id="how-we-use-it">How we use it</h2><ul><li>To reply to your enquiry and prepare proposals.</li><li>To send the newsletter you subscribed to. Every email has an unsubscribe link.</li><li>To assess job applications.</li><li>To keep the website secure and improve it.</li></ul><p>We do not sell your personal information, and we do not use it for purposes unrelated to the reasons above.</p>
<h2 id="sharing">Sharing</h2><p>We share information only with service providers who help us run our business (such as email and hosting providers) under confidentiality obligations, or where the law requires it.</p>
<h2 id="retention">Retention and security</h2><p>We keep enquiry data for as long as needed to respond and to maintain our business records, and application data for up to twelve months unless you ask us to delete it sooner. Access is limited to staff who need it.</p>
<h2 id="your-rights">Your rights</h2><p>You may ask to access, correct or delete your personal information, or withdraw consent, in line with the Digital Personal Data Protection Act, 2023. Write to <a href="mailto:{C["email"]}">{C["email"]}</a> and we will respond within a reasonable time.</p>
<h2 id="cookies">Cookies and analytics</h2><p>We use a small number of first-party cookies. We do not use advertising cookies and we do not share analytics data with ad networks.</p><ul><li><b>Essential</b> (always on): <code>yn_consent</code> remembers your cookie choice for 6 months. Form and security cookies keep enquiries safe.</li><li><b>Analytics</b> (only if you allow them): <code>yn_vid</code> recognises a returning browser for up to 13 months and <code>yn_sid</code> groups pages into one visit (30 minutes). With them we record the pages you view, time on page, how far you scroll, clicks on buttons such as call or WhatsApp, your device, browser and screen size, the site that referred you, your IP address and an approximate location derived from it. If you send us an enquiry, this visit history is linked to it so we can understand what you were looking for.</li><li><b>Without analytics consent</b> we still count page views anonymously: no cookie is set and your IP address is not stored.</li></ul><p>You can change your choice at any time with the <a href="#cookie-settings" data-cookie-settings>cookie settings</a> link in the footer, or block cookies in your browser. Analytics data is kept for up to 25 months.</p>
<h2 id="contact">Contact</h2><p>Yenetch, A-402, 4th Floor, VentureX, Landmark Cyber Park, Sector 67, Gurugram, Haryana. Email <a href="mailto:{C["email"]}">{C["email"]}</a>, phone {esc(C["phone"])}.</p>'''
    else:
        body = f'''<p>These terms govern your use of www.yenetch.com. By using the site you agree to them. Engagements for services or products are governed by separate written agreements.</p>
<h2 id="content">Website content</h2><p>Content on this site is provided for general information. We work to keep it accurate, but it is not professional advice for your specific situation, and prices, features and availability may change.</p>
<h2 id="intellectual-property">Intellectual property</h2><p>The Yenetch name, logo, text, graphics and code on this site belong to Yenetch or its licensors. Client names and results are shown with reference to published work. Stock photography is used under the Unsplash licence.</p>
<h2 id="acceptable-use">Acceptable use</h2><p>Do not misuse the site, attempt to gain unauthorised access, submit false information or send unsolicited promotional messages through our forms.</p>
<h2 id="links">Third-party links</h2><p>Links to client and partner websites are provided for reference. We are not responsible for their content or practices.</p>
<h2 id="liability">Limitation of liability</h2><p>To the extent permitted by law, Yenetch is not liable for any indirect loss arising from use of this website.</p>
<h2 id="law">Governing law</h2><p>These terms are governed by the laws of India, and courts in Gurugram, Haryana have jurisdiction.</p>
<h2 id="contact">Contact</h2><p>Questions about these terms: <a href="mailto:{C["email"]}">{C["email"]}</a>.</p>'''
    return body


def legal(kind):
    today = date.today().strftime("%-d %B %Y")
    title, h = ("Privacy policy", ["Privacy", "policy."]) if kind == "privacy" else ("Terms of use", ["Terms of", "use."])
    body = legal_body(kind) if bind.MODE == "preview" else f'<%= Yenetch.Data.SiteContent.LegalHtml("{kind}") %>'
    return f'''
<section class="phero"><div class="wrap wrap--text">
  {crumbs([("Home", "/"), (title, None)])}
  <h1>{h1_lines(h)}</h1>
  <p class="phero__lead">Last updated {today}.</p>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap wrap--text"><div class="prose">{body}</div></div></section>
'''


def not_found():
    return f'''
<section class="nf dark"><div class="wrap">
  <div class="nf__cube" aria-hidden="true">{mark()}</div>
  <span class="kicker">Error 404</span>
  <h1>This page thought outside the box a little too far.</h1>
  <p class="phero__lead">The link may be old or mistyped. Here are some good places to go instead.</p>
  <div class="actions"><a class="btn btn--blue" href="{L("/")}">Go to home</a><a class="btn btn--line" href="{L("/services")}">Browse services</a><a class="btn btn--line" href="{L("/contact")}">Contact us</a></div>
</div></section>
'''


# =================================================================== growth tools
def _aspx_only(markup):
    """Server-only markup (live Google reviews, data that needs the database). Nothing in the preview."""
    return markup if bind.MODE == "aspx" else ""


def _x(expr, preview):
    """A value from the server (aspx) or a fixed preview text."""
    return f"<%: {expr} %>" if bind.MODE == "aspx" else esc(preview)


def book_page():
    heading = _x("Yenetch.Crm.Bookings.Config.Heading", "Book a free consultation")
    intro = _x("Yenetch.Crm.Bookings.Config.Intro", "Pick a time for a 30-minute call with a specialist. Tell us what you want to achieve, and you will leave with a clear next step. No cost, no obligation.")
    return f'''
<section class="phero"><div class="wrap">
  {crumbs([("Home", "/"), ("Book a call", None)])}
  <span class="kicker">Book a call</span>
  <h1 class="bk__title">{heading}</h1>
  <p class="phero__lead">{intro}</p>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap contact">
  <div class="contact__form bk" data-book>
    <div class="bk__step" data-bk-step="day">
      <div class="bk__label"><b>1</b><span>Pick a day</span><small data-bk-tz></small></div>
      <div class="bk__days" data-bk-days role="listbox" aria-label="Available days"><p class="bk__loading">Loading open times…</p></div>
    </div>
    <div class="bk__step" data-bk-step="time" hidden>
      <div class="bk__label"><b>2</b><span>Pick a time</span><small data-bk-dayname></small></div>
      <div class="bk__times" data-bk-times role="listbox" aria-label="Available times"></div>
    </div>
    <form class="cform bk__form" data-bk-form hidden novalidate>
      <div class="bk__label"><b>3</b><span>Your details</span><small data-bk-chosen></small></div>
      <div class="bk__grid">
        <div class="cform__row"><label for="bk-name">Your name *</label><input class="field" id="bk-name" name="name" required maxlength="120" autocomplete="name"></div>
        <div class="cform__row"><label for="bk-email">Email *</label><input class="field" id="bk-email" name="email" type="email" required maxlength="160" autocomplete="email"></div>
        <div class="cform__row"><label for="bk-phone">Phone *</label><input class="field" id="bk-phone" name="phone" type="tel" required maxlength="40" autocomplete="tel"></div>
        <div class="cform__row"><label for="bk-company">Company</label><input class="field" id="bk-company" name="company" maxlength="160" autocomplete="organization"></div>
      </div>
      <div class="cform__row"><label for="bk-topic">What would you like to discuss?</label><select class="field" id="bk-topic" name="topic" data-bk-topics></select></div>
      <fieldset class="cform__row bk__modes"><legend>How should we talk?</legend><div data-bk-modes></div></fieldset>
      <div class="cform__row"><label for="bk-notes">Anything we should prepare? (optional)</label><textarea class="field" id="bk-notes" name="notes" rows="3" maxlength="2000"></textarea></div>
      <input type="text" name="website" tabindex="-1" autocomplete="off" aria-hidden="true" style="position:absolute;left:-9999px">
      <div data-captcha></div>
      <p class="apl__error" role="alert" hidden data-bk-error></p>
      <div class="bk__actions"><button class="btn btn--blue" type="submit">Confirm booking</button><button class="btn btn--line" type="button" data-bk-back>Change time</button></div>
      <p class="cform__note">You'll get a confirmation email with a calendar invite. We use your details only for this call. See our <a href="{L("/privacy")}">privacy policy</a>.</p>
    </form>
    <div class="cform__done" data-bk-done hidden>{icon("check")}<h2>You're booked.</h2><p data-bk-done-text>We've emailed you the details and a calendar invite.</p><a class="btn btn--line" href="{L("/")}">Back to the website</a></div>
    <div class="bk__closed" data-bk-closed hidden><h2>Online booking is closed right now.</h2><p>Call or WhatsApp us and we'll find a time.</p></div>
  </div>
  <aside class="contact__side">
    <div class="ccard ccard--social"><small>What to expect</small>
      <ul class="bk__expect"><li>{icon("check")}A specialist in your area, not a salesperson</li><li>{icon("check")}A clear next step, whatever you decide</li><li>{icon("check")}Free, with no obligation</li></ul></div>
    <a class="ccard" href="{tel_href()}">{icon("phone")}<span><small>Prefer to talk now?</small><b>{co("phone")}</b></span></a>
    <a class="ccard" href="{wa_href("Hi Yenetch, I'd like to book a call.")}" target="_blank" rel="noopener">{social_icon("whatsapp")}<span><small>WhatsApp</small><b>Chat with us</b></span></a>
  </aside>
</div></section>
'''


AUDIT_FAQ = [
    ("What does the free website audit check?", "It checks the things that decide whether people and search engines can find and trust your website: page title and description, headings, image text, mobile setup, HTTPS and security headers, page size and loading speed, social sharing tags, structured data, and whether robots.txt and a sitemap exist."),
    ("Is the website audit really free?", "Yes. The audit is free and takes under a minute. You get your score straight away and a copy by email. If you want, a specialist can walk you through the fixes on a free call."),
    ("How is the score calculated?", "Each check carries points. Important ones such as a missing title, no HTTPS or no mobile setup cost more than small ones. The score is out of 100, split into SEO, speed, mobile, security and social sharing."),
    ("Does the audit change anything on my website?", "No. It reads your home page the same way a search engine does. Nothing is changed and no login is needed."),
    ("How often should I audit my website?", "After every redesign or big content change, and at least once a quarter. Small problems such as a missing description or a slow image creep in over time."),
]


def audit_page():
    faq = faq_static(AUDIT_FAQ, "Website audit questions, answered.")
    checks = [("SEO", "Title, description, headings, image text, canonical address and indexing."), ("Speed", "Response time, page size, compression and the number of scripts and styles."),
              ("Mobile", "Viewport setup, readable text and tap-friendly pages."), ("Security", "HTTPS, secure redirects, mixed content and security headers."),
              ("Social sharing", "Open Graph and Twitter tags that control how links look when shared."), ("Search setup", "robots.txt, sitemap.xml, structured data and language.")]
    tiles = "".join(f'<li class="fx-up"><b>{t}</b><span>{d}</span></li>' for t, d in checks)
    return f'''
<section class="phero dark"><div class="wrap">
  {crumbs([("Home", "/"), ("Free website audit", None)])}
  <span class="kicker">Free website audit tool</span>
  <h1>{h1_lines(["Free website audit:", 'SEO, speed &amp; <span class="blue">mobile check.</span>'])}</h1>
  <p class="phero__lead">Enter your website and get a free SEO audit in under a minute. See what helps and what hurts your Google ranking, loading speed, mobile experience and security, with plain-English fixes.</p>
</div></section>
<section class="sec--tight" style="padding-top:0"><div class="wrap">
  <div class="au" data-audit>
    <form class="au__form" data-au-form novalidate>
      <div class="au__row">
        <div class="cform__row au__url"><label for="au-url">Website address *</label><input class="field" id="au-url" name="url" required maxlength="300" placeholder="yourbusiness.com" inputmode="url" autocomplete="url"></div>
        <div class="cform__row"><label for="au-email">Email for your report *</label><input class="field" id="au-email" name="email" type="email" required maxlength="160" autocomplete="email" placeholder="you@company.com"></div>
      </div>
      <div class="au__row au__row--opt">
        <div class="cform__row"><label for="au-name">Name</label><input class="field" id="au-name" name="name" maxlength="120" autocomplete="name"></div>
        <div class="cform__row"><label for="au-phone">Phone (optional)</label><input class="field" id="au-phone" name="phone" type="tel" maxlength="40" autocomplete="tel"></div>
      </div>
      <input type="text" name="website" tabindex="-1" autocomplete="off" aria-hidden="true" style="position:absolute;left:-9999px">
      <div data-captcha></div>
      <p class="apl__error" role="alert" hidden data-au-error></p>
      <div class="au__go"><button class="btn btn--blue" type="submit" data-au-btn>Audit my website</button><span class="cform__note">Free. Your report is also emailed to you. See our <a href="{L("/privacy")}">privacy policy</a>.</span></div>
    </form>
    <div class="au__busy" data-au-busy hidden><span class="au__spin" aria-hidden="true"></span><p>Checking your website… this takes up to 30 seconds.</p></div>
    <div class="au__result" data-au-result hidden aria-live="polite"></div>
  </div>
</div></section>
<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">What we check</span><h2>30+ checks that decide how your website ranks and converts.</h2>
    <p>Search engines and visitors judge a website in seconds. The audit looks at the same signals Google uses to understand, rank and show your pages.</p></div>
  <ul class="au__checks">{tiles}</ul>
</div></section>
{faq}
{cta("Want us to fix it for you?", "Book a free call and a specialist will walk you through your audit and the fixes that matter most.")}
'''


# =================================================================== pricing and the plan builder (/pricing)
PLANS_SEED = json.loads((WEB / "App_Data/seed/plans.json").read_text(encoding="utf-8"))
ADDONS_SEED = json.loads((WEB / "App_Data/seed/addons.json").read_text(encoding="utf-8"))

PRICING_FAQ = [
    ("Are these prices final?", "They are honest estimates for typical projects. After a short call about your goals and scope, you get a written proposal with a fixed price. Items marked \"from\" depend on scope."),
    ("Do prices include GST?", "No. Prices are before GST, which is added at 18% for clients in India. Clients outside India are usually not charged GST on exported services."),
    ("Can I pay in my own currency?", "Yes. Choose your currency at the top of the page to see estimates in it. Proposals and payment links can be issued in major currencies."),
    ("What is a minimum term?", "Monthly plans such as SEO or social media need a few months to show results, so some have a minimum of 3 or 6 months. After that, you can stop with 30 days' notice."),
    ("Can I combine services?", "Yes. Add plans from several services and any extras to build one custom plan. You get a single estimate, one point of contact and one invoice."),
    ("How do I pay?", "By bank transfer, UPI, card or net banking through a secure Razorpay payment link sent with your proposal."),
]


GOALS_SEED = json.loads((WEB / "App_Data/seed/goals.json").read_text(encoding="utf-8"))
PAIRS_SEED = json.loads((WEB / "App_Data/seed/pairs.json").read_text(encoding="utf-8"))
CARD_FEATURES = 4  # as Pricing.CardFeatures


def _plan_cards_preview(slug, on_service=False):
    """Plan cards for the static preview (the live site renders them from the database: Pricing.PlanCards)."""
    plans = [p for p in PLANS_SEED if p["service"] == slug]
    if not plans:
        return ""
    per = {"monthly": "/month", "yearly": "/year", "hourly": "/hour"}
    out = '<div class="plans" data-plans>'
    for p in plans:
        feats = p.get("features", [])
        shown = "".join(f"<li>{esc(f)}</li>" for f in feats[:CARD_FEATURES])
        more = (f'<details class="plan__more"><summary>{len(feats) - CARD_FEATURES} more included</summary><ul class="plan__list">'
                + "".join(f"<li>{esc(f)}</li>" for f in feats[CARD_FEATURES:]) + "</ul></details>") if len(feats) > CARD_FEATURES else ""
        frm = "<small>From</small> " if p.get("priceType") == "From" else ""
        setup = f'<p class="plan__setup">+ <span data-inr="{p["setupFee"]}">₹{p["setupFee"]:,}</span> one-time set-up</p>' if p.get("setupFee") else ""
        cls = "btn--blue" if p.get("popular") else "btn--line"
        btn = (f'<a class="btn {cls} btn--sm" href="{L("/pricing")}">Choose {esc(p["name"])}</a>' if on_service
               else f'<button type="button" class="btn {cls} btn--sm" data-add-plan="{p["id"]}" aria-pressed="false">Add to my plan</button>')
        flag = '<span class="plan__flag">Most popular</span>' if p.get("popular") else ""
        out += (f'<article class="plan{" plan--popular" if p.get("popular") else ""}" data-plan="{p["id"]}">{flag}'
                f'<h3>{esc(p["name"])}</h3><p class="plan__tag">{esc(p.get("tagline", ""))}</p>'
                f'<span class="plan__price">{frm}<b data-inr="{p["price"]}">₹{p["price"]:,}</b><small>{per.get(p["billing"], "")}</small></span>{setup}'
                f'<ul class="plan__list">{shown}</ul>{more}<p class="plan__time">{esc(p.get("timeline", ""))}</p><div class="plan__cta">{btn}</div></article>')
    return out + "</div>"


def _finder_preview(svcs):
    """Pricing.Finder for the static preview."""
    have = {s["slug"] for s in svcs}
    chips = ""
    for g in GOALS_SEED:
        ss = [x for x in g["services"] if x in have]
        if ss:
            chips += (f'<button type="button" class="gchip" aria-pressed="false" data-goal="{g["id"]}" data-services="{" ".join(ss)}"><b>{esc(g["name"])}</b>'
                      + (f'<small>{esc(g["hint"])}</small>' if g.get("hint") else "") + "</button>")
    return ('<div class="pfind" data-finder hidden><div class="pfind__head"><h2>What do you need help with?</h2><p>Pick one or more. We show only the plans that fit.</p></div>'
            f'<div class="pfind__chips" role="group" aria-label="Your goals">{chips}</div>'
            '<div class="pfind__bar"><label class="sr-only" for="psearch">Search services</label><input class="field field--sm pfind__search" id="psearch" type="search" placeholder="Or search: SEO, app, CRM, hosting…" autocomplete="off" data-psearch>'
            '<p class="pfind__count" data-pcount aria-live="polite"></p><button type="button" class="pfind__clear" data-pclear hidden>Clear</button></div></div>')


def _sections_preview(svcs):
    """Pricing.Sections for the static preview: folded service rows grouped by area."""
    per = {"monthly": "/month", "yearly": "/year", "hourly": "/hour"}
    out = ""
    for pillar in D["pillars"]:
        rows = ""
        for s in (x for x in svcs if x["pillar"] == pillar["id"]):
            plans = [p for p in PLANS_SEED if p["service"] == s["slug"]]
            low = min(plans, key=lambda p: p["price"])
            teaser = f'{len(plans)} plan{"s" if len(plans) != 1 else ""} · from <b data-inr="{low["price"]}">₹{low["price"]:,}</b>{per.get(low["billing"], "")}'
            words = esc(" ".join([s["name"], s["summary"]] + s.get("keywords", [])).lower())
            rows += (f'<details class="psvc" id="plans-{s["slug"]}" data-svc="{s["slug"]}" data-find="{words}">'
                     f'<summary class="psvc__sum"><span class="psvc__name"><h3>{esc(s["name"])}</h3><span class="psvc__picked" data-picked hidden>In your plan</span></span>'
                     f'<span class="psvc__meta">{teaser}</span><span class="psvc__chev" aria-hidden="true"></span></summary>'
                     f'<div class="psvc__body"><div class="psvc__head"><p>{esc(s["summary"])}</p><a class="more" href="{L("/services/" + s["slug"])}">About this service</a></div>'
                     f'{_plan_cards_preview(s["slug"])}</div></details>')
        if rows:
            out += f'<div class="pgroup" data-pgroup><h2 class="ppillar">{esc(pillar["name"])}</h2>{rows}</div>'
    return out + '<p class="pmore" data-pmore hidden><button type="button" class="btn btn--line btn--sm" data-pshowall></button></p>'


def currency_switch():
    return ('<div class="cur" data-currency-switch><label for="cur-select">Show prices in</label>'
            '<select class="field field--sm" id="cur-select" data-currency-select><option value="INR">₹ INR</option></select></div>')


def service_plans(slug):
    """Plan cards on a service page (only when the service has plans)."""
    if bind.MODE == "aspx":
        return "<%= Yenetch.Web.Pricing.ServicePlans(Svc.Slug) %>"
    cards = _plan_cards_preview(slug, True)
    if not cards:
        return ""
    return (f'<div class="svc-plans"><div class="svc-plans__bar"><h3>Plans</h3>{currency_switch()}</div>{cards}'
            f'<p class="svc-plans__more"><a class="more" href="{L("/pricing")}#builder">Combine with other services in the plan builder</a></p></div>')


def pricing_page():
    heading = _x("Yenetch.Data.Pricing.Heading", "Simple plans. Or build your own.")
    intro = _x("Yenetch.Data.Pricing.Intro", "Pick a plan for any service, or combine services and extras into one custom plan. You get an instant estimate, and a specialist confirms it with you.")
    note = _x("Yenetch.Data.Pricing.Note", "Prices are estimates before GST. Your final quote is confirmed after a short call about your goals and scope.")
    if bind.MODE == "aspx":
        sections = "<%= Yenetch.Web.Pricing.Sections() %>"
        tabs = "<%= Yenetch.Web.Pricing.Finder() %>"
        builder_data = '<script type="application/json" id="builder-data"><%= Yenetch.Data.Pricing.BuilderJson() %></script>'
        preview_note = "<%= Yenetch.Web.Pricing.PreviewNote() %>"
        ld = "<%= Yenetch.Data.Pricing.CatalogLd() %>"
    else:
        svcs = [s for s in D["services"] if any(p["service"] == s["slug"] for p in PLANS_SEED)]
        tabs = _finder_preview(svcs)
        sections = _sections_preview(svcs)
        data = {"show": True, "tax": {"name": "GST", "pct": 18},
                "goals": [{"id": g["id"], "name": g["name"], "services": [x for x in g["services"] if x in {v["slug"] for v in svcs}]} for g in GOALS_SEED],
                "services": [{"slug": v["slug"], "name": v["name"], "pairs": PAIRS_SEED.get(v["slug"], [])} for v in svcs],
                "plans": [{"id": p["id"], "service": p["service"], "serviceName": SVC[p["service"]]["name"], "name": p["name"], "price": p["price"], "billing": p["billing"],
                           "setup": p.get("setupFee", 0), "from": p.get("priceType") == "From", "popular": p.get("popular", False)} for p in PLANS_SEED],
                "addons": [{"id": a["id"], "service": a.get("service"), "group": a.get("group") or "Extras", "name": a["name"], "description": a.get("description", ""),
                            "price": a["price"], "billing": a["billing"], "unit": a.get("unit"), "min": max(1, a.get("min", 1)), "max": a.get("max", 0)} for a in ADDONS_SEED]}
        builder_data = f'<script type="application/json" id="builder-data">{safe(json.dumps(data, ensure_ascii=False))}</script>'
        preview_note, ld = "", ""
    faq = faq_static(PRICING_FAQ, "Pricing questions, answered.")
    done = (f'<div class="cform__done" data-pb-done hidden>{icon("check")}<h2>Your quote is on its way.</h2>'
            f'<p data-pb-done-text>We have emailed your estimate. A specialist will call you within one working day.</p><a class="btn btn--line" href="{L("/book")}">Book a call now</a></div>')
    return f"""
<section class="phero phero--compact"><div class="wrap">
  {crumbs([("Home", "/"), ("Pricing", None)])}
  <span class="kicker">Pricing &amp; plans</span>
  <h1>{heading}</h1>
  <p class="phero__lead">{intro}</p>
  <div class="phero__tools">{currency_switch()}<a class="btn btn--blue btn--sm" href="#builder">Build a custom plan</a></div>
  {preview_note}
</div></section>
<section class="sec--tight pricing-page" style="padding-top:0"><div class="wrap">
  {tabs}
  <div class="psvcs">{sections}</div>
</div></section>
<div class="pbar" data-pb-bar hidden><span class="pbar__text" data-pb-bar-text></span><a class="btn btn--blue btn--sm" href="#builder" data-pb-bar-go>View my plan</a></div>
<section class="sec paper" id="builder"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Plan builder</span><h2>Build your custom plan.</h2><p>Add plans from any service above, then the extras you need. Your estimate updates as you go.</p></div>
  <div class="pb" data-builder>
    {builder_data}
    <div class="pb__main">
      <div class="pb__empty" data-pb-empty><p>Your plan is empty. Pick what you need above, open a service and choose <b>Add to my plan</b>.</p></div>
      <div class="pb__suggest" data-pb-suggest hidden></div>
      <div class="pb__extras" data-pb-extras></div>
    </div>
    <aside class="pb__side"><div class="pb__card" data-pb-summary aria-live="polite">
      <h3>Your plan</h3>
      <ul class="pb__lines" data-pb-lines></ul>
      <form class="pb__code" data-pb-code><label for="pb-code" class="sr-only">Offer code</label><input class="field field--sm" id="pb-code" name="code" placeholder="Offer code" maxlength="40" autocomplete="off"><button class="btn btn--line btn--sm" type="submit">Apply</button></form>
      <p class="pb__offer" data-pb-offer hidden></p>
      <dl class="pb__totals" data-pb-totals></dl>
      <p class="pb__note">{note}</p>
      <button class="btn btn--blue pb__go" type="button" data-pb-go disabled>Get my quote</button>
    </div></aside>
  </div>
  <div class="pb__form-wrap" data-pb-form-wrap hidden>
    <form class="cform pb__form" data-pb-form novalidate>
      <h3>Where should we send your quote?</h3>
      <div class="bk__grid">
        <div class="cform__row"><label for="pb-name">Your name *</label><input class="field" id="pb-name" name="name" required maxlength="120" autocomplete="name"></div>
        <div class="cform__row"><label for="pb-email">Email *</label><input class="field" id="pb-email" name="email" type="email" required maxlength="160" autocomplete="email"></div>
        <div class="cform__row"><label for="pb-phone">Phone *</label><input class="field" id="pb-phone" name="phone" type="tel" required maxlength="40" autocomplete="tel"></div>
        <div class="cform__row"><label for="pb-company">Company</label><input class="field" id="pb-company" name="company" maxlength="160" autocomplete="organization"></div>
      </div>
      <div class="cform__row"><label for="pb-notes">Anything we should know? (optional)</label><textarea class="field" id="pb-notes" name="notes" rows="3" maxlength="2000"></textarea></div>
      <input type="text" name="website" tabindex="-1" autocomplete="off" aria-hidden="true" style="position:absolute;left:-9999px">
      <div data-captcha></div>
      <p class="apl__error" role="alert" hidden data-pb-error></p>
      <div class="bk__actions"><button class="btn btn--blue" type="submit">Email me this quote</button></div>
      <p class="cform__note">A specialist will call you within one working day. We use your details only for this enquiry. See our <a href="{L("/privacy")}">privacy policy</a>.</p>
    </form>
    {done}
  </div>
</div></section>
{faq}
{cta("Not sure what you need?", "Book a free call and we will recommend the right plan for your goals and budget.", "Help me choose a plan")}
{ld}
"""


# =================================================================== files the site reads at runtime
def write_runtime_files():
    """SocialIcons.g.cs (icon paths for Site.SocialIconPath) and the legal page seeds in App_Data/seed/,
    which the site copies into the database on first run."""
    (WEB / "Code/Data/SocialIcons.g.cs").write_text(social_icons_cs(), encoding="utf-8-sig")
    seed = WEB / "App_Data/seed"
    seed.mkdir(parents=True, exist_ok=True)
    for kind in ("privacy", "terms"):
        (seed / f"{kind}.html").write_text(legal_body(kind) + "\n", encoding="utf-8")
    (seed / "landing.json").write_text(json.dumps(LANDING_SEED, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


# =================================================================== landing pages (/{slug}, managed in Admin > Content > Landing pages)
LANDING_SEED = [
    {
        "slug": "digital-marketing-agency-in-jaipur", "city": "Jaipur", "industry": "",
        "seoTitle": "Digital Marketing Agency in Jaipur | SEO, Ads & Leads | Yenetch",
        "seoDescription": "Yenetch is a digital marketing agency in Jaipur for SEO, Google and Meta ads, social media and lead generation. Free consultation and a written growth plan.",
        "kicker": "Digital marketing agency in Jaipur",
        "headline": "Get more leads in Jaipur with marketing that reports to revenue.",
        "lead": "SEO, Google Ads, Meta Ads and social media run by one Jaipur team. You see every rupee, every lead and every sale on one dashboard.",
        "photo": "city-jaipur",
        "intro": ["Jaipur businesses compete for attention with national brands and local rivals on the same Google page. Our Jaipur delivery centre plans, runs and reports your marketing so it brings enquiries you can call, not just clicks.",
                  "We start with a free audit of your website, ads and Google Business Profile, then share a written plan with the channels, budget and lead targets that fit your goal."],
        "highlights": ["Local SEO and Google Business Profile for Jaipur searches", "Google and Meta ads with lead tracking to sales", "Social media content made in Jaipur", "Monthly reports in plain English"],
        "stats": [{"value": "100+", "label": "clients served"}, {"value": "2019", "label": "building since"}, {"value": "30+", "label": "specialists"}],
        "benefits": [{"title": "Leads, not vanity metrics", "text": "Every campaign is measured by enquiries and sales. Calls, forms and WhatsApp chats are tracked back to the ad or keyword that brought them."},
                     {"title": "Rank for Jaipur searches", "text": "Local SEO, Google Business Profile and review management so you show up when people nearby search for what you sell."},
                     {"title": "One accountable team", "text": "Strategy, design, ads and reporting sit with one team in Jaipur, so nothing falls between agencies."},
                     {"title": "Budgets that make sense", "text": "Start small, prove what works, then scale. No long lock-in, and you own every account and asset."}],
        "services": ["performance-marketing", "seo", "social-media-marketing", "lead-generation"],
        "process": [{"name": "Free audit", "text": "We review your website, ads and Google listing and find the quick wins."},
                    {"name": "Growth plan", "text": "A written plan with channels, budget, timeline and lead targets."},
                    {"name": "Launch", "text": "Campaigns, landing pages and tracking go live in about two weeks."},
                    {"name": "Improve monthly", "text": "We test, report and move budget to what brings the most leads."}],
        "faqs": [{"q": "How much does digital marketing cost in Jaipur?", "a": "Most Jaipur businesses start with a management fee plus an ad budget they control. We share a written proposal with fees after a free consultation, so you know the cost before you commit."},
                 {"q": "How soon will I see results?", "a": "Paid ads usually bring enquiries in the first few weeks. SEO builds over three to six months. Your plan shows what to expect month by month."},
                 {"q": "Do you only work with Jaipur businesses?", "a": "No. Our Jaipur delivery centre works with clients across India, but we know the Jaipur market well and can meet in person."},
                 {"q": "Will I own my ad accounts and website?", "a": "Yes. Every account, page and asset is set up in your name and stays with you."}],
        "ctaTitle": "Grow your Jaipur business with a clear plan.",
        "ctaText": "Book a free call and get a written marketing plan with budget, channels and lead targets.",
        "topic": "Digital marketing",
    },
    {
        "slug": "software-development-company-in-gurugram", "city": "Gurugram", "industry": "",
        "seoTitle": "Software Development Company in Gurugram | Web, App & CRM | Yenetch",
        "seoDescription": "Custom software, web apps, mobile apps and CRM built by a software development company in Gurugram. Fixed-scope proposals and a demo every two weeks.",
        "kicker": "Software development company in Gurugram",
        "headline": "Custom software from Gurugram, delivered in two-week sprints.",
        "lead": "Web apps, mobile apps, CRM and automation built by a Gurugram team that shows you working software every two weeks.",
        "photo": "city-gurugram",
        "intro": ["Off-the-shelf tools rarely fit how your business really works. Our Gurugram head office designs and builds software around your process, from customer portals and booking systems to CRMs and internal dashboards.",
                  "Every project starts with a free consultation and a written proposal with scope, timeline and cost. You see a working demo every two weeks, so there are no surprises at launch."],
        "highlights": ["Web apps, portals and dashboards", "Android and iOS apps", "CRM, ERP and workflow automation", "Support and maintenance after launch"],
        "stats": [{"value": "100+", "label": "clients served"}, {"value": "2019", "label": "building since"}, {"value": "30+", "label": "specialists"}],
        "benefits": [{"title": "Built around your process", "text": "We map how your team works today, then build software that removes manual steps instead of adding new ones."},
                     {"title": "Progress you can see", "text": "A working demo every two weeks and a shared board, so you always know what is done and what is next."},
                     {"title": "Modern, proven technology", "text": ".NET, React, Flutter and cloud hosting chosen for speed, security and easy hiring later."},
                     {"title": "You own the code", "text": "Full source code, documentation and access are handed over. No lock-in."}],
        "services": ["custom-software", "web-development", "mobile-apps", "crm-erp"],
        "process": [{"name": "Discovery", "text": "A free call to understand goals, users and must-have features."},
                    {"name": "Proposal", "text": "Scope, screens, timeline and a fixed or monthly cost in writing."},
                    {"name": "Build in sprints", "text": "Design and development with a demo every two weeks."},
                    {"name": "Launch and support", "text": "Testing, go-live, training and ongoing maintenance."}],
        "faqs": [{"q": "How much does custom software cost in Gurugram?", "a": "It depends on features and integrations. After a free consultation you get a written proposal with a fixed price or a monthly team cost, so you can plan the budget."},
                 {"q": "How long does it take to build an app?", "a": "A focused first version usually takes 8 to 16 weeks. We often launch a smaller version first and add features based on real use."},
                 {"q": "Can you take over an existing project?", "a": "Yes. We review the code, fix urgent issues and continue development or maintenance."},
                 {"q": "Can we meet your team in Gurugram?", "a": "Yes. Our head office is in Gurugram and we are happy to meet in person or online."}],
        "ctaTitle": "Have a software idea or a process to automate?",
        "ctaText": "Book a free call with a Gurugram engineer and get a written proposal with scope, timeline and cost.",
        "topic": "Software development",
    },
    {
        "slug": "seo-company-in-gurugram", "city": "Gurugram", "industry": "",
        "seoTitle": "SEO Company in Gurugram | Local SEO & Google Rankings | Yenetch",
        "seoDescription": "SEO company in Gurugram for local SEO, technical fixes, content and Google Business Profile. Free website audit and a written plan to rank and get leads.",
        "kicker": "SEO company in Gurugram",
        "headline": "Rank higher on Google and turn searches into enquiries.",
        "lead": "Technical SEO, content and local SEO from a Gurugram team, with clear monthly reporting on rankings, traffic and leads.",
        "photo": "mk-seo",
        "intro": ["Most people choose from the first few Google results. Our Gurugram SEO team fixes what holds your website back, writes content people search for, and builds your local presence so you appear when buyers are ready.",
                  "Start with our free website audit to see your score in a minute, then book a call to get a plan for your business."],
        "highlights": ["Technical SEO and page speed fixes", "Keyword research and content that ranks", "Google Business Profile and reviews", "Monthly ranking and lead reports"],
        "stats": [{"value": "100+", "label": "clients served"}, {"value": "2019", "label": "building since"}, {"value": "30+", "label": "specialists"}],
        "benefits": [{"title": "Fix the foundations first", "text": "Speed, mobile, indexing and structured data are fixed early, so every page you publish has a fair chance to rank."},
                     {"title": "Content people search for", "text": "Pages and articles planned around real searches in your market, written to answer questions and win enquiries."},
                     {"title": "Win local searches", "text": "Google Business Profile, local pages and review requests that help you appear in the map results around Gurugram."},
                     {"title": "Clear reporting", "text": "Rankings, traffic and leads in one simple monthly report, with what we did and what comes next."}],
        "services": ["seo", "content-marketing", "brand-orm", "web-development"],
        "process": [{"name": "Audit", "text": "A full technical, content and local SEO review of your site."},
                    {"name": "Plan", "text": "Keywords, pages and fixes ranked by impact and effort."},
                    {"name": "Fix and publish", "text": "Technical fixes, new pages and content every month."},
                    {"name": "Report", "text": "Monthly report on rankings, traffic and enquiries."}],
        "faqs": [{"q": "How long does SEO take to work?", "a": "Technical fixes can help within weeks. Most businesses see clear ranking and traffic growth in three to six months, depending on competition."},
                 {"q": "How much does SEO cost in Gurugram?", "a": "SEO is usually a monthly fee based on the number of pages, content and locations. You get a written proposal after a free consultation."},
                 {"q": "Can you guarantee the first position on Google?", "a": "No honest agency can. We commit to the work, the reporting and the targets in your plan, and we show what each month delivered."},
                 {"q": "Can I check my website first?", "a": "Yes. Use our free website audit tool for an instant score with fixes, then book a call if you want help."}],
        "ctaTitle": "See where your website stands today.",
        "ctaText": "Book a free SEO call, or run the free website audit first and bring your report.",
        "topic": "SEO",
    },
]


def landing_page(lp=None):
    """lp: the landing page dict in the preview. In aspx the code-behind exposes Lp, LpServices, LpCases, FaqLd, CrumbLd, PlaceLd."""
    if bind.MODE == "preview":
        p = P(lp)
        services = ([prep_service(s) for s in D["services"] if s["slug"] in lp.get("services", [])], None)
        cases_v = P([prep_case(c) for c in D["caseStudies"] if set(c["services"]) & set(lp.get("services", []))][:2])
        faqld = faq_ld(lp.get("faqs", []))
        crumb = breadcrumb_ld([("Home", "/"), (lp["kicker"], "/" + lp["slug"])])
        place = ""
    else:
        p = X("Lp")
        services = (None, "LpServices")
        cases_v = X("LpCases")
        faqld, crumb, place = X("FaqLd"), '<script type="application/ld+json"><%= CrumbLd %></script>', '<script type="application/ld+json"><%= PlaceLd %></script>'
    num = lambda i: fmt(i, lambda v: f"{v+1:02d}", lambda e: f'({e} + 1).ToString("00")')
    intro = each(p.intro, lambda x, i: f"<p>{x}</p>")
    highlights = each(p.highlights, lambda x, i: f"<li>{x}</li>")
    stats = each(p.stats, lambda s, i: f'<div class="stat fx-up"><b data-count>{s.value}</b><span>{s.label}</span></div>', "Yenetch.Models.Stat")
    benefits = each(p.benefits, lambda b, i: f'<div class="ocard fx-up"><b class="num">{num(i)}</b><h3>{b.title}</h3><p>{b.text}</p></div>', "Yenetch.Models.TitleText")
    process = each(p.process, lambda s, i: f'<li class="fx-up"><h3>{s.name}</h3><p>{s.text}</p></li>', "Yenetch.Models.ProcessStep")
    svc = each(services, lambda r, i: f'<a class="rtile" href="{href(r.url)}"><h3>{r.name}</h3><p>{r.summary}</p><span class="more">Learn more</span></a>', "Yenetch.Models.Service")
    cases = each(cases_v, lambda c, i: case_card(c, i), "Yenetch.Models.CaseStudy")
    reviews = "" if bind.MODE == "preview" else '<asp:PlaceHolder runat="server"><%= Yenetch.Crm.Reviews.WidgetHtml() %></asp:PlaceHolder>'
    book = fmt(p.topic, lambda v: L("/book") + "?topic=" + re.sub(r"\s+", "+", v or ""), lambda e: f'"/book?topic=" + HttpUtility.UrlEncode({e})')
    return f'''
<section class="phero dark phero--svc"><div class="wrap phero__grid">
  <div>{crumbs([("Home", "/"), (p.kicker, None)])}
    <span class="kicker">{p.kicker}</span>
    <h1 class="h1-svc">{p.headline}</h1>
    <p class="phero__lead">{p.lead}</p>
    <div class="actions"><a class="btn btn--blue" href="{book}">Book a free call</a><button class="btn btn--line" type="button" data-chat="{p.topic}">Get a free proposal</button></div></div>
  <div class="phero__visual fx-right">{media(p.photo, "media media--hero", eager=True)}</div>
</div></section>

{when_any(p.stats, f'<section class="sec--tight"><div class="wrap"><div class="stats">{stats}</div></div></section>')}

<section class="sec" id="overview"><div class="wrap">
  <div class="head fx-up"><span class="kicker">{p.kicker}</span><h2>Why businesses choose Yenetch.</h2></div>
  <div class="prose-lite fx-up">{intro}</div>
  <ul class="checks fx-up">{highlights}</ul>
</div></section>

{when_any(p.benefits, f"""<section class="sec dark"><div class="wrap">
  <div class="head fx-up"><span class="kicker">What you get</span><h2>What changes for your business.</h2></div>
  <div class="ocards">{benefits}</div>
</div></section>""")}

{when_any(P(services[0]) if bind.MODE == "preview" else X(services[1]), f"""<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Services</span><h2>Everything you need, from one team.</h2></div>
  <div class="rtiles">{svc}</div>
</div></section>""")}

{when_any(p.process, f"""<section class="sec paper"><div class="wrap">
  <div class="head fx-up"><span class="kicker">How it works</span><h2>Simple steps, no surprises.</h2></div>
  <ol class="engine engine--4">{process}</ol>
</div></section>""")}

{when_any(cases_v, f"""<section class="sec"><div class="wrap">
  <div class="head fx-up"><span class="kicker">Results</span><h2>Proof from our clients.</h2></div>
  <div class="stack">{cases}</div></div></section>""")}

{reviews}
{faq_bound(p.faqs, faqld)}
{cta(p.ctaTitle, p.ctaText, p.topic)}
{crumb}{place}
'''
