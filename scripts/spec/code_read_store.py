#!/usr/bin/env python3
"""THE STORE OF CODE READS and the outcome of a record: the half of the code reads the inventory view reads (kb/Work
PB5759; DESIGN-spec-conformance-review.md section 10.6.1). `code_reads.py` is the rest (the record's shape, the import,
`work`, `adjudicate`, `withdraw`, `plan` and the CODE READS audit) and the only writer.

WHY TWO FILES. `inventory_view.py` is in the conformance-suite generator's import closure (scripts/build_targets.json, the
BUILD TARGETS audit), and the view prints the code reads' counts (`view_line`). The shape checks need the citation
checker (`cite.py`, which reads the transcriptions) and the lineage reads' digest, whose closure is no input of the
generator; so the view's half imports only what the view already imports (`inventory_schema`, `inventory_store`,
`lineage`), and the store and the outcome have this one home.

THE OUTCOME OF A RECORD is ONE function, `outcome`, which the audit, the view and the planner all ask:
  confirmed     the reader CONFORMS and the refuter UPHELD it; or an adjudication whose verdict is CONFORMS;
  contradicted  the reader is not CONFORMS; or an adjudication whose verdict is not CONFORMS (it names its `work`);
  undecided     the refuter OVERTURNED or DOUBTFUL a CONFORMS, or the reader answered UNDECIDED (it did not decide),
                and nobody has adjudicated it;
  stale         the rule's catalog text is no longer the text it was read against: it counts as no read.
"""
from __future__ import annotations

import pathlib
import sys
from collections import Counter
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import inventory_store  # noqa: E402  (the one-file-per-rule store: its reader, its writer, its file names)
import lineage  # noqa: E402  (which rows are KEPT, and a row's own witnesses)
from inventory_schema import REPO, Edition, Schema  # noqa: E402

Row = dict[str, Any]

#: The outcomes of a record (`outcome`), in the order the views print them.
CONFIRMED, CONTRADICTED, UNDECIDED, STALE = "confirmed", "contradicted", "undecided", "stale"
OUTCOMES = (CONFIRMED, CONTRADICTED, UNDECIDED, STALE)
#: The reader's verdict that the refuter attacks and that confirms a rule: the inventory schema's own word
#: (`code_reads.Context.of` checks it is in the vocabulary); every other verdict contradicts the shared one.
CONFORMS = "CONFORMS"
#: The reader's word for "I did not decide" (the reader brief's): no verdict of the inventory's vocabulary, accepted as the
#: absence of one. A record that holds it is `undecided` whatever its refuter says, and its row is still owed a read.
READER_UNDECIDED = "UNDECIDED"
#: The refuter's verdict that upholds a CONFORMS.
UPHELD = "UPHELD"
#: The keys a record carries, in the order it is written.
RECORD_KEYS = ("rule-id", "rule-digest", "read", "reader", "refuter", "adjudication", "work")


def store_dir(year: int, root: pathlib.Path = REPO) -> pathlib.Path:
    """The store of the code reads of the older edition `year` in the tree at `root`."""
    return root / "tests" / "version-matrix" / f"code-reads-{year}"


def load(year: int, root: pathlib.Path = REPO, directory: pathlib.Path | None = None) -> dict[str, dict[str, Any]]:
    """Every record of `year`'s store (`directory`, the tracked store by default), by rule id; an absent store holds none,
    and a stray or misnamed file is a SystemExit naming each (`inventory_store.read_store`)."""
    return inventory_store.read_store(directory or store_dir(year, root))


def write(records: dict[str, dict[str, Any]], directory: pathlib.Path) -> int:
    """Make the store at `directory` hold exactly `records` (`inventory_store.write_store`: one file per rule, atomic)."""
    return inventory_store.write_store([{k: r[k] for k in RECORD_KEYS if k in r} for _, r in sorted(records.items())],
                                       directory)


def outcome(record: dict[str, Any], rows: dict[str, Row]) -> str:
    """The outcome of `record` against the edition's `rows` by id (module docstring): `stale` when the rule's text moved
    under it, else the adjudication's, else the reader's (an UNDECIDED reader leaves it undecided) and the refuter's."""
    row = rows.get(record["rule-id"])
    if row is None or row["rule-digest"] != record["rule-digest"]:
        return STALE
    if adj := record.get("adjudication"):
        return CONFIRMED if adj["verdict"] == CONFORMS else CONTRADICTED
    if record["reader"]["verdict"] == READER_UNDECIDED:
        return UNDECIDED
    if record["reader"]["verdict"] != CONFORMS:
        return CONTRADICTED
    return CONFIRMED if (record.get("refuter") or {}).get("refutation") == UPHELD else UNDECIDED


def owed(rows: dict[str, Row], schema: Schema, records: dict[str, dict[str, Any]], witnessed: bool = False) -> list[str]:
    """The KEPT rows of `rows` (in their order) that no confirmed or contradicted record decides: a stale or an undecided
    record is still owed. `witnessed` keeps those that carry a test-ref of their own (`lineage.own_witness_refs`). A chained
    row the view computes OK with no test-ref of its own is closed on a derivation of its own (kb/Work PB5822, the one
    predicate `lineage.derivation_refusals`, asked through the state): it needs no code read and is not owed one
    (kb/Work PB5842)."""
    decided = {rid for rid, r in records.items() if outcome(r, rows) in (CONFIRMED, CONTRADICTED)}
    return [rid for rid, row in rows.items() if lineage.chained(row, schema) and rid not in decided
            and not (row.get("state") == "OK" and not lineage.own_witness_refs(row, schema))
            and (not witnessed or lineage.own_witness_refs(row, schema))]


def counts(rows: dict[str, Row], schema: Schema, records: dict[str, dict[str, Any]]) -> dict[str, int]:
    """Each outcome's count over `records`, and the KEPT rows still `owed` a read."""
    by = Counter(outcome(r, rows) for r in records.values())
    return {**{o: by.get(o, 0) for o in OUTCOMES}, "owed": len(owed(rows, schema, records))}


def count_text(c: dict[str, int]) -> str:
    """The counts as every view prints them."""
    return (f"code reads {c[CONFIRMED]} confirmed · {c[CONTRADICTED]} contradicted · {c[UNDECIDED]} undecided · "
            f"{c[STALE]} stale · {c['owed']} KEPT rows owed a read")


def view_line(ed: Edition, schema: Schema, rows: list[Row]) -> str | None:
    """The inventory view's line for `ed` (`inventory_view.summary`), or None for an edition whose store holds no record
    (the newest edition never has one). It changes no row's state: it counts the store against the rows."""
    if ed.newest or not (records := load(ed.year)):
        return None
    return f"       {ed.year}: {count_text(counts({r['rule-id']: r for r in rows}, schema, records))} (kb/Work PB5759)"
