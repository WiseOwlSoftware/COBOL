#!/usr/bin/env python3
"""work_inventory.py — THE WORK REGISTER AGAINST THE TRACEABILITY INVENTORY (kb/Work PB245, PB2878; the two halves of
the invariant that keeps the burn-down and the register the same list).

    python scripts/spec/work_inventory.py --self-test     # the checks against a register built to break each one

`work.py check` calls :func:`problems` on the live register, so these are AUDITS: they run in the gate's audit phase
before the slot, in CI's `audits` job on every event and in the battery — on a change that touches only `kb/` as on
any other, and with no build. They were two Unit drift tests (ClosesRowsBackLinkDriftTests, DefectiveRowCoverageDriftTests)
that read every `kb/Work/` note through a C# twin of `work.py`'s frontmatter reader, which made every note a test input:
no register-only change could skip the test legs (the inert lane, DESIGN-test-build-ci.md §3.18). The twin, its parity
fixture's second engine and the `work.py parity` comparison went with them: the register has ONE reader, `work.py`'s.

THE INVARIANT, in two directions:

  COVERAGE (the registration wave of 2026-08-31). Every inventory row whose verdict is DEFECTIVE is claimed, in
  `inventory_rows`, by a LIVE note (a status that is not terminal); and every `inventory_rows` claim names a rule-id the
  inventory has. `tests/version-matrix/traceability-inventory.json` says which spec rules the compiler does not yet
  implement correctly, `kb/Work/` says what is left to do, and nothing held them together: measured 2026-08-31, of 138
  rows verdicted PARTIAL / NOT-IMPLEMENTED / DIVERGES, 131 were invisible to `work.py next`. The link is the NOTE→ROW
  direction on purpose: a note that lands keeps naming its rows, so a landed note whose rows were never re-verdicted turns
  this red, which is the event most worth failing on and the one a prose scrape of the rows' `notes` cannot see.

  BACK-LINK (owner decision 2026-09-19, kb/Work PB245). Every `kind: defect` note that has LANDED names the rows it
  closed in `closes_rows`, or says why it closed none (`closes_rows: []` AND a `closes_rows_reason:` sentence), and every
  row it names is one the inventory calls closed today. The GAP denominator counted closed work as open for months —
  thirteen of fourteen §15 rows closed CONFORMS on first re-measurement, on mechanisms seven landings had already shipped
  — until the back-link was made DATA rather than a habit. The reason must be a sentence, not a token: this can stop
  SILENCE and does not grade prose.

WHAT IS NOT CHECKED, decided (kb/Work PB720): the rule-id-shaped tokens in a note's BODY. A note's prose argues about
the rows as they stood when it was written, and editing an argument to change an id breaks the argument; the
frontmatter lists are the claims, and they are checked.

⛔ NOTHING HERE CARRIES ITS OWN COPY OF A VOCABULARY. "Defective" is `Schema.defective` (a verdict whose `resolves` flag is
false), so a verdict added to the schema extends the check with no edit here; "terminal" is `work.TERMINAL_STATUSES`,
whose complement is LIVE, so an unrecognized status fails SAFE (it keeps a note holding its rows rather than dropping
them out of coverage). The predicate for "closed" is the row's computed `state`, not its verdict: a CONFORMS row whose
covering test is missing is still a GAP row, and a landing that claimed it would be claiming work the number the owner
watches has not been told about.

⛔ THE POPULATION IS ASKED FIRST (:func:`population_findings`): a register that failed to load, a field deleted wholesale
or an inventory that came back empty would satisfy every predicate below while measuring nothing. And `--self-test` drives
the SAME predicates with a register built to break each one and one built to pass it, so the failures prove
discrimination rather than blanket rejection.
"""
from __future__ import annotations

import argparse
import sys
from collections.abc import Collection, Iterable, Mapping
from typing import Any

#: The shortest thing accepted as a stated reason: a SENTENCE ("no inventory row tracks this mechanism; the defect is in
#: the harness"), not a token. A floor on effort and not a judge of content: a determined writer can pad it, and what it
#: forecloses is the silence the field replaced.
MIN_REASON_LENGTH = 20
#: The spellings that say nothing, for the ones that squeeze past the floor.
VACUOUS_REASONS = ("n/a", "na", "none", "nothing", "tbd", "todo", "unknown", "-", "no rows", "not applicable")

#: The smallest populations at which the checks mean anything. Measured on the live tree 2026-10-24: 2,901 notes, 4,214
#: rows; these floors are far below them, so they catch a register or an inventory that FAILED TO LOAD, not a small one.
MIN_NOTES, MIN_OBLIGED_NOTES, MIN_ROWS, MIN_CLOSES_CLAIMS = 100, 50, 1000, 100

Note = Mapping[str, Any]
Row = Mapping[str, Any]


def is_vacuous(reason: object) -> bool:
    """A `closes_rows_reason` that is absent, too short to be a sentence, or one of the spellings of nothing."""
    r = str(reason or "").strip().strip('"').strip().lower()
    return len(r) < MIN_REASON_LENGTH or r in VACUOUS_REASONS


def _list(note: Note, key: str) -> list[str]:
    """A list field's members; a value that is not a list (a scalar written where a list belongs) is `closes_rows_shape`'s
    finding in `work.py check`, not this module's."""
    value = note.get(key)
    return value if isinstance(value, list) else []


def is_live(note: Note, terminal: Collection[str]) -> bool:
    return note.get("status") not in terminal


def obliged(note: Note, terminal: Collection[str]) -> bool:
    """Subject to the back-link obligation: a DEFECT that has reached a terminal status. Analyses, adjudications and
    owner decisions record their outcome in prose and close no row by landing; a defect is the kind whose landing changes
    what the inventory says."""
    return note.get("kind") == "defect" and not is_live(note, terminal)


def landed_notes_that_say_nothing(notes: Iterable[Note], terminal: Collection[str]) -> list[str]:
    """Landed defect notes that say NOTHING about what they closed — the silence PB245 is about."""
    return [f"{n['_file']}: status {n.get('status')} and no `closes_rows` — name the inventory rows this landing "
            "re-verdicted, or write `closes_rows: []` with a `closes_rows_reason:` saying why it closed none. A landing "
            "that records neither is how fourteen §15 rows held the GAP open on mechanisms seven landings had already "
            "closed (kb/Work/PB245)"
            for n in sorted(notes, key=lambda n: n["_file"])
            if obliged(n, terminal) and not _list(n, "closes_rows") and is_vacuous(n.get("closes_rows_reason"))]


def closed_rows_the_inventory_denies(notes: Iterable[Note], rows: Iterable[Row]) -> list[str]:
    """Rows a note claims to have CLOSED that the inventory does not agree are closed: no such rule-id, or the row's
    computed `state` is still GAP. A claim on a note that has NOT landed is checked too: a row named as closed is a
    factual claim whatever the note's status, and a wrong one is worth catching before the landing inherits it."""
    known = {r["rule-id"]: r for r in rows}
    bad: list[str] = []
    for n in sorted(notes, key=lambda n: n["_file"]):
        for claim in _list(n, "closes_rows"):
            row = known.get(claim)
            if row is None:
                bad.append(f"{n['_file']}: closes_rows names '{claim}', which is not a rule-id in the inventory — a "
                           "claim that satisfies nothing while looking like a closed row")
            elif row.get("state") != "OK":
                bad.append(f"{n['_file']}: closes_rows names '{claim}', whose verdict is "
                           f"'{row.get('verdict') or '(none)'}' and whose state is '{row.get('state') or '(none)'}' — that "
                           "row still counts toward the GAP, so either the landing owes it a `record_verdicts` batch or "
                           "the claim is wrong")
    return bad


def unclaimed_defective_rows(rows: Iterable[Row], notes: Iterable[Note], defective: Collection[str],
                             terminal: Collection[str]) -> list[str]:
    """Defective rows that no LIVE note claims — the set the coverage check holds at empty."""
    claimed = {c for n in notes if is_live(n, terminal) for c in _list(n, "inventory_rows")}
    return [f"{r['rule-id']}: verdict {r['verdict']}, and no open kb/Work note names it in `inventory_rows` — register it "
            "(a note), or record the verdict it has earned"
            for r in sorted(rows, key=lambda r: r["rule-id"])
            if r.get("verdict") in defective and r["rule-id"] not in claimed]


def claims_naming_no_row(rows: Iterable[Row], notes: Iterable[Note]) -> list[str]:
    """A claim on a rule-id the inventory does not have. A typo here is worse than useless: it satisfies nothing while
    LOOKING like ownership, so the row it meant to claim stays uncovered and the note reads as done."""
    known = {r["rule-id"] for r in rows}
    return [f"{n['_file']}: inventory_rows names '{claim}', which is not a rule-id in the inventory"
            for n in sorted(notes, key=lambda n: (n["_file"]))
            for claim in sorted(_list(n, "inventory_rows"))
            if claim not in known]


def population_findings(notes: list[Note], rows: list[Row], defective: Collection[str],
                        terminal: Collection[str]) -> list[str]:
    """A MISSING observation is not a NEGATIVE one: findings when the register or the inventory is too small, or a field
    the checks read is gone, for any of which every predicate would pass over nothing."""
    out: list[str] = []
    if len(rows) <= MIN_ROWS:
        out.append(f"only {len(rows)} inventory rows parsed — the inventory did not load")
    if len(notes) <= MIN_NOTES:
        out.append(f"only {len(notes)} work notes parsed — the register did not load")
    n_obliged = sum(obliged(n, terminal) for n in notes)
    if n_obliged <= MIN_OBLIGED_NOTES:
        out.append(f"only {n_obliged} landed defect note(s) — the register did not load, or the status vocabulary "
                   "changed underneath this check")
    if sum(len(_list(n, "closes_rows")) for n in notes) <= MIN_CLOSES_CLAIMS:
        out.append("the register claims almost no closed rows — the `closes_rows` key this check reads is gone; "
                   "re-derive it with scripts/spec/backfill_closes_rows.py")
    if not any(_list(n, "inventory_rows") for n in notes):
        out.append("no work note carries an `inventory_rows` claim — the frontmatter key this check reads is gone")
    if not any(r.get("verdict") in defective for r in rows):
        out.append("no inventory row carries a defective verdict — the schema's `resolves` flags or the rows' `verdict` "
                   "field are gone")
    if not any(r.get("state") == "OK" for r in rows):
        out.append("no inventory row is OK — the rows' computed `state` field is gone")
    return out


def problems(notes: list[Note], rows: list[Row], defective: Collection[str], terminal: Collection[str]) -> list[str]:
    """Every finding for a LIVE register against the inventory: the population first, then both directions."""
    return (population_findings(notes, rows, defective, terminal)
            + landed_notes_that_say_nothing(notes, terminal)
            + closed_rows_the_inventory_denies(notes, rows)
            + unclaimed_defective_rows(rows, notes, defective, terminal)
            + claims_naming_no_row(rows, notes))


def live_problems(notes: list[Note], terminal: Collection[str]) -> list[str]:
    """The findings for the repository's own inventory and schema (`work.py check` calls this with its own
    TERMINAL_STATUSES: the vocabulary has one home, and importing it from here would load `work.py` twice)."""
    import inventory_schema  # noqa: PLC0415  (this directory's; imported here so --self-test needs neither file)

    if not inventory_schema.INVENTORY_PATH.exists():
        return [f"inventory missing: {inventory_schema.INVENTORY_PATH} — run python scripts/spec/build_inventory.py"]
    return problems(notes, inventory_schema.load_inventory(), inventory_schema.load_schema().defective,
                    terminal)


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    """⛔ THE EVIDENCE THAT THESE CHECKS INSPECT ANYTHING: a register built to break each one, and one built to pass it."""
    import work  # noqa: PLC0415

    failures: list[str] = []

    def expect(label: str, got: object, want: object) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    terminal = work.TERMINAL_STATUSES

    def row(rule_id: str, verdict: str, state: str) -> dict[str, str]:
        return {"rule-id": rule_id, "verdict": verdict, "state": state}

    def note(pb: str, kind: str, status: str, closes: list[str] | None = None, reason: str = "",
             rows: list[str] | None = None) -> dict[str, Any]:
        return {"_file": f"{pb}.md", "id": pb, "kind": kind, "status": status, "inventory_rows": rows or [],
                "closes_rows": closes if closes is not None else [], "closes_rows_reason": reason}

    nothing = lambda notes: len(landed_notes_that_say_nothing(notes, terminal))   # noqa: E731
    denies = lambda notes, inv: len(closed_rows_the_inventory_denies(notes, inv))  # noqa: E731
    inventory = [row("GR-1.1-1", "CONFORMS", "OK"), row("GR-1.1-2", "PARTIAL", "GAP"), row("GR-1.1-3", "", "GAP"),
                 row("GR-1.1-4", "DOCUMENTED-NON-SUPPORT", "OK"),
                 # ⛔ A RESOLVING VERDICT IS NOT A CLOSED ROW: `state` is computed from the verdict AND its evidence, so a
                 # CONFORMS row whose covering test is missing still counts toward the GAP, and the GAP counting closed
                 # work as open is the entire harm kb/Work/PB245 measured.
                 row("GR-1.1-5", "CONFORMS", "GAP")]

    # ── back-link, half one: a landed defect that says nothing ───────────────────────────────────────────────────
    expect("a landed defect with no rows and no reason says nothing", nothing([note("PB1", "defect", "landed")]), 1)
    expect("a retired defect (the other terminal status) says nothing either", nothing([note("PB1", "defect", "retired")]), 1)
    expect("naming a row is an answer", nothing([note("PB1", "defect", "landed", ["GR-1.1-1"])]), 0)
    expect("a reason that is a sentence is an answer",
           nothing([note("PB1", "defect", "landed", [], "no inventory row tracks this harness defect at all")]), 0)
    expect("a token is not a reason, however it is spelled or padded to look like one",
           [nothing([note("PB1", "defect", "landed", [], r)]) for r in ("n/a", "   none   ", "not applicable", '"tbd"')],
           [1, 1, 1, 1])
    expect("still open, or not a defect: the obligation belongs to a defect's landing",
           [nothing([note("PB1", "defect", s)]) for s in ("open", "half", "owner", "blocked")]
           + [nothing([note("PB1", k, "landed")]) for k in ("analysis", "adjudication", "decision")], [0] * 7)

    # ── back-link, half two: a claim the inventory denies ────────────────────────────────────────────────────────
    expect("a landed note naming a row that is STILL defective — the event this exists for",
           denies([note("PB1", "defect", "landed", ["GR-1.1-2"])], inventory), 1)
    expect("an UNADJUDICATED row is not closed either: nobody looked is not it conforms",
           denies([note("PB1", "defect", "landed", ["GR-1.1-3"])], inventory), 1)
    expect("a typo'd rule-id satisfies nothing while looking like a closed row",
           denies([note("PB1", "defect", "landed", ["GR-1.1-99"])], inventory), 1)
    expect("both resolving verdicts are accepted: the vocabulary is the schema's",
           denies([note("PB1", "defect", "landed", ["GR-1.1-1", "GR-1.1-4"])], inventory), 0)
    expect("a claim on a note that has not landed is checked too",
           denies([note("PB1", "defect", "open", ["GR-1.1-2"])], inventory), 1)
    expect("every wrong claim of every note is reported",
           denies([note("PB1", "defect", "landed", ["GR-1.1-2", "GR-1.1-3"]),
                   note("PB2", "defect", "landed", ["GR-1.1-1", "GR-1.1-99"])], inventory), 3)
    expect("a CONFORMS verdict whose row is still GAP is refused as a closed row",
           denies([note("PB1", "defect", "landed", ["GR-1.1-5"])], inventory), 1)
    expect("a closes_rows written as a scalar is the shape check's finding, not a crash here",
           denies([{**note("PB1", "defect", "landed"), "closes_rows": "GR-1.1-1"}], inventory), 0)

    # ── coverage ─────────────────────────────────────────────────────────────────────────────────────────────────
    defective = {"PARTIAL", "NOT-IMPLEMENTED", "DIVERGES"}
    cover = [row("GR-1.1-1", "PARTIAL", "GAP"), row("GR-1.1-2", "CONFORMS", "OK"), row("GR-1.1-3", "DIVERGES", "GAP")]

    def unclaimed(notes: list[Note], inv: list[Row] = cover) -> int:
        return len(unclaimed_defective_rows(inv, notes, defective, terminal))

    expect("a defective row nothing claims (two of them)", unclaimed([]), 2)
    expect("claimed by a note that has LANDED or been retired: the fix shipped and the row was never re-verdicted",
           [unclaimed([note("PB1", "defect", s, rows=["GR-1.1-1"]), note("PB2", "defect", "open", rows=["GR-1.1-3"])])
            for s in ("landed", "retired")], [1, 1])
    expect("claimed by live notes of every non-terminal status: all of them count",
           [unclaimed([note("PB1", "defect", s, rows=["GR-1.1-1"]), note("PB2", "defect", "open", rows=["GR-1.1-3"])])
            for s in ("open", "half", "owner", "blocked")], [0] * 4)
    expect("a status added tomorrow counts as live, so its note keeps holding its rows",
           unclaimed([note("PB1", "defect", "in-review", rows=["GR-1.1-1", "GR-1.1-3"])]), 0)
    expect("a non-defective row needs no claim, and a claim on one is no error",
           (unclaimed([], [row("GR-1.1-2", "CONFORMS", "OK")]),
            len(claims_naming_no_row(cover, [note("PB1", "defect", "open", rows=["GR-1.1-2"])]))), (0, 0))
    expect("a typo'd claim satisfies nothing while looking like ownership: BOTH checks notice",
           (len(claims_naming_no_row(cover, [note("PB1", "defect", "open", rows=["GR-1.1-99"])])),
            unclaimed([note("PB1", "defect", "open", rows=["GR-1.1-99"]), note("PB2", "defect", "open", rows=["GR-1.1-3"])])),
           (1, 1))

    # ── population, the schema and the live register ─────────────────────────────────────────────────────────────
    expect("an empty register and inventory is a finding per missing observation, never a pass",
           len(population_findings([], [], defective, terminal)), 7)
    big_rows = [row(f"GR-9.9-{i}", "CONFORMS", "OK") for i in range(MIN_ROWS + 1)] + [row("GR-9.8-1", "PARTIAL", "GAP")]
    big_notes = ([note(f"PB{i}", "defect", "landed", [f"GR-9.9-{i}"]) for i in range(MIN_NOTES + 1)]
                 + [note("PB900000", "defect", "open", rows=["GR-9.8-1"])])
    expect("a register and an inventory at population raise no population finding",
           population_findings(big_notes, big_rows, defective, terminal), [])
    expect("a register at population that agrees with its inventory has no finding at all",
           problems(big_notes, big_rows, defective, terminal), [])
    expect("a defect that lands without re-verdicting its row is caught at population",
           len(problems(big_notes + [note("PB900001", "defect", "landed", ["GR-9.8-1"])], big_rows, defective, terminal)), 1)

    import inventory_schema  # noqa: PLC0415

    schema_defective = inventory_schema.load_schema().defective
    expect("the schema's defective verdicts are derived from its `resolves` flags, and PARTIAL is one",
           ("PARTIAL" in schema_defective, "CONFORMS" in schema_defective, "" in schema_defective), (True, False, False))
    print(f"=== WORK-INVENTORY SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--self-test", action="store_true", required=True)
    ap.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    return self_test()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
