# Yenetch website (v11)

Visual Studio solution with an ASP.NET Web Forms **Web Application** project (.NET Framework 4.8), a generator that renders every page from one set of templates, and a static preview.

```
Yenetch.sln
Yenetch.Web/
  Yenetch.Web.csproj
  Site.Master (+ .cs, .designer.cs)   head, SEO and Open Graph tags, JSON-LD, nav with mega menus, footer
  Default.aspx                        home
  DigitalMarketing.aspx               /digital-marketing
  SoftwareDevelopment.aspx            /software-development
  TalentResourcing.aspx               /talent-resourcing
  Services, ServiceDetail             /services, /services/{slug} (one SEO page per service)
  Products, ProductDetail             /products, /products/{slug}
  CaseStudies, CaseStudyDetail        /case-studies, /case-studies/{slug}
  Blog, BlogArticle                   /blog, /blog/category/{slug}, /blog/{slug}
  About (team), Careers, Contact, SolutionFinder, Privacy, Terms, NotFound
  Code/Models/Content.cs              content model
  Code/Data/SiteContent.cs            website content from the database (files as fallback); Photos helper
  Code/Data/ContentStore.cs           CmsItems / CmsSettings: content sections, seeding, admin saves
  Code/Data/ContentSchema.cs          the admin form for each content section
  Code/Data/Site.cs                   company details and lists that page markup binds to
  Code/Data/Seo.cs                    JSON-LD builders (Organization, Service, Product, Article, FAQ, Breadcrumbs)
  Code/Data/BlogRepository.cs         IBlogRepository: JSON + HTML files now, SQL Server via BlogSource=Sql
  Code/Data/BlogHtml.cs               table of contents and FAQ schema from article HTML
  Code/Data/LeadStore.cs              lead storage (App_Data/leads.jsonl)
  Handlers/Lead.ashx                  chatbot, finder and newsletter leads (JSON POST), served at /api/lead (also /api/pulse, /api/apply; see Global.asax)
  Handlers/SitemapHandler.cs          /sitemap.xml, generated from content
  robots.txt
  Global.asax                         clean routes
  assets/css/site.css                 design system
  assets/js/site.js, finder.js, yenbot.js, dock.js (mobile dock, contact sheet, rocket back-to-top)
  assets/data/yenetch.json            company, services, products, case studies, team, industries, careers, FAQs, finder rules
  assets/data/pages.json              long-form copy per service and product page
  assets/data/blog/*.html             article bodies (+ _meta.json)
  assets/img/                         Y-Box logo (SVG + PNG), favicons, og-image.png, stock/ photos; mark paths in tools/logo_faces.json
  App_Data/sql/schema.sql             BlogPosts, BlogTags, Leads tables (optional SQL blog)
  App_Data/sql/crm.sqlserver.sql      CRM, analytics and newsletter tables (created automatically)
  App_Data/sql/content.sqlserver.sql  website content, settings and lead attachment tables (created automatically)
  App_Data/seed/                      first-run copy of privacy, terms and page text (loaded into the database once)
  Code/Crm/                           leads, follow-ups, users, analytics, reports, newsletter, email
  Admin/                              admin panel at /admin (see "Admin panel" below)
  Handlers/Pulse.ashx                 first-party analytics endpoint
  Unsubscribe.aspx                    /newsletter/unsubscribe
  assets/js/consent.js                cookie banner and analytics tracking
  assets/admin/                       admin styles and charts
preview/                              static HTML of every page, no .NET needed
tools/build.py                        generator entry point
tools/sitegen/                        bind.py (data binding), ui.py (components), pages.py (page templates)
```

## Run it
1. Open `Yenetch.sln` in Visual Studio 2019 or 2022 (ASP.NET and web development workload).
2. Press F5. IIS Express serves it at https://localhost:44321/.
3. For IIS: publish the project, use an Integrated pipeline, .NET CLR v4.0 app pool, and give the app pool write access to `App_Data`.

## Admin panel
Open **/admin**. Everything below runs from the same project, no extra services.

**First run**
1. The `CrmDb` connection string in `Web.config` points at LocalDB, database `Yenetch`. On F5 the database and tables are created for you. For production, point it at your SQL Server (create an empty database first if the app login cannot create one).
2. Go to **/admin/setup**, enter the `AdminSetupKey` from `Web.config`, and create your admin account. **Change `AdminSetupKey` to your own value before going live.** Setup closes once an account exists.
3. Add your team under **Team**. Roles: **Admin** (everything), **Manager** (all leads, reports, analytics, newsletter), **Sales** (own and unassigned leads only).

**What is in it**
- **Leads:** every chatbot, solution finder and contact form enquiry lands here, typed (Marketing, Development, Talent, Product and so on), with source, channel, city and the visitor's website journey. Add leads by hand, assign them (the owner gets an email), move them through New, Contacted, Qualified, Proposal, Negotiation, Won or Lost, filter, sort, bulk assign and export to CSV.
- **Follow-ups:** log calls, emails, WhatsApp and meetings with a due date. Overdue, today and upcoming lists, snooze and done.
- **Reports:** leads by month, source, channel, type, status and owner, win rate, pipeline value and first response time.
- **Analytics:** visitors, visits, pageviews, visit length, bounce, live visitors, a daily graph, channels (search, social, AI assistants, email, paid, referral), sources, UTM campaigns, pages with time and scroll depth, landing and exit pages, countries, cities, devices, browsers, OS, languages, clicks (call, WhatsApp, chatbot, CTA) and busiest hours.
- **Visitors:** each visitor who accepted analytics cookies, with location, IP, device, every visit and every page in order, and the lead they became.
- **Blog:** write posts in a visual editor (headings, bold, links, lists, quotes, note boxes, tables, images, or raw HTML), with a cover image, category, tags, summary and Google title and description. Paste from Word or Google Docs and the formatting is cleaned up. Save as a draft, publish now, schedule a future date, or hide a post. **Preview** shows a draft on the real article page while you are signed in. The built-in articles are listed too: open one and save to replace it with your edited copy (delete the copy to bring the original back). Uploaded images go to `/uploads/blog/`. Admins and Managers only.
- **Newsletter:** subscribers from the website signup, add or import by hand, export. Write a campaign with a live preview, send yourself a test, then send to all active subscribers. Every email has a one-click unsubscribe link. Sending runs in the background at the speed set in **Email settings > Newsletter speed** (default 20 a minute, at most 200 an hour; ask your host for its limit). You can schedule a send for later, pause, resume or stop it, and retry failed addresses. Progress is saved per recipient (table NewsDeliveries), so if the site restarts the send carries on by itself within a minute of the next visit, without emailing anyone twice. If the mail server says it is busy, sending waits and tries again (up to 3 times per address).

**Email (hello@yenetch.com on GenX Hosting)**
Open **/admin → Settings → Email settings** (Admins only) and fill in:
- **Outgoing mail server (SMTP):** `mail.yenetch.com` (the host name in your GenX Hosting mail panel if it differs).
- **Port:** `587`, **Security:** SSL/TLS (STARTTLS). Port 465 does not work with .NET's mail client, so use 587.
- **Username:** the full address, `hello@yenetch.com`, and its mailbox password. The password is stored encrypted.
- **Send from:** `hello@yenetch.com`, **Sender name:** `Yenetch`.
- **Send lead alerts to:** one or more addresses, separated by commas.
- **Replies go to** (optional), and the thank-you email to people who enquire (on by default).

Press **Save and send test**. If it fails, the page shows the mail server's exact message. Set `SiteUrl` in Web.config to the live address so links and the logo in emails are right. For best delivery, make sure the domain's SPF and DKIM records in the GenX panel include the mail server.

Every email has the Y-Box logo embedded (shows even when images are blocked), a branded HTML layout and a plain-text version. New lead alerts include Call and WhatsApp buttons, reply straight to the visitor, and carry any file the visitor attached. Visitors get a short confirmation with your phone and WhatsApp. Until a mail server is set, emails are saved as `.eml` files in `App_Data/mail`; failures are logged to `App_Data/mail-errors.log`.

**Job applications:** every **Apply** button on the Careers page opens a popup form (name, email, phone, role, experience, city, notice period, skills, CV as PDF or Word up to 5 MB or a CV link, LinkedIn or portfolio, note). Applications land in **/admin → Applications** (Admins and Managers): search by name, email, skills, city or notes; filter by status, team, role and minimum experience; sort by date, experience, rating or name; shortlist or reject in one click. Each application page has the CV, status (New, Shortlisted, Interview, Offered, Hired, Rejected), a 1 to 5 rating, team notes, Call and WhatsApp buttons, and an email box with Shortlisted, Interview and Not a fit templates (sent from your company address; replies come to you). The team gets an alert with the CV attached, and the candidate gets a confirmation. Set **Send job applications to** in Email settings, or they go to the lead alert address. CVs are stored in `App_Data/resumes` (back it up) and deleted with the application.

**Attachments on the contact form:** visitors can attach one file (PDF, Word, Excel, PowerPoint, text or image, up to 5 MB). Files are stored in `App_Data/attachments` (never served directly), listed on the lead page, and downloadable only by signed-in staff who can see that lead.

**Cookies, privacy and location**
- The cookie banner asks before any tracking cookie is set. Visitors who choose "Necessary only" are counted anonymously (no cookies, no IP stored). Visitors who accept get a visitor cookie (13 months) and a session cookie (30 minutes), which join their visits into a journey. The privacy page explains this, and "Cookie settings" in the footer reopens the banner.
- City and region come from Cloudflare headers if the site is behind Cloudflare (turn on "Add visitor location headers"). Otherwise set `GeoIpToken` to an ipinfo.io token (results are cached, so each IP is looked up once). Without either, only the country is guessed from the time zone.
- Behind a load balancer or proxy, set `TrustForwardedFor` to `true` so the real visitor IP is recorded.
- Analytics older than `AnalyticsRetentionMonths` (25) is deleted automatically each day.

**Hosting notes:** give the app pool write access to `App_Data` and `uploads`. Back up the database, `uploads`, `App_Data/attachments` and `App_Data/resumes`. When you publish from Visual Studio, keep "Remove additional files at destination" off so uploaded blog images are not deleted, and include `uploads` in your backups. The admin is excluded in `robots.txt` and sends `noindex`. Use HTTPS in production so the sign-in cookie is secure.

## Growth, security and SEO tools (v10)
All of these live in the admin and need no extra services. Admin-only pages are under **SEO & security**.

- **Booked calls** (`/book`, admin **Booked calls**, **Availability**): visitors pick a free 30-minute slot (call, video or office). Set working hours per day (two ranges for a lunch break), slot length, gap between calls, notice, how far ahead and closed dates under **Availability**. Both sides get an email with a calendar invite; visitors can cancel from their email. Cancelling in the admin emails the visitor.
- **Website audit** (`/website-audit`, admin **Website audits**): a free SEO, speed and security check of any site, with a score and plain-language fixes. Each audit is saved as a lead. Internal and private addresses are refused. Limited to 8 per visitor per hour.
- **Landing pages** (admin **Landing pages**): pages such as `/seo-company-in-gurugram` with their own Google title, text, numbers, services, steps and FAQs. Three are included; add more from the admin. They are added to the sitemap with local business and FAQ data for Google.
- **Automatic emails** (admin **Automatic emails**): a welcome series for new subscribers, a follow-up series for new enquiries, and a review request for clients marked Won. All are **off** until you turn them on. Use "Send all emails to me now" to check them first.
- **Google reviews** (admin **Google reviews**): add your review link, and optionally a Google Place ID and API key to pull your rating automatically, or type the rating yourself. "Show on site" adds the rating to the home and contact pages. "Ask for a review" on a won lead emails the client a short link.
- **Daily summary** (Email settings): one email each morning (9 AM IST by default) with new leads, follow-ups due, booked calls and website numbers. Sales users get only their own leads. On by default.
- **Tracking & scripts**: Google Analytics 4, Tag Manager, Google Ads, Meta Pixel, LinkedIn, Clarity, Google and Bing verification codes, plus your own custom scripts. All tracking waits for cookie consent.
- **robots.txt**: rule builder (block AI training crawlers, extra blocked paths) or write your own, with a preview of what crawlers see. Served at `/robots.txt`.
- **Google for Jobs**: add a description to a role in **Careers** and it is sent to Google Jobs automatically. Roles without a description are not sent.
- **Two-step sign-in** (My account): authenticator app (Google or Microsoft Authenticator) with 10 backup codes, or a code by email. "Trust this browser for 30 days" is optional. Five wrong codes lock sign-in for 15 minutes. Admins can require it for everyone and reset a person's two-step under **Security & backups**. A sign-in from a new browser sends an alert email.
- **Backups** (Security & backups): every night at 2 AM IST the site saves every table and uploaded file to a zip in `App_Data/backups` (newest 14 kept). Download one any time. Keep your host's SQL Server backups on as well.

**Timers need the site awake.** Daily summary, backups, automatic emails and newsletter sending run inside the site. On IIS set the app pool **Idle Time-out** to 0 and **Start Mode** to AlwaysRunning (in Plesk ask GenX if you cannot change it). Otherwise they run on the first visit after the scheduled time.

## Sales tools (v11)

Everything below is managed in the admin. The database tables are added automatically on first run (App_Data/sql/sales.*.sql).

### Before going live: checklist
1. **Prices are examples.** Check every plan in **Website content > Plans & prices** and **Plan builder extras**, then switch on **Pricing & popups > Show prices to visitors**. Until then visitors see "Price on request" (signed-in staff see a preview).
2. **AI assistant:** add an Anthropic API key in **AI assistant** (and set a monthly spend limit in the Anthropic console).
3. **Payments:** add Razorpay keys and the webhook in **Integrations** (test keys first).
4. **Spam protection:** add Cloudflare Turnstile keys in **Security & backups > Spam protection**.
5. **Privacy policy:** add a line about the AI chat (questions are processed by Anthropic and kept for quality review), the CAPTCHA provider, and that prices may be shown in your currency based on your country.
6. Change **AdminSetupKey** in Web.config (this repository is public).

### Pricing, plans and the plan builder (/pricing)
- **Plans** (Website content > Plans & prices): per service, a name, price in rupees, Fixed or From, one-time / monthly / yearly / hourly, optional set-up fee and minimum months, features, timeline, "most popular". Plans show on **/pricing** and on each **service page**.
- **Extras** (Website content > Plan builder extras): items with a quantity (extra pages, blog posts, developer hours…).
- **Plan builder:** visitors add plans and extras, type an offer code, and see an instant estimate (first payment, then monthly, with the tax note). "Get my quote" emails them the estimate, alerts the team, and creates or updates a **lead** (source: Plan builder) with the quote attached. All prices are recalculated on the server.
- **Currency:** prices are stored in INR. Visitors see their currency (from Cloudflare's country header or the location cache, else their browser language) and can switch. Rates come free and daily from open.er-api.com (backup: frankfurter.app); fix any rate by hand in **Pricing & popups**. Foreign prices are rounded to tidy figures (199, 1,249) unless you turn that off.
- **Turning a quote into a proposal:** open the lead and click **Make proposal** next to the quote.

### Offers and popups
- **Offers** (Website content > Offers): headline, details, badge, discount (percent, amount, or none), optional coupon code, start and end dates, the pages they show on (`/`, `/pricing`, `/services/*`, `*`) and services they apply to. Each can show as a **banner**, a once-per-visitor **popup**, and in the **chatbot**. Codeless offers apply automatically in the plan builder; the best one wins. Offers outside their dates disappear on their own.
- **Free website audit popup** (Pricing & popups): on exit intent (desktop), after a delay, or after scrolling; at most once every N days; never on excluded pages; waits until the cookie choice is made. It hands the visitor to /website-audit with the audit already running.

### AI assistant
- Admin > **AI assistant**: add one or more Anthropic API keys (stored encrypted). They are tried top to bottom; a key that is rate limited, out of credit, rejected or busy **rests** automatically and the next key answers. With no working key the chat uses its built-in answers, so visitors always get a reply.
- The assistant answers only about Yenetch, using Website content, plans, prices (only when public), offers, products, case studies and FAQs. It does not invent prices, and when someone wants a quote or call it hands over to the chat's lead form. Extra instructions, a per-visitor limit and a daily cap are on the same page, with recent conversations (linked to the lead they became).
- Model: Claude Opus 5.5 by default (Sonnet 5.5 and Haiku 4.5 are cheaper options per key). The site calls the Messages API directly over HTTPS; the official C# SDK needs a newer .NET than this site runs on.

### Lead scoring
- Every lead gets a **score from 0 to 100**: contact quality (company email, phone), how it came in (booking, plan builder, referral…), budget, website behaviour (pages, visits, time, pricing and case-study views), proposal opened/accepted, and minus points when it goes quiet. Won = 100, Lost/Junk = 0.
- **Lead scoring** page: points per signal, Hot/Warm thresholds, automatic priority (a priority set by hand is kept). The lead list has a sortable **Score** column; each lead shows why it scored what it did. Scores update on every change and nightly.

### Proposals and payments
- **Proposals** (Sales menu): create from a lead (**Create proposal**) or a plan-builder quote. Lines from your plans and extras (converted to the proposal's currency) or typed by hand; discount, tax (GST), deposit due on acceptance, validity, terms and an optional client portal link.
- **Send by email** with **To, CC and BCC**; addresses are suggested as you type (the client, your team, and people you emailed before). Replies go to the sender.
- The client opens a private link (**/proposal/…**, not indexed), can **download a PDF** (print), **accept** with their typed name or decline with a reason. You get an email when they first open it, accept or decline; the sidebar counts proposals that were opened but not answered. Accepting moves the lead to **Won**.
- **Razorpay:** with keys in **Integrations**, a payment link is created when the client accepts (or by you, for any amount), shown on the proposal and in the confirmation email. Payment is confirmed by the Razorpay webhook (`/api/razorpay`, event `payment_link.paid`) and by Razorpay's signed return from checkout; the client gets a receipt. Payments received another way can be recorded by hand.

### Integrations (billing / project app)
- **Client portal address** with `{email}`, `{name}`, `{company}`, `{proposal}`, `{lead}`: shown to clients after they accept and in receipts, so billing, tasks and progress are tracked in your other app.
- **Webhooks:** JSON POSTs to your app for `lead.created`, `quote.created`, `proposal.sent`, `proposal.viewed`, `proposal.accepted`, `proposal.declined` and `payment.received`, signed with `X-Yenetch-Signature: sha256=<HMAC-SHA256 of the body with your secret>`. A log of the last calls and a test button are on the page.
- WhatsApp Business API: a placeholder card; click-to-WhatsApp keeps working until the API is connected.

### Blog authors (E-E-A-T)
Google ranks advice better when it can see that real, experienced people wrote it ("experience, expertise, authoritativeness and trust"). To use it:
1. **Website content > Blog authors > Add**: name, page address, job title, a one-or-two-sentence bio, a longer profile, a square photo, topics, and LinkedIn/X/website links.
2. In each blog post, choose that author in the **Author** field (names are suggested).
3. The article then shows the author's photo and a link to **/blog/author/their-address** (a profile page listing their articles), an author box at the end, and tells Google who wrote it (schema.org Person). Author pages are in the sitemap.
A team account ("Yenetch Editorial Team") is included for articles written together.

### Speed and images
- Uploaded JPG/PNG images are turned upright, stripped of camera/GPS data, limited to 2000px and re-compressed; copies at 480, 960 and 1600px are made, plus **WebP** copies made by the admin's browser (about a third smaller). Pages use them with `srcset`, so phones download small files. Images below the first screen load lazily, including images inside articles.
- The content the scripts need (chatbot, finder, builder) is now one cached file, `/api/site-data?v=…`, instead of ~60 KB repeated inside every page.

### Security
- **Contact details:** email addresses, phone numbers and WhatsApp links are encoded or broken up with hidden decoy text in the page HTML, and turned back into normal clickable links by the site script, so visitors see nothing different while address-harvesting bots read nothing useful. Structured data keeps the phone number (Google uses it for local results) but no longer lists the email.
- **Spam:** hidden honeypot fields, a minimum fill time, rate limits per visitor on every form, the chat and the APIs, cross-site request checks, and optional **Cloudflare Turnstile** or **reCAPTCHA v3**.
- **Content:** blog and rich-text HTML is cleaned with an allow-list (no scripts, event handlers or javascript: links; YouTube/Vimeo/Maps embeds allowed). All database queries use parameters.
- **Headers and cookies:** Content-Security-Policy baseline (no framing by other sites, no plugins, forms post only to this site), HttpOnly + SameSite cookies (HTTPS-only in Release builds), no server version headers, TRACE blocked, `.sql/.log/.bak` never served.
- Secrets (API keys, Razorpay secret, webhook and CAPTCHA secrets, SMTP password) are encrypted with the machine key. **If you move the site to a new server, set a fixed `<machineKey>` in Web.config first or re-enter those keys.**

### Language versions (recommendation, not built yet)
The approach large companies use: a separate address per language (`/hi/…`), each with human-reviewed translations stored as content, `hreflang` links between versions, translated titles and descriptions, and a language switcher that never redirects automatically. Avoid auto-translate widgets: Google does not index them and quality suffers. This touches every page template, so it is best done as its own step once you choose the languages.

### Testing locally
`tools/csproj_sync.py` adds new files to the Visual Studio project. A GitHub Actions workflow (`.github/workflows/build.yml`) compiles the solution on Windows on every push.

## Editing content
All business content lives in the SQL database and is edited in **/admin → Website content** (Admins and Managers). Changes show on the site straight away, with no rebuild or file edits.

| Section | What you can change |
|---|---|
| Company and contact | name, phone, WhatsApp, email, offices and addresses, social media links, stats, values |
| Privacy policy, Terms | full page text in a visual editor |
| Team | leadership (name, role, what they do, photo upload, LinkedIn), squads |
| Services, Products | every card and detail page, including pricing, timelines and page copy |
| Case studies, Clients, Testimonials, Industries | add, edit, reorder, hide; client logos (every logo fitted to the same 140x44 box, greyscale until hovered; the name shows when there is no logo) and reviewer photos (the initial shows when there is none) |
| Careers | open roles, perks, hiring steps |
| Process, FAQs, Solution finder | steps, questions and answers, finder rules |
| Page titles (SEO) | Google title and description for any page address |

Every list supports add, edit, delete, reorder (up and down) and show/hide. Uploaded images go to `/uploads/content/`. Blog posts are under **Blog** (see above).

**How it works:** content is stored as one row per item in `CmsItems`. The first time the site runs against an empty database it copies everything from `assets/data/*.json`, `App_Data/seed/` and the built-in blog articles into the database; after that the files are only a fallback if the database cannot be reached. `ContentSource` in Web.config: `Database` (default) or `Files` (ignore the database).

**Still part of the page design** (change in `tools/sitegen`): section headings and marketing copy, the three service pillars as page structure, which case studies each page features, and the mosaic photos.

**Photos:** built-in stock photo keys map to files in `assets/img/stock/photos.json`; any image field also accepts an uploaded image or a full address. With `StockCdn=true` in Web.config, stock photos load full-resolution from the Unsplash CDN.

## Mobile dock and back-to-top
- On phones and tablets (900px and below) a frosted dock sits at the bottom: Home, Services, a centre menu button, Ask (chatbot) and Contact. Contact opens a blurred sheet with Call, WhatsApp, Email, Let's talk, Solution finder and Ask Yenetch. Phone, WhatsApp and email come from the database.
- The back-to-top button is a rocket: it appears after scrolling, shakes with a small flame on hover, and on click ignites with smoke and sparks, then launches while the page scrolls to the top. Visitors who prefer reduced motion get an instant jump.

## Changing layout (sitegen)
Pages are rendered by `tools/sitegen`. Each template produces both the static preview and the data-bound `.aspx` markup, so the two stay identical.

```
python3 tools/build.py            # writes .aspx, .designer.cs, Site.Master and preview/
python3 tools/build.py preview    # preview only
```

The script overwrites the `.aspx`, `.designer.cs` and `Site.Master` files, plus code-behinds for simple static pages. Code-behinds with logic (details, Blog, Contact, NotFound) are hand-written and left alone. If you prefer to edit `.aspx` files directly, stop using the script.

## Design system
- Type: Inter Tight for headlines, Inter for text.
- Colour: black and white, with Tech Blue `#0066FF` as the only highlight.
- Motion: CSS scroll-driven animations (fly in from the sides and from below, stacking case-study cards, word-by-word reveals, a pinned product rail) and count-up numbers. Browsers without support get a static page, and motion is off for visitors who choose reduced motion.
- Visuals: product screens, dashboards and the animated Y-Box hero mark are HTML, CSS and SVG; photography is from Unsplash (credits in `assets/img/stock/CREDITS.md`).

## SEO
Unique title and description per page, canonical and Open Graph tags, JSON-LD on every template, breadcrumbs, `/sitemap.xml` and `robots.txt`.

## Content rules
- CRED, Byju's and Unacademy are excluded everywhere.
- Case-study numbers come from the company profile PDF and should be confirmed before launch.
