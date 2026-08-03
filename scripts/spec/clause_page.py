#!/usr/bin/env python3
"""Resolve a CLAUSE to the printed page that carries it — the inverse the de-paging left missing.

    python scripts/spec/clause_page.py 14.9.1              # -> folio + PDF page
    python scripts/spec/clause_page.py 14.9.1 13.18.40     # several
    python scripts/spec/clause_page.py 14.9.1 --render E:/Temp/gc0   # and render the page(s) to PNG

⛔ WHY THIS EXISTS. Every figure/geometry tool in this directory runs PAGE → CLAUSE: it walks the PDF and asks
what clause a page carries. Nothing ran the other way, because the rule catalog used to stamp a `page` on every
rule and callers just read it. **De-paging removed that field** (plan §0: "Rows are CLAUSE-keyed (no page
field)"), and anything that still asks for it is broken — `.claude/workflows/spec-grammar-conformance.js` hands
each of its agents a command that does exactly that, so the whole grammar↔spec audit could not run.

⚠ IT FAILS LOUDLY ON A MISS, and that is the point. The workflow's dead lookup did not raise — it filtered on
`r['section'] == '14.9.1'` while every catalog row is a SUB-clause (`14.9.1.2`, `14.9.1.3`, …), so it printed
NOTHING and the agent proceeded with no page and no rules. A resolver that returns empty for a clause that
plainly exists is the same defect this repo keeps closing: a missing observation read as a negative one.

Reuses `render_figure.headings()` rather than re-implementing the bold-numbered-heading test (one rule, one
place); the folio is the PDF page number minus the 30 pages of front matter, which plan §0 records.
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))

#: Printed folio = PDF page − FRONT_MATTER (plan §0: "the `#page-N` anchors remain PDF sequence, which runs
#: folio + 30"). Kept as a named constant because it is a fact about this document, not a magic number.
FRONT_MATTER = 30


def find_pdf() -> pathlib.Path:
    private = REPO / "specs-private"
    pdfs = sorted(private.glob("*.pdf")) if private.is_dir() else []
    if not pdfs:
        sys.exit("!! the licensed PDF is absent — run: git submodule update --init specs-private")
    return pdfs[0]


def clause_key(c: str) -> tuple:
    return tuple(int(p) if p.isdigit() else 0 for p in c.split("."))


def resolve(clauses: list[str]) -> dict[str, tuple[int, int, str]]:
    """clause -> (pdf_page, folio, the heading text found). Missing clauses are simply absent from the result."""
    import fitz                                                    # PyMuPDF, as the sibling tools use it
    import render_figure                                           # noqa: E402  (path injected above)

    doc = fitz.open(find_pdf())
    wanted = {c: re.compile(rf"^{re.escape(c)}(?:\s|$)") for c in clauses}
    found: dict[str, tuple[int, int, str]] = {}
    for pno in range(doc.page_count):
        if len(found) == len(wanted):
            break
        page = doc[pno]
        try:
            heads = render_figure.headings(page)
        except Exception:                                          # noqa: BLE001 — a page we cannot parse is not fatal
            continue
        for _y, text in heads:
            for c, pat in wanted.items():
                if c not in found and pat.match(text):
                    found[c] = (pno + 1, pno + 1 - FRONT_MATTER, text)
    doc.close()
    return found


def main(argv: list[str]) -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("clauses", nargs="+", help="clause numbers, e.g. 14.9.1 13.18.40")
    ap.add_argument("--render", type=pathlib.Path,
                    help="also render each resolved page to PNG in this directory (via page_workunit.py)")
    ap.add_argument("--dpi", type=int, default=300)
    a = ap.parse_args(argv)

    found = resolve(a.clauses)
    missing = [c for c in a.clauses if c not in found]

    for c in sorted(a.clauses, key=clause_key):
        if c in found:
            pdf_page, folio, text = found[c]
            print(f"  {c:<14} PDF page {pdf_page:<5} printed folio {folio:<5} {text[:60]}")
    if missing:
        # ⛔ LOUD. A clause that does not resolve means the caller is about to work from no page at all.
        print(f"\n⛔ {len(missing)} clause(s) DID NOT RESOLVE to a printed page: {', '.join(missing)}",
              file=sys.stderr)
        print("   A clause heading is a bold numbered line in the body. If the clause exists in the catalog but "
              "not here,\n   the heading test or the clause number is wrong — do NOT proceed without a page.",
              file=sys.stderr)
        return 1

    if a.render:
        import subprocess
        pages = [str(found[c][0]) for c in a.clauses]
        a.render.mkdir(parents=True, exist_ok=True)
        cmd = [sys.executable, str(HERE / "page_workunit.py"), *pages, "--out", str(a.render), "--dpi", str(a.dpi)]
        print(f"\nrendering: {' '.join(cmd)}")
        return subprocess.run(cmd, cwd=REPO).returncode
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
