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

#: A parenthesised or semicolon-introduced clause citation inside a rule's own text — the form Annex A.1 uses to
#: point an implementor-defined documentation obligation at the clause that creates it:
#: "(13.18.40, PICTURE clause, General rule 15; 13.18.60, USAGE clause, General rule 3)".
XREF_CITATION = re.compile(r"[(;]\s*(\d+(?:\.\d+)*)\s*,")

#: ⛔ THE TRANSCRIPTION SPELLS A COBOL OPERAND NAME'S HYPHEN TWO WAYS AND A PREDICATE CANNOT SEE THE DIFFERENCE.
#: 29 catalog rules carry U+2011 NON-BREAKING HYPHEN where every other rule carries ASCII '-': SR-13.18.14.3-12
#: reads "Identifier‑1 shall be described in the file, working‑storage, …". A selector arm written
#: `\bidentifier-1\b` — the obvious spelling, and the one four of these six selectors need — silently matches
#: NOTHING there, and a selector that silently under-selects is the failure mode feedback_measure_the_selectors_-
#: complement names: the drift test stays green and the rows stay unstamped. Folding it HERE, in the one place
#: both engines read the text through, is the fix that makes the next selector immune; putting the alternation in
#: each pattern instead would be the same rule written down N times, which is what CLAUDE.md rule 5 forbids.
#: Only the two unambiguous HYPHEN code points are folded — U+2013 EN DASH is left alone, because the standard
#: uses it both as the MINUS-sign glyph and as a clause-range separator ("13.16–13.18").
HYPHENS = {0x2010: "-", 0x2011: "-"}


def section_matches(prefix: str, section: str) -> bool:
    """Does `section` fall under `prefix`, COMPONENT-WISE?

    ⛔ THIS IS THE ONE RULE THAT MUST NOT BE A RAW `startswith`, AND IT IS WHY THIS FUNCTION EXISTS AT ALL.
    A clause number is a dotted PATH, not a string: §13.18.30 is not inside §13.18.3, and §13.18.40 (PICTURE) is
    not inside §13.18.4 (BACKGROUND-COLOR). A raw prefix test says otherwise. Measured on the A.4.2 screen
    selector, whose clause arm names §13.18.3, §13.18.4, §13.18.6, §13.18.7 and §13.18.9 among others: the raw
    test selects 538 rules where the component-wise test selects 149 — 389 extra, 33 of them already adjudicated
    (32 CONFORMS, 1 DOCUMENTED-NON-SUPPORT), i.e. a silent flip of thirty-two verified rows to non-support.

    Trailing dots in the DATA are accepted and ignored: "13.18.3" and "13.18.3." mean the same path here, so the
    defensive spelling a previous author reached for cannot become load-bearing again.
    """
    p = [c for c in prefix.strip().strip(".").split(".") if c]
    s = [c for c in section.strip().strip(".").split(".") if c]
    return len(p) <= len(s) and s[: len(p)] == p


class DerivedSelector:
    """One `derived-verdicts` entry — a verdict that follows MECHANICALLY from one owner determination.

    ⛔ THE PREDICATE IS DATA AND BOTH ENGINES READ IT. This class and
    `tests/Cobol.Net.Tests.Unit/DerivedVerdictDriftTests.Select` are the two evaluators; neither carries a copy of
    a selector. Until 2026-09-02 the schema's own $comment claimed that already — and it was false: no Python
    consumer of `derived-verdicts` existed, so the PB198 batch was produced by hand and the "two readers" that
    justify writing the predicate as data were one reader and an aspiration.

    THE SHAPE IS `arms`, and the arms are a DISJUNCTION of CONJUNCTIONS. Within an arm every field that is
    present must hold; a rule is selected when ANY arm holds and NO global `excludes-patterns` entry matches its
    text. Four modules of the 2026-09-02 landing needed four different scoping mechanisms and the flat
    requires-sections/requires-pattern pair could express only two of them:
      · `sections`       the rule's OWN clause (§8.8.1.4 is titled for the declined mode — PB198's lesson)
      · `pattern`        the rule's TEXT
      · `sections` + `pattern` together — an AND-gate. `file-name-1` is STATEMENT-LOCAL: in §13.4.5.4 it is the
        file description entry's own subject, so A.4.13's text arm ungated selects 176 rules instead of 12. The
        same gate is what reaches a rule scoped by OPERAND NAME — §12.3.7.3's data-name-1/-2 are the CURSOR and
        CRT STATUS operands and nothing else, and neither a clause arm nor a bare text arm can see them.
      · `xref-sections`  a clause citation inside the rule's text — how an Annex A.1 documentation obligation
        names the clause that creates it. Derived from the clause numbers, so adding a module clause extends the
        A.1 arm for free instead of silently not extending it.
      · `kinds` / `excludes-kinds` the rule's KIND, per arm. A general-format diagram (kind FMT) is evidence
        about a CLAUSE, never about one of its formats, so the TEXT axis must not select one: without this,
        FMT-14.9.1.2 and FMT-14.9.11.2 — the ACCEPT and DISPLAY formats, both PARTIAL, both carrying the
        compiler's most-used statement — flip to non-support because their optional format names screen-name-1.
        The CLAUSE axis still takes FMT-13.17.2, whose every format IS screen.
    """

    def __init__(self, name: str, raw: dict[str, Any]) -> None:
        self.name = name
        self._raw = raw
        self.verdict: str = raw["verdict"]
        self.decision: str = raw.get("decision", "")
        self.excludes = [re.compile(p, re.IGNORECASE) for p in raw.get("excludes-patterns", [])]
        self.arms: list[dict[str, Any]] = []
        for i, arm in enumerate(raw["arms"]):
            keys = {k for k in arm if not k.startswith("$")}
            if not keys & {"sections", "pattern", "xref-sections", "kinds"}:
                # An arm with only NEGATIVE fields selects the whole catalog. Refusing it here is cheap; the
                # alternative is a 4,311-row batch that validates perfectly and is entirely wrong.
                raise SystemExit(f"derived-verdicts.{name} arm[{i}]: no positive field — it would select every rule")
            self.arms.append({
                "sections": list(arm.get("sections", [])),
                "xref-sections": list(arm.get("xref-sections", [])),
                "pattern": re.compile(arm["pattern"], re.IGNORECASE) if "pattern" in arm else None,
                "kinds": set(arm.get("kinds", [])),
                "excludes-kinds": set(arm.get("excludes-kinds", [])),
            })

    @staticmethod
    def text_of(rule: dict[str, Any]) -> str:
        """The rule text a predicate is matched against — hyphen-normalised. See HYPHENS."""
        return rule.get("text", "").translate(HYPHENS)

    def _arm_selects(self, arm: dict[str, Any], rule: dict[str, Any]) -> bool:
        kind = rule.get("kind", "")
        if arm["kinds"] and kind not in arm["kinds"]:
            return False
        if kind in arm["excludes-kinds"]:
            return False
        if arm["sections"] and not any(section_matches(p, rule.get("section", "")) for p in arm["sections"]):
            return False
        text = self.text_of(rule)
        if arm["xref-sections"]:
            cited = XREF_CITATION.findall(text)
            if not any(section_matches(p, c) for p in arm["xref-sections"] for c in cited):
                return False
        if arm["pattern"] is not None and not arm["pattern"].search(text):
            return False
        return True

    def select(self, rules: list[dict[str, Any]]) -> list[str]:
        out = []
        for r in rules:
            if any(x.search(self.text_of(r)) for x in self.excludes):
                continue
            if any(self._arm_selects(a, r) for a in self.arms):
                out.append(r["id"])
        return out


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
        #: The DERIVED-VERDICT selectors (`derived-verdicts` in the schema) — the `$comment` key is documentation.
        self.derived: dict[str, DerivedSelector] = {
            name: DerivedSelector(name, entry)
            for name, entry in raw.get("derived-verdicts", {}).items()
            if not name.startswith("$")
        }

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

    def anchor_obliged(self, row: dict[str, Any]) -> bool:
        """Must this row CARRY the anchor `anchor_for` computes — i.e. does its VERDICT claim a determination?

        ⛔ `anchor_for` says what the anchor IS (a function of the row); this says whether the row owes it.
        CONFORMS, PARTIAL and DIVERGES each assert something about what the register says, so each owes its own
        §7 row. DOCUMENTED-NON-SUPPORT asserts the opposite: the conditioning facility is not implemented, so
        Annex A.1's preamble withdraws the item ("the item is not required if the optional or
        processor-dependent feature is not implemented") and there is no determination left to anchor. The
        exempt verdicts are DATA on the kind (`anchor-exempt-verdicts`), read here and by `AnchorObliged` on the
        C# side, so the writer and the gate cannot disagree.
        """
        if self.anchor_for(row) is None:
            return False
        kind = self.kinds.get(row.get("kind") or "", {})
        return (row.get("verdict") or "") not in set(kind.get("anchor-exempt-verdicts", []))

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

        ⚠ Both costs are charged only to a verdict that CLAIMS a determination — the `anchor_obliged` predicate,
        not `anchor_for`. A DOCUMENTED-NON-SUPPORT DOC row declines the facility, which withdraws the A.1 item
        (A.1's preamble: the item is not required if the optional or processor-dependent feature is not
        implemented), so there is no §7 row to anchor and nothing implemented to observe; the row still owes its
        WITNESS test, which is the clause that keeps it open. ⛔ Reading `anchor_for` here instead was a real
        defect (kb/Work PB315): the C# twin `DerivedState` had ALWAYS asked `AnchorObliged` and said in its own
        comment that "this side and the Python writer read ONE rule" — they did not, and the disagreement was
        unobservable until a declined DOC row first earned a witness test, at which point three rows could never
        close and `EveryRowState_IsDerived_NotAsserted` went red on a correct batch.
        """
        verdict = row.get("verdict") or ""
        if verdict not in self.resolving:
            return "GAP"
        if not all(row.get(f) for f in self.requires(verdict)):
            return "GAP"
        if self.anchor_obliged(row):
            if self.anchor_for(row) not in self.locations(row):
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
