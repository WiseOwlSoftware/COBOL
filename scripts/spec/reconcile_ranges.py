#!/usr/bin/env python3
"""The page-range plan of an edition's reconciliation (kb/Work PB5255): every page of the transcription cut into batches
of BATCH consecutive pages, each batch marked by what it holds, ordered by priority and split into two halves of equal
estimated work, one per account. The plan is DATA (`docs/rearchitecture/spec-reconciliation/<edition>/ranges.json`),
generated here and checked here, never typed: a hand-typed page list once cost a whole sweep (the spec-reconcile
workflow's header tells it).

    python scripts/spec/reconcile_ranges.py --edition 2002 --write --ambiguous-from <transcribe run>/local.json
    python scripts/spec/reconcile_ranges.py --edition 2002 --check [--ambiguous-from <transcribe run>/local.json]
    python scripts/spec/reconcile_ranges.py --self-test

WHAT A BATCH IS MARKED WITH
  figure     a general format is drawn on one of its pages (sweep_figures.general_format_pages, the same reading the
             work unit page_workunit.py reports to the reconciling agent);
  ambiguous  pages whose structure the text-layer conversion reported ambiguous (transcribe_pdf.py `status`: images,
             flowcharts, strokes outside every table). That reading exists only in the conversion run's private state
             (its `local.json`, outside the repository), so the plan CARRIES the list as an input: `--write` takes it
             from `--ambiguous-from`, and `--check` regenerates the plan from the carried list and, given
             `--ambiguous-from`, also holds the carried list to the run's;
  clauses    pages of a clause the plan names (`FLAGGED`: for 2002 and 2014 the two clauses whose figure count the
             generator and the text layer read differently, kb/Work PB4813 and PB5268), read from the transcription's
             own headings.
A batch that is ambiguous or holds a flagged clause is FLAGGED.

ORDER AND HALVES. Flagged batches first, then figure batches, then the rest, each class in page order. Each batch's
estimated work is its page count plus one per general format drawn on it (a figure is compared glyph by glyph, about
a page of prose's work). The batches are dealt in that order, each to the half with less work so far (account1 on a
tie), so both halves start with the flagged and figure batches and end within one batch's work of each other. Each
half is cut into runs of RUN_BATCHES batches: one run is one spec-reconcile Workflow call, its `pages` the argument.
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import json
import pathlib
import re
import sys
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
import sweep_figures  # noqa: E402  (THE reading of a figure page)

BATCH = 12          # pages per compare agent: the spec-reconcile workflow's BATCH
RUN_BATCHES = 6     # batches per Workflow call
HALVES = ("account1", "account2")
RECON = REPO / "docs" / "rearchitecture" / "spec-reconciliation"


@dataclasses.dataclass(frozen=True)
class Plan:
    """An edition's reconciliation plan inputs: its directory under spec-reconciliation/ and the clauses it flags."""
    directory: str
    flagged: tuple[str, ...]


#: The editions with a page-range plan. An edition with no row exits naming the table (spec_pdf.edition_data).
PLANS: dict[int, Plan] = {
    2002: Plan("2002", ("11.1.1", "13.14.1")),
    # The two clauses whose figure count the generator and the text layer read differently (kb/Work PB5268).
    2014: Plan("2014", ("11.2.1", "13.16.1")),
}
CLAUSE_HEADING = re.compile(r"^#{1,6}\s+((?:\d+|[A-Z])(?:\.\d+)*)\s")


def plan_path(edition: int, root: pathlib.Path = RECON) -> pathlib.Path:
    return root / spec_pdf.edition_data(PLANS, edition, "reconciliation page-range plan (reconcile_ranges.PLANS)").directory / "ranges.json"


def clause_pages(lines: list[str], clause: str) -> list[int]:
    """The pages the clause's text stands on: from its heading to the next heading that is not one of its own
    subclauses. A clause the transcription does not head is an error."""
    page, start, out = 0, None, set()
    for line in lines:
        if (m := spec_pdf.PAGE_HEADING.match(line)):
            page = int(m.group(1))
            continue
        if (h := CLAUSE_HEADING.match(line)):
            number = h.group(1)
            if start is None and number == clause:
                start = page
            elif start is not None and not number.startswith(clause + "."):
                break
        if start is not None and line.strip():
            out.add(page)
    if start is None:
        sys.exit(f"FATAL: the transcription heads no clause {clause}")
    return sorted(out)


def formats_by_page(lines: list[str], edition: int) -> collections.Counter:
    return collections.Counter(d.page for d in sweep_figures.drawn_figures(lines, sweep_figures.page_furniture(edition))
                               if d.general_format)


def compact(pages: list[int]) -> str:
    """`1-12,25-36,40`: the spec-reconcile workflow's page-range spelling."""
    out, run = [], []
    for p in sorted(pages):
        if run and p == run[-1] + 1:
            run.append(p)
            continue
        if run:
            out.append(f"{run[0]}-{run[-1]}" if len(run) > 1 else str(run[0]))
        run = [p]
    if run:
        out.append(f"{run[0]}-{run[-1]}" if len(run) > 1 else str(run[0]))
    return ",".join(out)


def build(text: str, edition: int, ambiguous: list[int], source: str) -> dict:
    lines = text.splitlines()
    plan = spec_pdf.edition_data(PLANS, edition, "reconciliation page-range plan (reconcile_ranges.PLANS)")
    try:
        count = spec_pdf.page_count(text)  # THE page-heading rule (spec_pdf.pages_of): 1..N, each once
    except spec_pdf.PageHeadingError as ex:
        sys.exit(f"FATAL: {ex}")
    if not count:
        sys.exit("FATAL: the transcription keeps no `## Page N` headings, so it has no pages to plan")
    if sorted(set(ambiguous)) != ambiguous or any(not 1 <= p <= count for p in ambiguous):
        sys.exit(f"FATAL: the ambiguous pages must be distinct, sorted and within 1..{count}")
    formats = formats_by_page(lines, edition)
    clauses = {c: clause_pages(lines, c) for c in plan.flagged}
    batches = []
    for n, first in enumerate(range(1, count + 1, BATCH), start=1):
        pages = list(range(first, min(first + BATCH, count + 1)))
        amb = [p for p in pages if p in ambiguous]
        held = sorted(c for c, cp in clauses.items() if set(cp) & set(pages))
        figs = [p for p in pages if formats[p]]
        flagged = bool(amb or held)
        batches.append({"batch": n, "pages": compact(pages), "figure_pages": figs, "ambiguous": amb, "clauses": held,
                        "flagged": flagged, "figure": bool(figs),
                        "work": len(pages) + sum(formats[p] for p in pages),
                        "priority": 0 if flagged else 1 if figs else 2})
    order = sorted(batches, key=lambda b: (b["priority"], b["batch"]))
    work = {h: 0 for h in HALVES}
    dealt: dict[str, list[dict]] = {h: [] for h in HALVES}
    for b in order:
        half = min(HALVES, key=lambda h: (work[h], HALVES.index(h)))
        b["half"] = half
        work[half] += b["work"]
        dealt[half].append(b)
    halves = {}
    for h in HALVES:
        runs = []
        for i in range(0, len(dealt[h]), RUN_BATCHES):
            chunk = dealt[h][i:i + RUN_BATCHES]
            pages = [p for b in chunk for p in expand(b["pages"])]
            runs.append({"run": f"{h}-r{i // RUN_BATCHES + 1:02d}", "batches": [b["batch"] for b in chunk],
                         "pages": compact(pages), "page_count": len(pages), "work": sum(b["work"] for b in chunk)})
        halves[h] = {"work": work[h], "batches": len(dealt[h]),
                     "flagged_batches": sum(b["flagged"] for b in dealt[h]),
                     "figure_batches": sum(b["figure"] for b in dealt[h]), "runs": runs}
    return {
        "edition": edition,
        "about": ("The page-range plan of the reconciliation of the " + str(edition) + " transcription against its "
                  "rendered PDF pages (kb/Work PB5255), generated and checked by scripts/spec/reconcile_ranges.py; "
                  "never edit it by hand. One run is one spec-reconcile Workflow call whose `pages` is the run's."),
        "pages": count,
        "batch_pages": BATCH,
        "run_batches": RUN_BATCHES,
        "work_rule": "a batch's work is its page count plus one per general format drawn on it",
        "figure_pages": sorted(formats),
        "general_formats": sum(formats.values()),
        "ambiguous_pages": ambiguous,
        "ambiguous_source": source,
        "flagged_clauses": clauses,
        "summary": {"batches": len(batches), "flagged": sum(b["flagged"] for b in batches),
                    "figure": sum(b["figure"] for b in batches),
                    "flagged_or_figure": sum(b["flagged"] or b["figure"] for b in batches)},
        "halves": halves,
        "batches": batches,
    }


def expand(spec: str) -> list[int]:
    out = []
    for tok in spec.split(","):
        lo, _, hi = tok.partition("-")
        out += list(range(int(lo), int(hi or lo) + 1))
    return out


def ambiguous_from(path: pathlib.Path) -> list[int]:
    """The pages the conversion run's `local.json` records as structurally ambiguous (transcribe_pdf.py `status`)."""
    log = json.loads(path.read_text(encoding="utf-8"))
    return sorted(int(name[1:]) for name, rec in log.items() if rec.get("ambiguous"))


def render(doc: dict) -> str:
    return json.dumps(doc, indent=1, ensure_ascii=False) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--write", action="store_true", help="generate the plan and write it")
    mode.add_argument("--check", action="store_true", help="regenerate the plan and fail if the file differs")
    mode.add_argument("--self-test", action="store_true", help="run the self-test on planted text")
    ap.add_argument("--ambiguous-from", type=pathlib.Path,
                    help="the conversion run's local.json (transcribe_pdf.py's state): the ambiguous pages")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()

    out = plan_path(args.edition)
    text = spec_pdf.markdown(args.edition).read_text(encoding="utf-8")
    if args.write:
        if not args.ambiguous_from:
            sys.exit("--write needs --ambiguous-from: the ambiguous pages are the conversion run's reading")
        doc = build(text, args.edition, ambiguous_from(args.ambiguous_from), "transcribe_pdf.py status: the "
                    "text-layer conversion run's local.json, pages whose record carries `ambiguous`")
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(render(doc), encoding="utf-8", newline="\n")
        print(f"wrote {out.relative_to(REPO).as_posix()}")
    else:
        if not out.is_file():
            sys.exit(f"FATAL: {out.relative_to(REPO).as_posix()} does not exist; generate it with --write")
        held = json.loads(out.read_text(encoding="utf-8"))
        if args.ambiguous_from and ambiguous_from(args.ambiguous_from) != held["ambiguous_pages"]:
            print("FAIL: the plan's ambiguous pages are not the conversion run's; regenerate with --write")
            return 1
        doc = build(text, args.edition, held["ambiguous_pages"], held["ambiguous_source"])
        if render(doc) != out.read_text(encoding="utf-8"):
            print(f"FAIL: {out.relative_to(REPO).as_posix()} is not the plan the transcription gives; regenerate it "
                  "with --write")
            return 1
        print(f"OK  {out.relative_to(REPO).as_posix()} is the plan the transcription gives")
    s = doc["summary"]
    print(f"  {doc['pages']} pages, {s['batches']} batches of {BATCH}: {s['flagged']} flagged, {s['figure']} with a "
          f"figure ({len(doc['figure_pages'])} figure pages, {doc['general_formats']} general formats), "
          f"{len(doc['ambiguous_pages'])} ambiguous pages")
    for h, v in doc["halves"].items():
        print(f"  {h}: {v['batches']} batches ({v['flagged_batches']} flagged, {v['figure_batches']} figure), work "
              f"{v['work']}, {len(v['runs'])} runs; first run {v['runs'][0]['run']} pages {v['runs'][0]['pages']}")
    return 0


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, ok: bool) -> None:
        print(("  ok    " if ok else "  FAIL  ") + name)
        if not ok:
            failures.append(name)

    def page(n: int, *body: str) -> list[str]:
        return [f"## Page {n}", "", *body, ""]

    fmt = ['<pre style="line-height:1">', "<u>MOVE</u> identifier-1", "</pre>"]
    lines = ["# Preface", ""]
    for n in range(1, 31):
        if n == 14:
            lines += page(n, "##### 11.1.1 General format", "", *fmt)
        elif n == 15:
            lines += page(n, "###### 11.1.1.1 More of it", "", "text", "", "##### 11.1.2 Syntax rules", "", "rule")
        elif n == 27:
            lines += page(n, "##### 13.14.1 General format", "", *fmt, "", *fmt)
        elif n == 28:
            lines += page(n, "##### 13.14.2 Syntax rules", "", "rule")
        elif n == 3:
            lines += page(n, "##### 9.9 MOVE", "", "###### 9.9.1 General format", "", *fmt)
        else:
            lines += page(n, f"Prose of page {n}.")
    text = "\n".join(lines) + "\n"
    check("a clause's pages run from its heading through its subclauses to the next clause",
          clause_pages(lines, "11.1.1") == [14, 15] and clause_pages(lines, "13.14.1") == [27])
    check("compact and expand are inverses over runs and singletons",
          compact([1, 2, 3, 5, 7, 8]) == "1-3,5,7-8" and expand("1-3,5,7-8") == [1, 2, 3, 5, 7, 8])
    doc = build(text, 2002, [2, 30], "planted")
    b = {x["batch"]: x for x in doc["batches"]}
    check("thirty pages are three batches, the last short, every page in exactly one",
          [x["pages"] for x in doc["batches"]] == ["1-12", "13-24", "25-30"]
          and sorted(p for x in doc["batches"] for p in expand(x["pages"])) == list(range(1, 31)))
    check("marks: batch 1 ambiguous and a figure, batch 2 the clause 11.1.1, batch 3 ambiguous and 13.14.1",
          (b[1]["ambiguous"], b[1]["figure_pages"], b[2]["clauses"], b[3]["ambiguous"], b[3]["clauses"])
          == ([2], [3], ["11.1.1"], [30], ["13.14.1"]))
    check("work is pages plus one per general format",
          (b[1]["work"], b[2]["work"], b[3]["work"]) == (13, 13, 8))
    check("the halves are dealt in priority order to the lighter half, account1 on a tie",
          (b[1]["half"], b[2]["half"], b[3]["half"]) == ("account1", "account2", "account1")
          and doc["halves"]["account1"]["work"] == 21 and doc["halves"]["account2"]["work"] == 13)
    doc = build(text, 2002, [], "planted")
    runs = {h: [r["batches"] for r in doc["halves"][h]["runs"]] for h in HALVES}
    check("with nothing ambiguous the flagged clauses still come first, then the figure batch, each run in that order",
          [x["priority"] for x in doc["batches"]] == [1, 0, 0]
          and runs == {"account1": [[2]], "account2": [[3, 1]]}
          and doc["halves"]["account2"]["runs"][0]["pages"] == "1-12,25-30")
    for name, call in (("an edition with no plan row exits naming the table", lambda: build(text, 2023, [], "x")),
                       ("a clause the transcription does not head exits", lambda: clause_pages(lines, "99.9")),
                       ("an ambiguous page outside the transcription exits", lambda: build(text, 2002, [31], "x")),
                       ("page headings that do not run 1..N exit", lambda: build("## Page 1\n## Page 3\n", 2002, [], "x")),
                       ("a transcription that keeps no pages exits", lambda: build("# 2023\n", 2002, [], "x"))):
        try:
            call()
            check(name, False)
        except SystemExit as ex:
            check(name, bool(ex.code))
    print("=== RECONCILE RANGES SELF-TEST: " + ("PASS" if not failures else f"FAIL ({len(failures)})") + " ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
