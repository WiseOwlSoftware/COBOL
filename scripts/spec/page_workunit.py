#!/usr/bin/env python3
"""Emit a self-contained PDF-vs-markdown reconciliation work unit for each printed page given.

The markdown is a transcription of the canonical PDF. Four bugs in the rule-catalog extractor traced back to
transcription artefacts (page furniture inside rules, a heading demoted to bold, split sentences), and the owner
directed a systematic reconciliation: compare the PDF against the markdown page by page and repair the markdown.

This produces the unit of that work. For PDF page N it writes:

    <out>/spec_p<N>.png       the page rendered at --dpi (default 200; 300 for dense/diagram pages) by render-spec-page.py
    <out>/page-<N>.md         the transcription's page N: from its `## Page N` heading to the next (spec_pdf.pages_of)
    <out>/page-<N>.json       {page, png, md, md_source_line, md_lines, has_figure}

`has_figure` is true when a GENERAL FORMAT is drawn on the page (sweep_figures.general_format_pages, the one reading
of "a figure page" the page-range plan reconcile_ranges.py uses too), and the last line printed names the figure pages
and the prose pages of the run, so a reconciling agent words its priority from the unit, never from an assumption.

A page the transcription does not hold is an error (exit 2), never an empty slice: an agent handed an empty `.md`
would compare the page with nothing and could report the page clean. A transcription that keeps no pages (2023
dropped its `## Page N` headings) has no slice at all: the unit is the rendered page alone (`md` null), and the run
says so.

An agent then READS the png, READS the md, and reports discrepancies. Everything it needs is on disk, so units
fan out cleanly and no agent has to re-derive the mapping (feedback_agent_dispatch: each agent its OWN input).

    python scripts/spec/page_workunit.py 95 688 --out E:/Temp/specrecon
    python scripts/spec/page_workunit.py --range 100 120 --out E:/Temp/specrecon
    python scripts/spec/page_workunit.py 95 --out E:/Temp/specrecon --edition 2002   # another edition (default 2023)
    python scripts/spec/page_workunit.py --self-test
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
import sweep_figures  # noqa: E402  (THE reading of a figure page)
RENDER = REPO / "scripts" / "render-spec-page.py"


def units(text: str, pages: list[int], edition: int) -> tuple[list[dict], list[int]]:
    """The work unit of each page (without its rendered image) and the pages the transcription does not hold. A
    transcription with no `## Page N` heading keeps no pages: every unit's `md` is None and none is missing."""
    by_page = spec_pdf.pages_of(text)
    keeps_pages = len(by_page) > 1
    figures = sweep_figures.general_format_pages(text.splitlines(), edition) if keeps_pages else set()
    starts: dict[int, int] = {}
    for m in spec_pdf.PAGE_HEADING.finditer(text):
        starts.setdefault(int(m.group(1)), text.count("\n", 0, m.start()) + 1)
    made, missing = [], []
    for p in pages:
        body = by_page.get(p) if keeps_pages else None
        if keeps_pages and body is None:
            missing.append(p)
            continue
        made.append({"page": p, "md": body, "md_source_line": starts.get(p),
                     "md_lines": len(body.splitlines()) if body is not None else 0,
                     "has_figure": p in figures if keeps_pages else None})
    return made, missing


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("pages", nargs="*", type=int)
    ap.add_argument("--range", nargs=2, type=int, metavar=("FIRST", "LAST"))
    ap.add_argument("--out")
    ap.add_argument("--dpi", type=int, default=200)
    ap.add_argument("--self-test", action="store_true", help="run the self-test on planted text (no PDF is read)")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()

    pages = list(args.pages)
    if args.range:
        pages += list(range(args.range[0], args.range[1] + 1))
    if not pages:
        sys.exit("no pages given")
    if not args.out:
        sys.exit("--out is required: the directory this run's work units are written to")

    text = spec_pdf.markdown(args.edition).read_text(encoding="utf-8")
    made, missing = units(text, pages, args.edition)
    if missing:
        print(f"FATAL: the {args.edition} transcription holds no `## Page N` heading for page(s) {missing}; "
              "a missing page is itself a finding, never an empty work unit", file=sys.stderr)
        return 2

    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    # Render in ONE call: PyMuPDF opens the PDF per invocation, so batching matters over a thousand pages.
    subprocess.run([sys.executable, str(RENDER), *[str(p) for p in pages],
                    "--dpi", str(args.dpi), "--out", str(out), "--edition", str(args.edition)],
                   check=True, cwd=str(REPO))

    for u in made:
        p = u["page"]
        md = None
        if u["md"] is not None:
            md = out / f"page-{p}.md"
            md.write_text(u["md"], encoding="utf-8", newline="\n")
        meta = {"page": p, "png": str(out / f"spec_p{p}.png"), "md": str(md) if md else None,
                "md_source_line": u["md_source_line"], "md_lines": u["md_lines"], "has_figure": u["has_figure"]}
        (out / f"page-{p}.json").write_text(json.dumps(meta, indent=1), encoding="utf-8", newline="\n")

    print(f"wrote {len(made)} work unit(s) to {out}")
    if made and made[0]["md"] is None:
        print(f"  the {args.edition} transcription keeps no pages: each unit is the rendered page alone (no page-N.md)")
        return 0
    figs = [u["page"] for u in made if u["has_figure"]]
    prose = [u["page"] for u in made if not u["has_figure"]]
    print(f"  figure pages (a general format is drawn): {figs or 'none'}")
    print(f"  prose pages (no general format drawn): {prose or 'none'}")
    return 0


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, ok: bool) -> None:
        print(("  ok    " if ok else "  FAIL  ") + name)
        if not ok:
            failures.append(name)

    planted = "\n".join([
        "# Preface", "", "## Page 1", "", "Prose of page one.", "",
        "## Page 2", "", "##### 7.1.2 COPY statement", "", "###### 7.1.2.1 General format", "",
        '<pre style="line-height:1">', "<u>COPY</u> text-name-1", "</pre>", "",
        "## Page 3", "", "More prose.", ""])
    made, missing = units(planted, [1, 2, 3], 2002)
    check("each page's unit is its `## Page N` slice, numbered from its heading's line",
          [(u["page"], u["md_source_line"], u["md"].splitlines()[0]) for u in made]
          == [(1, 3, "## Page 1"), (2, 7, "## Page 2"), (3, 17, "## Page 3")])
    check("has_figure is true exactly on the page a general format is drawn on",
          [u["has_figure"] for u in made] == [False, True, False])
    check("nothing missing when every page is held", missing == [])
    made, missing = units(planted, [2, 9], 2002)
    check("a page the transcription does not hold is reported missing, never an empty unit",
          missing == [9] and [u["page"] for u in made] == [2])
    made, missing = units("# 2023\n\nNo pages kept.\n", [5], 2023)
    check("a transcription that keeps no pages gives the rendered page alone, nothing missing",
          missing == [] and made == [{"page": 5, "md": None, "md_source_line": None, "md_lines": 0,
                                      "has_figure": None}])
    print("=== PAGE WORKUNIT SELF-TEST: " + ("PASS" if not failures else f"FAIL ({len(failures)})") + " ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
