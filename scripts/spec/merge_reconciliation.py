#!/usr/bin/env python3
"""Assemble an edition's spec-reconciliation ledger from whatever agent files exist on disk.

Every compare and verify agent of the spec-reconcile workflow writes its own JSON file into the edition's directory
BEFORE returning, so the work survives a rate limit, a session limit, or a stopped workflow. This merges those files
into the edition's ledger and a human-readable report.

Safe to run MID-RUN: it reports partial coverage rather than failing, so progress is visible while agents are still
working, and nothing has to be re-derived after an interruption.

    python scripts/spec/merge_reconciliation.py --edition 2002                # merge + report
    python scripts/spec/merge_reconciliation.py --edition 2002 --expect 882   # also report sweep coverage vs a target
    python scripts/spec/merge_reconciliation.py --self-test

THE EDITIONS (`EDITIONS`, one row each; an edition with no row exits naming the table, kb/Work PB4815):
  2023  agents/ -> LEDGER.json + REPORT.md, the claims-and-verdicts shape its readers (spec-grammar-impact.js,
        REPAIR-PLAN.md) take. The 2023 sweep is complete; the row keeps it reproducible.
  2002  2002/ -> LEDGER-2002.json + 2002/REPORT.md, in the shape of LEDGER-1985.json (kb/Work PB4850, PB5255):
        `edition`, `about`, `pages`, `claims`, `sweeps`, `boxes`, plus `pages_swept`. That is the shape the tool after
        the reconciliation reads: verify_publishable.py's `check_ledger` holds a transcription that keeps its pages to
        it (every claim decided, every APPLIED edit on its page). Each claim carries the agent's report and the
        verifier's verdict as `batch` (CONFIRMED, REFUTED or UNVERIFIED); a refuted claim is decided REFUTED with the
        verifier's reason, and a confirmed one is decided by its repair. A re-merge KEEPS each claim's decision,
        reason and edits and the ledger's sweeps, so merging mid-repair never loses a decision.
  1985 has no row: its ledger was assembled from the Message Batches passes (kb/Work PB4850), never by this tool, and
  merging over it would destroy its decisions.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import sys
from collections import Counter
from typing import Callable
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)

DIR = REPO / "docs" / "rearchitecture" / "spec-reconciliation"
SEV_ORDER = {"normative": 0, "structural": 1, "cosmetic": 2}
#: How much of a claim's `pdf_says` identifies it, to match a verdict or a kept decision to its claim.
KEY_CHARS = 120


@dataclasses.dataclass(frozen=True)
class Row:
    """An edition's reconciliation files, relative to the spec-reconciliation directory, and its ledger's writer."""
    source: str
    ledger: str
    report: str
    shape: str          # "verdicts" (2023's LEDGER.json) or "decisions" (LEDGER-1985.json's)


EDITIONS: dict[int, Row] = {
    2023: Row("agents", "LEDGER.json", "REPORT.md", "verdicts"),
    2002: Row("2002", "LEDGER-2002.json", "2002/REPORT.md", "decisions"),
}


def load(directory: pathlib.Path, pattern: str) -> list[tuple[pathlib.Path, dict]]:
    out = []
    for p in sorted(directory.glob(pattern)):
        try:
            out.append((p, json.loads(p.read_text(encoding="utf-8"))))
        except Exception as exc:  # noqa: BLE001 - a malformed agent file must not lose the others
            print(f"  ! unreadable, skipped: {p.name} ({exc})")
    return out


def key(page, pdf_says) -> tuple:
    return (page, (pdf_says or "").strip()[:KEY_CHARS])


def merged_rows(compares: list[tuple[pathlib.Path, dict]], verifies: list[tuple[pathlib.Path, dict]]):
    """Every claim of the compare files with its verdict, and the pages swept."""
    pages_swept: set[int] = set()
    claims: list[dict] = []
    for _, doc in compares:
        pages_swept.update(doc.get("pages_checked") or doc.get("batch") or [])
        claims.extend(doc.get("findings") or [])

    # Match each claim to its verdict. Keying on (page, kind) ALONE is wrong: a page can carry two findings of the
    # same kind (2023's p286 has two incorrect-figure claims), and a single-slot index silently attaches one verdict to
    # the wrong claim while leaving the other unverified. Verify files carry the CLAIM, so match on its text
    # first, then fall back to consuming same-(page, kind) verdicts in filename order.
    by_claim: dict[tuple, dict] = {}
    pool: dict[tuple, list[dict]] = {}
    for _, doc in verifies:
        claim = doc.get("claim") or {}
        if (claim.get("pdf_says") or "").strip():
            by_claim[key(doc.get("page"), claim.get("pdf_says"))] = doc.get("verdict") or {}
        pool.setdefault((doc.get("page"), doc.get("kind")), []).append(doc.get("verdict") or {})

    rows = []
    for c in claims:
        v = by_claim.get(key(c.get("page"), c.get("pdf_says")))
        if v is None:
            bucket = pool.get((c.get("page"), c.get("kind")))
            v = bucket.pop(0) if bucket else None
        rows.append({
            **c,
            "verified": v is not None,
            "real": (v or {}).get("real"),
            "verifier": (v or {}).get("corrected_description") or (v or {}).get("reasoning", ""),
        })
    return rows, pages_swept


def write_verdicts(row: Row, root: pathlib.Path, rows: list[dict], pages_swept: set[int], counts: tuple[int, int],
                   edition: int) -> None:
    """2023's LEDGER.json: the confirmed, refuted and unverified claims."""
    (root / row.ledger).write_text(json.dumps({
        "pages_swept": sorted(pages_swept),
        "compare_files": counts[0], "verify_files": counts[1],
        "confirmed": [r for r in rows if r["real"] is True],
        "refuted": [r for r in rows if r["real"] is False],
        "unverified": [r for r in rows if not r["verified"]],
    }, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


#: A decision a person or a repair made on a claim: kept across re-merges.
DECIDED = ("decision", "reason", "edits")


def write_decisions(row: Row, root: pathlib.Path, rows: list[dict], pages_swept: set[int], counts: tuple[int, int],
                    edition: int) -> None:
    """The LEDGER-1985.json shape: every claim numbered on its page, with its verdict as `batch` and its decision."""
    path = root / row.ledger
    kept: dict[tuple, dict] = {}
    old: dict = {}
    if path.is_file():
        old = json.loads(path.read_text(encoding="utf-8"))
        for c in old.get("claims", []):
            kept[key(c.get("page"), c.get("pdf_says"))] = {k: c[k] for k in DECIDED if k in c}
    claims, on_page = [], Counter()
    for r in sorted(rows, key=lambda r: r.get("page", 0)):
        page = r.get("page")
        claim = {"page": page, "i": on_page[page], "kind": r.get("kind"), "severity": r.get("severity"),
                 "batch": "UNVERIFIED" if not r["verified"] else "CONFIRMED" if r["real"] else "REFUTED",
                 "pdf_says": r.get("pdf_says", ""), "markdown_says": r.get("markdown_says", ""),
                 "why_it_matters": r.get("why_it_matters", ""), "suggested_repair": r.get("suggested_repair", ""),
                 "verifier": r["verifier"]}
        on_page[page] += 1
        decided = kept.get(key(page, r.get("pdf_says")))
        if decided:
            claim.update(decided)
        elif claim["batch"] == "REFUTED":
            claim.update({"decision": "REFUTED", "reason": r["verifier"]})
        claims.append(claim)
    path.write_text(json.dumps({
        "edition": edition,
        "about": (f"The reconciliation of the {edition} transcription against the rendered PDF page, merged by "
                  "scripts/spec/merge_reconciliation.py from the spec-reconcile workflow's per-agent files in "
                  f"spec-reconciliation/{row.source}/ (kb/Work PB5255). Each claim carries the compare agent's report "
                  "and the adversarial verifier's verdict as `batch` (CONFIRMED, REFUTED, UNVERIFIED); a refuted claim "
                  "is decided REFUTED with the verifier's reason, a confirmed claim is decided by its repair (APPLIED "
                  "with its edits, ALREADY, FORMAT, FIGURE or KEPT with a reason, verify_publishable.DECISIONS). A "
                  "re-merge keeps every decision and sweep."),
        "pages": len(pages_swept),
        "pages_swept": sorted(pages_swept),
        "compare_files": counts[0], "verify_files": counts[1],
        "claims": claims,
        "sweeps": old.get("sweeps", []),
        "boxes": old.get("boxes", []),
    }, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


WRITERS: dict[str, Callable] = {"verdicts": write_verdicts, "decisions": write_decisions}


def merge(edition: int, expect: int, root: pathlib.Path = DIR) -> int:
    row = spec_pdf.edition_data(EDITIONS, edition, "reconciliation ledger (merge_reconciliation.EDITIONS)")
    source = root / row.source
    if not source.is_dir():
        sys.exit(f"nothing to merge: {source} does not exist")
    compares = load(source, "compare-*.json")
    verifies = load(source, "verify-*.json")
    rows, pages_swept = merged_rows(compares, verifies)
    rows.sort(key=lambda r: (SEV_ORDER.get(r.get("severity"), 9), r.get("page", 0)))
    confirmed = [r for r in rows if r["real"] is True]
    refuted = [r for r in rows if r["real"] is False]
    unverified = [r for r in rows if not r["verified"]]

    print(f"edition {edition}: merging {source}")
    print(f"agent files      : {len(compares)} compare · {len(verifies)} verify")
    print(f"pages swept      : {len(pages_swept)}" + (f" of {expect} expected" if expect else ""))
    if expect and len(pages_swept) < expect:
        print(f"  ⚠ INCOMPLETE — {expect - len(pages_swept)} page(s) not yet swept; re-run the workflow for those")
    print(f"claims           : {len(rows)}")
    print(f"  confirmed      : {len(confirmed)}  {dict(Counter(r['severity'] for r in confirmed))}")
    print(f"  refuted        : {len(refuted)}")
    print(f"  unverified     : {len(unverified)}" + ("  ⚠ verdicts missing — do not act on these yet" if unverified else ""))

    WRITERS[row.shape](row, root, rows, pages_swept, (len(compares), len(verifies)), edition)

    lines = [f"# Spec reconciliation ledger, {edition} edition (generated)", "",
             f"> Merged from the per-agent files in `{row.source}/` by `scripts/spec/merge_reconciliation.py "
             f"--edition {edition}`.",
             "> Each agent writes its own file before returning, so an interrupted run loses nothing.", "",
             f"**{len(pages_swept)} pages swept · {len(confirmed)} confirmed · {len(refuted)} refuted · "
             f"{len(unverified)} unverified**", ""]
    if confirmed:
        lines += ["## Confirmed — repair these", ""]
        for r in confirmed:
            lines += [f"### p{r['page']} · {r['kind']} · **{r['severity']}**", "",
                      f"- **PDF:** {r.get('pdf_says','')}",
                      f"- **Markdown:** {r.get('markdown_says','')}",
                      f"- **Why:** {r.get('why_it_matters','')}",
                      f"- **Repair:** {r.get('suggested_repair','—')}",
                      f"- **Verifier:** {r.get('verifier','')}", ""]
    if unverified:
        lines += ["## Unverified claims — NOT yet actionable", ""]
        for r in unverified:
            lines += [f"- p{r['page']} · {r['kind']} · {r['severity']} — {r.get('why_it_matters','')[:200]}"]
        lines.append("")
    if refuted:
        lines += ["## Refuted — deliberately NOT repaired", ""]
        for r in refuted:
            lines += [f"- p{r['page']} · {r['kind']} — {r.get('verifier','')[:240]}"]
        lines.append("")
    report = root / row.report
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"\nwrote {row.ledger} and {row.report} under {root.name}/")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--expect", type=int, default=0, help="expected page count, to report coverage")
    ap.add_argument("--self-test", action="store_true", help="run the self-test in a scratch directory")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    return merge(args.edition, args.expect)


def self_test() -> int:
    import contextlib
    import io
    import subprocess
    import tempfile
    failures: list[str] = []

    def check(name: str, ok: bool) -> None:
        print(("  ok    " if ok else "  FAIL  ") + name)
        if not ok:
            failures.append(name)

    def write(path: pathlib.Path, doc: dict) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(doc), encoding="utf-8")

    claim = {"page": 5, "kind": "incorrect-text", "severity": "normative", "pdf_says": "shall be", "markdown_says": "may be",
             "why_it_matters": "a rule"}
    twin = {**claim, "pdf_says": "the other one", "markdown_says": "the othr one"}
    late = {**claim, "page": 7, "pdf_says": "unverified text"}
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp) / "spec-reconciliation"
        write(root / "2002" / "compare-p1-p12.json", {"batch": list(range(1, 13)), "pages_checked": list(range(1, 13)),
                                                      "findings": [claim, twin]})
        write(root / "2002" / "compare-p13-p14.json", {"batch": [13, 14], "pages_checked": [13, 14], "findings": [late]})
        write(root / "2002" / "verify-p5-incorrect-text-0.json",
              {"page": 5, "kind": "incorrect-text", "claim": {"pdf_says": "the other one"},
               "verdict": {"real": False, "reasoning": "present, reflowed"}})
        write(root / "2002" / "verify-p5-incorrect-text-1.json",
              {"page": 5, "kind": "incorrect-text", "claim": {"pdf_says": "shall be"}, "verdict": {"real": True,
                                                                                                    "reasoning": "seen"}})
        write(root / "agents" / "compare-p1-p2.json", {"batch": [1, 2], "pages_checked": [1, 2], "findings": []})
        with contextlib.redirect_stdout(io.StringIO()):
            merge(2002, 0, root)
        ledger = json.loads((root / "LEDGER-2002.json").read_text(encoding="utf-8"))
        check("--edition 2002 merges from 2002/ into LEDGER-2002.json in LEDGER-1985.json's keys",
              {"edition", "about", "pages", "claims", "sweeps", "boxes"} <= set(ledger) and ledger["edition"] == 2002
              and ledger["pages"] == 14 and (root / "2002" / "REPORT.md").is_file())
        by = {(c["page"], c["i"]): c for c in ledger["claims"]}
        check("each verdict reaches its own claim by the claim's text, numbered on its page",
              by[(5, 0)]["batch"] == "CONFIRMED" and by[(5, 1)]["batch"] == "REFUTED" and by[(7, 0)]["batch"] == "UNVERIFIED")
        check("a refuted claim is decided REFUTED with the verifier's reason; a confirmed or unverified one is undecided",
              by[(5, 1)].get("decision") == "REFUTED" and by[(5, 1)].get("reason") == "present, reflowed"
              and "decision" not in by[(5, 0)] and "decision" not in by[(7, 0)])
        check("2023's files are not read by a 2002 merge, and its ledger is not written",
              not (root / "LEDGER.json").exists())
        ledger["claims"][0].update({"decision": "APPLIED", "edits": [{"was": "may be", "now": "shall be"}]})
        ledger["sweeps"] = [{"page": 5, "sweep": "s", "was": "may", "now": "shall"}]
        (root / "LEDGER-2002.json").write_text(json.dumps(ledger), encoding="utf-8")
        with contextlib.redirect_stdout(io.StringIO()):
            merge(2002, 0, root)
        again = json.loads((root / "LEDGER-2002.json").read_text(encoding="utf-8"))
        check("a re-merge keeps a claim's decision and edits and the ledger's sweeps",
              again["claims"][0].get("decision") == "APPLIED" and again["claims"][0].get("edits")
              and again["sweeps"] == ledger["sweeps"])
        with contextlib.redirect_stdout(io.StringIO()):
            merge(2023, 0, root)
        old = json.loads((root / "LEDGER.json").read_text(encoding="utf-8"))
        check("--edition 2023 merges from agents/ into LEDGER.json in its claims-and-verdicts shape",
              set(old) == {"pages_swept", "compare_files", "verify_files", "confirmed", "refuted", "unverified"}
              and old["pages_swept"] == [1, 2])
        for name, ed in (("1985 has no row: it exits naming the table, never merging over LEDGER-1985.json", 1985),):
            try:
                merge(ed, 0, root)
                check(name, False)
            except SystemExit as ex:
                check(name, "merge_reconciliation.EDITIONS" in str(ex.code) and "1985" in str(ex.code))
        try:
            with contextlib.redirect_stdout(io.StringIO()):
                merge(2002, 0, pathlib.Path(tmp) / "absent")
            check("an edition whose directory does not exist exits", False)
        except SystemExit as ex:
            check("an edition whose directory does not exist exits", "nothing to merge" in str(ex.code))
    bad = subprocess.run([sys.executable, __file__, "--edition", "1999"], capture_output=True, text=True)
    check("an edition the standard has no transcription of is refused by the argument itself",
          bad.returncode == 2 and "1999" in bad.stderr)
    print("=== MERGE RECONCILIATION SELF-TEST: " + ("PASS" if not failures else f"FAIL ({len(failures)})") + " ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
