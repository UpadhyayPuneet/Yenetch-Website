"""Dual-mode binding: the same template renders static HTML for the preview and Web Forms markup for the site.

Templates never touch raw data directly. They read values through two small wrappers:
  P  (preview)  wraps real JSON data and renders escaped text.
  X  (aspx)     wraps a C# expression and renders <%: expr %> (page scope) or <%#: expr %> (inside a Repeater).
JSON keys are camelCase; the C# model uses the same names in PascalCase, so `svc.summary` becomes `Svc.Summary`.
"""
import html

MODE = "preview"          # set by build.py: "preview" or "aspx"
esc = lambda s: html.escape("" if s is None else str(s), quote=True)
pascal = lambda k: k[:1].upper() + k[1:]


class P:
    def __init__(self, v): self.v = v
    def __getattr__(self, k):
        if k.startswith("__"): raise AttributeError(k)
        return P(self.v.get(k) if isinstance(self.v, dict) else None)
    def __getitem__(self, k): return self.__getattr__(k)
    def __str__(self): return esc(self.v)
    def __bool__(self): return bool(self.v)
    @property
    def raw(self): return "" if self.v is None else str(self.v)


class X:
    def __init__(self, expr, item=False): self.expr, self.item = expr, item
    def __getattr__(self, k):
        if k.startswith("__"): raise AttributeError(k)
        return X(f"{self.expr}.{pascal(k)}", self.item)
    def __getitem__(self, k): return self.__getattr__(k)
    def __str__(self): return f"<%#: {self.expr} %>" if self.item else f"<%: {self.expr} %>"
    @property
    def raw(self): return f"<%# {self.expr} %>" if self.item else f"<%= {self.expr} %>"


def page(name, data):
    """A page-level object: real data in the preview, a code-behind field (e.g. `Svc`) in aspx."""
    return P(data) if MODE == "preview" else X(name)


def fmt(v, py, cs):
    """Formatted value. py(value) for the preview, cs(expr) -> C# expression for aspx."""
    if isinstance(v, P):
        return esc(py(v.v))
    x = X(cs(v.expr), v.item)
    return str(x)


def attr_raw(v, py, cs):
    """Like fmt, but unescaped (for numbers inside style attributes and similar)."""
    if isinstance(v, P):
        return str(py(v.v))
    return X(cs(v.expr), v.item).raw


def each(src, fn, typ="System.String", py_list=None):
    """Loop. src is a P list, an X expression (a List<T> on the C# side), or a (py_list, cs_expr) tuple.
    fn(item, index) returns markup."""
    if isinstance(src, tuple):
        py_list, cs = src
        src = P(py_list) if MODE == "preview" else X(cs)
    if isinstance(src, P):
        return "".join(fn(P(x), P(i)) for i, x in enumerate(src.v or []))
    item = X("Item", True)
    body = fn(item, X("Container.ItemIndex", True))
    q = '"' if "'" in src.expr else "'"  # quotes that do not clash with string literals in the expression
    return (f'<asp:Repeater runat="server" ItemType="{typ}" DataSource={q}<%# {src.expr} %>{q}><ItemTemplate>'
            f'{body}</ItemTemplate></asp:Repeater>')


def when(cond, html_, cs=None):
    """Conditional block. cond is a P (truthiness) or an X; cs(expr) builds the C# bool (default: not empty)."""
    if isinstance(cond, P):
        return html_ if cond.v else ""
    if isinstance(cond, bool):
        return html_ if cond else ""
    test = cs(cond.expr) if cs else f"!string.IsNullOrEmpty(Convert.ToString({cond.expr}))"
    return f"<asp:PlaceHolder runat=\"server\" Visible='<%# {test} %>'>{html_}</asp:PlaceHolder>"


def has(v):
    """Condition for 'list has items'."""
    if isinstance(v, P):
        return v
    return (v, lambda e: f"({e} != null && {e}.Count > 0)")


def when_any(v, html_):
    if isinstance(v, P):
        return when(v, html_)
    return when(v, html_, cs=lambda e: f"({e} != null && {e}.Count > 0)")


def is_preview():
    return MODE == "preview"
