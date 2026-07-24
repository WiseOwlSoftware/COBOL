#!/usr/bin/env python3
"""Generate Obsidian reference notes for the COBOL.NET bound-tree (IR) node types.

Parses the bound-node C# declarations under src/Cobol.Net.Compiler/Binding/Bound/ and emits one
markdown note per type into kb/Reference/Bound/ (a gitignored build output — regenerated, never
hand-edited). Each note carries the type's `///` <summary>, base type (as a wiki-link), and source
location. Also emits an index note with a documentation-debt list (types missing a `///` summary).

This is the pilot slice of the code-reference layer (docs/DOC_INDEX.md "Derived knowledge base").
Run:  python scripts/gen-vault-reference.py
"""
import os, re, glob, io, html, shutil

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BOUND_DIR = os.path.join(ROOT, "src", "Cobol.Net.Compiler", "Binding", "Bound")
OUT = os.path.join(ROOT, "kb", "Reference", "Bound")

DECL = re.compile(
    r'^\s*(?:(?:public|internal|private|protected|abstract|sealed|partial|static|file)\s+)+'
    r'(record|class)\s+([A-Z][A-Za-z0-9_]*)')
BASE_CTOR = re.compile(r'\)\s*:\s*([A-Za-z_][A-Za-z0-9_.]*)')
BASE_PLAIN = re.compile(r'\b(?:record|class)\s+[A-Za-z0-9_]+(?:<[^>]*>)?\s*:\s*([A-Za-z_][A-Za-z0-9_.]*)')


def clean_summary(docblock: str) -> str:
    m = re.search(r'<summary>(.*?)</summary>', docblock, re.S)
    if not m:
        return ""
    s = m.group(1)
    s = re.sub(r'<see\s+cref="[^"]*?([A-Za-z0-9_]+)"\s*/>', r'`\1`', s)
    s = re.sub(r'<see\s+cref="[^"]*"\s*>(.*?)</see>', r'`\1`', s)
    s = re.sub(r'<paramref\s+name="([^"]*)"\s*/>', r'`\1`', s)
    s = re.sub(r'<[^>]+>', '', s)          # strip any remaining tags
    s = html.unescape(re.sub(r'\s+', ' ', s)).strip()
    return s


def base_of(lines, i):
    window = "\n".join(lines[i:i + 10])
    m = BASE_CTOR.search(window) or BASE_PLAIN.search(window)
    return m.group(1) if m else None


def summary_of(lines, i):
    j = i - 1
    while j >= 0 and lines[j].strip().startswith('['):   # skip attribute lines
        j -= 1
    doc = []
    while j >= 0 and lines[j].strip().startswith('///'):
        doc.append(lines[j].strip()[3:].strip())
        j -= 1
    doc.reverse()
    return clean_summary("\n".join(doc))


def collect():
    types = {}
    for f in sorted(glob.glob(os.path.join(BOUND_DIR, "*.cs"))):
        rel = os.path.relpath(f, ROOT).replace("\\", "/")
        lines = io.open(f, encoding="utf-8").read().splitlines()
        for i, line in enumerate(lines):
            m = DECL.match(line)
            if not m:
                continue
            name = m.group(2)
            if name in types:            # first declaration wins
                continue
            types[name] = dict(name=name, kind=m.group(1), base=base_of(lines, i),
                               summary=summary_of(lines, i), src=rel, line=i + 1)
    return [types[k] for k in sorted(types)]


def base_link(base):
    if base and re.match(r'^Bound', base):
        return f"[[kb/Reference/Bound/{base}|{base}]]"
    return f"`{base}`" if base else "—"


NOTE = """---
title: {name}
kind: {kind}
base: {base}
source: {src}
sourceLine: {line}
generated: true
tags:
  - cobolsharp
  - ir
  - reference
  - generated
---

# `{name}`

> ⚙ **Generated** from `{src}` (line {line}) — do not edit; regenerate with `scripts/gen-vault-reference.py`.

**Kind:** {kind} · **Base:** {baselink}

{summary}

## See also
- [[kb/Spec/Lookup/IR Mapping]] — the full per-node table (semantics · phase · runtime).
- [[kb/IR/Node Types]] · [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]]
"""


def main():
    types = collect()
    if os.path.isdir(OUT):
        shutil.rmtree(OUT)
    os.makedirs(OUT)

    debt = []
    for t in types:
        summ = t["summary"] or "> ⚠ No `///` summary in the source — documentation debt."
        if not t["summary"]:
            debt.append(t)
        io.open(os.path.join(OUT, t["name"] + ".md"), "w", encoding="utf-8", newline="\n").write(
            NOTE.format(name=t["name"], kind=t["kind"], base=(t["base"] or "—"),
                        baselink=base_link(t["base"]), src=t["src"], line=t["line"], summary=summ))

    # index / MOC
    idx = io.StringIO()
    idx.write("---\ntitle: Bound Node Reference (generated)\ngenerated: true\n"
              "tags:\n  - cobolsharp\n  - ir\n  - reference\n  - generated\n  - moc\n---\n\n")
    idx.write("# Bound Node Reference (generated)\n\n")
    idx.write(f"> ⚙ Generated from `src/Cobol.Net.Compiler/Binding/Bound/*.cs` by "
              f"`scripts/gen-vault-reference.py` — **{len(types)} types**. Build output (gitignored); "
              f"do not edit. Companion to the hand-curated [[kb/Spec/Lookup/IR Mapping]].\n\n")
    idx.write(f"## Types ({len(types)})\n")
    for t in types:
        s = (" — " + t["summary"]) if t["summary"] else ""
        idx.write(f"- [[kb/Reference/Bound/{t['name']}|{t['name']}]]{s}\n")
    idx.write(f"\n## Documentation debt ({len(debt)} without a `///` summary)\n")
    if debt:
        for t in debt:
            idx.write(f"- `{t['name']}` — `{t['src']}:{t['line']}`\n")
    else:
        idx.write("- none — every type has a `///` summary. 🎉\n")
    io.open(os.path.join(OUT, "_Index.md"), "w", encoding="utf-8", newline="\n").write(idx.getvalue())

    print(f"generated {len(types)} type notes + _Index into kb/Reference/Bound/")
    print(f"documentation debt (no ///): {len(debt)}")
    print("sample:", ", ".join(t["name"] for t in types[:6]))
    withbase = sum(1 for t in types if t["base"])
    print(f"types with a resolved base: {withbase}/{len(types)}")


if __name__ == "__main__":
    main()
