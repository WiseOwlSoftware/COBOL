#!/usr/bin/env python3
"""THE RULE LINEAGE: one rule list per COBOL edition, linked by one change record per adjacent pair (kb/Work PB4991).

    python scripts/spec/lineage.py --self-test

The design is DESIGN-spec-conformance-review.md §10.2 (owner, 2026-11-08). In short:

- Each edition has its own catalog and its own inventory store (`inventory_store.inventory_dir`). The 2023 store is the
  newest held edition's; an OLDER edition's row carries `next`, its one change record against the next HELD edition:
  `{"kind": "KEPT"|"CHANGED"|"REMOVED", "edition": <int>, "to": [<rule ids of that edition>], "bridge": <bool>,
  "reason": "<why>", "source": "<the audit run or adjudication that decided it>"}`, its `reason` omitted when it states
  none (one stored form: an empty reason is refused, kb/Work PB4995). Its keys and its kinds are DATA, the
  schema's `next` (`inventory_schema.ChangeRecord`, kb/Work PB4993): no code here spells a kind. A rule of the newer
  edition that no older `to` names is NEW: derived, never written. `bridge` is true while an edition between the two is
  not in the lineage (1985 straight to 2023 until 2002 and 2014 are transcribed, kb/Work PB4813, and until the 1985 to
  2002 records link the held 2002, kb/Work PB5256: `inventory_schema.Edition.linked`).
- `status` is the rule's standing in its own edition: `normal`, archaic (§2023:4.2.12: still required, discouraged) or
  obsolete (§1985:I.1.5.2.3, §2023:4.2.13: still required, to be deleted at the next revision), a fact of the rule read
  from its text into its catalog (`element_standing.py`), never stored. Deprecation is KEPT into an obsolete rule, then
  REMOVED.
- A verdict is held once per chain, on the chain's newest row: a row whose record is of the chained kind (KEPT) carries
  none of the schema's `next.verdict-fields` (`next_errors` refuses them), and its state is its chain's at its own edition
  (`state_of`): every `to` row is OK, and every `to` row's `editions` band includes the older edition, which is what
  places the chain's test there. A CHANGED or REMOVED row, and a row not yet classified, is judged on its own fields by
  `Schema.state_for`.

Each edition (its catalog, the catalog key that locates a rule, its store and the adjudicated fields the store holds)
is the schema's `editions.of`, read as `inventory_schema.Edition`; this module is the one reader of the change record's
shape (`next_errors`), the one definition of an older row's state (`state_of`) and of the band an older edition's chains
derive for the next edition's rules (`band_drift`: a rule exists at the older edition exactly when a KEPT record carries
into it, and the stored band token leaves the bands once the two agree, kb/Work PB4996). The view (`inventory_view.py`) joins
each edition's catalog with its store through it and runs the audit, and `record_verdicts.py --edition` writes through
it; nothing else spells the rules.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from typing import Any, Iterable, NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from inventory_schema import Edition, Schema, edition, load_schema  # noqa: E402  (the schema's `editions.of`, `next`)
import inventory_schema  # noqa: E402  (the repository root, for the candidate pairing's file)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402


def chained(row: dict[str, Any], schema: Schema) -> bool:
    """Is `row`'s change record of the chained kind (KEPT), whose verdict is its chain's newest row's?"""
    return (row.get("next") or {}).get("kind") == schema.next.chained.name


def next_errors(row: dict[str, Any], ed: Edition, schema: Schema, targets: dict[str, dict[str, Any]]) -> list[str]:
    """Why `row`'s `next` is not a legal change record of edition `ed` (the schema's `next`) — empty when it is, or when
    the row has none yet. `targets` are the next held edition's rows by id."""
    errs: list[str] = []
    nxt = row.get("next")
    if nxt in (None, {}, ""):
        return errs
    if not isinstance(nxt, dict):
        return [f"next is {type(nxt).__name__}, not an object"]
    shape = schema.next
    if unknown := set(nxt) - shape.keys:
        errs.append(f"next carries unknown key(s) {sorted(unknown)}; it may carry {sorted(shape.keys)}")
    kind, to = shape.kinds.get(nxt.get("kind")), nxt.get("to")
    if kind is None:
        errs.append(f"next.kind {nxt.get('kind')!r} is not one of {list(shape.kinds)}")
    want = ed.next_held
    if nxt.get("edition") != want:
        errs.append(f"next.edition must be {want}, the next held edition after {ed.year}")
    bridged = ed.bridged
    if nxt.get("bridge") is not bridged:
        errs.append(f"next.bridge must be {bridged}: an edition between {ed.year} and {want} is "
                    f"{'not ' if bridged else ''}missing")
    if not isinstance(to, list) or not all(isinstance(t, str) for t in to):
        errs.append("next.to must be a list of rule ids")
        to = []
    for t in to:
        if t not in targets:
            errs.append(f"next.to names {t!r}, which is no rule of edition {want}")
    errs += stale_targets(nxt, to, targets)
    if not (nxt.get("source") or "").strip():
        errs.append("next.source names the audit run or the adjudication that decided it")
    if kind is None:
        return errs
    if kind.to and not to:
        errs.append(f"a {kind.name} record names the rule(s) of the next edition it carries into")
    if not kind.to and to:
        errs.append(f"a {kind.name} record names no rule of the next edition")
    if kind.reason and not (nxt.get("reason") or "").strip():
        errs.append(f"a {kind.name} record states its reason (the words that differ, or what deleted the feature)")
    elif "reason" in nxt and not (nxt["reason"] or "").strip():
        errs.append("next.reason is empty: a record that states no reason omits the key (one stored form)")
    if kind.chained and (own := [f for f in shape.verdict_fields if row.get(f)]):
        errs.append(f"a {kind.name} rule holds no verdict of its own (its chain's newest rule does), but it carries {own}")
    return errs


#: THE TEXT A CHANGE RECORD WAS DECIDED AGAINST, on its targets' side (kb/Work PB5256, branch review R1-4). The record's own
#: rule carries its `rule-digest` (kb/Work PB698); its `to` rules are named by id alone, and an id moves when a corrected
#: transcription renumbers its block (one rule a reconciliation restores to READ's general rules shifts the text under 26
#: ids). So the record carries `digests`, each `to` rule's `rule-digest` when it was recorded (the writer stamps them,
#: `target_digests`), and a target whose text has since changed makes the record STALE, by name.
STALE = "STALE"


def target_digests(to: Iterable[str], targets: dict[str, dict[str, Any]]) -> dict[str, str]:
    """The `digests` a record into `to` carries: each target's `rule-digest` in the next edition's view now. The one
    writer stamps them (record_verdicts.py), so a record is decided against the text the view holds when it is written."""
    return {t: targets[t]["rule-digest"] for t in to if t in targets}


def stale_targets(nxt: dict[str, Any], to: list[str], targets: dict[str, dict[str, Any]]) -> list[str]:
    """Why the `to` side of the record `nxt` is stale or unstamped: a target with no digest, a digest of a rule it does
    not name, or a target whose text is no longer the text its digest records."""
    if not to:
        return []
    digests = nxt.get("digests")
    if not isinstance(digests, dict):
        return ["next.digests is missing: a record names its `to` rules with the digest of the text it was decided against "
                "(record_verdicts.py stamps it)"]
    errs = [f"next.digests names {d!r}, which next.to does not" for d in sorted(set(digests) - set(to))]
    for t in to:
        if t not in digests:
            errs.append(f"next.digests carries no digest of {t!r}")
        elif t in targets and digests[t] != targets[t].get("rule-digest"):
            errs.append(f"{STALE}: next.to names {t!r}, whose text changed since the record was decided (digest "
                        f"{digests[t]}, now {targets[t].get('rule-digest')}): re-decide the record against the text as it is")
    return errs


def state_of(row: dict[str, Any], ed: Edition, schema: Schema, targets: dict[str, dict[str, Any]]) -> str:
    """`row`'s state at its own edition: a chained (KEPT) row's is its chain's, every other row's is its own
    (`Schema.state_for`). `targets` are the next held edition's rows by id, each with its own state."""
    if not chained(row, schema):
        return schema.state_for(row)
    band = ed.spelling
    to = row["next"].get("to") or []
    for t in to:
        target = targets.get(t)
        if target is None or target.get("state") != "OK":
            return "GAP"
        if "editions" in target and band not in schema.split(target.get("editions", "") or "", schema.editions_sep):
            return "GAP"
    return "OK" if to else "GAP"


def new_rules(ed: Edition, rows: Iterable[dict[str, Any]], targets: dict[str, dict[str, Any]]) -> list[str]:
    """The next edition's rules no row of `ed` carries into: NEW at that edition (derived, never written)."""
    reached = {t for r in rows for t in ((r.get("next") or {}).get("to") or [])}
    return [t for t in targets if t not in reached]


class BandDrift(NamedTuple):
    """How far the stored bands of the next held edition's rules are from the positions the chains of edition `ed`
    derive (`chain_bands`, DESIGN-spec-conformance-review.md section 10.2, kb/Work PB4996). The stored `ed` token leaves
    the bands, and every reader takes the positions at `ed` from `chain_bands` alone, when all three are empty."""
    unstored: list[str]      # rules a chained record reaches whose stored band lacks `ed` or an edition the bridge spans
    unproven: list[str]      # rules whose stored band names `ed` that no chained record reaches
    unclassified: list[str]  # rules of `ed` with no change record yet: until each is classified, `unproven` is provisional


def spanned(ed: Edition, schema: Schema) -> list[str]:
    """The band spellings a chained record of `ed` proves of the rule it carries into: `ed`'s own and those of every
    edition between `ed` and the next held one (a bridge composes through them)."""
    years = [e.year for e in schema.all_editions]
    return [e.spelling for e in schema.all_editions[years.index(ed.year):years.index(ed.next_held)]]


def chain_bands(ed: Edition, rows: Iterable[dict[str, Any]], schema: Schema) -> dict[str, tuple[str, ...]]:
    """THE BAND RULE, its one definition (DESIGN-spec-conformance-review.md section 10.2, kb/Work PB4996): the band
    positions edition `ed`'s chains derive for the rules of the next held edition. A rule exists at `ed` exactly when a
    chained (KEPT) record of `rows` (edition `ed`'s) carries into it, and then at every edition the bridge spans as well
    (`spanned`); a CHANGED record proves no band (the newer rule's requirement is not the older one's). Each rule a
    chain reaches maps to those spellings; a rule absent from the result has no position at `ed` or between `ed` and
    the next held edition. The conformance suite's band reader (S5, kb/Work PB4683) reads the positions below the next
    held edition from here, and `band_drift` measures the stored bands against it."""
    proved = tuple(spanned(ed, schema))
    return {t: proved for r in rows if chained(r, schema) for t in (r["next"].get("to") or [])}


def band_drift(ed: Edition, rows: Iterable[dict[str, Any]], targets: dict[str, dict[str, Any]],
               schema: Schema) -> BandDrift:
    """The drift between the stored bands of `targets` (the next held edition's rows, which carry `editions`) and the
    positions `rows`' chains (edition `ed`'s) derive of them (`chain_bands`); see `BandDrift`."""
    rows = list(rows)
    derived = chain_bands(ed, rows, schema)

    def band(t: str) -> list[str]:
        return schema.split(targets[t].get("editions", "") or "", schema.editions_sep)

    return BandDrift(
        unstored=[t for t in targets if t in derived and not set(derived[t]) <= set(band(t))],
        unproven=[t for t in targets if t not in derived and ed.spelling in band(t)],
        unclassified=[r["rule-id"] for r in rows if not (r.get("next") or {}).get("kind")])


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    import copy  # noqa: PLC0415
    from inventory_schema import SCHEMA_PATH, editions_of, held_edition  # noqa: PLC0415
    from inventory_store import inventory_dir  # noqa: PLC0415
    schema = load_schema()
    raw = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    ed = edition(1985)
    targets = {"SR-13.18.38.3-1": {"rule-id": "SR-13.18.38.3-1", "state": "OK", "editions": "85,2002,2014,2023",
                                   "rule-digest": "d1"},
               "SR-13.18.38.3-2": {"rule-id": "SR-13.18.38.3-2", "state": "OK", "editions": "2002,2014,2023",
                                   "rule-digest": "d2"},
               "GR-14.9.21.4-2": {"rule-id": "GR-14.9.21.4-2", "state": "GAP", "editions": "85,2002,2014,2023",
                                  "rule-digest": "d3"}}

    def row(**nxt: Any) -> dict[str, Any]:
        base = {"kind": "KEPT", "edition": 2023, "to": ["SR-13.18.38.3-1"], "bridge": True, "source": "planted"}
        record = {**base, **nxt}
        if "digests" not in nxt and record.get("to"):
            record["digests"] = target_digests(record["to"], targets)
        return {"rule-id": "1985-SR-VI.5.8.3-1", "verdict": "", "next": record}

    def errs(r: dict[str, Any], s: Schema = schema) -> list[str]:
        return next_errors(r, ed, s, targets)

    ok(ed.adjudicated[-1] == "next" and "editions" not in ed.adjudicated and "status" not in ed.adjudicated
       and edition(2023).newest and not ed.newest,
       "an older edition's store holds next, not editions; status is the catalog's; 2023 is the newest held edition")
    ok(ed.next_held == 2023 and ed.bridged and not edition(2023).bridged,
       "1985's next held edition is 2023, a bridge while 2002 and 2014 are not held")
    ok(ed.blank()["next"] == {} and edition(2023).blank()["editions"] == "" and ed.locator == "locator",
       "an unadjudicated rule's fields are empty, and each edition names the catalog key that locates a rule")
    ok(schema.next.chained.name == "KEPT" and set(schema.next.kinds) == {"KEPT", "CHANGED", "REMOVED"},
       "the schema's next names the three kinds of section 10.2, and KEPT is the chained one")
    ok(errs(row()) == [], "a KEPT record into a rule of the next edition, bridged, is legal")
    ok(any("bridge must be True" in e for e in errs(row(bridge=False))),
       "a 1985 record that does not say it bridges 2002 and 2014 is refused")
    ok(any("no rule of edition 2023" in e for e in errs(row(to=["SR-99.9.9-1"]))),
       "a KEPT record into a rule the next edition lacks is refused")
    ok(any("names no rule" in e for e in errs(row(kind="REMOVED", reason="deleted")))
       and errs(row(kind="REMOVED", to=[], reason="the communication module is deleted")) == [],
       "a REMOVED record names no rule, and states its reason")
    ok(any("states its reason" in e for e in errs(row(kind="CHANGED"))), "a CHANGED record states its reason")
    ok(any("omits the key" in e for e in errs(row(reason=""))) and errs(row(reason="the same in other words")) == [],
       "a KEPT record states a reason or omits the key: an empty reason is not stored (one stored form)")
    ok(any("carries into" in e for e in errs(row(kind="CHANGED", to=[], reason="x"))),
       "a CHANGED record names the rule(s) it carries into")
    ok(any("is not one of" in e for e in errs(row(kind="DEPRECATED"))), "a kind the schema does not name is refused")
    ok(any("unknown key" in e for e in errs(row(evidence="§2023:13.18.38.3 SR1"))),
       "a record carrying a key the schema does not name (its evidence is read from the catalogs) is refused")
    ok(any("holds no verdict" in e for e in errs(dict(row(), verdict="CONFORMS"))),
       "a KEPT rule holding a verdict of its own is refused: the chain's newest rule holds it")
    moved = {**targets, "SR-13.18.38.3-1": dict(targets["SR-13.18.38.3-1"], **{"rule-digest": "d-other"})}
    ok([e for e in next_errors(row(), ed, schema, moved) if e.startswith(STALE)]
       == ["STALE: next.to names 'SR-13.18.38.3-1', whose text changed since the record was decided (digest d1, now "
           "d-other): re-decide the record against the text as it is"],
       "a record whose target's text changed under its id is STALE, by name (branch review R1-4)")
    ok(any("next.digests is missing" in e for e in errs(row(digests=None)))
       and any("names 'X'" in e for e in errs(row(digests={"SR-13.18.38.3-1": "d1", "X": "d"})))
       and errs(row(kind="REMOVED", to=[], reason="gone")) == [],
       "a record naming `to` rules carries their digests and no other; a REMOVED record names none and carries none")
    # The kinds are DATA (kb/Work PB4993): a kind the schema alone adds is validated by its flags, with no code change.
    split = copy.deepcopy(raw)
    split["next"]["kinds"]["SPLIT"] = {"meaning": "planted", "to": True, "reason": True, "chained": False}
    s_split = Schema(split)
    ok(errs(row(kind="SPLIT", reason="two rules"), s_split) == []
       and any("states its reason" in e for e in errs(row(kind="SPLIT"), s_split)),
       "a kind the schema alone adds is read from its flags")
    two = copy.deepcopy(raw)
    two["next"]["kinds"]["CHANGED"]["chained"] = True
    try:
        Schema(two).next
        ok(False, "a schema with two chained kinds is refused")
    except SystemExit:
        ok(True, "a schema with two chained kinds is refused")
    # A history reader joins the newest edition at a commit whose schema predates `next` (the gate's WITNESS LOSS audit
    # reads main's): that schema loads, and only a reader that asks for the change record is refused by name.
    before = copy.deepcopy(raw)
    del before["next"]
    old = Schema(before)
    try:
        old.next
        ok(False, "a schema before the change record loads, and asking it for `next` is refused")
    except SystemExit:
        ok(old.state_for({"rule-id": "x", "verdict": ""}) == "GAP",
           "a schema before the change record loads, and asking it for `next` is refused")
    ok(state_of(row(), ed, schema, targets) == "OK", "a KEPT row is OK when its chain's row is OK and its band includes 85")
    ok(state_of(row(to=["SR-13.18.38.3-2"]), ed, schema, targets) == "GAP",
       "a KEPT row whose chain's test is not placed at 85 (the band starts at 2002) is a GAP")
    ok(state_of(row(to=["SR-13.18.38.3-1", "GR-14.9.21.4-2"]), ed, schema, targets) == "GAP",
       "a KEPT row into two rules is a GAP while either is")
    ok(state_of({"rule-id": "x", "verdict": ""}, ed, schema, targets) == "GAP", "an unclassified row with no verdict is a GAP")
    ok(new_rules(ed, [row()], targets) == ["SR-13.18.38.3-2", "GR-14.9.21.4-2"],
       "a rule of the next edition no record reaches is NEW")
    ok(spanned(ed, schema) == ["85", "2002", "2014"], "a bridged 1985 chain proves 85 and every edition the bridge spans")
    ok(chain_bands(ed, [row(), row(kind="CHANGED", to=["SR-13.18.38.3-2"], reason="x"), dict(row(), next={})], schema)
       == {"SR-13.18.38.3-1": ("85", "2002", "2014")},
       "the band rule: a KEPT chain places its rule at 85 and through the bridge; a CHANGED or unclassified record places "
       "nothing")
    drift = band_drift(ed, [row(), row(to=["SR-13.18.38.3-2"]), dict(row(), next={})], targets, schema)
    ok(drift == BandDrift(unstored=["SR-13.18.38.3-2"], unproven=["GR-14.9.21.4-2"], unclassified=["1985-SR-VI.5.8.3-1"]),
       "the band drift: a chained rule whose stored band lacks 85, a stored 85 no chain reaches, an unclassified rule")
    ok(band_drift(ed, [row(kind="CHANGED", reason="x")], {"SR-13.18.38.3-1": targets["SR-13.18.38.3-1"]}, schema).unproven
       == ["SR-13.18.38.3-1"], "a CHANGED record proves no band: the newer rule's requirement is not the older one's")
    ok(band_drift(ed, [row()], {"SR-13.18.38.3-1": dict(targets["SR-13.18.38.3-1"], editions="85,2023")}, schema).unstored
       == ["SR-13.18.38.3-1"], "a bridged chain whose stored band skips an edition the bridge spans is unstored there")

    # Branch review R1-1: an edition is the schema's `editions.of` and nothing else, so adding one there is the whole
    # change: the order, the bridges, the newest edition, the band spelling and the store all follow, with no KeyError.
    unheld, held = copy.deepcopy(raw), copy.deepcopy(raw)
    unheld["editions"]["of"].append({"year": 2027, "spelling": "2027"})
    held["editions"]["of"].append({"year": 2027, "spelling": "2027", "catalog": "docs/rearchitecture/spec-rule-catalog-2027.json",
                                   "locator": "section", "store": "tests/version-matrix/inventory-2027",
                                   "adjudicated": list(edition(2023).adjudicated)})
    s_unheld, s_held = Schema(unheld), Schema(held)
    ok(s_unheld.newest.year == 2023 and s_unheld.edition(2023).newest and s_unheld.editions[-1] == "2027"
       and s_unheld.spell_editions(["2027", "2023"]) == "2023,2027",
       "an edition named in the schema alone is in the order and the bands, and is not held")
    e85, e23, e27 = (held_edition(editions_of(held), y) for y in (1985, 2023, 2027))
    ok(e27.newest and s_held.newest == e27 and not e23.newest and e23.next_held == 2027 and not e23.bridged
       and e85.next_held == 2023 and e85.bridged and inventory_dir(e27).name == "inventory-2027",
       "a held edition named in the schema alone becomes the newest; 2023 then carries a change record into it, unbridged")
    kept23 = {"rule-id": "SR-13.18.38.3-1", "verdict": "",
              "next": {"kind": "KEPT", "edition": 2027, "to": ["SR-1"], "digests": {"SR-1": "d27"}, "bridge": False,
                       "source": "planted"}}
    t27 = {"SR-1": {"rule-id": "SR-1", "state": "OK", "editions": "85,2002,2014,2023,2027", "rule-digest": "d27"}}
    ok(next_errors(kept23, e23, s_held, t27) == [] and state_of(kept23, e23, s_held, t27) == "OK"
       and state_of(kept23, e23, s_held, {"SR-1": dict(t27["SR-1"], editions="2027")}) == "GAP",
       "a 2023 row KEPT into 2027 is judged by its chain at its own spelling, 2023")

    # kb/Work PB5256: a held edition no record names yet (`linked: false`) has its rows, and the lineage bridges over it;
    # linking it is the one change that routes the older edition's records into it.
    unlinked, linked = copy.deepcopy(raw), copy.deepcopy(raw)
    for r, flag in ((unlinked, False), (linked, True)):
        r["editions"]["of"] = [dict(e, catalog="c.json", locator="section", store="tests/version-matrix/inventory-2002",
                                    adjudicated=list(edition(1985).adjudicated), linked=flag) if e["year"] == 2002 else e
                               for e in r["editions"]["of"]]
    u85, u02 = (held_edition(editions_of(unlinked), y) for y in (1985, 2002))
    l85, l02 = (held_edition(editions_of(linked), y) for y in (1985, 2002))
    ok(u85.next_held == 2023 and u85.bridged and u02.held and not u02.linked and u02.next_held == 2023 and u02.bridged
       and spanned(u85, Schema(unlinked)) == ["85", "2002", "2014"],
       "an unlinked held 2002 is bridged over: 1985's records still name 2023, and 2002's own name 2023 over 2014")
    ok(l85.next_held == 2002 and not l85.bridged and l02.next_held == 2023 and l02.bridged,
       "linking 2002 routes 1985's records into it, unbridged")
    from inventory_schema import link_errors  # noqa: PLC0415
    ok(len(link_errors(editions_of(linked), lambda y: y != 2002)) == 1
       and "2002 is linked" in link_errors(editions_of(linked), lambda y: y != 2002)[0]
       and link_errors(editions_of(unlinked), lambda y: y != 2002) == []
       and link_errors(editions_of(linked), lambda y: True) == [],
       "an edition is linked only once its transcription is reconciled: its ids may still move (branch review R1-4)")
    newest_unlinked = copy.deepcopy(raw)
    newest_unlinked["editions"]["of"][-1]["linked"] = False
    try:
        editions_of(newest_unlinked)
        refused = False
    except SystemExit:
        refused = True
    ok(refused, "the newest held edition ends the lineage: an unlinked one is refused")

    def rule(rid: str, kind: str, text: str, subject: str = "MOVE statement") -> dict[str, Any]:
        return {"id": rid, "kind": kind, "subject": subject, "text": text}

    older = [rule("2002-SR-1", "SR", "Identifier-1 shall be numeric."),
             rule("2002-SR-2", "SR", "Identifier-2 shall be numeric — or not."),
             rule("2002-GR-1", "GR", "Identifier-1 shall be numeric."),
             rule("2002-GR-2", "GR", "This International Standard says so."),
             rule("2002-SR-3", "SR", "Identifier-1 shall be numeric.", "ADD statement")]
    newer = [rule("SR-1", "SR", "Identifier-1 shall be numeric."), rule("SR-2", "SR", "Identifier-2 shall be numeric - or not."),
             rule("GR-1", "GR", "This document says so."), rule("SR-9", "SR", "Identifier-1 shall be numeric.", "ADD statement"),
             rule("SR-10", "SR", "Identifier-1 shall be numeric.", "ADD statement")]
    got = candidates(older, newer)
    ok(got["pairs"] == [{"rule-id": "2002-SR-1", "to": ["SR-1"], "match": "exact"},
                        {"rule-id": "2002-SR-2", "to": ["SR-2"], "match": "normalized"},
                        {"rule-id": "2002-SR-3", "to": ["SR-9", "SR-10"], "match": "exact"}]
       and got["unpaired"] == ["2002-GR-1", "2002-GR-2"]
       and got["counts"] == {"rules": 5, "paired": 3, "exact": 2, "normalized": 1, "ambiguous": 1, "unpaired": 2},
       "the candidate pairing matches construct, kind and words only: typography is no difference, a changed word, "
       "another kind or another construct is no candidate, and two matches are both listed")
    ok(pairing_text("1\\) a’s  “X” — b…\n  c. d") == "1) a's \"X\" - b... c) d"
       and all(pairing_text(a) != pairing_text(b) for a, b in (
           ('" {character-1} ... "', '" [character-1] ... "'), ("<u>MOVE</u> x", "MOVE x"), ("A | B", "A B"),
           ("x shall be", "x may be"), ("x shall be", "x SHALL be"), ("NOT x", "x"), ("31 digits", "36 digits"),
           ("a ... b", "a b"))),
       "the pairing erases typography only (an escape, typographic quotes, dashes and ellipsis glyph, an item's "
       "delimiter, white space) and never a bracket, a brace, a bar, an ellipsis, an underline, a word, a case or a "
       "number (branch review R1-3: 2002's braces against 2023's brackets are no pair)")
    print(f"=== LINEAGE SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


# -- the candidate pairing: where the adjudication of an edition's change records starts ----------------------------------

#: WHAT THE PAIRING'S NORMALIZATION MAY ERASE, and nothing else (kb/Work PB5256, branch review R1-3): typography. A
#: Markdown escape (`1\)`), a typographic quote or dash (’ “ ” — –, read as ' " -), the ellipsis glyph (… read as ...),
#: the delimiter of a list item's number or letter (`1.` and `1)`, `a.` and `a)`) and the runs of white space a line wrap
#: leaves. It may NEVER erase a bracket, a brace, a choice bar, an ellipsis, an underline (`<u>`), a word, a letter's
#: case, a number or any other character: the review found `cite.norm`, which keeps words only, pairing 2002's
#: "{character-1} ..." (at least one character) with 2023's "[character-1] ..." (the zero-length literal), a change of
#: meaning, so a general format's notation is part of the key like every other character.
_TYPOGRAPHY = str.maketrans({"\u2018": "'", "\u2019": "'", "\u201a": "'", "\u201b": "'", "\u201c": '"', "\u201d": '"',
                             "\u201e": '"', "\u2013": "-", "\u2014": "-", "\u2012": "-", "\u2015": "-", "\u2212": "-",
                             "\u2010": "-", "\u2011": "-"})
_ESCAPE = re.compile(r"\\(?=[^\w\s])")
_ITEM_DELIMITER = re.compile(r"(?:(?<=^)|(?<=\s))(\d+|[a-z])[.)](?=\s)")


def pairing_text(text: str) -> str:
    """`text` with its typography erased and nothing else (see `_TYPOGRAPHY`): the text a `normalized` pair shares."""
    t = _ESCAPE.sub("", text).translate(_TYPOGRAPHY).replace("\u2026", "...")
    return re.sub(r"\s+", " ", _ITEM_DELIMITER.sub(r"\1)", t)).strip()


def candidate_key(rule: dict[str, Any]) -> tuple[str, str, str]:
    """What a candidate pairing compares: the rule's construct (its catalog `subject`, the clause title its rules rule,
    in either case), its kind (the rule block's title: Syntax rules, General rules, ...) and its text with its typography
    erased (`pairing_text`)."""
    return rule["subject"].strip().lower(), rule["kind"], pairing_text(rule["text"])


def candidates(older: Iterable[dict[str, Any]], newer: Iterable[dict[str, Any]]) -> dict[str, Any]:
    """THE CANDIDATE PAIRING of each rule of `older` (an edition's catalog rules) with the rules of `newer` (the next
    linked held edition's) whose construct, kind and words are the same (`candidate_key`): `exact` when the text is
    identical, `normalized` when only its typography differs. NO JUDGMENT: a rule whose words changed at all is unpaired,
    a rule two newer rules match lists both (`ambiguous`), and no change record is written. It is where the adjudication
    of the older edition's records starts (DESIGN-spec-conformance-review.md section 10.2), never a verdict."""
    by_key: dict[tuple[str, str, str], list[dict[str, Any]]] = {}
    for r in newer:
        by_key.setdefault(candidate_key(r), []).append(r)
    pairs, unpaired = [], []
    for r in older:
        hits = by_key.get(candidate_key(r), [])
        if not hits:
            unpaired.append(r["id"])
            continue
        pairs.append({"rule-id": r["id"], "to": [h["id"] for h in hits],
                      "match": "exact" if all(h["text"] == r["text"] for h in hits) else "normalized"})
    counts = {"rules": len(pairs) + len(unpaired), "paired": len(pairs),
              "exact": sum(p["match"] == "exact" for p in pairs),
              "normalized": sum(p["match"] == "normalized" for p in pairs),
              "ambiguous": sum(len(p["to"]) > 1 for p in pairs), "unpaired": len(unpaired)}
    return {"counts": counts, "pairs": pairs, "unpaired": unpaired}


def candidates_path(year: int) -> pathlib.Path:
    return inventory_schema.REPO / "docs" / "rearchitecture" / f"spec-lineage-candidates-{year}.json"


def candidates_document(year: int) -> dict[str, Any]:
    """The candidate pairing of the held edition `year` with its next linked held edition, as committed."""
    ed = edition(year)
    if ed.next_held is None:
        raise SystemExit(f"--candidates: {year} is the newest held edition; its rules carry into no later one")
    found = candidates(ed.rules(), edition(ed.next_held).rules())
    return {"_comment": [
        f"THE CANDIDATE PAIRING of each {year} rule with the {ed.next_held} rules of the same construct, kind and words",
        "(lineage.candidates, kb/Work PB5256): `exact` text, or `normalized` (cite.norm: words only). No judgment and no",
        "verdict: a rule whose words changed is unpaired, two matches are both listed, and nothing here is a change record.",
        f"The adjudication of the {year} change records (record_verdicts.py --edition {year}) starts from it.",
        f"Regenerate: python scripts/spec/lineage.py --candidates {year}; --check fails when it is stale."],
        "edition": year, "next": ed.next_held, "bridge": ed.bridged, **found}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The rule lineage across editions (kb/Work PB4991). The audit's kept change "
                                             "records are written by audit_batch.py change-batch.")
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--candidates", type=int, nargs="?", const=0, metavar="YEAR",
                    help="write the candidate pairing of the held edition YEAR's rules with its next linked edition's; "
                         "with no YEAR, of every older held edition (inventory_schema.HELD): the gate's LINEAGE "
                         "CANDIDATES audit, so a newly held edition needs no new audit line")
    ap.add_argument("--check", action="store_true", help="with --candidates: fail when the committed pairing is stale")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.candidates is not None:
        years = [a.candidates] if a.candidates else [y for y in inventory_schema.HELD if not edition(y).newest]
        stale = []
        for year in years:
            doc, path = candidates_document(year), candidates_path(year)
            print(f"{year} -> {doc['next']}: " + ", ".join(f"{k} {v}" for k, v in doc["counts"].items()))
            text = json.dumps(doc, indent=1, ensure_ascii=False) + "\n"
            if a.check:
                if not path.exists() or path.read_text(encoding="utf-8") != text:
                    stale.append(year)
                print(f"{'⛔ STALE' if year in stale else '✓ current'}: {path.relative_to(inventory_schema.REPO)}")
                continue
            path.write_text(text, encoding="utf-8", newline="\n")
            print(f"wrote {path.relative_to(inventory_schema.REPO)}")
        if a.check:
            print(f"=== LINEAGE CANDIDATES: {'GREEN' if not stale else f'RED (stale: {stale}; regenerate: lineage.py --candidates)'} ===")
        return 1 if stale else 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
