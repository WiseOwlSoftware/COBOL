"""docchecks/inventory.py — the traceability inventory's anchors INTO Markdown documents (kb/Work PB4453).

A row's `code-location` may be `docs/CONFORMANCE.md#DOC-A.1-19` (the §7 determination its evidence is) and its `notes` may
spell `path#Symbol` for a document too (346 such anchors today, every one into docs/CONFORMANCE.md). The Unit test
SpecTraceabilityInventoryDriftTests resolved every anchor by reading the file, which made CONFORMANCE.md a compiler-test
input; the C# test now resolves an anchor into any file but a document, and this module resolves the ones into a document by
the rule it used: the file exists, and the symbol is a whole identifier in its text (no identifier character on either side).
"""
from __future__ import annotations

import json
import re
from pathlib import Path

from . import common
from .common import Check

import inventory_schema as schema  # noqa: E402
import inventory_view  # noqa: E402

KINDS_PATH = "tests/doc-facts/inventory-anchor-kinds.json"
#: A `path#Symbol` spelling inside a row's prose: the form a code-location is written in.
NOTES_LOCATION = re.compile(r"(?<![A-Za-z0-9_./-])((?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+\.[A-Za-z0-9]+)#([A-Za-z0-9_.<>-]+)")


def whole_word(symbol: str, text: str) -> bool:
    return re.search(r"(?<![A-Za-z0-9_])" + re.escape(symbol) + r"(?![A-Za-z0-9_])", text) is not None


def anchor_findings(rows: list[dict], texts: dict[str, str], code_location_sep: str, prose_of,
                    document_kinds: tuple[str, ...] = (".md",)) -> list[str]:
    """Every anchor into a file the compiler test does not resolve (a code-location `X.md#Symbol`, or one spelled in a row's
    prose) that names a missing file or a symbol that is no longer a whole identifier in it. `texts` is each such file's text
    by path; `document_kinds` are the extensions this audit resolves and the Unit test does not (`tests/doc-facts/inventory-anchor-kinds.json`)."""
    out: list[str] = []
    checked = 0

    def judge(rule_id: str, where: str, path: str, symbol: str) -> None:
        nonlocal checked
        if not path.lower().endswith(document_kinds):
            return
        checked += 1
        if path not in texts:
            out.append(f"{rule_id}: {where} '{path}#{symbol}' — no such file")
        elif not whole_word(symbol, texts[path]):
            out.append(f"{rule_id}: {where} '{path}#{symbol}' — '{symbol}' is no longer in that file")

    for r in rows:
        for loc in common.split_locations(r.get("code-location") or "", code_location_sep):
            path, hash_, symbol = loc.partition("#")
            if hash_:
                judge(r["rule-id"], "code-location", path, symbol)
        for m in NOTES_LOCATION.finditer(prose_of(r.get("notes") or "")):
            judge(r["rule-id"], "notes cite", m.group(1), m.group(2).rstrip(".-"))
    if checked < 100:
        out.append(f"only {checked} Markdown anchor(s) found in the inventory: the reader is broken and the check is vacuous")
    return out


def _inventory(root: Path) -> list[str]:
    rows = inventory_view.inventory()
    sch = schema.load_schema()
    resolved = tuple(json.loads(common.text(root, KINDS_PATH))["documentKinds"])
    paths = {p for r in rows for p in _paths(r, sch.code_location_sep, resolved)}
    texts = {p: common.text(root, p) for p in paths if (root / p).is_file()}
    return anchor_findings(rows, texts, sch.code_location_sep, schema.prose_of, resolved)


def _paths(row: dict, sep: str, resolved: tuple[str, ...]) -> set[str]:
    found = {loc.partition("#")[0] for loc in common.split_locations(row.get("code-location") or "", sep) if "#" in loc}
    found |= {m.group(1) for m in NOTES_LOCATION.finditer(schema.prose_of(row.get("notes") or ""))}
    return {p for p in found if p.lower().endswith(resolved)}


CHECKS = (Check("INVENTORY ANCHORS into documents resolve", _inventory),)


def self_test() -> list[str]:
    rows = [{"rule-id": f"DOC-A.1-{n}", "code-location": f"src/a.cs#X; docs/CONFORMANCE.md#DOC-A.1-{n}", "notes": ""} for n in range(120)]
    texts = {"docs/CONFORMANCE.md": " ".join(f"DOC-A.1-{n}" for n in range(120)) + " DOC-A.1-1000x"}
    noted = {"rule-id": "SR-1", "code-location": "", "notes": "see docs/CONFORMANCE.md#DOC-A.1-7. and docs/gone.md#X"}
    ident = lambda s: s  # noqa: E731 — the prose reader of a fabricated row: no retired-witness mark
    return common.self_test_arms([
        ("whole anchors resolve", anchor_findings(rows, texts, "; ", ident), []),
        ("a symbol the file lost is a finding", len(anchor_findings(rows + [{"rule-id": "X", "code-location": "docs/CONFORMANCE.md#DOC-A.1-999"}],
                                                                     texts, "; ", ident)), 1),
        ("a symbol that is only a prefix of an identifier is not whole", len(anchor_findings(
            rows + [{"rule-id": "X", "code-location": "docs/CONFORMANCE.md#DOC-A.1-100"}], {**texts, "docs/CONFORMANCE.md": texts["docs/CONFORMANCE.md"].replace("DOC-A.1-100 ", "DOC-A.1-100x ")}, "; ", ident)), 2),
        ("a file that is not there is a finding", len(anchor_findings(rows + [noted], texts, "; ", ident)), 1),
        ("a source file's anchor is the compiler test's, not judged here", anchor_findings(rows + [{"rule-id": "Y", "code-location": "src/q.cs#Nope"}],
                                                                                           texts, "; ", ident), []),
        ("too few anchors measure nothing", len(anchor_findings(rows[:3], texts, "; ", ident)), 1),
    ])
