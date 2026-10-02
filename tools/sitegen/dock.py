"""Mobile dock, contact sheet and back-to-top rocket.

Emitted once per page by tools/build.py, after the footer, in both the preview wrapper and Site.Master.
Behaviour lives in Yenetch.Web/assets/js/dock.js; styles in the "18. Mobile dock & rocket" block of site.css.
Contact details bind to the database on the live site through the helpers in ui (co, tel_href, wa_href, mail_href).
"""
from .ui import L, co, tel_href, wa_href, mail_href, icon, social_icon, mark

_I = {
    "home": '<path d="M4 11.2 12 4.5l8 6.7"/><path d="M6.2 9.6v9.9h4.3v-5.4h3v5.4h4.3V9.6"/>',
    "grid": '<rect x="4" y="4" width="6.6" height="6.6" rx="1.9"/><rect x="13.4" y="4" width="6.6" height="6.6" rx="1.9"/>'
            '<rect x="4" y="13.4" width="6.6" height="6.6" rx="1.9"/><rect x="13.4" y="13.4" width="6.6" height="6.6" rx="1.9"/>',
    "ask": '<path d="M4 7a3 3 0 0 1 3-3h10a3 3 0 0 1 3 3v7a3 3 0 0 1-3 3h-6.5L6 20.5V17H7a3 3 0 0 1-3-3z"/>'
           '<path d="M12 7.3l.95 2.25 2.25.95-2.25.95L12 13.7l-.95-2.25-2.25-.95 2.25-.95z" fill="currentColor" stroke="none"/>',
    "card": '<rect x="3.5" y="5" width="17" height="14" rx="3.2"/><circle cx="9" cy="10.8" r="2.1"/>'
            '<path d="M5.9 16.2c.6-1.5 1.7-2.3 3.1-2.3s2.5.8 3.1 2.3M14.6 10h3.4M14.6 13.4h2.4"/>',
    "pen": '<path d="M4.5 19.5h4l10.2-10.2a2.2 2.2 0 0 0-3.1-3.1L5.4 16.4z"/><path d="m13.8 8 2.9 2.9"/>',
    "compass": '<circle cx="12" cy="12" r="8.6"/><path d="m15.6 8.4-2.1 5.1-5.1 2.1 2.1-5.1z"/>',
    "close": '<path d="M7 7l10 10M17 7 7 17"/>',
}


def _ico(name):
    return (f'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" '
            f'stroke-linejoin="round" aria-hidden="true">{_I[name]}</svg>')


def dock():
    """iOS-style floating bottom bar, shown on phones and small tablets only."""
    return f'''<nav class="dock" aria-label="Quick actions">
  <a class="dock__item" href="{L("/")}" data-dock="home">{_ico("home")}<span>Home</span></a>
  <a class="dock__item" href="{L("/services")}" data-dock="services">{_ico("grid")}<span>Services</span></a>
  <button class="dock__menu" type="button" aria-controls="nav-links" aria-expanded="false" aria-label="Open menu"><span class="dock__bars" aria-hidden="true"><i></i><i></i><i></i></span></button>
  <button class="dock__item" type="button" data-chat="" data-dock="ask" aria-label="Ask Yenetch, open the assistant">{_ico("ask")}<span>Ask</span></button>
  <button class="dock__item" type="button" data-dock="contact" aria-haspopup="dialog" aria-controls="csheet" aria-expanded="false">{_ico("card")}<span>Contact</span></button>
</nav>'''


def contact_sheet():
    """Frosted square sheet with every way to reach Yenetch."""
    def tile(i, tag, attrs, cls, glyph, label, sub):
        return (f'<{tag} class="ctile2 ctile2--{cls}" style="--i:{i}" {attrs}><span class="ctile2__ico">{glyph}</span>'
                f'<b>{label}</b><small>{sub}</small></{tag}>')
    tiles = "".join([
        tile(0, "a", f'href="{tel_href()}"', "call", icon("phone"), "Call", co("phone")),
        tile(1, "a", f'href="{wa_href("Hi Yenetch")}" target="_blank" rel="noopener"', "wa", social_icon("whatsapp"), "WhatsApp", "Chat now"),
        tile(2, "a", f'href="{mail_href()}"', "mail", icon("mail"), "Email", co("email")),
        tile(3, "a", f'href="{L("/contact")}"', "talk", _ico("pen"), "Let&#39;s talk", "Send a brief"),
        tile(4, "a", f'href="{L("/solution-finder")}"', "finder", _ico("compass"), "Solution finder", "4 quick questions"),
        tile(5, "button", 'type="button" data-chat=""', "ask", mark(), "Ask Yenetch", "Instant answers"),
    ])
    return f'''<div class="csheet" id="csheet" role="dialog" aria-modal="true" aria-labelledby="csheet-title" hidden>
  <div class="csheet__scrim" data-csheet-close></div>
  <div class="csheet__panel">
    <div class="csheet__head"><div><h2 id="csheet-title">Talk to Yenetch</h2><p>Real people. Replies within one working day.</p></div>
      <button class="csheet__close" type="button" data-csheet-close aria-label="Close contact options">{_ico("close")}</button></div>
    <div class="csheet__grid">{tiles}</div>
  </div>
</div>'''


def rocket():
    """Back-to-top rocket: inline SVG ship with a layered, flickering flame."""
    return '''<button class="rk" type="button" aria-label="Back to top" data-rocket>
  <span class="rk__pad" aria-hidden="true"><svg class="rk__ring" viewBox="0 0 60 60"><circle cx="30" cy="30" r="28.5"/></svg></span>
  <span class="rk__ship" aria-hidden="true"><svg class="rk__svg" viewBox="0 0 48 100" focusable="false">
    <defs>
      <linearGradient id="rkBody" x1="0" x2="1"><stop offset="0" stop-color="#C9CCD4"/><stop offset=".32" stop-color="#FFFFFF"/><stop offset=".62" stop-color="#EEF0F4"/><stop offset="1" stop-color="#9CA1AD"/></linearGradient>
      <linearGradient id="rkCap" x1="0" x2="1"><stop offset="0" stop-color="#0047B8"/><stop offset=".38" stop-color="#2F86FF"/><stop offset="1" stop-color="#003C9E"/></linearGradient>
      <linearGradient id="rkFin" x1="0" x2="1"><stop offset="0" stop-color="#3A3A40"/><stop offset="1" stop-color="#0A0A0B"/></linearGradient>
      <linearGradient id="rkNoz" x1="0" x2="1"><stop offset="0" stop-color="#4A4A52"/><stop offset=".45" stop-color="#8A8A94"/><stop offset="1" stop-color="#1C1C1F"/></linearGradient>
      <radialGradient id="rkGlass" cx=".36" cy=".34" r=".75"><stop offset="0" stop-color="#9CC4FF"/><stop offset=".45" stop-color="#0066FF"/><stop offset="1" stop-color="#002E7A"/></radialGradient>
      <linearGradient id="rkF1" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFD66B"/><stop offset=".45" stop-color="#FF7A1A"/><stop offset="1" stop-color="#FF3D00" stop-opacity="0"/></linearGradient>
      <linearGradient id="rkF2" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFF7D6"/><stop offset=".55" stop-color="#FFC43D"/><stop offset="1" stop-color="#FF8A00" stop-opacity="0"/></linearGradient>
      <linearGradient id="rkF3" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFFFFF"/><stop offset=".7" stop-color="#E3F0FF"/><stop offset="1" stop-color="#9CC4FF" stop-opacity="0"/></linearGradient>
      <radialGradient id="rkGlow"><stop offset="0" stop-color="#FFB547" stop-opacity=".85"/><stop offset="1" stop-color="#FF6A00" stop-opacity="0"/></radialGradient>
      <clipPath id="rkClip"><path d="M24 6C32.5 14 35 25 35 38v22c0 3-2 5-5 5H18c-3 0-5-2-5-5V38c0-13 2.5-24 11-32z"/></clipPath>
    </defs>
    <g class="rk__flame">
      <ellipse class="rk__glow" cx="24" cy="76" rx="13" ry="9" fill="url(#rkGlow)"/>
      <path class="rk__f1" d="M15.8 71c-.6 9 3.4 17.5 8.2 28 4.8-10.5 8.8-19 8.2-28z" fill="url(#rkF1)"/>
      <path class="rk__f2" d="M18.2 71c-.2 7 2.6 13 5.8 20.5 3.2-7.5 6-13.5 5.8-20.5z" fill="url(#rkF2)"/>
      <path class="rk__f3" d="M20.6 71c0 4.6 1.5 8.6 3.4 13 1.9-4.4 3.4-8.4 3.4-13z" fill="url(#rkF3)"/>
    </g>
    <path d="M13 44c-5 3-8 8-8 14v8c0 1.2 1.1 1.7 2.1 1L13 62z" fill="url(#rkFin)"/>
    <path d="M35 44c5 3 8 8 8 14v8c0 1.2-1.1 1.7-2.1 1L35 62z" fill="url(#rkFin)"/>
    <path d="M17.2 65h13.6l1.6 6.2H15.6z" fill="url(#rkNoz)"/>
    <g clip-path="url(#rkClip)">
      <rect x="0" y="0" width="48" height="70" fill="url(#rkBody)"/>
      <rect x="0" y="0" width="48" height="18.5" fill="url(#rkCap)"/>
      <rect x="0" y="18.5" width="48" height="1.2" fill="#fff" opacity=".7"/>
      <rect x="0" y="55" width="48" height="2" fill="#1C1C1F" opacity=".85"/>
      <rect x="0" y="57" width="48" height="8" fill="#0A0A0B" opacity=".06"/>
      <path d="M19 10c-2.4 5-3.3 12-3.3 20v26" stroke="#fff" stroke-width="1.6" stroke-linecap="round" fill="none" opacity=".75"/>
    </g>
    <circle cx="24" cy="33" r="6.3" fill="#1C1C1F"/>
    <circle cx="24" cy="33" r="4.7" fill="url(#rkGlass)"/>
    <ellipse cx="22.3" cy="31.2" rx="1.7" ry="1.1" fill="#fff" opacity=".85" transform="rotate(-35 22.3 31.2)"/>
    <path d="M22.6 52h2.8v15.2a1.4 1.4 0 0 1-2.8 0z" fill="url(#rkFin)"/>
  </svg></span>
</button>'''


def chrome():
    """Everything this module adds to a page, in DOM order."""
    return rocket() + "\n" + contact_sheet() + "\n" + dock()
