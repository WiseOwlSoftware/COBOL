#!/usr/bin/env python3
"""The import of a disposition campaign's records (kb/Work PB5452) on planted data: the latest slice's record wins, an
OVERTURNED refuter record's correction wins over its reader's disposition, a `restates` or `not-a-rule` stands only on
an UPHELD record of its own slice, a hold leaves its unit pending, a `restates` chain resolves, the `not-rules` blocks
take their records; and each refusal fires on its planted failure: a stale key imported, a refutation ignored, a
pending unit with no record."""
import copy
import sys

import selftest_cases
from standard_streams import ensure_utf8

M = selftest_cases.subject(__file__)   # scripts/spec/import_dispositions.py


def _row_id(kind, clause, label):
    return f"1985-{kind}-{clause.split(':')[1]}-{label}"


#: The tree: two clauses' units, each (label, digest, order).
TREE = {"§1985:IV.1": [("p1", "aaa", 0), ("p2", "bbb", 0), ("p3", "ccc", 0)],
        "§1985:IV.2": [("p1", "ddd", 0), ("p2", "eee", 0)]}


def _r(clause, unit, digest, disposition, **more):
    return {"clause": clause, "unit": unit, "digest": digest, "order": 0, "disposition": disposition, **more}


def _ref(clause, unit, digest, outcome, **more):
    return {"clause": clause, "unit": unit, "digest": digest, "outcome": outcome, "refuter": "planted", **more}


def _slices():
    first = ("85u-01", [_r("§1985:IV.1", "p1", "aaa", "GR", definitional=True),
                        _r("§1985:IV.1", "p2", "bbb", "restates", rules=["1985-GR-IV.1-p1"]),
                        _r("§1985:IV.1", "p3", "ccc", "not-a-rule", **{"class": "lead-in"}),
                        _r("§1985:IV.2", "p1", "ddd", "SR"),
                        _r("§1985:IV.2", "p9", "zzz", "GR")],          # a key a re-segmentation retired
             [_ref("§1985:IV.1", "p2", "bbb", "UPHELD"), _ref("§1985:IV.1", "p3", "ccc", "OVERTURNED", corrected="SR"),
              _ref("§1985:IV.2", "p1", "ddd", "OVERTURNED", corrected="restates 1985-GR-IV.1-p2")])
    second = ("85r-01", [_r("§1985:IV.2", "p1", "ddd", "restates", rules=["1985-GR-IV.1-p2"]),
                         _r("§1985:IV.2", "p2", "eee", "GR", hold="a later list")],
              [_ref("§1985:IV.2", "p1", "ddd", "UPHELD")])
    return [first, second]


SKIP = [{"clause": "§1985:IV.2", "unit": "p9", "digest": "zzz", "order": 0, "file": "85u-01.json"}]


def _decide(slices=None, skip=SKIP, tree=TREE):
    return M.decide(tree, slices if slices is not None else _slices(), skip, lambda n: f"refutations/{n}-refute.jsonl",
                    _row_id)


def case_01_the_records_decide_every_unit(expect):
    out = _decide()
    expect.equal("no refusal", out.refusals, [])
    e = {(c, k[0]): v for c, es in out.entries.items() for k, v in es.items()}
    expect.equal("a reader's row stands, definitional riding with it", (e["§1985:IV.1", "aaa"]["disposition"],
                                                                        e["§1985:IV.1", "aaa"].get("definitional")),
                 ("GR", True))
    expect.equal("an OVERTURNED record's correction wins over its reader", e["§1985:IV.1", "ccc"]["disposition"], "SR")
    expect.equal("the later slice wins, and its UPHELD record lets a restates stand",
                 (e["§1985:IV.2", "ddd"]["disposition"], e["§1985:IV.2", "ddd"]["refuted"]),
                 ("restates", "refutations/85r-01-refute.jsonl"))
    expect.equal("a hold leaves its unit pending", e["§1985:IV.2", "eee"]["disposition"], "pending")
    expect.equal("a restates of a unit that restates resolves to that unit's rules",
                 (e["§1985:IV.2", "ddd"]["rules"], e["§1985:IV.2", "ddd"]["restated_via"]),
                 (["1985-GR-IV.1-p1"], ["1985-GR-IV.1-p2"]))


def case_02_a_stale_key_imported_is_refused(expect):
    out = _decide(skip=[])
    expect("a record of a key no unit of the tree has", any("no unit of the tree has this key" in f
                                                            for f in out.refusals), out.refusals)
    phantom = SKIP + [{"clause": "§1985:IV.2", "unit": "p8", "digest": "yyy", "order": 0, "file": "85u-01.json"}]
    expect("a skip no record holds", any("a skip no record holds" in f for f in _decide(skip=phantom).refusals))


def case_03_a_refutation_ignored_is_refused(expect):
    slices = _slices()
    slices[1] = ("85r-01", slices[1][1], [])            # the restates' UPHELD record never written
    out = _decide(slices)
    expect("a restates whose correction no later record upholds", any(
        "IV.2 p1" in f and "no UPHELD refuter record" in f and "OVERTURNED" not in f for f in out.refusals),
        out.refusals)
    slices = _slices()[:1]                               # the OVERTURNED correction to restates, never refuted itself
    slices[0][1][3]["disposition"] = "SR"
    out = _decide([slices[0], ("85r-01", [_r("§1985:IV.2", "p2", "eee", "GR")], [])])
    expect("a row an earlier refuter OVERTURNED to restates stays out of nothing without a later UPHELD record",
           any("IV.2 p1" in f and "OVERTURNED it" in f for f in out.refusals), out.refusals)
    slices = _slices()
    slices[0][2][0] = _ref("§1985:IV.1", "p2", "bbb", "OVERTURNED", corrected="GR")
    e = _decide(slices).entries["§1985:IV.1"]["bbb", 0]
    expect.equal("an OVERTURNED restates is the correction's row, never the reader's restates", e["disposition"], "GR")


def case_04_a_pending_unit_with_no_record_is_refused(expect):
    tree = copy.deepcopy(TREE)
    tree["§1985:IV.2"].append(("p3", "fff", 0))
    expect("a unit of the tree no record disposes", any("IV.2 p3: a unit of the tree with no record" in f
                                                       for f in _decide(tree=tree).refusals))


def case_05_the_not_rules_blocks_take_their_records(expect):
    declared = {"§1985:II.1": {"disposition": "not-rules"}, "§1985:II.2": {"disposition": "not-rules"},
                "§1985:II.3": {"disposition": "rules", "kind": "GR"}}
    measured = {"§1985:II.1": {"digest": "d1"}, "§1985:II.2": {"digest": "d2"}}
    recs = [{"unit": "§1985:II.1", "digest": "old", "outcome": "UPHELD"},
            {"unit": "§1985:II.2", "digest": "d2", "outcome": "OVERTURNED", "corrected": "GR"}]
    blocks, committed, refused = M.refute_blocks(declared, measured, recs, "refutations/85b-01-refute.jsonl")
    expect.equal("an upheld block names its record and the digest its text has now; an overturned one is pending",
                 (blocks["§1985:II.1"].get("refuted"), blocks["§1985:II.1"]["digest"], blocks["§1985:II.2"]["disposition"],
                  committed[0]["digest"], committed[0]["digest_read"], refused),
                 ("refutations/85b-01-refute.jsonl", "d1", "pending", "d1", "old", []))
    _b, _c, refused = M.refute_blocks(declared, measured, recs[:1], "r")
    expect("a not-rules block with no record is refused", any("§1985:II.2" in f and "no refuter record" in f
                                                             for f in refused))


def case_06_an_earlier_imports_entries_stand(expect):
    # A later campaign (85w) disposes only the units still pending: every other unit keeps the entry an earlier import
    # wrote, and a pending unit no slice holds is still refused (kb/Work PB5452, the 85w import).
    tree = copy.deepcopy(TREE)
    tree["§1985:IV.2"].append(("p3", "fff", 0))
    tree["§1985:IV.2"].append(("p4", "ggg", 0))
    standing = {("§1985:IV.2", "fff", 0): {"unit": "p3", "digest": "fff", "order": 0, "disposition": "GR"},
                ("§1985:IV.2", "ggg", 0): {"unit": "p4", "digest": "ggg", "order": 0, "disposition": "pending"}}
    out = M.decide(tree, _slices(), SKIP, lambda n: f"refutations/{n}-refute.jsonl", _row_id, standing)
    expect.equal("a decided entry no slice holds stands, untouched", out.entries["§1985:IV.2"].get(("fff", 0)),
                 standing["§1985:IV.2", "fff", 0])
    expect("a PENDING entry no slice holds is a unit with no record",
           any("IV.2 p4: a unit of the tree with no record" in f for f in out.refusals), out.refusals)
    expect.equal("the standing units are told apart from the decided", out.standing, {("§1985:IV.2", "fff", 0)})
    tree["§1985:IV.2"].pop()
    slices = _slices()
    slices[1][1].append(_r("§1985:IV.2", "p3", "fff", "SR"))
    out = M.decide(tree, slices, SKIP, lambda n: f"refutations/{n}-refute.jsonl", _row_id, standing)
    expect.equal("a slice holding a standing unit's key decides it again", out.entries["§1985:IV.2"]["fff", 0]
                 ["disposition"], "SR")


def case_07_a_restates_is_verified_against_the_catalog_and_this_import(expect):
    # 85w-05's p170 restates 1985-GR-III.2-p173, a row of its own slice: an id of THE SAME import stands, a catalog id
    # stands, and an id neither has is refused at the import (it would otherwise surface only after the mint).
    catalog = {"1985-GR-IV.9-3"}
    slices = _slices()
    slices[1][1][0]["rules"] = ["1985-GR-IV.1-p2", "1985-GR-IV.9-3"]
    out = M.decide(TREE, slices, SKIP, lambda n: f"refutations/{n}-refute.jsonl", _row_id, None, catalog)
    expect.equal("a row of this import and a catalog row both stand", out.refusals, [])
    expect.equal("the chain still resolves through this import's restates",
                 sorted(out.entries["§1985:IV.2"]["ddd", 0]["rules"]), ["1985-GR-IV.1-p1", "1985-GR-IV.9-3"])
    slices = _slices()
    slices[1][1][0]["rules"] = ["1985-GR-IV.1-p7"]
    out = M.decide(TREE, slices, SKIP, lambda n: f"refutations/{n}-refute.jsonl", _row_id, None, catalog)
    expect("an id no catalog row and no unit of the import is", any(
        "restates 1985-GR-IV.1-p7, which is no row of the catalog" in f for f in out.refusals), out.refusals)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
