"""docchecks/vcr.py — docs/VERSION_CHANGE_REFERENCE.md against tests/version-matrix/constructs.json and the ISO text
(kb/Work PB4453; was the Conformance test VcrDriftTests and scripts/gen-vcr.ps1, which ran it with an environment variable).

Four rules: every `<!-- gate:CONSTRUCT-ID -->` anchor names a construct constructs.json holds (forward coverage); every
`§clause` and every `§clause `quoted text`` the document writes resolves in specs/ISO_COBOL_2023.md (the standard's text is
read HERE, never by a compiler test); the document cites no spec LINE number (it does not survive a spec edit); and the
generated gating status index between the GEN:VCR-STATUS markers is what the anchors and constructs.json render.
`python scripts/doc_audits.py --write-vcr` regenerates the index.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

from . import common
from .common import Check

CONSTRUCTS = "tests/version-matrix/constructs.json"
SPEC = "specs/ISO_COBOL_2023.md"
START, END = "<!-- GEN:VCR-STATUS START -->", "<!-- GEN:VCR-STATUS END -->"
GATE_ANCHOR = re.compile(r"<!--\s*((?:gate:[^\s>]+\s*)+)-->")
# A clause number is either the body's dotted decimal (14.9.39.4) or an annex's letter-headed form (E.2, A.4.14); the
# title is optional because all 178 headings of clause 3 are a bare number. Same grammar as scripts/spec/cite.py.
SPEC_HEADING = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s+.*?)?\s*$")
SPEC_REF = re.compile(r"§([0-9A-Z][0-9A-Za-z.]*)")
CITATION = re.compile(r"§([0-9A-Z][0-9A-Za-z.]*)\s+`([^`]+)`")
LINE_REF = re.compile(r"@[0-9]{3,6}(\s*[-–]\s*[0-9]{3,6})?")


def gate_anchors(vcr_lines: list[str]) -> list[tuple[str, list[str]]]:
    """Only real change-table rows carry anchors (a line starting `| <rownum> |`), so the legend prose that shows
    `<!-- gate:CONSTRUCT-ID -->` as an EXAMPLE is not parsed as a gate. EVERY anchor comment on the row counts (kb/Work PB725)."""
    out = []
    for ln in vcr_lines:
        rm = common.ROW_NUMBER.match(ln)
        if not rm:
            continue
        ids = [t[len("gate:"):] for m in GATE_ANCHOR.finditer(ln) for t in m.group(1).split() if t.startswith("gate:")]
        if ids:
            out.append((rm.group(1), ids))
    return out


def row_sort_key(row: str) -> int:
    digits = re.match(r"[0-9]*", row).group(0)
    return int(digits) if digits else 2**31 - 1


def render_index(vcr_lines: list[str], constructs: dict[str, dict]) -> str:
    by_id: dict[str, set[str]] = {}
    for row, ids in gate_anchors(vcr_lines):
        for i in ids:
            by_id.setdefault(i, set()).add(row)
    out = ["| Construct | Introduced | Removed/obsolete | Diagnostic | Status | VCR row(s) |", "| --- | --- | --- | --- | --- | --- |"]
    for cid in sorted(by_id):
        c = constructs[cid]  # the forward-coverage check guarantees existence; a KeyError here is a real drift
        window = (f"removed {c['removedIn']}" if c.get("removedIn") is not None
                  else f"obsolete {c['obsoleteIn']}" if c.get("obsoleteIn") is not None else "—")
        status = "done" if (c.get("status") or "active") == "active" else "pending"
        rows = ", ".join(sorted(by_id[cid], key=lambda r: (row_sort_key(r), r)))
        out.append(f"| {cid} | {c['introducedIn']} | {window} | {c['diagnosticCode']} | {status} | {rows} |")
    return "\n".join(out)


def load_constructs(root: Path) -> dict[str, dict]:
    return {c["id"]: c for c in json.loads(common.text(root, CONSTRUCTS))["constructs"]}


def anchor_findings(vcr_lines: list[str], constructs: dict[str, dict]) -> list[str]:
    return [f"VCR row {row}: gate:{i} names a construct absent from constructs.json (forward coverage)"
            for row, ids in gate_anchors(vcr_lines) for i in ids if i not in constructs]


def normalize(s: str) -> str:
    return re.sub(r"\s+", " ", re.sub(r"[^\w\s]", " ", s)).strip().lower()


def clause_regions(spec_lines: list[str]) -> dict[str, list[str]]:
    heads = [(m.group(1), i) for i, ln in enumerate(spec_lines) if (m := SPEC_HEADING.match(ln))]
    regions: dict[str, list[str]] = {}
    for k, (num, at) in enumerate(heads):
        end = heads[k + 1][1] if k + 1 < len(heads) else len(spec_lines)
        regions.setdefault(num, []).extend(normalize(spec_lines[i]) for i in range(at, end))
    return regions


def citation_findings(vcr_lines: list[str], regions: dict[str, list[str]]) -> list[str]:
    bad = []
    for i, ln in enumerate(vcr_lines, 1):
        for m in SPEC_REF.finditer(ln):
            clause = m.group(1).rstrip(".")
            if clause not in regions:
                bad.append(f"line {i}: §{clause} is not a clause of the standard")
        for m in CITATION.finditer(ln):
            clause = m.group(1).rstrip(".")
            if clause in regions:
                needle = normalize(m.group(2))
                if not any(needle in line for line in regions[clause]):
                    bad.append(f"line {i}: §{clause} exists but does not contain `{m.group(2)}`")
    return [f"VERSION_CHANGE_REFERENCE.md spec citation does not resolve (re-derive with `python scripts/spec/cite.py --find "
            f"\"<text>\"`): {b}" for b in bad]


def line_number_findings(vcr_lines: list[str]) -> list[str]:
    bad = []
    for i, ln in enumerate(vcr_lines, 1):
        for m in LINE_REF.finditer(ln):
            digits = m.group(0).lstrip("@")
            # `@2023` / `@2014` name an EDITION, not a line.
            if len(digits) == 4 and digits[0] == "2" and 1985 <= int(digits) <= 2100:
                continue
            bad.append(f"line {i}: {m.group(0)}")
    return [f"VERSION_CHANGE_REFERENCE.md cites a spec LINE number, which does not survive a spec edit: cite the clause and "
            f"quote the sentence instead: {b}" for b in bad]


def index_findings(vcr: str, rendered: str) -> list[str]:
    s, e = vcr.find(START), vcr.find(END)
    if not (0 <= s < e):
        return [f"VCR generated-block markers not found ({START} … {END})"]
    committed = vcr[s + len(START):e].strip("\n")
    if committed == rendered:
        return []
    return ["docs/VERSION_CHANGE_REFERENCE.md gating status index is out of sync: regenerate it with "
            "`python scripts/doc_audits.py --write-vcr`"]


def write_index(root: Path) -> None:
    vcr = common.text(root, common.VCR)
    rendered = render_index(vcr.split("\n"), load_constructs(root))
    s, e = vcr.index(START), vcr.index(END)
    (root / common.VCR).write_text(vcr[:s + len(START)] + "\n" + rendered + "\n" + vcr[e:], encoding="utf-8", newline="\n")


def _anchors(root: Path) -> list[str]:
    return anchor_findings(common.lines(root, common.VCR), load_constructs(root))


def _citations(root: Path) -> list[str]:
    return citation_findings(common.lines(root, common.VCR), clause_regions(common.lines(root, SPEC)))


def _line_numbers(root: Path) -> list[str]:
    return line_number_findings(common.lines(root, common.VCR))


def _index(root: Path) -> list[str]:
    vcr = common.text(root, common.VCR)
    return index_findings(vcr, render_index(vcr.split("\n"), load_constructs(root)))


CHECKS = (
    Check("VCR GATE ANCHORS name real constructs", _anchors),
    Check("VCR SPEC CITATIONS resolve in the standard", _citations),
    Check("VCR cites no spec line number", _line_numbers),
    Check("VCR GATING STATUS INDEX is in sync", _index),
)


def self_test() -> list[str]:
    constructs = {"alpha": {"id": "alpha", "introducedIn": 2002, "diagnosticCode": "COBOL1", "status": "active"},
                  "beta": {"id": "beta", "introducedIn": 2014, "removedIn": 2023, "diagnosticCode": "COBOL2", "status": "pending"}}
    vcr = ["| 7.20 | <!-- gate:alpha --> <!-- gate:beta -->", "| 28 | <!-- gate:alpha gate:beta -->", "legend `<!-- gate:ghost -->`"]
    spec = ["## 14.9.1 Title", "the quick brown fox", "## 14.9.2 Next", "other text"]
    rendered = render_index(vcr, constructs)
    framed = f"x\n{START}\n{rendered}\n{END}\ny"
    return common.self_test_arms([
        ("both spellings of a multi-anchor row are read", gate_anchors(vcr), [("7.20", ["alpha", "beta"]), ("28", ["alpha", "beta"])]),
        ("an anchor naming no construct is a finding", len(anchor_findings(["| 3 | <!-- gate:ghost -->"], constructs)), 1),
        ("the legend's example is no anchor", anchor_findings(vcr, constructs), []),
        ("the index lists rows numerically then by name",
         render_index(["| 100 | <!-- gate:alpha -->", "| 7.20 | <!-- gate:alpha -->", "| 28 | <!-- gate:alpha -->"], constructs).split("\n")[2].rsplit("| ", 1)[1],
         "7.20, 28, 100 |"),
        ("the status and window columns", rendered.split("\n")[3], "| beta | 2014 | removed 2023 | COBOL2 | pending | 7.20, 28 |"),
        ("an index equal to the rendering is in sync", index_findings(framed, rendered), []),
        ("a stale index is a finding", len(index_findings(framed.replace("2002", "1999"), rendered)), 1),
        ("a missing marker is a finding", len(index_findings("no markers", rendered)), 1),
        ("a resolving citation is clean", citation_findings(["§14.9.1 `quick BROWN fox`"], clause_regions(spec)), []),
        ("an unknown clause is a finding", len(citation_findings(["see §99.9"], clause_regions(spec))), 1),
        ("a quote the clause lacks is a finding", len(citation_findings(["§14.9.2 `quick brown`"], clause_regions(spec))), 1),
        ("a spec line number is a finding", len(line_number_findings(["see @1234 - 1300"])), 1),
        ("an edition marker is not a line number", line_number_findings(["added @2023 and @2014"]), []),
    ])
