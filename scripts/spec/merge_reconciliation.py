#!/usr/bin/env python3
"""Assemble an edition's spec-reconciliation ledger from the agent files on disk, refusing any it cannot trust.

Every compare and verify agent of the spec-reconcile workflow writes its own JSON file into the edition's directory
BEFORE returning, so the work survives a rate limit, a session limit, or a stopped workflow. This validates those
files and merges them into the edition's ledger and a human-readable report.

    python scripts/spec/merge_reconciliation.py --edition 2002            # validate and report; writes nothing
    python scripts/spec/merge_reconciliation.py --edition 2002 --write    # also write the ledger and the report
    python scripts/spec/merge_reconciliation.py --edition 2002 --check    # the committed ledger and report ARE the merge
    python scripts/spec/merge_reconciliation.py --edition 2002 --root <dir>   # a planted spec-reconciliation directory
    python scripts/spec/merge_reconciliation.py --compare-summary <compare file>   # what one compare FILE records
    python scripts/spec/merge_reconciliation.py --claim-key <compare file> <claim id>   # a claim and its key, from the FILE
    python scripts/spec/merge_reconciliation.py --self-test

THE FILE IS THE RECORD (review R3-1). The workflow's agents run the two file commands: a compare agent returns
`--compare-summary`'s line for the file it wrote, and a verify agent takes its claim and the claim's key from
`--claim-key`. So nothing the workflow counts or a verifier copies is what an agent retyped, and the key a verdict
carries is computed from the very file this merge reads.

WHAT IS TRUSTED (kb/Work PB5255 review round 1). No missing, empty or unreadable input is ever replaced by a default:
each makes its file INVALID, the merge names the file and the problem, exits 1 and writes nothing.
  - A COMPARE file `compare-p<first>-p<last>.json` holds `batch` (its pages, ascending, the first and last its name's),
    `pages_checked` (the pages the agent compared), `pages_unread` (each `{page, reason}`: a page it could not render or
    read) and `findings`. Every page of the batch is in exactly one of the two lists, and no page outside the batch is
    in either: a page is SWEPT only when its agent says it compared it, never because it was in the batch. Batches of
    two files never share a page.
  - A FINDING carries `id` = `p<page>-<n>`, n counting its page's findings from 1 in order, written by the compare
    agent, plus `page` (a page it checked), `kind`, `severity`, `pdf_says`, `markdown_says` and `why_it_matters`.
  - A VERIFY file holds `claim_id` (the id of the claim it judged, echoed by the verify agent), `claim_key` (the
    claim's content fingerprint, `claim_key`, which the verifier copies from `--claim-key`), the claim's `page`
    and `kind`, and `verdict` {`real`: true or false, `reasoning`}. A verdict attaches to EXACTLY the claim its id names,
    once, and only when its `claim_key` is that claim's: an id is reused when a batch is re-run, so a verdict of an
    earlier run whose claim the id no longer holds is refused by name, never attached and never ignored. A verdict
    naming no claim, or a second verdict for one claim, is invalid. A claim with no verdict is UNVERIFIED and stays
    open; it never takes another claim's verdict.
THE RE-RUN RULE (review R2-1). A re-run batch's compare file replaces the earlier one under the same name, so the run
on disk is the one that counts. Its earlier verify files are deleted with it (README, "Re-running a batch"); one left
behind is refused unless it judged a claim identical in every field to one the new run holds, in which case it is a
verdict on exactly that claim and attaches. The merge refuses rather than having the workflow delete files: a workflow
cannot see the disk, and an agent told to delete could delete a verdict that was valid; a refusal loses nothing.
Partial coverage is not an error: a batch with no compare file yet is UNSWEPT, and the report names those pages
against the transcription's page count, so the merge is safe to run while agents are still working.

THE EDITIONS (`EDITIONS`, one row each; an edition with no row exits naming the table, kb/Work PB4815):
  2023  agents/ -> LEDGER.json + REPORT.md, the claims-and-verdicts shape its readers (spec-grammar-impact.js,
        REPAIR-PLAN.md) take. The 2023 sweep is complete; the row keeps it reproducible (`--check`).
  2002  2002/ -> LEDGER-2002.json + 2002/REPORT.md, in the shape of LEDGER-1985.json (kb/Work PB4850, PB5255):
        `edition`, `about`, `book_pages` (the transcription's page count), `pages_swept` (the pages compared),
        `pages_unread` (with each reason), `claims`, `sweeps`, `boxes`. That is the shape verify_publishable.py's
        `check_ledger` holds a transcription that keeps its pages to. Each claim carries its `id`, the agent's report
        and the verifier's verdict as `batch` (CONFIRMED, REFUTED or UNVERIFIED); a claim refuted BY ITS OWN verdict is
        decided REFUTED with the verifier's reason, a confirmed one is decided by its repair. A re-merge KEEPS each
        claim's decision, reason and edits (by its id) and the ledger's sweeps and boxes; a kept decision whose claim is
        gone or now says something else is refused, never dropped or moved.
  2014  2014/ -> LEDGER-2014.json + 2014/REPORT.md, the same shape and rules as 2002 (kb/Work PB5268: its transcription
        keeps its pages, `## Page N`, 1..952).
  1985 has no row: its ledger was assembled from the Message Batches passes (kb/Work PB4850), never by this tool, and
  merging over it would destroy its decisions.

WHO WRITES THE LEDGER (review R1-3). A run's branch lands only its agent files, whose names never collide across runs
or accounts; the ledger is written once, by `--write` on the one branch that closes a set of runs, and `--check` proves
the committed ledger is the merge of the files on that branch. The README states the procedure.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import re
import sys
from collections import Counter
from typing import Callable
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
from reconcile_ranges import compact  # noqa: E402  (THE page-range spelling)
from verify_publishable import RECONCILIATION  # noqa: E402  (THE path of every edition's reconciliation ledger)

DIR = REPO / "docs" / "rearchitecture" / "spec-reconciliation"
SEV_ORDER = {"normative": 0, "structural": 1, "cosmetic": 2}
COMPARE_NAME = re.compile(r"^compare-p(\d+)-p(\d+)\.json$")
#: A claim's identity: `p<page>-<n>`, n counting the page's findings from 1. The compare agent writes it on each finding
#: and the verify agent echoes it as `claim_id` (the workflow's prompts), so a verdict names its claim exactly.
CLAIM_ID = re.compile(r"^p(\d+)-([1-9]\d*)$")
FINDING_TEXT = ("kind", "pdf_says", "markdown_says", "why_it_matters")


def claim_key(claim: dict) -> str:
    """A claim's CONTENT identity (kb/Work PB5255 review R2-1): a fingerprint of its page and every text field
    (FINDING_TEXT), two 32-bit FNV-1a hashes over the UTF-16 code units of those fields joined by U+001F, as 16 hex
    digits. An id (`p5-1`) is reused when a batch is re-run, so it cannot alone say which claim a verdict judged; this
    can. It is computed HERE ONLY, from the compare FILE: a verifier gets it from `--claim-key <file> <id>` (claim_of) and
    copies it (review R3-1: a key computed from a compare agent's return value differed from the file's on one retyped
    space). Agents retype long quotes inexactly (203 of 2023's 346 verify files differ from their claim in whitespace,
    escaping or length), so the key is copied, never the text compared. A verdict attaches only when its key is its
    claim's; a kept decision only when the ledger claim's key is the claim's."""
    text = "\x1f".join([str(claim.get("page"))] + [str(claim.get(k)) for k in FINDING_TEXT])
    raw = text.encode("utf-16-le")
    units = [raw[i] | raw[i + 1] << 8 for i in range(0, len(raw), 2)]
    out = ""
    for h in (0x811C9DC5, 0x050C5D1F):
        for u in units:
            h = ((h ^ u) * 0x01000193) & 0xFFFFFFFF
        out += f"{h:08x}"
    return out


@dataclasses.dataclass(frozen=True)
class Row:
    """An edition's reconciliation files, relative to the spec-reconciliation directory, and its ledger's writer."""
    source: str
    ledger: str
    report: str
    shape: str          # "verdicts" (2023's LEDGER.json) or "decisions" (LEDGER-1985.json's)


def ledger_of(edition: int) -> str:
    """`edition`'s ledger relative to the spec-reconciliation directory, from verify_publishable.RECONCILIATION (the one
    home of the ledger paths, which `reconciled` reads): an edition it does not name is a KeyError, never a guessed path."""
    return pathlib.PurePosixPath(RECONCILIATION[edition]).relative_to(DIR.relative_to(REPO).as_posix()).as_posix()


EDITIONS: dict[int, Row] = {
    2023: Row("agents", ledger_of(2023), "REPORT.md", "verdicts"),
    2002: Row("2002", ledger_of(2002), "2002/REPORT.md", "decisions"),
    2014: Row("2014", ledger_of(2014), "2014/REPORT.md", "decisions"),
}


@dataclasses.dataclass
class Collected:
    """What the agent files on disk prove, once every file is valid."""
    compare_files: int = 0
    verify_files: int = 0
    checked: set[int] = dataclasses.field(default_factory=set)
    unread: dict[int, str] = dataclasses.field(default_factory=dict)
    claims: list[dict] = dataclasses.field(default_factory=list)
    verdicts: dict[str, dict] = dataclasses.field(default_factory=dict)

    def rows(self) -> list[dict]:
        """Every claim with ITS OWN verdict (by id), or none: in severity order, then page, then file order."""
        out = []
        for c in self.claims:
            v = self.verdicts.get(c["id"])
            out.append({**c, "verified": v is not None, "real": v["real"] if v else None,
                        "verifier": (v.get("corrected_description") or v["reasoning"]) if v else ""})
        out.sort(key=lambda r: (SEV_ORDER[r["severity"]], r["page"]))
        return out


def read_json(path: pathlib.Path, errors: list[str]) -> dict | None:
    try:
        doc = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        errors.append(f"{path.name}: unreadable ({exc})")
        return None
    if not isinstance(doc, dict):
        errors.append(f"{path.name}: not a JSON object")
        return None
    return doc


def is_pages(v) -> bool:
    return isinstance(v, list) and all(isinstance(p, int) and not isinstance(p, bool) for p in v)


def compare_problems(name: str, doc: dict) -> list[str]:
    """What makes one compare file untrustworthy, on its own (the cross-file checks are collect's)."""
    out: list[str] = []
    m = COMPARE_NAME.match(name)
    batch = doc.get("batch")
    if not m:
        return ["the name is not compare-p<first>-p<last>.json"]
    if not is_pages(batch) or not batch:
        return ["`batch` is missing or not a list of pages"]
    if batch != sorted(set(batch)) or (batch[0], batch[-1]) != (int(m[1]), int(m[2])):
        out.append(f"`batch` {compact(batch) if batch == sorted(set(batch)) else batch} is not the ascending pages "
                   f"p{m[1]}..p{m[2]} its name gives")
    checked, unread = doc.get("pages_checked"), doc.get("pages_unread")
    if not is_pages(checked):
        out.append("`pages_checked` is missing or not a list of pages")
        checked = []
    if not isinstance(unread, list) or not all(
            isinstance(u, dict) and isinstance(u.get("page"), int) and isinstance(u.get("reason"), str)
            and u["reason"].strip() for u in unread):
        out.append("`pages_unread` is missing or an entry is not {page, reason} with a reason")
        unread = []
    unread_pages = [u["page"] for u in unread]
    for what, pages in (("pages_checked", checked), ("pages_unread", unread_pages)):
        if len(set(pages)) != len(pages):
            out.append(f"`{what}` lists a page twice")
        if outside := sorted(set(pages) - set(batch)):
            out.append(f"`{what}` lists page(s) {compact(outside)} outside the batch")
    if both := sorted(set(checked) & set(unread_pages)):
        out.append(f"page(s) {compact(both)} are both checked and unread")
    if neither := sorted(set(batch) - set(checked) - set(unread_pages)):
        out.append(f"page(s) {compact(neither)} of the batch are neither checked nor unread")
    findings = doc.get("findings")
    if not isinstance(findings, list):
        return out + ["`findings` is missing or not a list"]
    numbered: dict[int, list[int]] = {}
    for k, f in enumerate(findings):
        at = f"finding {k}"
        if not isinstance(f, dict):
            out.append(f"{at} is not an object")
            continue
        page, fid = f.get("page"), f.get("id")
        idm = CLAIM_ID.match(fid) if isinstance(fid, str) else None
        if not isinstance(page, int) or page not in checked:
            out.append(f"{at} is on page {page!r}, not a page the agent checked")
        if not idm or int(idm[1]) != page:
            out.append(f"{at} has id {fid!r}, not p{page}-<n>")
        else:
            numbered.setdefault(page, []).append(int(idm[2]))
        if f.get("severity") not in SEV_ORDER:
            out.append(f"{at} has severity {f.get('severity')!r}, not one of {sorted(SEV_ORDER)}")
        out += [f"{at} has no `{key}`" for key in FINDING_TEXT if not isinstance(f.get(key), str) or not f[key].strip()]
    out += [f"the findings of page {p} are numbered {ns}, not 1..{len(ns)} in order"
            for p, ns in numbered.items() if ns != list(range(1, len(ns) + 1))]
    return out


def collect(source: pathlib.Path, book_pages: int | None) -> tuple[Collected, list[str]]:
    """Validate every agent file of `source`; the problems name their files. `book_pages`, when the transcription keeps
    its pages, bounds every batch."""
    errors: list[str] = []
    got = Collected()
    owner: dict[int, str] = {}
    claims: dict[str, dict] = {}
    for path in sorted(source.glob("compare-*.json")):
        got.compare_files += 1
        doc = read_json(path, errors)
        if doc is None:
            continue
        problems = compare_problems(path.name, doc)
        batch = doc.get("batch") if is_pages(doc.get("batch")) else []
        if book_pages and (beyond := [p for p in batch if not 1 <= p <= book_pages]):
            problems.append(f"page(s) {compact(beyond)} are not in the transcription's 1..{book_pages}")
        for p in batch:
            if p in owner:
                problems.append(f"page {p} is also the batch of {owner[p]}")
        if problems:
            errors += [f"{path.name}: {p}" for p in problems]
            continue
        owner.update({p: path.name for p in batch})
        got.checked.update(doc["pages_checked"])
        got.unread.update({u["page"]: u["reason"].strip() for u in doc["pages_unread"]})
        for f in doc["findings"]:
            claims[f["id"]] = f
            got.claims.append(f)
    for path in sorted(source.glob("verify-*.json")):
        got.verify_files += 1
        doc = read_json(path, errors)
        if doc is None:
            continue
        cid, verdict = doc.get("claim_id"), doc.get("verdict")
        claim = claims.get(cid) if isinstance(cid, str) else None
        if claim is None:
            errors.append(f"{path.name}: `claim_id` {cid!r} names no claim of a valid compare file")
        elif cid in got.verdicts:
            errors.append(f"{path.name}: a second verdict for claim {cid} ({got.verdicts[cid]['file']} has one)")
        elif doc.get("claim_key") != claim_key(claim):
            errors.append(f"{path.name}: it judged claim key {doc.get('claim_key')!r}, but {cid} is now the claim keyed "
                          f"{claim_key(claim)!r}: a verdict of another run of the batch (delete it with the batch's "
                          "other stale files; the batch's compare file on disk is the run that counts)")
        elif not isinstance(verdict, dict) or not isinstance(verdict.get("real"), bool) \
                or not isinstance(verdict.get("reasoning"), str) or not verdict["reasoning"].strip():
            errors.append(f"{path.name}: `verdict` is not {{real: true|false, reasoning}}")
        else:
            got.verdicts[cid] = {**verdict, "file": path.name}
    return got, errors


def render_verdicts(row: Row, got: Collected, old: dict | None, edition: int, book_pages: int) -> tuple[dict, list]:
    """2023's LEDGER.json: the confirmed, refuted and unverified claims."""
    rows = got.rows()
    return {
        "pages_swept": sorted(got.checked),
        "pages_unread": [{"page": p, "reason": got.unread[p]} for p in sorted(got.unread)],
        "compare_files": got.compare_files, "verify_files": got.verify_files,
        "confirmed": [r for r in rows if r["real"] is True],
        "refuted": [r for r in rows if r["real"] is False],
        "unverified": [r for r in rows if not r["verified"]],
    }, []


#: A decision a person or a repair made on a claim: kept across re-merges, by the claim's id.
DECIDED = ("decision", "reason", "edits")


def render_decisions(row: Row, got: Collected, old: dict | None, edition: int, book_pages: int) -> tuple[dict, list]:
    """The LEDGER-1985.json shape: every claim with its id, numbered on its page, its verdict as `batch`, its decision."""
    errors: list[str] = []
    kept: dict[str, dict] = {}
    for c in (old or {}).get("claims", []):
        if any(k in c for k in DECIDED) and not (c.get("decision") == "REFUTED" and c.get("batch") == "REFUTED"
                                                 and c.get("reason") == c.get("verifier")):
            kept[c["id"]] = c
    by_id = {c["id"]: c for c in got.claims}
    for cid, c in kept.items():
        now = by_id.get(cid)
        if now is None:
            errors.append(f"{row.ledger}: claim {cid} is decided {c.get('decision')!r} but no agent file holds it")
        elif claim_key(now) != claim_key(c):
            errors.append(f"{row.ledger}: claim {cid} is decided {c.get('decision')!r} on another claim than its agent "
                          "file now holds (a re-run batch renumbered its claims, or changed its kind or text): decide "
                          "it again")
    claims = []
    for r in sorted(got.rows(), key=lambda r: (r["page"], int(CLAIM_ID.match(r["id"])[2]))):
        claim = {"id": r["id"], "page": r["page"], "i": int(CLAIM_ID.match(r["id"])[2]) - 1, "kind": r["kind"],
                 "severity": r["severity"],
                 "batch": "UNVERIFIED" if not r["verified"] else "CONFIRMED" if r["real"] else "REFUTED",
                 "pdf_says": r["pdf_says"], "markdown_says": r["markdown_says"],
                 "why_it_matters": r["why_it_matters"], "suggested_repair": r.get("suggested_repair", ""),
                 "verifier": r["verifier"]}
        if r["id"] in kept:
            claim.update({k: kept[r["id"]][k] for k in DECIDED if k in kept[r["id"]]})
        elif claim["batch"] == "REFUTED":
            claim.update({"decision": "REFUTED", "reason": r["verifier"]})
        claims.append(claim)
    return {
        "edition": edition,
        "about": (f"The reconciliation of the {edition} transcription against the rendered PDF page, merged by "
                  "scripts/spec/merge_reconciliation.py from the spec-reconcile workflow's per-agent files in "
                  f"spec-reconciliation/{row.source}/ (kb/Work PB5255). `book_pages` is the transcription's page "
                  "count; `pages_swept` the pages a compare agent compared; `pages_unread` those it could not read, "
                  "each with its reason. Each claim carries its id, the compare agent's report and its own verifier's "
                  "verdict as `batch` (CONFIRMED, REFUTED, UNVERIFIED); a refuted claim is decided REFUTED with the "
                  "verifier's reason, a confirmed claim is decided by its repair (APPLIED with its edits, ALREADY, "
                  "FORMAT, FIGURE or KEPT with a reason, verify_publishable.DECISIONS). A re-merge keeps every "
                  "decision by claim id, and every sweep and box."),
        "book_pages": book_pages,
        "pages_swept": sorted(got.checked),
        "pages_unread": [{"page": p, "reason": got.unread[p]} for p in sorted(got.unread)],
        "compare_files": got.compare_files, "verify_files": got.verify_files,
        "claims": claims,
        "sweeps": (old or {}).get("sweeps", []),
        "boxes": (old or {}).get("boxes", []),
    }, errors


RENDERERS: dict[str, Callable] = {"verdicts": render_verdicts, "decisions": render_decisions}


def report(row: Row, edition: int, got: Collected, unswept: list[int]) -> str:
    rows = got.rows()
    confirmed = [r for r in rows if r["real"] is True]
    refuted = [r for r in rows if r["real"] is False]
    unverified = [r for r in rows if not r["verified"]]
    lines = [f"# Spec reconciliation ledger, {edition} edition (generated)", "",
             f"> Merged from the per-agent files in `{row.source}/` by `scripts/spec/merge_reconciliation.py "
             f"--edition {edition} --write`.",
             "> Each agent writes its own file before returning, so an interrupted run loses nothing.", "",
             f"**{len(got.checked)} pages swept · {len(got.unread)} unread · {len(unswept)} unswept · "
             f"{len(confirmed)} confirmed · {len(refuted)} refuted · {len(unverified)} unverified**", ""]
    if got.unread:
        lines += ["## Unread pages: compared by no agent", ""]
        lines += [f"- p{p}: {got.unread[p]}" for p in sorted(got.unread)]
        lines.append("")
    if unswept:
        lines += ["## Unswept pages: no compare file yet", "", compact(unswept), ""]
    if confirmed:
        lines += ["## Confirmed — repair these", ""]
        for r in confirmed:
            lines += [f"### {r['id']} · p{r['page']} · {r['kind']} · **{r['severity']}**", "",
                      f"- **PDF:** {r['pdf_says']}",
                      f"- **Markdown:** {r['markdown_says']}",
                      f"- **Why:** {r['why_it_matters']}",
                      f"- **Repair:** {r.get('suggested_repair') or '—'}",
                      f"- **Verifier:** {r['verifier']}", ""]
    if unverified:
        lines += ["## Unverified claims — NOT yet actionable", ""]
        for r in unverified:
            lines += [f"- {r['id']} · {r['kind']} · {r['severity']} — {r['why_it_matters'][:200]}"]
        lines.append("")
    if refuted:
        lines += ["## Refuted — deliberately NOT repaired", ""]
        for r in refuted:
            lines += [f"- {r['id']} · {r['kind']} — {r['verifier'][:240]}"]
        lines.append("")
    return "\n".join(lines) + "\n"


def merge(edition: int, mode: str = "report", root: pathlib.Path = DIR, transcription: str | None = None) -> int:
    """Validate and report (`report`), also write the ledger and report (`write`), or prove the committed ones are this
    merge (`check`). `transcription` is the edition's Markdown (its page count bounds the batches and names the unswept
    pages); by default the tracked file."""
    row = spec_pdf.edition_data(EDITIONS, edition, "reconciliation ledger (merge_reconciliation.EDITIONS)")
    source = root / row.source
    if not source.is_dir():
        sys.exit(f"nothing to merge: {source} does not exist")
    if transcription is None:
        transcription = spec_pdf.markdown(edition).read_text(encoding="utf-8")
    book_pages = spec_pdf.page_count(transcription)
    got, errors = collect(source, book_pages)
    ledger_path, report_path = root / row.ledger, root / row.report
    old = json.loads(ledger_path.read_text(encoding="utf-8")) if ledger_path.is_file() else None
    ledger, more = RENDERERS[row.shape](row, got, old, edition, book_pages)
    errors += more

    unswept = sorted(set(range(1, book_pages + 1)) - got.checked - set(got.unread)) if book_pages else []
    rows = got.rows()
    print(f"edition {edition}: merging {source}")
    print(f"agent files      : {got.compare_files} compare · {got.verify_files} verify")
    print(f"pages swept      : {len(got.checked)}" + (f" of {book_pages}" if book_pages else ""))
    for p in sorted(got.unread):
        print(f"  ⚠ unread p{p}: {got.unread[p]}")
    if unswept:
        print(f"  unswept        : {len(unswept)} page(s): {compact(unswept)}")
    print(f"claims           : {len(rows)}")
    print(f"  confirmed      : {sum(r['real'] is True for r in rows)}  "
          f"{dict(Counter(r['severity'] for r in rows if r['real'] is True))}")
    print(f"  refuted        : {sum(r['real'] is False for r in rows)}")
    unverified = [r["id"] for r in rows if not r["verified"]]
    print(f"  unverified     : {len(unverified)}" + (f"  ⚠ no verdict yet: {', '.join(unverified[:20])}"
                                                    if unverified else ""))
    if errors:
        for e in errors:
            print(f"  ✗ {e}")
        files = sorted({e.split(":", 1)[0] for e in errors})
        print(f"=== MERGE REFUSED: {len(errors)} problem(s) in {len(files)} file(s) ({', '.join(files[:10])}); "
              "nothing written ===")
        return 1

    texts = {ledger_path: json.dumps(ledger, indent=1, ensure_ascii=False) + "\n",
             report_path: report(row, edition, got, unswept)}
    if mode == "check":
        stale = [p for p, t in texts.items() if not p.is_file() or p.read_text(encoding="utf-8") != t]
        print(f"=== MERGE CHECK: {'STALE ' + ', '.join(p.relative_to(root).as_posix() for p in stale) if stale else 'CURRENT'}"
              f" (edition {edition}) ===")
        return 1 if stale else 0
    if mode == "write":
        for p, t in texts.items():
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(t, encoding="utf-8", newline="\n")
        print(f"\nwrote {row.ledger} and {row.report} under {root.name}/")
    return 0


def read_compare(path: pathlib.Path) -> tuple[dict | None, list[str]]:
    """One compare file and what makes it untrustworthy, by the merge's own rules (compare_problems)."""
    errors: list[str] = []
    doc = read_json(path, errors) if path.is_file() else None
    if doc is None:
        return None, errors or [f"{path.name}: no such file"]
    return doc, [f"{path.name}: {p}" for p in compare_problems(path.name, doc)]


def compare_summary(path: pathlib.Path) -> tuple[dict, bool]:
    """What a compare FILE records, as the compare agent returns it (review R3-1): the file is the record, and the
    agent's return value is this command's output, so the workflow counts what the file says, never what the agent
    retyped. One JSON line: the file, its batch, the pages checked and unread, the claim ids, and its problems."""
    doc, problems = read_compare(path)
    if doc is None or problems:
        return {"file": path.name, "valid": False, "problems": problems}, False
    return {"file": path.name, "valid": True, "batch": compact(doc["batch"]),
            "pages_checked": compact(doc["pages_checked"]), "pages_unread": doc["pages_unread"],
            "claims": [f["id"] for f in doc["findings"]], "problems": []}, True


def claim_of(path: pathlib.Path, cid: str) -> tuple[dict | None, str]:
    """The claim `cid` of a compare FILE with its claim_key, or why there is none (review R3-1): the ONE source of the
    key a verifier copies is the file the merge will read, never a compare agent's return value, which can differ from
    its file by a retyped space and would have every verdict of the batch refused after its verifiers ran."""
    doc, problems = read_compare(path)
    if doc is None or problems:
        return None, "; ".join(problems)
    found = [f for f in doc["findings"] if f["id"] == cid]
    if not found:
        return None, f"{path.name} holds no claim {cid} (its claims: {[f['id'] for f in doc['findings']] or 'none'})"
    return {"claim_id": cid, "claim_key": claim_key(found[0]), "claim": found[0]}, ""


def main() -> int:
    ap = argparse.ArgumentParser()
    mode = ap.add_mutually_exclusive_group()
    mode.add_argument("--compare-summary", type=pathlib.Path, metavar="FILE",
                      help="print what one compare file records, as a JSON line (exit 1 naming its problems)")
    mode.add_argument("--claim-key", nargs=2, metavar=("FILE", "CLAIM_ID"),
                      help="print a claim of a compare file with its claim_key, as a JSON line (exit 1 naming why not)")
    mode.add_argument("--write", action="store_true", help="write the ledger and the report (the closing branch only)")
    mode.add_argument("--check", action="store_true", help="exit 1 unless the committed ledger and report are the merge")
    mode.add_argument("--self-test", action="store_true", help="run the self-test in a scratch directory")
    ap.add_argument("--root", type=pathlib.Path, default=DIR, help="the spec-reconciliation directory (default: tracked)")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.compare_summary:
        summary, ok = compare_summary(args.compare_summary)
        print(json.dumps(summary, ensure_ascii=False))
        return 0 if ok else 1
    if args.claim_key:
        found, why = claim_of(pathlib.Path(args.claim_key[0]), args.claim_key[1])
        if found is None:
            print(f"FATAL: {why}", file=sys.stderr)
            return 1
        print(json.dumps(found, ensure_ascii=False))
        return 0
    return merge(args.edition, "write" if args.write else "check" if args.check else "report", args.root)


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

    def write(path: pathlib.Path, doc) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(doc if isinstance(doc, str) else json.dumps(doc), encoding="utf-8", newline="\n")

    def run(mode: str, root: pathlib.Path, edition: int = 2002) -> tuple[int, str]:
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            rc = merge(edition, mode, root, BOOK)
        return rc, out.getvalue()

    BOOK = "".join(f"## Page {p}\ntext\n" for p in range(1, 41))
    claim = {"id": "p5-1", "page": 5, "kind": "incorrect-text", "severity": "normative", "pdf_says": "shall be",
             "markdown_says": "may be", "why_it_matters": "a rule"}
    twin = {**claim, "id": "p5-2", "pdf_says": "the other one", "markdown_says": "the othr one"}
    late = {**claim, "id": "p13-1", "page": 13, "pdf_says": "unverified text"}

    def compare(first: int, last: int, findings=(), unread=(), **over) -> dict:
        batch = list(range(first, last + 1))
        doc = {"batch": batch, "pages_checked": [p for p in batch if p not in dict(unread)],
               "pages_unread": [{"page": p, "reason": r} for p, r in unread], "findings": list(findings)}
        return {**doc, **over}

    def verify(c: dict, real: bool, why: str) -> dict:
        return {"claim_id": c["id"], "claim_key": claim_key(c), "page": c["page"], "kind": c["kind"],
                "claim": {"pdf_says": c["pdf_says"]}, "verdict": {"real": real, "reasoning": why}}

    def good(root: pathlib.Path) -> pathlib.Path:
        d = root / "2002"
        write(d / "compare-p1-p12.json", compare(1, 12, [claim, twin]))
        write(d / "compare-p13-p14.json", compare(13, 14, [late], unread=[(14, "the render came back blank")]))
        write(d / "verify-p5-2.json", verify(twin, False, "present, reflowed"))
        write(d / "verify-p5-1.json", verify(claim, True, "seen"))
        return d

    def refused(name: str, files: dict, needle: str) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            d = good(root)
            for fname, doc in files.items():
                if doc is None:
                    (d / fname).unlink()
                else:
                    write(d / fname, doc)
            rc, out = run("write", root)
            check(name, rc == 1 and needle in out and "MERGE REFUSED" in out and not (root / "LEDGER-2002.json").exists())

    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        good(root)
        write(root / "agents" / "compare-p1-p2.json", compare(1, 2))
        rc, out = run("report", root)
        check("the default mode validates and reports and writes nothing", rc == 0 and not (root / "LEDGER-2002.json").exists())
        rc, out = run("write", root)
        ledger = json.loads((root / "LEDGER-2002.json").read_text(encoding="utf-8"))
        check("--write merges 2002/ into LEDGER-2002.json: book_pages the transcription's, pages_swept the pages checked",
              rc == 0 and ledger["edition"] == 2002 and ledger["book_pages"] == 40
              and ledger["pages_swept"] == list(range(1, 14)) and (root / "2002" / "REPORT.md").is_file()
              and {"edition", "about", "book_pages", "pages_swept", "pages_unread", "claims", "sweeps", "boxes"} <= set(ledger))
        check("an unread page is recorded unswept with its reason, in the ledger and the report",
              ledger["pages_unread"] == [{"page": 14, "reason": "the render came back blank"}]
              and 14 not in ledger["pages_swept"]
              and "p14: the render came back blank" in (root / "2002" / "REPORT.md").read_text(encoding="utf-8"))
        check("the pages no compare file covers are named against the book's page count",
              "15-40" in out and "15-40" in (root / "2002" / "REPORT.md").read_text(encoding="utf-8"))
        by = {c["id"]: c for c in ledger["claims"]}
        check("each verdict reaches the claim its id names, numbered on its page",
              by["p5-1"]["batch"] == "CONFIRMED" and by["p5-2"]["batch"] == "REFUTED" and by["p13-1"]["batch"] == "UNVERIFIED"
              and (by["p5-1"]["i"], by["p5-2"]["i"]) == (0, 1))
        check("a claim refuted by its own verdict is decided REFUTED; a confirmed or unverified one is undecided",
              by["p5-2"].get("decision") == "REFUTED" and by["p5-2"].get("reason") == "present, reflowed"
              and "decision" not in by["p5-1"] and "decision" not in by["p13-1"])
        check("2023's files are not read by a 2002 merge, and its ledger is not written", not (root / "LEDGER.json").exists())
        check("--check passes when the committed ledger is the merge", run("check", root)[0] == 0)
        write(root / "2002" / "compare-p15-p16.json", compare(15, 16))
        rc, out = run("check", root)
        check("--check fails naming the stale ledger when an agent file landed after it", rc == 1 and "STALE LEDGER-2002.json" in out)
        (root / "2002" / "compare-p15-p16.json").unlink()
        ledger["claims"][0].update({"decision": "APPLIED", "edits": [{"was": "may be", "now": "shall be"}]})
        ledger["sweeps"] = [{"page": 5, "sweep": "s", "was": "may", "now": "shall"}]
        write(root / "LEDGER-2002.json", ledger)
        run("write", root)
        again = json.loads((root / "LEDGER-2002.json").read_text(encoding="utf-8"))
        check("a re-merge keeps a claim's decision and edits by its id, and the ledger's sweeps",
              again["claims"][0]["id"] == "p5-1" and again["claims"][0].get("decision") == "APPLIED"
              and again["claims"][0].get("edits") and again["sweeps"] == ledger["sweeps"])
        write(root / "2002" / "compare-p1-p12.json", compare(1, 12, [{**claim, "pdf_says": "a new claim"}, twin]))
        rc, out = run("write", root)
        check("a kept decision whose claim now says something else is refused, never moved to the new claim",
              rc == 1 and "claim p5-1 is decided 'APPLIED' on another claim" in out)
        # R2-2: the same quote under another kind is another claim; its own fresh verdict does not carry the decision.
        rekinded = {**claim, "kind": "missing-heading"}
        write(root / "2002" / "compare-p1-p12.json", compare(1, 12, [rekinded, twin]))
        write(root / "2002" / "verify-p5-1.json", verify(rekinded, True, "seen again"))
        rc, out = run("write", root)
        check("a kept decision whose claim kept its quote but changed its kind is refused (one key for verdicts and "
              "decisions)", rc == 1 and "claim p5-1 is decided 'APPLIED' on another claim" in out
              and "verify-p5-1.json" not in out)
        write(root / "2002" / "compare-p1-p12.json", compare(1, 12, [twin]))
        (root / "2002" / "verify-p5-1.json").unlink()
        write(root / "2002" / "verify-p5-1.json", verify({**twin, "id": "p5-1"}, False, "x"))
        rc, out = run("write", root)
        check("a kept decision whose claim is gone is refused, never dropped",
              rc == 1 and "p5-1 is decided 'APPLIED' but no agent file holds it" in out)
        run("write", root, 2023)
        old = json.loads((root / "LEDGER.json").read_text(encoding="utf-8"))
        check("--edition 2023 merges from agents/ into LEDGER.json in its claims-and-verdicts shape",
              set(old) == {"pages_swept", "pages_unread", "compare_files", "verify_files", "confirmed", "refuted",
                           "unverified"} and old["pages_swept"] == [1, 2])
        try:
            merge(1985, "write", root, BOOK)
            check("1985 has no row: it exits naming the table, never merging over LEDGER-1985.json", False)
        except SystemExit as ex:
            check("1985 has no row: it exits naming the table, never merging over LEDGER-1985.json",
                  "merge_reconciliation.EDITIONS" in str(ex.code) and "1985" in str(ex.code))
        try:
            run("write", root / "absent")
            check("an edition whose directory does not exist exits", False)
        except SystemExit as ex:
            check("an edition whose directory does not exist exits", "nothing to merge" in str(ex.code))

    # R1-1: a page is swept only when its agent says it compared it.
    refused("a compare file with pages_checked [] and no pages_unread is refused, never swept as its batch",
            {"compare-p1-p12.json": {"batch": list(range(1, 13)), "pages_checked": [], "findings": []}},
            "compare-p1-p12.json: `pages_unread` is missing")
    refused("a batch page in neither list is refused",
            {"compare-p13-p14.json": compare(13, 14, [late], pages_checked=[13], pages_unread=[])},
            "compare-p13-p14.json: page(s) 14 of the batch are neither checked nor unread")
    refused("a checked page outside the batch is refused",
            {"compare-p13-p14.json": compare(13, 14, [late], pages_checked=[13, 14, 500])},
            "`pages_checked` lists page(s) 500 outside the batch")
    refused("a page both checked and unread is refused",
            {"compare-p13-p14.json": compare(13, 14, [late], unread=[(14, "blank")], pages_checked=[13, 14])},
            "page(s) 14 are both checked and unread")
    refused("an unread page with no reason is refused",
            {"compare-p13-p14.json": compare(13, 14, [late], unread=[(14, " ")])}, "an entry is not {page, reason}")
    refused("a finding on a page the agent did not check is refused",
            {"compare-p13-p14.json": compare(13, 14, [{**late, "id": "p14-1", "page": 14}], unread=[(14, "blank")])},
            "finding 0 is on page 14, not a page the agent checked")
    refused("a batch whose pages do not match its file name is refused",
            {"compare-p13-p14.json": compare(13, 15, [late])}, "is not the ascending pages p13..p14")
    refused("two compare files sharing a page are refused, naming both",
            {"compare-p12-p13.json": compare(12, 13)}, "page 12 is also the batch of compare-p1-p12.json")
    refused("a batch beyond the transcription's pages is refused",
            {"compare-p41-p42.json": compare(41, 42)}, "page(s) 41-42 are not in the transcription's 1..40")
    refused("an unreadable compare file is refused by name, never skipped",
            {"compare-p13-p14.json": "{not json"}, "compare-p13-p14.json: unreadable")
    # R1-2: a verdict attaches to exactly the claim its id names, once.
    refused("a finding with no id is refused",
            {"compare-p1-p12.json": compare(1, 12, [{k: v for k, v in claim.items() if k != "id"}, twin])},
            "finding 0 has id None, not p5-<n>")
    refused("findings of one page numbered out of order are refused",
            {"compare-p1-p12.json": compare(1, 12, [twin, claim])}, "the findings of page 5 are numbered [2, 1]")
    refused("a verdict naming no known claim is refused",
            {"verify-p9-1.json": verify({**claim, "id": "p9-1", "page": 9}, False, "x")},
            "verify-p9-1.json: `claim_id` 'p9-1' names no claim")
    refused("a second verdict for one claim is refused",
            {"verify-p5-1-again.json": verify(claim, False, "x")}, "a second verdict for claim p5-1")
    # R2-1: an id is reused by a re-run; the verdict attaches only to the claim whose content it judged.
    refused("a verdict left from an earlier run of a re-run batch is refused by name, never attached to the new p5-1",
            {"compare-p1-p12.json": compare(1, 12, [{**claim, "pdf_says": "{| ADVANCING |} lost its choice bars"},
                                                   twin])},
            "verify-p5-1.json: it judged claim key")
    refused("a verdict with no claim_key is refused",
            {"verify-p5-1.json": {k: v for k, v in verify(claim, True, "x").items() if k != "claim_key"}},
            "verify-p5-1.json: it judged claim key None")
    refused("a verdict whose claim differs from its claim's only in kind is refused",
            {"verify-p5-1.json": verify({**claim, "kind": "missing-text"}, True, "x")}, "verify-p5-1.json: it judged")
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        d = good(root)
        write(d / "compare-p1-p12.json", compare(1, 12, [claim, twin], notes="a re-run that found the same claims"))
        rc, _ = run("write", root)
        by = {c["id"]: c for c in json.loads((root / "LEDGER-2002.json").read_text(encoding="utf-8"))["claims"]}
        check("a verdict of an earlier run attaches when the re-run holds the identical claim (same key)",
              rc == 0 and by["p5-1"]["batch"] == "CONFIRMED" and by["p5-2"]["batch"] == "REFUTED")
    check("claim_key is the content: page and every text field change it, severity and id do not",
          len({claim_key(claim), claim_key({**claim, "page": 6}), claim_key({**claim, "kind": "ocr-error"}),
               claim_key({**claim, "pdf_says": "x"}), claim_key({**claim, "markdown_says": "x"}),
               claim_key({**claim, "why_it_matters": "x"})}) == 6
          and claim_key(claim) == claim_key({**claim, "id": "p5-9", "severity": "cosmetic"}))
    refused("a verdict with no `real` is refused, never counted refuted",
            {"verify-p5-1.json": {**verify(claim, True, "x"), "verdict": {"reasoning": "x"}}}, "`verdict` is not")
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        a = {**claim, "id": "p20-1", "page": 20, "kind": "incorrect-figure", "pdf_says": "[ WITH POINTER ]"}
        b = {**a, "id": "p20-2", "pdf_says": "{| ADVANCING |} lost bars"}
        write(root / "2002" / "compare-p13-p24.json", compare(13, 24, [a, b]))
        write(root / "2002" / "verify-p20-1.json", verify(a, False, "A is present, reflowed"))
        run("write", root)
        by = {c["id"]: c for c in json.loads((root / "LEDGER-2002.json").read_text(encoding="utf-8"))["claims"]}
        check("a claim with no verdict of its own stays UNVERIFIED and undecided beside a refuted claim of its page and kind",
              by["p20-1"]["batch"] == "REFUTED" and by["p20-2"]["batch"] == "UNVERIFIED" and "decision" not in by["p20-2"])
    # R3-1: the key a verifier copies, and what a compare agent returns, come from the compare FILE.
    with tempfile.TemporaryDirectory() as tmp:
        f = pathlib.Path(tmp) / "compare-p1-p12.json"
        write(f, compare(1, 12, [claim, twin], unread=[(12, "blank render")]))
        returned = {**claim, "pdf_says": claim["pdf_says"] + " "}   # the agent's return retyped one space
        found, why = claim_of(f, "p5-1")
        check("--claim-key gives the FILE's key when the agent's return differs from its file by one space",
              found is not None and found["claim_key"] == claim_key(claim) != claim_key(returned)
              and found["claim"]["pdf_says"] == "shall be")
        found, why = claim_of(f, "p7-1")
        check("--claim-key for a claim id the file does not hold fails naming the file, the id and its claims",
              found is None and "compare-p1-p12.json holds no claim p7-1" in why and "p5-1" in why)
        cli = subprocess.run([sys.executable, __file__, "--claim-key", str(f), "p7-1"], capture_output=True, text=True)
        check("the --claim-key command exits 1 naming why", cli.returncode == 1 and "holds no claim p7-1" in cli.stderr)
        summary, ok = compare_summary(f)
        check("--compare-summary reports what the file records: pages checked and unread, claim ids",
              ok and summary["pages_checked"] == "1-11" and summary["pages_unread"][0]["page"] == 12
              and summary["claims"] == ["p5-1", "p5-2"])
        write(f, compare(1, 12, [claim], pages_checked=[], pages_unread=[]))
        summary, ok = compare_summary(f)
        found, why = claim_of(f, "p5-1")
        check("an invalid compare file gives no summary and no key, naming its problems",
              not ok and not summary["valid"] and "neither checked nor unread" in " ".join(summary["problems"])
              and found is None and "neither checked nor unread" in why)
        missing, ok = compare_summary(pathlib.Path(tmp) / "compare-p13-p24.json")
        check("a compare file that was never written is named, never summarised as empty",
              not ok and missing["problems"] == ["compare-p13-p24.json: no such file"])
    bad = subprocess.run([sys.executable, __file__, "--edition", "1999"], capture_output=True, text=True)
    check("an edition the standard has no transcription of is refused by the argument itself",
          bad.returncode == 2 and "1999" in bad.stderr)
    print("=== MERGE RECONCILIATION SELF-TEST: " + ("PASS" if not failures else f"FAIL ({len(failures)})") + " ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
