#!/usr/bin/env python3
"""The rule stage's screen on a planted pair of editions (kb/Work PB5589; DESIGN-spec-conformance-review.md section 10.5): the pending-clause diff and its marks, each text mechanism, the regression fixture, the KEPT writer and its refusals, the view triggers and the committed document."""
import sys

import fixtures as F
import selftest_cases
from standard_streams import ensure_utf8

L = selftest_cases.subject(__file__)   # scripts/spec/lineage_screen.py
import audit_batch  # noqa: E402  (the one writer of an older edition's change records; its directory is the subject's)




def case_00_the_pending_clause_diff_marks_the_dependents_of_a_changed_clause(expect):
    s = F.planted()
    marks = L.text_marks(s)
    ok = lambda cond, what: expect(what, bool(cond))  # noqa: E731
    ok(s.diff["8.8"]["class"] == L.CHANGED and s.diff["8.9"]["class"] == L.IDENTICAL
       and s.diff["8.8"]["differs"] == [{"older": "truncated", "newer": "exact"}, {"older": "32", "newer": "34"}],
       "the pending diff: a clause whose words changed is L.CHANGED with its differing words, an unchanged one IDENTICAL")
    ok(set(marks["pending-clause"]) == {"2002-GR-9.1.1.3-1", "2002-GR-9.1.1.3-2"},
       "a changed pending clause marks its dependents, by containment (9.1) and by invocation (standard arithmetic)")
    unchanged = F.planted()
    unchanged.clauses(2023).own["8.8"] = unchanged.clauses(2002).own["8.8"]
    unchanged.table["pending"] = {}
    ok(L.text_marks(unchanged)["pending-clause"] == {} and L.table_errors(unchanged) == [],
       "an unchanged pending clause marks nothing and needs no judgment")
    unjudged = F.planted()
    unjudged.table["pending"] = {}
    ok(any("does not judge what it governs" in e for e in L.table_errors(unjudged)),
       "a changed pending clause the table does not judge is a drift finding, by name")
    stale = F.planted()
    stale.table["pending"]["8.9"] = {"governs": [], "invokes": [], "why": "planted"}
    ok(any("identical" in e for e in L.table_errors(stale)), "a judgment of an identical clause is a drift finding")


def case_01_each_text_mechanism_marks_its_planted_pair(expect):
    s = F.planted()
    marks = L.text_marks(s)
    ok = lambda cond, what: expect(what, bool(cond))  # noqa: E731
    ok(marks["definition-dropped"] == {"2002-GR-9.3.1.3-1": "alphabet name (newer: undefined)"},
       "the widened definition flag: a term the older text defines and the newer does not marks its user")
    ok(set(marks["cross-reference"]) == {"2002-GR-9.2.1.3-1"} and set(marks["normalized"]) == {"2002-GR-9.1.1.3-2"},
       "a text pattern and a pair's match each mark their planted row")
    ok(set(marks["band-formats"]) == {"2002-GR-9.2.1.3-1"}, "a pair whose band-formats differ is marked")
    ok(set(marks["clause-identity"]) == {"2002-GR-9.4.1.3-1", "2002-GR-9.4.1.3-2"}
       and "1 row(s)" in marks["clause-identity"]["2002-GR-9.4.1.3-1"],
       "whole-clause identity: a clause whose newer twin has a row no older rule pairs with marks every rule of it")
    ok(set(marks["behaviour-switch"]) == {"2002-GR-9.4.1.3-1", "2002-GR-9.4.1.3-2"},
       "a behaviour that changed after 2002 marks the rules of its clause; one that changed in 2002 marks nothing")
    nob = F.planted()
    nob.table["behaviours"] = {}
    ok(any("names no clause" in e for e in L.table_errors(nob)),
       "a dialect behaviour of the window with no 2002 clause is a drift finding (the next one is automatic)")
    ok(marks["held"] == {"2002-GR-9.4.1.3-2": "a planted hold"}, "a listed hold marks its row with its reason")
    ok(L.empty_slots(s.table) == ["removed-context"], "a trigger with no mechanism is a named empty slot")


def case_02_the_regression_fixture_fails_when_a_different_pair_goes_unmarked(expect):
    s = F.planted()
    marks = L.text_marks(s)
    ok = lambda cond, what: expect(what, bool(cond))  # noqa: E731
    fixture = F.planted()
    fixture.labels = {"2002-GR-9.2.1.3-1": "DIFFERENT", "2002-GR-9.1.1.3-1": "SAME"}
    ok(L.table_errors(fixture) == [] and L.label_findings(fixture, L.text_marks(fixture))["same-marked"]
       == {"2002-GR-9.1.1.3-1": ["pending-clause"]}, "the regression fixture: a DIFFERENT pair a trigger marks passes, "
       "and a SAME pair a trigger marks is listed as the screen's cost")
    lost = F.planted()
    lost.labels = {"2002-GR-9.2.1.3-1": "DIFFERENT"}
    lost.table["triggers"] = [t for t in lost.table["triggers"] if t["name"] not in ("band-formats", "cross-reference")]
    ok(any("regression fixture" in e and "2002-GR-9.2.1.3-1" in e for e in L.table_errors(lost)),
       "the regression fixture: dropping the L.triggers that catch a DIFFERENT pair fails --check, naming the pair")
    measured = F.planted()
    measured.labels = {"2002-GR-9.2.1.3-1": "DIFFERENT"}
    measured.table["triggers"] = [dict(t, excludes=False) if t["name"] in ("band-formats", "cross-reference") else t
                                  for t in measured.table["triggers"]]
    ok(any("regression fixture" in e for e in L.table_errors(measured))
       and "2002-GR-9.2.1.3-1" in L.screen_document(measured)["measured"]
       and "2002-GR-9.2.1.3-1" not in L.screen_document(measured)["marks"],
       "a measured-only trigger (excludes: false) is committed under `measured`, bars nothing and catches no fixture row")


def case_03_the_writer_refuses_every_marked_pair_and_writes_the_unmarked_one(expect):
    s = F.planted()
    marks = L.text_marks(s)
    ok = lambda cond, what: expect(what, bool(cond))  # noqa: E731
    # THE KEPT WRITER: the rule stage over the planted pairs, then the one writer of change records (audit_batch).
    writer = F.planted()
    try:
        L.rule_stage(writer, {})
        ok(False, "the rule stage refuses to write while the table has an empty slot")
    except SystemExit:
        ok(True, "the rule stage refuses to write while the table has an empty slot")
    writer.table["triggers"] = [x for x in writer.table["triggers"] if x["mechanism"] is not None]
    writer.table["triggers"].append({"name": "target-state", "mechanism": "target-state", "at": "view",
                                     "params": {"accept": ["CONFORMS"]}, "reason": "planted view reason"})
    ok_target = {"verdict": "CONFORMS", "state": "OK", "editions": "2002,2014,2023"}
    disp, excluded = L.rule_stage(writer, {n: dict(ok_target, **{"rule-id": n}) for n in writer.newer_rules})
    recs = audit_batch.change_records(L.edition(2002), disp, L.RULE_STAGE_SOURCE)
    ok([r["rule-id"] for r in recs] == ["2002-GR-9.5.1.3-1"]
       and recs[0]["next"]["kind"] == "KEPT" and recs[0]["next"]["to"] == ["GR-9.5.1.3-1"]
       and recs[0]["next"]["source"] == L.RULE_STAGE_SOURCE and recs[0]["next"]["edition"] == 2023,
       "the writer writes a KEPT record for the one pair no trigger marks, into its target, from the rule stage")
    ok("2002-GR-9.2.1.3-1" in excluded and excluded["2002-GR-9.2.1.3-1"]["band-formats"].startswith("planted (")
       and len(excluded) == len(writer.pairs) - 1,
       "the writer refuses every marked pair and lists it with each trigger's reason and detail")
    _, refused = L.rule_stage(writer, {n: dict(ok_target, **{"rule-id": n}, verdict="PARTIAL") for n in writer.newer_rules})
    ok("target-state" in refused["2002-GR-9.5.1.3-1"], "a view trigger refuses a pair whose target is not CONFORMS")
    ok(L.rule_stage_refusals([{"rule-id": "2002-GR-9.2.1.3-1", "next": {"kind": "KEPT", "source": L.RULE_STAGE_SOURCE}},
                                    {"rule-id": "2002-GR-9.5.1.3-1", "next": {"kind": "KEPT", "source": L.RULE_STAGE_SOURCE}},
                                    {"rule-id": "2002-GR-9.2.1.3-1", "next": {"kind": "KEPT", "source": "an adjudication"}}],
                                   L.screen_document(writer)) == [
           "2002-GR-9.2.1.3-1: a rule-stage KEPT record on a rule the screen marks (cross-reference: 8.4.2; band-formats: "
           "band-formats [1, 2] against [[1]]): withdraw it; the pair is judged with what marks it"],
       "the LINEAGE SCREEN audit refuses a stored rule-stage record on a rule the committed screen marks, and no other")


def case_04_view_triggers_unknown_mechanisms_and_the_committed_document(expect):
    s = F.planted()
    marks = L.text_marks(s)
    ok = lambda cond, what: expect(what, bool(cond))  # noqa: E731
    ok(L.view_marks({"mechanism": "target-state", "params": {"accept": ["CONFORMS"]}}, s.pairs[:2], 2002, "2002",
                  {"GR-9.1.1.3-1": {"verdict": "CONFORMS", "state": "OK"}, "GR-9.1.1.3-2": {"verdict": "", "state": "GAP"}})
       == {"2002-GR-9.1.1.3-2": "GR-9.1.1.3-2 -/GAP"}
       and L.view_marks({"mechanism": "target-band"}, s.pairs[:1], 2002, "2002",
                      {"GR-9.1.1.3-1": {"editions": "2014,2023"}}) == {"2002-GR-9.1.1.3-1": "GR-9.1.1.3-1 band 2014,2023"},
       "the view triggers: a target not CONFORMS and OK, and a band that does not name 2002, each mark the pair")
    try:
        L.triggers({"triggers": [{"name": "x", "mechanism": "nonesuch", "reason": "r"}]})
        ok(False, "a trigger naming an unknown mechanism is refused")
    except SystemExit:
        ok(True, "a trigger naming an unknown mechanism is refused")
    doc = L.screen_document(s)
    ok(doc["counts"]["pending"] == {"identical": 1, "near": 0, "changed": 1, "removed": 0}
       and doc["marks"]["2002-GR-9.4.1.3-2"].keys() == {"clause-identity", "behaviour-switch", "held"},
       "the screen file holds each mark by rule and trigger, and the pending classes")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
