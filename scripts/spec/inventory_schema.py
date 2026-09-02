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
  · SHAPE      — verdict is in the vocabulary, required fields present, editions legal, rule-id known, and — for a
                 kind that declares evidence rules (`kinds`) — the computed register anchor is present and every
                 fragment on an `anchored-files` path is a legal anchor of that file's space. All of it decidable
                 from the record and the schema, with no disk access.
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
        self.spec_derived_required: bool = raw["test-ref"]["spec-derived-required"]
        self.disqualifying_methods = [re.compile(p) for p in raw["test-ref"]["disqualifying-method-patterns"]]
        #: Per-KIND evidence rules (`kinds` in the schema) — the `$comment` key is documentation, never a kind.
        self.kinds: dict[str, dict[str, Any]] = {
            k: v for k, v in raw.get("kinds", {}).items() if not k.startswith("$")}
        self._impl: dict[str, re.Pattern[str]] = {
            k: re.compile(v["implementation-pattern"]) for k, v in self.kinds.items()
            if v.get("implementation-pattern")}
        #: Files whose '#fragment' is a NAMED ANCHOR SPACE, mapped to the pattern every fragment must match.
        self.anchored_files: dict[str, re.Pattern[str]] = {
            f: re.compile(p) for f, p in raw["code-location"].get("anchored-files", {}).items()}

    def locations(self, row: dict[str, Any]) -> list[str]:
        """The row's `code-location` list, split on the schema's own separator."""
        return self.split(row.get("code-location", "") or "", self.code_location_sep)

    def anchor_for(self, row: dict[str, Any]) -> str | None:
        """The register anchor this row's KIND obliges, COMPUTED from the row's own rule-id — or None.

        ⛔ Written ONCE per language and read by everything that needs it (`state_for` here,
        `record_verdicts.validate`, and `AnchorFor` on the C# side). The anchor is a function of the row, so a
        determination cannot be filed under the wrong item the way kb/Work A11 recorded: the §15.3.3.2
        fractional-seconds determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's
        accuracy, because the number was inherited and never re-derived.
        """
        kind = self.kinds.get(row.get("kind") or "")
        if not kind or not kind.get("anchor-template"):
            return None
        return kind["anchor-template"].replace("{rule-id}", row.get("rule-id", ""))

    def is_observable(self, row: dict[str, Any]) -> bool:
        """Does this row name a site in the compiler a program could observe the determination through?

        The predicate is the row's OWN `code-location` list read through its kind's `implementation-pattern` —
        the greenfield `src/Cobol.Net.*` predicate `audit_annex_a1.py`'s source sweep already applies. A path is
        falsifiable by an independent reader; an opinion is not. A row of a kind that declares no
        implementation-pattern is vacuously observable, so adding a kind never tightens an existing one.
        """
        rx = self._impl.get(row.get("kind") or "")
        if rx is None:
            return True
        anchor = self.anchor_for(row)
        return any(rx.search(loc) for loc in self.locations(row) if loc != anchor)

    @property
    def resolving(self) -> set[str]:
        """The verdicts that can close a GAP — everything else leaves the row open by construction."""
        return {v for v, d in self.verdicts.items() if d["resolves"]}

    def requires(self, verdict: str) -> list[str]:
        return list(self.verdicts[verdict]["requires"])

    def is_spec_derived(self, reference: str) -> bool:
        """Can this ONE test-ref be the basis for closing a row?

        Two ways to fail. The FORM can be inherently differential — a NIST CCVS golden, a characterization
        snapshot — which CLAUDE.md rule 1 makes a regression net rather than authority. Or the form can be
        spec-derived-capable while the specific test is not: an xUnit test named `*_MatchesLegacy` says in its own
        name that its expected value came from the legacy engine.
        """
        scheme, _, body = reference.partition(":")
        form = self.test_ref_forms.get(scheme)
        if form is None or not form.get("spec-derived"):
            return False
        method = body.rsplit(".", 1)[-1].strip()
        return not any(rx.search(method) for rx in self.disqualifying_methods)

    def state_for(self, row: dict[str, Any]) -> str:
        """A row is OK only when its verdict resolves, its required evidence is present, AND a SPEC-DERIVED test
        covers it.

        This is the definition of DONE from `DESIGN-spec-conformance-review.md` §1, evaluated rather than
        asserted: (a) a located implementation or a recorded non-support decision, (b) a spec-verified verdict,
        (c) a covering spec-derived test. A row that merely says CONFORMS with nothing behind it stays a GAP —
        the point being that the GAP count is the v1.0 completion metric and must never be cheaper to move than
        the work it stands for.

        ⛔ The spec-derived clause is separate from `requires` ON PURPOSE, and it is the whole reason
        CONFORMS-but-untested is expressible: the rule can be verified against the code (a real, recordable
        finding) while no test yet pins it (so the row is not done). Folding the test into `requires` would force
        every such row to be misrecorded as PARTIAL or, worse, closed on whatever test happened to touch the
        function — which on the first Phase-B batch would have closed seven rows on NIST goldens and a
        `*_MatchesLegacy` differential.

        ⛔ A ROW OF A KIND THAT DECLARES EVIDENCE RULES PAYS THOSE TOO, and both are extra COSTS rather than an
        escape: its computed register anchor must be among its locations (a determination filed under the wrong
        item is not evidence for this row), and it must name an implementing site — a DOC row with nothing in the
        compiler to observe stays a GAP, because closing it would widen §1(a)'s definition of DONE and that is
        the owner's to widen (kb/Work PB280 Q2). The spec-derived test clause below then applies unchanged.
        """
        verdict = row.get("verdict") or ""
        if verdict not in self.resolving:
            return "GAP"
        if not all(row.get(f) for f in self.requires(verdict)):
            return "GAP"
        if (anchor := self.anchor_for(row)) is not None:
            if anchor not in self.locations(row):
                return "GAP"
            if not self.is_observable(row):
                return "GAP"
        if not self.spec_derived_required:
            return "OK" if row.get("test-ref") else "GAP"
        refs = self.split(row.get("test-ref", ""), self.test_ref_sep)
        return "OK" if any(self.is_spec_derived(r) for r in refs) else "GAP"

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
