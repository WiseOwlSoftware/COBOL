#!/usr/bin/env python3
"""The P14 traceability-inventory schema, loaded from data — imported by every Python consumer.

⛔ WHERE THE RULES LIVE. The verdict vocabulary, each verdict's `resolves` flag and each verdict's required
evidence fields are DATA, in `tests/version-matrix/inventory-schema.json`. Nothing here — and nothing in
`build_inventory.py`, `record_verdicts.py` or the C# gate — carries its own copy of that list. Adding a verdict is
an edit to the JSON and nothing else. This is CLAUDE.md rule 5 applied to the thing most likely to drift: the DA
wave's whole lesson (`feedback_one_rule_one_place`) was that every symptom traced back to one rule written down in
more than one place.

⚖ WHAT IS AND IS NOT DUPLICATED. Two engines evaluate these rules at two different times: this module at RECORD
time, and `SpecTraceabilityInventoryDriftTests` at BATTERY time. Both read the same JSON, so neither hard-codes a
verdict name, a flag or a field — what exists twice is the ten-line evaluator, not the rule. And the divergence
that would matter is itself gated: the C# side asserts every row's stored `state` equals the state the schema
implies, so a Python evaluator that drifted from the C# one turns the battery red instead of quietly inflating the
burn-down.

DIVISION OF LABOUR between the two, deliberately with NO overlap:
  · SHAPE      — verdict is in the vocabulary, required fields present, editions legal, rule-id known.
                 Checked HERE, at record time, so a malformed batch never reaches the inventory.
  · REFERENCE  — the code-location symbol still exists, the test-ref names a test that is really on disk.
                 Checked ONLY by the C# battery gate, because it must keep holding as the tree changes underneath
                 an already-recorded row. Re-checking it here would be the same rule in two places.
"""
from __future__ import annotations

import json
import pathlib
import re
from typing import Any

REPO = pathlib.Path(__file__).resolve().parents[2]
SCHEMA_PATH = REPO / "tests" / "version-matrix" / "inventory-schema.json"
CATALOG_PATH = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"
INVENTORY_PATH = REPO / "tests" / "version-matrix" / "traceability-inventory.json"

#: The adjudicated fields — the ones a re-run of build_inventory.py must carry forward, and the ones a verdict
#: record may set. Everything else on a row is regenerated from the catalog and is not the reviewer's to write.
ADJUDICATED = ("verdict", "code-location", "test-ref", "editions", "notes")


class Schema:
    """`tests/version-matrix/inventory-schema.json`, parsed."""

    def __init__(self, raw: dict[str, Any]) -> None:
        self._raw = raw
        self.verdicts: dict[str, dict[str, Any]] = raw["verdicts"]
        self.editions: list[str] = raw["editions"]
        self.code_location_re = re.compile(raw["code-location"]["pattern"])
        self.code_location_sep: str = raw["code-location"]["separator"]
        self.test_ref_sep: str = raw["test-ref"]["separator"]
        self.test_ref_forms: dict[str, dict[str, Any]] = raw["test-ref"]["forms"]

    @property
    def resolving(self) -> set[str]:
        """The verdicts that can close a GAP — everything else leaves the row open by construction."""
        return {v for v, d in self.verdicts.items() if d["resolves"]}

    def requires(self, verdict: str) -> list[str]:
        return list(self.verdicts[verdict]["requires"])

    def state_for(self, row: dict[str, Any]) -> str:
        """A row is OK only when its verdict resolves AND every field that verdict requires is filled.

        This is the definition of DONE from `DESIGN-spec-conformance-review.md` §1, evaluated rather than
        asserted: (a) a located implementation or a recorded non-support decision, (b) a spec-verified verdict,
        (c) a covering spec-derived test. A row that merely says CONFORMS with nothing behind it stays a GAP —
        which is the point, since the GAP count is the v1.0 completion metric and must never be cheaper to move
        than the work it stands for.
        """
        verdict = row.get("verdict") or ""
        if verdict not in self.resolving:
            return "GAP"
        return "OK" if all(row.get(f) for f in self.requires(verdict)) else "GAP"

    def split(self, value: str, sep: str) -> list[str]:
        return [p for p in (p.strip() for p in value.split(sep.strip())) if p]


def load_schema() -> Schema:
    if not SCHEMA_PATH.exists():
        raise SystemExit(f"inventory schema not found: {SCHEMA_PATH}")
    return Schema(json.loads(SCHEMA_PATH.read_text(encoding="utf-8")))


def load_catalog() -> list[dict[str, Any]]:
    if not CATALOG_PATH.exists():
        raise SystemExit(
            f"catalog not found: {CATALOG_PATH}\nRun: python scripts/spec/extract_rule_catalog.py")
    return json.loads(CATALOG_PATH.read_text(encoding="utf-8"))["rules"]


def load_inventory() -> list[dict[str, Any]]:
    if not INVENTORY_PATH.exists():
        raise SystemExit(
            f"inventory not found: {INVENTORY_PATH}\nRun: python scripts/spec/build_inventory.py")
    return json.loads(INVENTORY_PATH.read_text(encoding="utf-8"))


def write_inventory(rows: list[dict[str, Any]]) -> None:
    """Write the inventory ATOMICALLY — a temp file in the same directory, then an os-level replace.

    The inventory is a 1 MB file that accumulates many sessions of adjudicated work. A torn write (an interrupt, a
    full disk, two writers racing) would not merely lose the batch in flight, it would corrupt every verdict
    recorded before it, and the JSON would not necessarily fail to parse — a truncated array can still be a
    perfectly valid, silently shorter one.
    """
    INVENTORY_PATH.parent.mkdir(parents=True, exist_ok=True)
    tmp = INVENTORY_PATH.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(rows, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    tmp.replace(INVENTORY_PATH)
