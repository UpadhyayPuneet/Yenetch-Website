"""Adds new files under Yenetch.Web to Yenetch.Web.csproj so Visual Studio builds and publishes them.

    python3 tools/csproj_sync.py

C# files become <Compile> items (code-behind and designer files are linked to their page); pages, handlers, scripts,
styles, data and SQL files become <Content> items. Files already listed are left alone. Nothing is removed.
"""
import pathlib, re

ROOT = pathlib.Path(__file__).resolve().parent.parent
WEB = ROOT / "Yenetch.Web"
PROJ = WEB / "Yenetch.Web.csproj"
CONTENT_EXT = {".aspx", ".ashx", ".master", ".asax", ".js", ".css", ".json", ".sql", ".html", ".svg", ".png", ".jpg", ".webp", ".ico", ".txt", ".config"}
SKIP_DIRS = {"bin", "obj", "uploads", "packages", ".vs"}
SKIP_DATA = ("App_Data/mail", "App_Data/backups", "App_Data/attachments", "App_Data/resumes")


def rel(p):
    return str(p.relative_to(WEB)).replace("/", "\\")


def main():
    text = PROJ.read_text(encoding="utf-8-sig")
    listed = set(m.lower() for m in re.findall(r'(?:Compile|Content|None) Include="([^"]+)"', text))
    compile_items, content_items = [], []
    for p in sorted(WEB.rglob("*")):
        if not p.is_file() or any(part in SKIP_DIRS for part in p.relative_to(WEB).parts):
            continue
        r = rel(p)
        if r.replace("\\", "/").startswith(SKIP_DATA) or r.lower() in listed or r.endswith((".csproj", ".user")):
            continue
        if p.suffix == ".cs":
            m = re.match(r"(.+\.(aspx|ashx|master|asax))\.(designer\.)?cs$", p.name, re.I)
            if m and (p.parent / m.group(1)).exists():
                compile_items.append(f'    <Compile Include="{r}">\n      <DependentUpon>{m.group(1)}</DependentUpon>\n{"      <SubType>ASPXCodeBehind</SubType>" + chr(10) if not m.group(3) and m.group(2).lower() in ("aspx", "master") else ""}    </Compile>')
            else:
                compile_items.append(f'    <Compile Include="{r}" />')
        elif p.suffix.lower() in CONTENT_EXT and p.name.lower() not in ("web.config", "web.debug.config", "web.release.config"):
            content_items.append(f'    <Content Include="{r}" />')
    if not compile_items and not content_items:
        print("csproj: up to date")
        return
    block = "  <ItemGroup>\n" + "\n".join(content_items + compile_items) + "\n  </ItemGroup>\n"
    i = text.find("  <Import Project=\"$(MSBuildBinPath)")
    if i < 0:
        i = text.rfind("</Project>")
    text = text[:i] + block + text[i:]
    PROJ.write_text(text, encoding="utf-8-sig")
    print(f"csproj: added {len(content_items)} content and {len(compile_items)} code files")
    for line in content_items + compile_items:
        print("  ", re.search(r'Include="([^"]+)"', line).group(1))


if __name__ == "__main__":
    main()
