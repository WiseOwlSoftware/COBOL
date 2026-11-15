#!/usr/bin/env python3
"""The lineage screen on a planted pair of editions (kb/Work PB5589; DESIGN-spec-conformance-review.md section 10.5): the
pending-clause diff and its flags (a judged clause by its judgment, an unjudged one fail-closed), each mechanism on its
planted pair, an unplaced behaviour, the regression fixture's two expectations, a record's answers to the flags, and the
committed document."""
import re
import sys

import fixtures as F
import selftest_cases
from standard_streams import ensure_utf8

L = selftest_cases.subject(__file__)   # scripts/spec/lineage_screen.py


def case_00_the_pending_clause_diff_flags_the_dependents_of_a_changed_clause(expect):
    s = F.planted()
    marks = L.text_marks(s)
    expect("a clause whose words changed is CHANGED with its differing words, an unchanged one IDENTICAL",
           s.diff["8.8"]["class"] == L.CHANGED and s.diff["8.9"]["class"] == L.IDENTICAL
           and s.diff["8.8"]["differs"] == [{"older": "truncated", "newer": "exact"}, {"older": "32", "newer": "34"}])
    expect.equal("a judged changed clause flags its dependents by containment (9.1) and by invocation",
                 {r for r, d in marks["pending-clause"].items() if "8.8" in d}, {"2002-GR-9.1.1.3-1", "2002-GR-9.1.1.3-2"})
    unchanged = F.planted()
    for y in (2002, 2023):
        unchanged.clauses(y).own["8.8"] = unchanged.clauses(2002).own["8.8"]
        unchanged.clauses(y).own["8.7"] = unchanged.clauses(2002).own["8.7"]
    unchanged.table["pending"] = {}
    expect("an unchanged pending clause flags nothing and is owed nothing",
           L.text_marks(unchanged)["pending-clause"] == {} and L.undecided(unchanged) == [])
    stale = F.planted()
    stale.table["pending"]["8.9"] = {"governs": [], "invokes": [], "why": "planted"}
    expect("a judgment of an identical clause is a drift finding", any("identical" in e for e in L.table_errors(stale)))


def case_01_an_unjudged_changed_clause_fails_closed(expect):
    s = F.planted()
    marks = L.text_marks(s)["pending-clause"]
    expect.equal("UNKNOWN FAILS CLOSED: the unjudged changed clause 8.7 flags every pair under it and every pair naming "
                 "its title", {r for r, d in marks.items() if "8.7" in d}, {"2002-SR-8.7.1.2-1", "2002-GR-9.10.1.3-1"})
    expect("its flag says it is undecided", "undecided" in marks["2002-SR-8.7.1.2-1"])
    expect("it is owed (printed by --check), not a table error (a red gate)",
           any("8.7" in o and "no judgment" in o for o in L.undecided(s)) and L.table_errors(s) == [])
    judged = F.planted()
    judged.table["pending"]["8.7"] = {"governs": [], "invokes": [], "why": "planted: governs nothing"}
    expect("a judgment that the clause governs nothing is a decision: it flags nothing and is owed nothing",
           not any("8.7" in d for d in L.text_marks(judged)["pending-clause"].values())
           and not any("8.7" in o for o in L.undecided(judged)))


def case_02_each_mechanism_flags_its_planted_pair(expect):
    s = F.planted()
    marks = L.text_marks(s)
    expect.equal("definitions dropped: a term 2002 defines and the other edition does not", marks["definition-dropped"],
                 {"2002-GR-9.3.1.3-1": "alphabet name (newer: undefined)"})
    expect.equal("definitions changed: a term whose definition words differ", set(marks["definition-changed"]),
                 {"2002-GR-9.4.1.3-1", "2002-GR-9.4.1.3-2"})
    expect.equal("a text pattern", set(marks["cross-reference"]), {"2002-GR-9.2.1.3-1"})
    expect.equal("a pair's match", set(marks["normalized"]), {"2002-GR-9.1.1.3-2"})
    expect.equal("a pair with two targets", set(marks["ambiguous"]), {"2002-GR-9.9.1.3-1"})
    expect.equal("a rule whose whole text is absent from the 2014 text", set(marks["absent-from-2014"]),
                 {"2002-GR-9.3.1.3-1"})
    expect.equal("band-formats that differ", set(marks["band-formats"]), {"2002-GR-9.2.1.3-1"})
    expect.equal("a behaviour that differs between 2002 and 2023 flags its clause; one in force at both flags nothing",
                 set(marks["behaviour-switch"]), {"2002-GR-9.4.1.3-1", "2002-GR-9.4.1.3-2"})
    expect.equal("a listed hold", marks["held"], {"2002-GR-9.4.1.3-2": "a planted hold"})
    expect.equal("construct reach: a rule of a reached clause, and of a clause the context's formats name; a rule its "
                 "text confines elsewhere is not flagged", set(marks["communication-reach"]),
                 {"2002-GR-9.6.1.3-1", "2002-GR-9.8.1.3-1"})
    expect("whole-clause identity: a clause whose newer twin has a row no older rule pairs with marks every rule of it",
           {"2002-GR-9.4.1.3-1", "2002-GR-9.4.1.3-2"} <= set(marks["clause-identity"]))
    try:
        L.triggers({"triggers": [{"name": "x", "mechanism": None, "reason": "r"}]})
        expect("a trigger naming no mechanism, or an unknown one, is refused", False)
    except SystemExit:
        expect("a trigger naming no mechanism, or an unknown one, is refused", True)


def case_03_an_unplaced_behaviour_flags_every_pair(expect):
    s = F.planted()
    s.table["behaviours"] = {}
    flagged = L.text_marks(s)["behaviour-switch"]
    expect("a behaviour that differs and is placed in no clause flags every pair, and says so",
           set(flagged) == {p["rule-id"] for p in s.pairs} and all("unplaced" in d for d in flagged.values()))
    expect("it is owed, not a table error", any("b-2023" in o for o in L.undecided(s)) and L.table_errors(s) == [])
    placed = F.planted()
    placed.table["behaviours"]["b-2002"] = {"sections": ["9.5"], "why": "planted"}
    expect("placing a behaviour in force at both editions is a drift finding",
           any("b-2002" in e for e in L.table_errors(placed)))


def case_04_the_regression_fixture_pins_what_the_screen_catches(expect):
    s = F.planted()
    s.labels = [{"rule-id": "2002-GR-9.2.1.3-1", "verdict": "DIFFERENT", "expect": L.CAUGHT},
                {"rule-id": "2002-GR-9.1.1.3-1", "verdict": "SAME", "expect": L.CAUGHT},
                {"rule-id": "2002-GR-9.5.1.3-1", "verdict": "DIFFERENT", "expect": L.KNOWN_MISSES},
                {"rule-id": "2002-GR-9.6.1.3-1", "verdict": "DOUBTFUL", "expect": L.KNOWN_MISSES}]
    found = L.label_findings(s, L.text_marks(s))
    expect("a caught DIFFERENT pair a trigger flags passes; a known miss unflagged is no error",
           L.table_errors(s) == [] and found["different-unflagged"] == [])
    expect.equal("the known misses the triggers flag are counted (a DOUBTFUL counts with the DIFFERENT)",
                 found["known-misses-flagged"], "1 of 2")
    expect.equal("a SAME pair a trigger flags is listed as the screen's cost", found["same-flagged"], ["2002-GR-9.1.1.3-1"])
    lost = F.planted()
    lost.labels = s.labels
    lost.table["triggers"] = [t for t in lost.table["triggers"] if t["name"] not in ("band-formats", "cross-reference")]
    expect("dropping the triggers that catch a caught DIFFERENT pair fails --check, naming the pair",
           any("regression fixture" in e and "2002-GR-9.2.1.3-1" in e for e in L.table_errors(lost)))
    measured = F.planted()
    measured.labels = s.labels
    measured.table["triggers"] = [dict(t, **{"measured-only": True}) if t["name"] in ("band-formats", "cross-reference")
                                  else t for t in measured.table["triggers"]]
    doc = L.screen_document(measured)
    expect("a measured-only trigger is committed under `measured`, flags nothing and catches no fixture pair",
           any("regression fixture" in e for e in L.table_errors(measured))
           and "2002-GR-9.2.1.3-1" in doc["measured"] and "2002-GR-9.2.1.3-1" not in doc["marks"])


def case_05_a_record_on_a_flagged_pair_answers_each_flag(expect):
    doc = L.screen_document(F.planted())
    kept = {"kind": "KEPT", "to": ["GR-9.2.1.3-1"], "source": "a read"}
    refused = L.unanswered([{"rule-id": "2002-GR-9.2.1.3-1", "next": kept}], doc)
    expect("a record on a flagged pair whose decision answers no flag is refused, naming each trigger",
           len(refused) == 1 and "band-formats" in refused[0] and "cross-reference" in refused[0])
    flags = doc["marks"]["2002-GR-9.2.1.3-1"]

    def answer(name, detail=None):
        return {"flag": flags[name] if detail is None else detail, "answer": f"the 2002 rule still states it ({name})"}

    both = {"band-formats": answer("band-formats"), "cross-reference": answer("cross-reference")}
    expect("one that answers every flag, each for the flag's detail as the screen gives it, stands",
           L.unanswered([{"rule-id": "2002-GR-9.2.1.3-1", "next": {**kept, "answers": both}}], doc) == [])
    expect("one that answers one flag of two is refused for the other",
           any("cross-reference" in e for e in L.unanswered([{"rule-id": "2002-GR-9.2.1.3-1", "next": {
               **kept, "answers": {"band-formats": answer("band-formats")}}}], doc)))
    moved = {**both, "cross-reference": answer("cross-reference", "a clause the screen named before")}
    refused_moved = L.unanswered([{"rule-id": "2002-GR-9.2.1.3-1", "next": {**kept, "answers": moved}}], doc)
    expect("R2-2: an answer to the same trigger for ANOTHER detail is no answer: a changed flag needs a new one",
           len(refused_moved) == 1 and L.CHANGED_FLAG in refused_moved[0] and "cross-reference" in refused_moved[0])
    expect("a bare sentence (the shape before the answer named its flag) answers nothing",
           len(L.unanswered([{"rule-id": "2002-GR-9.2.1.3-1", "next": {**kept, "answers": {
               "band-formats": "governs format 1", "cross-reference": "the same"}}}], doc)) == 1)
    expect("a record on an unflagged pair needs no answer; a row with no record is no finding",
           L.unanswered([{"rule-id": "2002-GR-9.5.1.3-1", "next": {**kept, "to": ["GR-9.5.1.3-1"]}},
                         {"rule-id": "2002-GR-9.2.1.3-1", "next": {}}], doc) == [])
    stored = [{"rule-id": "2002-GR-9.2.1.3-1", "next": {**kept, "answers": moved}},
              {"rule-id": "2002-GR-9.2.1.3-1", "next": {**kept, "answers": both}}]
    owed = L.owed_records(stored, doc)
    expect("R2-1: a STORED record the screen now asks another question is OWED a re-read (listed), and one that answers "
           "every flag as it reads is not", len(owed) == 1 and "owed a re-read" in owed[0] and "cross-reference" in owed[0])


def case_06_the_committed_document(expect):
    doc = L.screen_document(F.planted())
    expect.equal("the pending classes", doc["counts"]["pending"], {"identical": 1, "near": 0, "changed": 2, "removed": 0})
    expect.equal("each flag by rule and trigger", set(doc["marks"]["2002-GR-9.4.1.3-2"]),
                 {"definition-changed", "behaviour-switch", "held"})
    expect("what the table owes", any("8.7" in o for o in doc["owed"]))


def case_07_a_pattern_holding_a_control_character_is_refused_at_load(expect):
    planted = {"triggers": [{"name": "reach", "mechanism": "construct-reach", "reason": "r",
                             "params": {"confined": "\b(?:only) in the file section", "clauses": ["13.14"]}}]}
    try:
        L.triggers(planted)
        expect("R2-3: a parameter a JSON \\b decoded to a backspace is refused at load, naming the trigger and the place",
               False)
    except SystemExit as e:
        expect("R2-3: a parameter a JSON \\b decoded to a backspace is refused at load, naming the trigger and the place",
               "reach" in str(e) and "params.confined" in str(e) and "U+0008" in str(e))
    live = {t["name"]: t for t in L.load_table(2002)["triggers"]}
    confined = re.compile(live["communication-reach"]["params"]["confined"], re.I)
    expect("the live 2002 table's communication-reach confinement matches a rule its text confines to another section "
           "(it never matched while the JSON held \\b, a backspace)",
           confined.search("The clause shall be specified only in the file section.") is not None
           and confined.search("The clause may be specified in any entry.") is None)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
