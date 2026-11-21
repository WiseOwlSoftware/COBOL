#!/usr/bin/env python3
"""IS EDITION N COMPLETE? A computed answer, never a claim (kb/Work PB5924).

    python scripts/spec/edition_status.py --edition {1985,2002,2014,2023} [--json]

It measures four parts and prints a verdict line. An edition is COMPLETE only when ALL FOUR are zero:

  1. INVENTORY GAP: the rows of the edition's traceability inventory in state GAP (`inventory_view.inventory`, `gaps`),
     plus every item that keeps its rule count a lower bound (`inventory_view.pending`: clauses pending adjudication,
     untyped rule-shaped headings), whose rules have no row yet; the verdict names them beside the GAP (`Part.shown`).
  2. CODE READS: the rows owed a read, owed a re-read, contradicted or undecided (`code_reads`, `code_read_store`). The
     newest held edition has no store (its rows carry their own verdicts): that is said, and it counts 0.
  3. OPEN WORK NOTES that block the edition: every note of the register whose status is not terminal and whose
     declared `editions:` field names it (`work.declared_editions`, the field's one reader; what the field means and its
     check are `work_editions.py`, kb/Work PB5995), by `work.load`, the one reader of the register. Each is HARM (a harm
     flag set, not process_only), else QUESTION (kind analysis, adjudication or decision), else TOOLING (process_only),
     else OTHER; the first test that fits decides. A non-terminal note that declares no editions makes the part NOT
     MEASURED, naming it: a note the measure cannot place is never read as blocking nothing (a false COMPLETE).
  4. CATALOG PROSE HOLES: the printed paragraphs of the transcription's normative region in no catalog row at or under
     their locator, none discounted until kb/Work PB5452 records dispositions, with the deontic sentences among them as a
     labelled floor (`catalog_prose_holes`, kb/Work PB479's measure; measured for 1985 only).

A part that cannot be measured (an edition the inventory does not hold, a transcription the hole reader does not read) is
NOT a zero: it prints `not measured` with why, and the verdict is NOT COMPLETE with that reason. Exit status 0 when
COMPLETE, 1 when not.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import sys
from typing import Callable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
import catalog_prose_holes  # noqa: E402  (the prose holes of a transcription)
import code_read_store as store  # noqa: E402  (the outcome and the owed rows of a store)
import code_reads  # noqa: E402  (the context of an edition's reads, the re-read predicate)
import inventory_schema  # noqa: E402  (the held editions)
import inventory_view  # noqa: E402  (each edition's rows and `gaps`)
import work  # noqa: E402  (THE reader of the register)
from standard_streams import ensure_utf8  # noqa: E402

CATEGORIES = ("HARM", "QUESTION", "TOOLING", "OTHER")
QUESTION_KINDS = ("analysis", "adjudication", "decision")


@dataclasses.dataclass(frozen=True)
class Part:
    """One measured part: its `count` (None when it could not be measured, and then `why` says why), a `lines` detail."""
    name: str
    count: int | None
    why: str = ""
    lines: tuple[str, ...] = ()
    shown: str = ""   # how the verdict names the count when `count` sums several things ("" = `<count> <name>`)
    data: dict = dataclasses.field(default_factory=dict)

    @property
    def measured(self) -> bool:
        return self.count is not None

    @property
    def text(self) -> str:
        """The part as the verdict names it."""
        return f"{self.name} not measured" if not self.measured else (self.shown or f"{self.count} {self.name}")


def unmeasured(name: str, why: str) -> Part:
    return Part(name, None, why)


def _loud(name: str, fn: Callable[[], Part]) -> Part:
    """`fn()`, or an unmeasured part carrying the refusal of the reader it asked (a SystemExit or a NotMeasured): an input a
    reader cannot resolve is reported, never read as zero."""
    try:
        return fn()
    except SystemExit as e:
        return unmeasured(name, str(e.code).strip().splitlines()[0] if e.code else "the reader refused")
    except catalog_prose_holes.NotMeasured as e:
        return unmeasured(name, str(e))


# -- the four parts ---------------------------------------------------------------------------------------------------------

def inventory_part(year: int) -> Part:
    """Part 1 of the edition's inventory as the working tree holds it: its rows, and the bound its catalog declares."""
    schema = inventory_schema.load_schema()
    return inventory_count(inventory_view.inventory(year, schema), inventory_view.pending(schema.edition(year)))


def inventory_count(rows: list[dict], bound: dict[str, int]) -> Part:
    """Part 1 from `rows` and the edition's `bound` (`inventory_view.pending`: what keeps its rule count a lower bound).
    ⛔ A BOUNDED COUNT IS NEVER A ZERO (kb/Work PB5994 review R1-1): a clause pending adjudication holds rules no row
    counts yet, so each pending item adds to the part's count and the verdict names it beside the GAP; an edition whose
    GAP reaches 0 with clauses pending stays NOT COMPLETE."""
    gap = inventory_view.gaps(rows)
    pending = sum(bound.values())
    lines = [f"{gap} of {len(rows)} rule rows in state GAP"]
    if bound:
        lines.append("the rule count is a lower bound: " + ", ".join(f"{n} {what}" for what, n in bound.items())
                     + " (their rules have no row yet, so each is counted here)")
    shown = f"{gap} inventory GAP" + "".join(f" + {n} {what}" for what, n in bound.items())
    return Part("inventory GAP", gap + pending, lines=tuple(lines), shown=shown if bound else "",
                data={"rows": len(rows), "gap": gap, "bound": dict(bound)})


def code_reads_part(year: int) -> Part:
    schema = inventory_schema.load_schema()
    edition = next((e for e in schema.all_editions if e.year == year), None)
    if edition is None or not edition.held:
        return unmeasured("code reads", f"edition {year} is not held by the inventory schema")
    if edition.newest:
        return Part("code reads", 0, lines=(f"{year} is the newest held edition: it has no code-read store (its rows carry "
                                            "their own verdicts), so 0 are owed",), data={"store": False})
    ctx = code_reads.Context.of(year, schema=schema)
    records = code_reads.load(ctx)
    owed = code_reads.owed(ctx, records)
    again = code_reads.reread(ctx, records)
    by = {o: sum(1 for r in records.values() if code_reads.outcome(r, ctx) == o) for o in store.OUTCOMES}
    contradicted, undecided = by[store.CONTRADICTED], by[store.UNDECIDED]
    lines = [f"{len(records)} code read(s) stored" if records else "no code-read store: every KEPT row is owed a read",
             f"{len(owed)} owed a read (a stale record, {by[store.STALE]}, counts as no read) · {len(again)} owed a re-read · "
             f"{contradicted} contradicted · {undecided} undecided"]
    return Part("code reads", len(owed) + len(again) + contradicted + undecided, lines=tuple(lines),
                data={"store": bool(records), "records": len(records), "owed": len(owed), "re-read": len(again),
                      "contradicted": contradicted, "undecided": undecided, "stale": by[store.STALE]})


def categorize(it: dict) -> str:
    """The category of an open note (module docstring); the first test that fits decides."""
    if not it.get("process_only") and any(it.get(f) for f in work.HARM_FLAGS):
        return "HARM"
    if it.get("kind") in QUESTION_KINDS:
        return "QUESTION"
    return "TOOLING" if it.get("process_only") else "OTHER"


def work_part(year: int, work_dir: pathlib.Path = work.WORK) -> Part:
    by: dict[str, list[str]] = {c: [] for c in CATEGORIES}
    undeclared: list[str] = []
    for it in work.load(work_dir):
        if it.get("status") in work.TERMINAL_STATUSES:
            continue
        eds = work.declared_editions(it)
        if eds is None:
            undeclared.append(it["id"])
        elif year in eds:
            by[categorize(it)].append(it["id"])
    if undeclared:
        ids = sorted(undeclared, key=work.id_order)
        return unmeasured("open work notes", f"{len(ids)} non-terminal note(s) declare no editions (kb/Work PB5995; "
                          f"python scripts/spec/work.py check): {' '.join(ids[:10])}{' ...' if len(ids) > 10 else ''}")
    total = sum(len(v) for v in by.values())
    lines = tuple(f"{c} {len(ids)}: {' '.join(sorted(ids, key=work.id_order))}" for c, ids in by.items() if ids)
    return Part("open work notes", total, lines=lines or ("none",), data={c: sorted(v, key=work.id_order) for c, v in by.items()})


def holes_part(year: int) -> Part:
    h = catalog_prose_holes.holes(year)
    d = h.as_doc()
    return Part("catalog prose holes", h.count,
                lines=(f"{h.count} printed paragraphs of the normative region are in no catalog row at or under their "
                       f"locator ({h.printed_paragraphs} measured, in {d['paragraphs-with-holes']} numbered paragraphs; "
                       f"{len(h.rowless)} numbered paragraphs have no row at all); no disposition recorded (kb/Work "
                       "PB5452), none discounted",
                       f"floor inside it: {h.deontic} deontic sentences in no row; lead: {h.elsewhere} of the paragraphs are "
                       "in a row of another clause"),
                data={k: d[k] for k in ("printed-paragraphs", "uncovered-paragraphs", "deontic-floor",
                                        "rowless-paragraphs", "elsewhere-lead", "paragraphs-with-holes", "by-section")})


#: The four sources of `status`, as callables of the year: a self-test plants others.
Sources = tuple[Callable[[int], Part], Callable[[int], Part], Callable[[int], Part], Callable[[int], Part]]
SOURCES: Sources = (inventory_part, code_reads_part, work_part, holes_part)


def status(year: int, sources: Sources = SOURCES) -> list[Part]:
    names = ("inventory GAP", "code reads", "open work notes", "catalog prose holes")
    return [_loud(n, lambda s=s: s(year)) for n, s in zip(names, sources)]


def verdict(year: int, parts: list[Part]) -> tuple[bool, str]:
    """(complete, the verdict line): COMPLETE only when every part is measured and zero."""
    if all(p.measured and p.count == 0 for p in parts):
        return True, f"=== EDITION {year}: COMPLETE ==="
    return False, f"=== EDITION {year}: NOT COMPLETE — {' · '.join(p.text for p in parts if not p.measured or p.count)} ==="


def render(year: int, parts: list[Part]) -> str:
    out = [f"edition {year}"]
    for i, p in enumerate(parts, 1):
        out.append(f"{i}. {p.name}: " + (str(p.count) + (f" ({p.shown})" if p.shown else "") if p.measured
                                         else f"not measured — {p.why}"))
        out += [f"     {line}" for line in p.lines]
    out.append(verdict(year, parts)[1])
    return "\n".join(out)


def as_doc(year: int, parts: list[Part]) -> dict:
    complete, line = verdict(year, parts)
    return {"edition": year, "complete": complete, "verdict": line,
            "parts": {p.name: {"count": p.count, "measured": p.measured, "why": p.why, **p.data} for p in parts}}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Is edition N complete? Four measured parts and a verdict (kb/Work PB5924).")
    ap.add_argument("--edition", type=int, required=True, choices=work.EDITIONS)
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args(argv)
    parts = status(a.edition)
    print(json.dumps(as_doc(a.edition, parts), indent=1, ensure_ascii=False) if a.json else render(a.edition, parts))
    return 0 if verdict(a.edition, parts)[0] else 1


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
