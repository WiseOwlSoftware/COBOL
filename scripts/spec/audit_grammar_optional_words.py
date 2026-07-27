#!/usr/bin/env python3
"""Cross-reference MEASURED underlining against the grammar's keyword spelling, to find optional words we require.

THE DEFECT CLASS. ISO §5.2.3: an uppercase word printed WITHOUT an underline is an OPTIONAL WORD — it may be
written or omitted with no change of meaning. §5.2.2: an underlined one is REQUIRED. That distinction is carried
ENTIRELY by typography, it is the first thing a transcription loses, and a grammar written from a transcription
that lost it will reject legal COBOL.

It has already produced five real compiler bugs: `ON` in SIZE ERROR and ON EXCEPTION (DEVLOG 1041), and `FROM`,
`AFTER`, `SECONDS` and `TO` in the MCS statements (DEVLOG 1042). Each was found by hand. This automates the
search.

HOW. For each statement's printed general format:
  * the PAGE is located from the markdown's `14.9.N.2 General format` heading and its enclosing page anchor;
  * the words on that page's FIGURE lines are measured — underlined or not — from the PDF's vector rectangles;
  * the grammar rules that mention the statement's leading keyword are scanned for those same words;
  * a word printed WITHOUT an underline but appearing as a BARE (non-optional) token is reported.

WHAT IT CANNOT DECIDE, and deliberately does not pretend to. A word may be required in one construct and optional
in another — `TO` is underlined in `ADD … TO` and bare in `SEND … TO`. This tool matches at STATEMENT
granularity, so a statement whose formats disagree about a word will produce a candidate that needs a human. It
reports CANDIDATES, not findings.

⚠ CONFIRM EVERY CANDIDATE AGAINST THE RAW RECTANGLES BEFORE CHANGING THE GRAMMAR. Two-letter words are the
weak case for underline detection — a 9 pt width floor once made this toolchain accuse two CORRECT pages
(`IN` p322, `TO` p673), and acting on that would have damaged the reference document. `figure_extract.py`'s
docstring records the fix; the discipline stands regardless.

    python scripts/spec/audit_grammar_optional_words.py
    python scripts/spec/audit_grammar_optional_words.py --statement READ
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
# The grammar is SPLIT ACROSS 11 FILES — CobolParserCore.g4 plus Grammar/Core/*.g4. An earlier version of this
# script read only the root file and therefore scanned a fraction of the rules, reporting a reassuring "3
# candidates" that was an artifact of not looking. obj/antlr-lib holds build-time COPIES of the same grammars and
# must be excluded, or every rule is counted twice.
GRAMMAR_DIR = REPO / "src" / "Cobol.Net.Frontend" / "Grammar"
PDF = next(iter(sorted((REPO / "specs").glob("*COBOL*.pdf"))), None)

RESERVED = re.compile(r"^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$")
# Words that are structural in the grammar rather than COBOL keywords, or that carry their own rules.
SKIP = {"ISO", "IEC", "NOTE", "FORMAT", "FORMATS", "GENERAL", "TRUE", "FALSE", "ALL"}


def statement_pages(md_lines):
    """Statement keyword -> the PDF page its general format is printed on."""
    page, out = None, {}
    for i, l in enumerate(md_lines):
        m = re.match(r'^<a id="page-(\d+)"></a>', l.strip())
        if m:
            page = int(m.group(1))
            continue
        h = re.match(r"^#{3,6}\s+14\.9\.\d+(?:\.\d+)*\s+(.+?)\s*$", l)
        if h and page:
            title = h.group(1)
            k = re.match(r"^([A-Z][A-Z0-9-]*)\s+statement", title)
            if k:
                out.setdefault(k.group(1), page)
    return out


def grammar_rules(text):
    """Rule name -> rule body, for the parser rules."""
    rules, name, buf = {}, None, []
    for line in text.splitlines():
        if line.lstrip().startswith("//"):
            continue
        m = re.match(r"^([a-z][A-Za-z0-9_]*)\s*$", line.rstrip())
        if m:
            if name:
                rules[name] = "\n".join(buf)
            name, buf = m.group(1), []
            continue
        if name is not None:
            buf.append(line)
            if line.strip() == ";":
                rules[name] = "\n".join(buf)
                name, buf = None, []
    return rules


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--statement", help="check only this statement keyword")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs/")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    from figure_extract import extract

    doc = fitz.open(PDF)
    md = SPEC_MD.read_text(encoding="utf-8").splitlines()
    files = sorted(f for f in GRAMMAR_DIR.rglob("*.g4") if "obj" not in f.parts)
    rules = {}
    for f in files:
        for n, b in grammar_rules(f.read_text(encoding="utf-8")).items():
            rules[f"{f.name}:{n}"] = b
    print(f"grammar files scanned: {len(files)}   parser rules: {len(rules)}")
    pages = statement_pages(md)
    if args.statement:
        pages = {k: v for k, v in pages.items() if k == args.statement.upper()}
        if not pages:
            sys.exit(f"no general-format page found for statement {args.statement!r}")

    print(f"statements with a located general format: {len(pages)}\n")
    candidates = 0
    for stmt, page in sorted(pages.items()):
        by_line = collections.defaultdict(list)
        for w in extract(doc[page - 1]):
            by_line[round(w["y0"] / 3)].append(w)
        plain, underlined = set(), set()
        for row in by_line.values():
            text = " ".join(w["text"] for w in row)
            if re.search(r"ISO/IEC\s*1989", text):
                continue
            if any(re.fullmatch(r"[a-z]{2,}", w["text"].strip(" []{}.,;:()")) for w in row):
                continue
            for w in row:
                t = w["text"].strip(" []{}.,;:()")
                if len(t) > 1 and RESERVED.match(t) and t not in SKIP:
                    (underlined if w["underlined"] else plain).add(t)
        # A word underlined ANYWHERE on the page is required somewhere in this statement's formats; only words
        # that are never underlined here are unambiguous optional-word candidates.
        plain -= underlined
        if not plain:
            continue

        bodies = {n: b for n, b in rules.items() if re.search(r"\b" + stmt.replace("-", "_") + r"\b", b)}
        for rule, body in sorted(bodies.items()):
            for w in sorted(plain):
                tok = w.replace("-", "_")
                if re.search(r"(?<![A-Za-z_])" + tok + r"(?![A-Za-z_0-9])\s*(?!\?)", body):
                    print(f"  CANDIDATE  {stmt:<12} p{page:<5} {w:<12} required in rule {rule}")
                    candidates += 1
    print(f"\n{candidates} candidate(s) — CONFIRM each against the raw rectangles before editing the grammar")
    return 0


if __name__ == "__main__":
    sys.exit(main())
