#!/usr/bin/env python3
"""The move and the split of an adjudicated row on planted data (kb/Work PB5449 item 6, decision PB5448 points 4 and 7):
a chain of moves carries each record once and only with its text; a split checks the old text against the parent's text
and the carved paragraphs, writes no record for a carved row, and keeps the parent's record only through an upheld
recheck; the older edition's change records follow (a move renames, a split re-stamps, a quoting reason goes STALE);
and every refusal fires on its planted failure."""
import copy
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

M = selftest_cases.subject(__file__)   # scripts/spec/rule_moves.py
D = M.rule_digest

#: The planted catalog after a regeneration: a paragraph was inserted before p6, so the clause's p6 and p7 now read p7
#: and p8; GR-9.9.4-2 lost its glued margin paragraph to the new row GR-9.9.4-p9.
TEXTS = {"GR-9.9.4-p6": "A new paragraph the repair split off.",
         "GR-9.9.4-p7": "The lock is held until the file is closed.",
         "GR-9.9.4-p8": "A record lock is released by an UNLOCK statement.",
         "GR-9.9.4-2": "The file is opened in the mode named.",
         "GR-9.9.4-p9": "If the file is shared, every lock it holds is released at its close."}
OLD_PARENT = TEXTS["GR-9.9.4-2"] + " " + TEXTS["GR-9.9.4-p9"]
FIELDS = ("verdict", "code-location", "test-ref", "notes")
BLANK = {f: "" for f in FIELDS}


def _store() -> dict:
    """The store before the batch: the records of the old p6 and p7 (decided against the texts p7 and p8 hold now), and
    the parent's record (decided against its text with the margin paragraph)."""
    return {"GR-9.9.4-p6": {"rule-id": "GR-9.9.4-p6", "rule-digest": D(TEXTS["GR-9.9.4-p7"]), "verdict": "CONFORMS",
                            "test-ref": "unit:LockTests.Held"},
            "GR-9.9.4-p7": {"rule-id": "GR-9.9.4-p7", "rule-digest": D(TEXTS["GR-9.9.4-p8"]), "verdict": "CONFORMS",
                            "test-ref": "unit:LockTests.Unlock"},
            "GR-9.9.4-2": {"rule-id": "GR-9.9.4-2", "rule-digest": D(OLD_PARENT), "verdict": "CONFORMS",
                           "test-ref": "unit:OpenTests.Mode", "notes": "Opened in its mode."}}


def _rows(store: dict) -> dict:
    return {rid: {"rule-id": rid, **BLANK, **{f: store.get(rid, {}).get(f, "") for f in FIELDS}} for rid in TEXTS}


MOVES = [{"rule-id": "GR-9.9.4-p8", "move-from": "GR-9.9.4-p7"},
         {"rule-id": "GR-9.9.4-p7", "move-from": "GR-9.9.4-p6"}]


def _split(outcome: str = "UPHELD", **over) -> dict:
    s = {"old-text": OLD_PARENT, "carved": [TEXTS["GR-9.9.4-p9"]], "rows": ["GR-9.9.4-p9"],
         "recheck": {"outcome": outcome, "refuter": "planted-refuter", "attempt": "the mode rule stands alone",
                     "date": "2026-11-20", "digest": D(TEXTS["GR-9.9.4-2"])}}
    return {"rule-id": "GR-9.9.4-2", "split": {**s, **over}}


def case_00_a_chain_of_moves_carries_each_record_once(expect):
    store = _store()
    expect.equal("the chain is valid", M.errors(MOVES, TEXTS, store, set()), [])
    rows = _rows(store)
    applied = M.apply(MOVES, rows, store, FIELDS, BLANK)
    expect.equal("p6's record is at p7, p7's at p8",
                 (rows["GR-9.9.4-p7"]["test-ref"], rows["GR-9.9.4-p8"]["test-ref"]),
                 ("unit:LockTests.Held", "unit:LockTests.Unlock"))
    expect.equal("the vacated p6 holds nothing (the new paragraph is unadjudicated)", rows["GR-9.9.4-p6"], {"rule-id": "GR-9.9.4-p6", **BLANK})
    expect.equal("the moves are reported", sorted(applied.moved), [("GR-9.9.4-p6", "GR-9.9.4-p7"), ("GR-9.9.4-p7", "GR-9.9.4-p8")])
    expect.equal("the answered ids", M.answered(MOVES), {"GR-9.9.4-p6", "GR-9.9.4-p7", "GR-9.9.4-p8"})


def case_01_a_move_is_refused_unless_its_text_moved_with_it(expect):
    store = _store()

    def refused(moves, others=frozenset(), st=None) -> list:
        return M.errors(moves, TEXTS, st or store, set(others))

    expect("a record moves only with its text", any("other text" in e for e in refused(
        [{"rule-id": "GR-9.9.4-p8", "move-from": "GR-9.9.4-p6"}, {"rule-id": "GR-9.9.4-p7", "move-from": "GR-9.9.4-p7x"}])))
    expect("a move of nothing", any("move of nothing" in e for e in refused([{"rule-id": "GR-9.9.4-p9", "move-from": "GR-9.9.4-p1"}])))
    expect("a target holding a record no move takes away", any("withdraw or move it first" in e for e in refused(
        [{"rule-id": "GR-9.9.4-p7", "move-from": "GR-9.9.4-p6"}])))
    st = copy.deepcopy(store)
    st["GR-9.9.4-p7"]["rule-digest"] = D(TEXTS["GR-9.9.4-p7"])
    expect("the view still accepts the record where it is", any("nothing moved" in e for e in refused(
        [{"rule-id": "GR-9.9.4-p8", "move-from": "GR-9.9.4-p7"}], st=st)))
    expect("an id moved twice", any("twice" in e for e in refused(MOVES + [{"rule-id": "GR-9.9.4-p8", "move-from": "GR-9.9.4-2"}])))
    expect("another record names a moved id", any("another record" in e for e in refused(MOVES, {"GR-9.9.4-p8"})))
    expect("a move carries only its two ids", any("carries only" in e for e in refused(
        [{"rule-id": "GR-9.9.4-p8", "move-from": "GR-9.9.4-p7", "verdict": "CONFORMS"}])))


def case_02_an_upheld_split_keeps_the_parent_and_writes_no_carved_record(expect):
    store = _store()
    split = [_split()]
    expect.equal("the split is valid", M.errors(split, TEXTS, store, set()), [])
    rows = _rows(store)
    applied = M.apply(split, rows, store, FIELDS, BLANK)
    parent = rows["GR-9.9.4-2"]
    expect("the parent keeps its verdict and test", parent["verdict"] == "CONFORMS" and parent["test-ref"] == "unit:OpenTests.Mode")
    expect("its notes keep their prose and mark the recheck",
           parent["notes"].startswith("Opened in its mode. ") and M.RECHECK_MARK in parent["notes"]
           and "GR-9.9.4-p9" in parent["notes"] and "planted-refuter" in parent["notes"], parent["notes"])
    expect.equal("the carved row inherits nothing", rows["GR-9.9.4-p9"], {"rule-id": "GR-9.9.4-p9", **BLANK})
    expect.equal("upheld", applied.upheld, ["GR-9.9.4-2"])


def case_03_a_split_whose_recheck_fails_opens_the_parent(expect):
    store = _store()
    split = [_split("REFUTED")]
    expect.equal("a refuted recheck is a valid split", M.errors(split, TEXTS, store, set()), [])
    rows = _rows(store)
    applied = M.apply(split, rows, store, FIELDS, BLANK)
    expect.equal("the parent opens", (rows["GR-9.9.4-2"], applied.opened), ({"rule-id": "GR-9.9.4-2", **BLANK}, ["GR-9.9.4-2"]))


def case_04_a_split_is_refused_unless_its_text_and_recheck_hold(expect):
    store = _store()

    def refused(rec, st=None) -> list:
        return M.errors([rec], TEXTS, st or store, set())

    expect("old text other than the record's", any("decided against" in e for e in refused(_split(**{"old-text": OLD_PARENT + " More."}))))
    expect("carved text out of order or missing", any("in order" in e for e in refused(
        _split(**{"carved": [TEXTS["GR-9.9.4-p9"], "Another sentence."]}))))
    st = copy.deepcopy(store)
    st["GR-9.9.4-p9"] = {"rule-id": "GR-9.9.4-p9", "rule-digest": D(TEXTS["GR-9.9.4-p9"]), "verdict": "CONFORMS"}
    expect("a carved row with a record", any("inherits nothing" in e for e in refused(_split(), st)))
    expect("a carved row holding none of the carved text", any("holds none" in e for e in refused(_split(rows=["GR-9.9.4-p8"]))))
    bare = _split()
    del bare["split"]["recheck"]
    expect("no recheck record", any("recheck record" in e for e in refused(bare)))
    other = _split()
    other["split"]["recheck"]["digest"] = D(OLD_PARENT)
    expect("a recheck of other text", any("recheck read other text" in e for e in refused(other)))
    expect("a split of an unrecorded rule", any("nothing to split" in e for e in refused(
        {"rule-id": "GR-9.9.4-p9", "split": _split()["split"]})))


def case_05_the_older_change_records_follow(expect):
    store = _store()
    ops = MOVES + [_split()]
    rows = _rows(store)
    applied = M.apply(ops, rows, store, FIELDS, BLANK)
    older = {
        "1985-GR-IX.1-1": {"rule-id": "1985-GR-IX.1-1", "next": {
            "kind": "KEPT", "to": ["GR-9.9.4-p6", "GR-9.9.4-p7"], "reason": "the lock rules",
            "digests": {"GR-9.9.4-p6": D(TEXTS["GR-9.9.4-p7"]), "GR-9.9.4-p7": D(TEXTS["GR-9.9.4-p8"])}}},
        "1985-GR-IX.1-2": {"rule-id": "1985-GR-IX.1-2", "next": {
            "kind": "KEPT", "to": ["GR-9.9.4-2"], "reason": "the open mode rule",
            "digests": {"GR-9.9.4-2": D(OLD_PARENT)}}},
        "1985-GR-IX.1-3": {"rule-id": "1985-GR-IX.1-3", "next": {
            "kind": "CHANGED", "to": ["GR-9.9.4-2"],
            "reason": "2023 adds that if the file is shared, every lock it holds is released at its close",
            "digests": {"GR-9.9.4-2": D(OLD_PARENT)}}},
        "1985-GR-IX.1-4": {"rule-id": "1985-GR-IX.1-4", "verdict": "CONFORMS"}}
    fresh, changed, sent = M.older_restamps(older, ops, TEXTS, applied)
    n1 = fresh["1985-GR-IX.1-1"]["next"]
    expect.equal("a chain renames each id once and keeps its digest",
                 (n1["to"], n1["digests"]), (["GR-9.9.4-p7", "GR-9.9.4-p8"],
                                            {"GR-9.9.4-p7": D(TEXTS["GR-9.9.4-p7"]), "GR-9.9.4-p8": D(TEXTS["GR-9.9.4-p8"])}))
    expect.equal("a split parent is re-stamped", fresh["1985-GR-IX.1-2"]["next"]["digests"],
                 {"GR-9.9.4-2": D(TEXTS["GR-9.9.4-2"])})
    expect.equal("a reason quoting the carved paragraph is left STALE for adjudication",
                 (fresh["1985-GR-IX.1-3"], sent), (older["1985-GR-IX.1-3"], ["1985-GR-IX.1-3"]))
    expect.equal("a record naming nothing moved is untouched", fresh["1985-GR-IX.1-4"], older["1985-GR-IX.1-4"])
    expect.equal("the changed records", sorted(changed), ["1985-GR-IX.1-1", "1985-GR-IX.1-2"])
    expect("a quotation is eight consecutive words, or the whole of a shorter paragraph",
           M.quotes("it says the lock is held until it is", TEXTS["GR-9.9.4-p7"]) is False
           and M.quotes("x the lock is held until the file is closed y", TEXTS["GR-9.9.4-p7"])
           and M.quotes("so: a short rule.", "A short rule."))


def case_05b_an_already_stale_older_record_is_never_re_stamped(expect):
    # Review R1-1 (round 1, kb/Work PB5449): a change record whose digest names an EARLIER text of the parent than the
    # split's old text was stale before the split; re-stamping it would make it fresh without anyone reading it again.
    store = _store()
    ops = [_split()]
    applied = M.apply(ops, _rows(store), store, FIELDS, BLANK)
    older = {"1985-GR-IX.1-5": {"rule-id": "1985-GR-IX.1-5", "next": {
        "kind": "KEPT", "to": ["GR-9.9.4-2"], "reason": "the open mode rule",
        "digests": {"GR-9.9.4-2": D("The file is opened in a mode.")}}}}
    fresh, changed, sent = M.older_restamps(older, ops, TEXTS, applied)
    expect.equal("its digest is left as it was, and it goes to adjudication",
                 (fresh["1985-GR-IX.1-5"], changed, sent), (older["1985-GR-IX.1-5"], [], ["1985-GR-IX.1-5"]))


def case_06_the_move_batch_names_only_recorded_ids(expect):
    batch = M.move_batch([("GR-9.9.4-p6", "GR-9.9.4-p7"), ("GR-9.9.4-p2", "GR-9.9.4-p3")], {"GR-9.9.4-p6"})
    expect.equal("one move record, for the id the store holds", batch, [{"rule-id": "GR-9.9.4-p7", "move-from": "GR-9.9.4-p6"}])


def case_07_an_applied_move_or_split_batch_leaves_its_tracked_copy(expect):
    """kb/Work PB6002, PB6020: the writer copies each applied batch that holds a move or a split to MOVES_DIR in the
    same operation (until 2026-11-21 a split's copy existed only by a hand copy, and a move left none, so the read
    records could not follow it); each move carries the slices that read its old id; a batch with neither leaves none, a
    batch applied from its own tracked file is its copy, and another batch's file of the same name is refused."""
    repo = scratch_dir("rule-moves-split-copy")
    split = {"rule-id": "GR-9.9.4-2", "split": {"old-text": OLD_PARENT, "carved": [], "rows": ["GR-9.9.4-p9"],
                                               "recheck": {}}}
    move = {"rule-id": "GR-9.9.4-p7", "move-from": "GR-9.9.4-p6"}
    verdict = {"rule-id": "GR-1", "verdict": "x"}
    scratch = repo / "scratch"
    scratch.mkdir()
    applied = scratch / "mint-splits.json"
    applied.write_text(json.dumps({"batch": "the mint", "records": [move, split, verdict]}),
                       encoding="utf-8", newline="\n")
    plain = scratch / "verdicts.json"
    plain.write_text(json.dumps([verdict]), encoding="utf-8", newline="\n")
    readers = {"GR-9.9.4-p6": ["s-02", "s-01"]}.get
    files, errs = M.tracked_copies([(applied, [move, split, verdict]), (plain, [verdict])], 1985, repo,
                                   lambda rid: readers(rid, []))
    dest = repo / M.MOVES_DIR.format(year=1985) / "mint-splits.json"
    expect.equal("the copy is the batch's moves and splits alone, under its label, at MOVES_DIR/<the batch's name>; a "
                 "batch of verdicts alone leaves none", (list(files), errs), ([dest], []))
    expect.equal("its text is the record the audit and the reads read: each move with the slices that read its old id",
                 json.loads(files[dest]), {"batch": "the mint", M.APPLIED: 1,
                                          "records": [{**move, M.READS: ["s-01", "s-02"]}, split]})
    dest.parent.mkdir(parents=True)
    dest.write_text(files[dest], encoding="utf-8", newline="\n")
    expect.equal("recorded_moves reads the copy back: (old, new, its readers), one list per batch",
                 M.recorded_moves(1985, repo), [[("GR-9.9.4-p6", "GR-9.9.4-p7", frozenset({"s-01", "s-02"}))]])
    expect.equal("a batch applied again whose copy is there, byte for byte, writes it again unchanged",
                 M.tracked_copies([(applied, [move, split])], 1985, repo, lambda rid: readers(rid, []))[1], [])
    expect.equal("a batch applied from its own tracked file is its copy already",
                 M.tracked_copies([(dest, [split])], 1985, repo, lambda rid: []), ({}, []))
    dest.write_text(json.dumps({"batch": "another", M.APPLIED: 1, "records": []}), encoding="utf-8", newline="\n")
    expect("another batch's tracked file of the same name refuses the apply, never overwritten",
           "already holds another batch" in " ".join(M.tracked_copies([(applied, [split])], 1985, repo,
                                                                      lambda rid: [])[1]))

def case_08_the_moves_are_followed_in_the_order_the_batches_were_applied(expect):
    """train-1985-b review R1-1: two chained batches whose file names sort OPPOSITE to their application (`z-first.json`
    moves A -> B and is applied first, `a-second.json` moves B -> C after it). The copies carry their place in the
    application order, and recorded_moves returns them in it, so a reader follows A -> B, then B -> C; before, the name
    order put B -> C first. A copy with no place, or two at one place, cannot be ordered and raises."""
    repo = scratch_dir("rule-moves-applied-order")
    scratch = repo / "scratch"
    scratch.mkdir()
    first, second = scratch / "z-first.json", scratch / "a-second.json"
    ab, bc = {"rule-id": "GR-1-p2", "move-from": "GR-1-p1"}, {"rule-id": "GR-1-p3", "move-from": "GR-1-p2"}
    for path, rec in ((first, ab), (second, bc)):
        path.write_text(json.dumps({"batch": path.stem, "records": [rec]}), encoding="utf-8", newline="\n")
    folder = repo / M.MOVES_DIR.format(year=1985)
    folder.mkdir(parents=True)
    for path, rec in ((first, ab), (second, bc)):          # two runs of the writer, in this order
        files, errs = M.tracked_copies([(path, [rec])], 1985, repo, lambda rid: ["s-18"] if rid == "GR-1-p1" else [])
        expect.equal(f"{path.name}: one copy, no refusal", (len(files), errs), (1, []))
        for dest, text in files.items():
            dest.write_text(text, encoding="utf-8", newline="\n")
    expect.equal("each copy carries its place in the application order",
                 [json.loads((folder / n).read_text(encoding="utf-8"))[M.APPLIED] for n in ("z-first.json", "a-second.json")],
                 [1, 2])
    expect.equal("recorded_moves follows them in that order, not the files' name order",
                 M.recorded_moves(1985, repo),
                 [[("GR-1-p1", "GR-1-p2", frozenset({"s-18"}))], [("GR-1-p2", "GR-1-p3", frozenset())]])
    again, _ = M.tracked_copies([(second, [bc])], 1985, repo, lambda rid: [])
    expect.equal("a batch applied again keeps its place", json.loads(next(iter(again.values())))[M.APPLIED], 2)
    (folder / "b-hand.json").write_text(json.dumps({"batch": "hand", "records": [ab]}), encoding="utf-8", newline="\n")
    try:
        M.recorded_moves(1985, repo)
        expect("a copy with no place raises", False)
    except ValueError as e:
        expect("a copy with no place raises", M.APPLIED in str(e))
    (folder / "b-hand.json").write_text(json.dumps({"batch": "hand", M.APPLIED: 2, "records": [ab]}),
                                        encoding="utf-8", newline="\n")
    try:
        M.recorded_moves(1985, repo)
        expect("two copies at one place raise", False)
    except ValueError as e:
        expect("two copies at one place raise", "another copy's place" in str(e))

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
