#!/usr/bin/env python3
"""THE MOVES AND SPLITS OF ADJUDICATED ROWS (kb/Work PB5449 item 6; decision PB5448 points 4 and 7).

A store record is keyed by its rule id and holds the digest of the text it was decided against (kb/Work PB698). Two
mechanical changes of the catalog move text under an id without changing what any rule says, and each has ONE
operation here, applied by the one writer of a store (`record_verdicts.py`) as a record of its batch:

  - A MOVE, `{"rule-id": "<new id>", "move-from": "<old id>"}`. A transcription repair that splits, joins or
    re-levels a paragraph renumbers the `p<n>` ids after it in its clause (decision PB5448 point 2), so an adjudicated
    paragraph row's id moves. The regeneration that renumbers writes the move batch itself (`move_batch`, from
    `text_units.moves`), and the record moves ONLY when its `rule-digest` is the digest of the text the new id holds
    now (the renumbering rule of section 10.2 of DESIGN-spec-conformance-review.md): a record whose text changed is
    re-adjudicated, never moved. Until the batch is applied the view refuses the record at its old id, so a catalog
    whose moves are not applied cannot pass `inventory_view.py --check`.
  - A SPLIT, `{"rule-id": "<parent>", "split": {"old-text", "carved", "rows", "recheck"}}`. A margin paragraph the
    page prints at the block's margin leaves the numbered rule whose text held it (point 7). The split checks that the
    record was decided against `old-text` and that `old-text` is the parent's text now with the `carved` pieces
    inserted whole, in order (`carved_from`: a piece is a run of carved lines, so a paragraph a display interrupts, which
    the parent keeps, is two pieces, and a carved row holds one or more consecutive pieces; kb/Work PB5452). A carved row (`rows`) INHERITS NOTHING: it must have no record, and gets none. The parent
    keeps its record only through its RECHECK (the three questions of section 10.3 asked of the remaining text, and
    its refuter: `outcome` UPHELD, `refuter`, `attempt`, `date`, and the `digest` of the text it read): an upheld
    recheck re-stamps the parent's digest and marks its notes; any other outcome withdraws the record, and the parent
    opens and goes to adjudication with the carved rows.

THE CHAINS FOLLOW THE TEXT (point 7). An older edition's change record names its next edition's rules in `next.to`,
with the digest of each (`next.digests`, kb/Work PB5256). A move renames the id there and keeps its digest (the text is
the same); a split re-stamps the parent's digest only for a record decided against the parent's old text (its digest
is `old-text`'s), and not when the record's `reason` QUOTES a carved paragraph (`QUOTE_WORDS` consecutive words): any
other record keeps its digest, reads STALE and goes to adjudication (`older_restamps`).

    Its cases: scripts/selftests/spec/rule_moves/test_rule_moves.py (python scripts/self_tests.py --only <it>).
"""
from __future__ import annotations

import dataclasses
import json
import pathlib
import sys
from typing import Any, Callable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cite import norm  # noqa: E402  (ONE notion of "the same words" as the citation checker's)
from inventory_view import rule_digest  # noqa: E402  (THE digest a store record carries, kb/Work PB698)

MOVE_FROM = "move-from"
SPLIT = "split"
#: The fields of a split, and of its parent's recheck record (kb/Work PB5374's refuter-record form).
SPLIT_FIELDS = ("old-text", "carved", "rows", "recheck")
RECHECK_FIELDS = ("outcome", "refuter", "attempt", "date", "digest")
UPHELD = "UPHELD"
#: How many consecutive normalized words of a carved paragraph a change record's `reason` must hold to QUOTE it (the
#: measure the design's "What the tree already shows" uses for a quotation).
QUOTE_WORDS = 8
#: The mark an upheld recheck leaves in the parent's notes: the record's history, after its prose.
RECHECK_MARK = "split-recheck:"

#: Where an edition's applied split batches are kept, tracked: a split is an operation on the store whose batch file is
#: otherwise gone once applied, and the WITNESS LOSS audit reads it here to tell a parent whose record a split withdrew
#: (kb/Work PB5452) from a hand edit.
SPLIT_DIR = "docs/rearchitecture/spec-splits-{year}"

Record = dict[str, Any]


def is_move(rec: Record) -> bool:
    return MOVE_FROM in rec


def is_split(rec: Record) -> bool:
    return SPLIT in rec


def split_parents(records: list[Record]) -> dict[str, Record]:
    """{parent id: its split} for every split among `records`."""
    return {r["rule-id"]: r[SPLIT] for r in records if is_split(r)}


def explains(split: Record, was: str | None, now: str | None) -> bool:
    """Does `split` account for a parent whose `rule-digest` went from `was` to `now`? It was decided against the split's
    `old-text` and the recheck read the remaining text it holds now (the two digests `errors` holds a split to)."""
    return rule_digest(split["old-text"]) == was and split["recheck"]["digest"] == now


def structural(rec: Record) -> bool:
    """Is `rec` a move or a split (an operation on records, never an adjudication of its own)?"""
    return is_move(rec) or is_split(rec)


def answered(records: list[Record]) -> set[str]:
    """The ids whose refusal by the view the batch's moves and splits answer: each move's old and new id, each split's
    parent."""
    out: set[str] = set()
    for rec in records:
        if is_move(rec):
            out |= {rec.get("rule-id", ""), rec.get(MOVE_FROM, "")}
        elif is_split(rec):
            out.add(rec.get("rule-id", ""))
    return out


def _same_words(a: str, b: str) -> bool:
    return norm(a.replace("\\", "")).split() == norm(b.replace("\\", "")).split()


def carved_from(old: str, new: str, carved: list[str]) -> bool:
    """Is `old` the words of `new` with the `carved` pieces inserted whole, in order? A margin paragraph usually ends its
    rule's text, but the displays among and after its lines stay in the rule (§1985:VI.6.3.4's figures, §1985:VI.6.4.6's
    code; kb/Work PB5452), so a piece may come from the middle."""
    o, n = (norm(t.replace("\\", "")).split() for t in (old, new))
    i = j = 0
    for c in (norm(p.replace("\\", "")).split() for p in carved):
        while o[i:i + len(c)] != c:
            if i >= len(o) or j >= len(n) or o[i] != n[j]:
                return False
            i, j = i + 1, j + 1
        i += len(c)
    return o[i:] == n[j:]


def errors(records: list[Record], texts: dict[str, str], store: dict[str, Record], others: set[str]) -> list[str]:
    """Every refusal of the moves and splits among `records` (each `where`-labelled by its position): `texts` is the
    catalog's text by rule id, `store` the edition's store records before the batch, `others` the ids the batch's other
    records name."""
    bad: list[str] = []
    moves = [(i, r) for i, r in enumerate(records) if is_move(r)]
    splits = [(i, r) for i, r in enumerate(records) if is_split(r)]
    sources = [r.get(MOVE_FROM) for _, r in moves]
    targets = [r.get("rule-id") for _, r in moves]
    for i, rec in moves:
        where, new, old = f"[{i}] move {rec.get(MOVE_FROM)!r} -> {rec.get('rule-id')!r}", rec.get("rule-id"), rec.get(MOVE_FROM)
        if set(rec) != {"rule-id", MOVE_FROM} or not all(isinstance(rec[k], str) and rec[k] for k in rec):
            bad.append(f"{where}: a move carries only its new rule-id and '{MOVE_FROM}', both non-empty")
            continue
        if new == old:
            bad.append(f"{where}: a move to the id it is at")
        if sources.count(old) > 1 or targets.count(new) > 1:
            bad.append(f"{where}: an id is moved from, or to, twice")
        if old in others or new in others:
            bad.append(f"{where}: another record of the batch names one of its ids")
        if old not in store:
            bad.append(f"{where}: the store holds no record of {old!r}: a move of nothing")
            continue
        if new not in texts:
            bad.append(f"{where}: {new!r} is not in the catalog")
            continue
        if store[old].get("rule-digest") != rule_digest(texts[new]):
            bad.append(f"{where}: the record was decided against other text than {new!r} holds now: re-adjudicate it, a "
                       "record moves only with its text (DESIGN-spec-conformance-review.md section 10.2)")
        if old in texts and store[old].get("rule-digest") == rule_digest(texts[old]):
            bad.append(f"{where}: the view accepts the record at {old!r} (its text is still there): nothing moved")
        if new in store and new not in sources:
            bad.append(f"{where}: {new!r} holds a record no move of this batch takes away: withdraw or move it first")
    parents = [r.get("rule-id") for _, r in splits]
    for i, rec in splits:
        parent, s = rec.get("rule-id"), rec.get(SPLIT)
        where = f"[{i}] split {parent!r}"
        if set(rec) != {"rule-id", SPLIT} or not isinstance(s, dict) or set(s) - set(SPLIT_FIELDS):
            bad.append(f"{where}: a split carries its rule-id and '{SPLIT}': {{{', '.join(SPLIT_FIELDS)}}}")
            continue
        old_text, carved, rows, recheck = s.get("old-text"), s.get("carved"), s.get("rows", []), s.get("recheck")
        if not isinstance(old_text, str) or not isinstance(carved, list) or not carved \
                or not all(isinstance(c, str) and c.strip() for c in carved) \
                or not isinstance(rows, list) or not all(isinstance(r, str) for r in rows):
            bad.append(f"{where}: 'old-text' is the text, 'carved' a non-empty list of paragraphs, 'rows' a list of ids")
            continue
        if parents.count(parent) > 1 or parent in others or parent in sources or parent in targets:
            bad.append(f"{where}: another record of the batch names its parent")
        if parent not in store:
            bad.append(f"{where}: the store holds no record of {parent!r}: nothing to split")
            continue
        if parent not in texts:
            bad.append(f"{where}: {parent!r} is not in the catalog")
            continue
        if rule_digest(old_text) != store[parent].get("rule-digest"):
            bad.append(f"{where}: 'old-text' is not the text the record was decided against (its rule-digest)")
        if not carved_from(old_text, texts[parent], carved):
            bad.append(f"{where}: the old text is not the parent's text now with the carved paragraphs, in order")
        for rid in rows:
            if rid not in texts:
                bad.append(f"{where}: the carved row {rid!r} is not in the catalog")
            elif not any(_same_words(texts[rid], " ".join(carved[a:b])) for a in range(len(carved))
                         for b in range(a + 1, len(carved) + 1)):
                bad.append(f"{where}: the carved row {rid!r} holds none of the carved paragraphs")
            if rid in store:
                bad.append(f"{where}: the carved row {rid!r} has a record: a carved row inherits nothing (decision "
                           "PB5448 point 4)")
            if rid in others or rid in targets or rid in parents:
                bad.append(f"{where}: another record of the batch names the carved row {rid!r}")
        if not isinstance(recheck, dict) or set(recheck) != set(RECHECK_FIELDS) \
                or not all(isinstance(v, str) and v.strip() for v in recheck.values()):
            bad.append(f"{where}: the parent keeps its record only through a recheck record "
                       f"{{{', '.join(RECHECK_FIELDS)}}}, each non-empty")
        elif recheck["digest"] != rule_digest(texts[parent]):
            bad.append(f"{where}: the recheck read other text than the parent's remaining text")
    return bad


@dataclasses.dataclass
class Applied:
    """What a batch's moves and splits did: each move (old, new), each upheld parent, each withdrawn parent."""
    moved: list[tuple[str, str]] = dataclasses.field(default_factory=list)
    upheld: list[str] = dataclasses.field(default_factory=list)
    opened: list[str] = dataclasses.field(default_factory=list)

    def changed(self) -> set[str]:
        """The rows the moves and splits changed, whose state is computed again."""
        return {i for pair in self.moved for i in pair} | set(self.upheld) | set(self.opened)


def apply(records: list[Record], rows: dict[str, Record], store: dict[str, Record], fields: tuple[str, ...],
          blank: dict[str, Any]) -> Applied:
    """Apply the (validated) moves and splits of `records` to the view's `rows` by id, in place: a moved record's
    adjudicated `fields` go to its new id from the store as it was (`store`, so a chain p6 -> p7 -> p8 moves each record
    once), and leave its old id; an upheld parent keeps its fields (the writer stamps the digest of its text now) with
    the recheck marked in its notes; a parent whose recheck is not upheld is blanked (`blank`), so it opens."""
    out = Applied()
    targets = {r["rule-id"] for r in records if is_move(r)}
    for rec in records:
        if is_move(rec) and (old := rec[MOVE_FROM]) in rows and old not in targets:
            rows[old].update(blank)
    for rec in records:
        if is_move(rec):
            new, old = rec["rule-id"], rec[MOVE_FROM]
            rows[new].update(blank)
            rows[new].update({f: store[old][f] for f in fields if f in store[old]})
            out.moved.append((old, new))
        elif is_split(rec):
            parent, s = rec["rule-id"], rec[SPLIT]
            if s["recheck"]["outcome"] == UPHELD:
                r = s["recheck"]
                mark = (f"{RECHECK_MARK} {', '.join(s.get('rows') or [])} carved {r['date']}; the remaining text "
                        f"rechecked UPHELD by {r['refuter']} ({r['attempt']})")
                rows[parent]["notes"] = f"{rows[parent].get('notes') or ''} {mark}".strip()
                out.upheld.append(parent)
            else:
                rows[parent].update(blank)
                out.opened.append(parent)
    return out


def quotes(reason: str, paragraph: str) -> bool:
    """Does `reason` QUOTE `paragraph`: hold `QUOTE_WORDS` consecutive normalized words of it (all of them, if fewer)?"""
    words, said = norm(paragraph).split(), f" {norm(reason)} "
    k = min(QUOTE_WORDS, len(words))
    return k > 0 and any(f" {' '.join(words[i:i + k])} " in said for i in range(len(words) - k + 1))


def older_restamps(older: dict[str, Record], records: list[Record], texts: dict[str, str],
                   applied: Applied) -> tuple[dict[str, Record], list[str], list[str]]:
    """The OLDER edition's store `older` (records naming this edition's rules in `next.to`) after the batch: a moved id
    renamed in `next.to` and `next.digests` (its digest kept: the text is the same); an upheld or opened parent's digest
    re-stamped to its text now (`texts`) only when its digest is the split's `old-text`'s; a record already stale (its
    digest names another text) or whose `reason` quotes a carved paragraph is left for adjudication (it reads STALE). Returns the new store, the ids re-pointed or re-stamped, and the ids sent to
    adjudication."""
    moved = dict(applied.moved)
    carved = {r["rule-id"]: r[SPLIT]["carved"] for r in records if is_split(r)}
    # A record is re-stamped only when it was decided against the parent's OLD text, as the split itself is held to the
    # stored digest: a record already STALE (its digest names an earlier text) stays so, and goes to adjudication.
    before = {r["rule-id"]: rule_digest(r[SPLIT]["old-text"]) for r in records if is_split(r)}
    out, changed, sent = {}, [], []
    for rid, rec in older.items():
        nxt = rec.get("next")
        if not isinstance(nxt, dict) or not isinstance(nxt.get("to"), list):
            out[rid] = rec
            continue
        # Every rename is read from the record as it was, so a chain p6 -> p7 -> p8 renames each id once.
        to = [moved.get(t, t) for t in nxt["to"]]
        digests = {moved.get(k, k): v for k, v in (nxt.get("digests") or {}).items()}
        touched = to != nxt["to"]
        for t in (t for t in nxt["to"] if t in carved):
            if digests.get(t) != before[t] or any(quotes(nxt.get("reason", ""), c) for c in carved[t]):
                sent.append(rid)
            elif digests.get(t) != (now := rule_digest(texts[t])):
                digests[t] = now
                touched = True
        if touched:
            out[rid] = {**rec, "next": {**nxt, "to": to, **({"digests": digests} if "digests" in nxt or digests else {})}}
            changed.append(rid)
        else:
            out[rid] = rec
    return out, changed, sent


def move_batch(moves: list[tuple[str, str]], stored: set[str]) -> list[Record]:
    """THE MOVE BATCH a regeneration writes (`text_units.moves`: each decided row unit's old and new id): a move record
    for each old id the store holds a record of, in the order of the moves."""
    return [{"rule-id": new, MOVE_FROM: old} for old, new in moves if old in stored]


def write_move_batch(moves: list[tuple[str, str]], stored: set[str], path: pathlib.Path,
                     command: Callable[[pathlib.Path], str]) -> str | None:
    """Write the move batch of `moves` to `path` when it moves any record; returns the line that tells the operator to
    apply it (`command(path)`), or None when nothing moved."""
    batch = move_batch(moves, stored)
    if not batch:
        return None
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps({"batch": "rule-moves", "records": batch}, indent=1) + "\n", encoding="utf-8",
                    newline="\n")
    return (f"⚠ {len(batch)} adjudicated row(s) moved: the view refuses their records until the move batch is applied: "
            f"{command(path)}")
