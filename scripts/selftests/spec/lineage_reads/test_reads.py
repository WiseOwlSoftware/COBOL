#!/usr/bin/env python3
"""The read records of an older edition's rules on planted reads (kb/Work PB5597; DESIGN-spec-conformance-review.md
section 10.6): the one normalization and its loud refusals, what a read becomes (a kept record only from a SAME its
refuter upheld, every screen flag answered for its detail, the pairing's targets, a witness at the row's own edition of
the kind the rule needs; a work item for every other verdict), the import's durable records, the audit of stored
records, and the band widening's plan, edits and transaction."""
import dataclasses
import json
import pathlib
import shutil
import subprocess
import sys

import fixtures as F
import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

R = selftest_cases.subject(__file__)   # scripts/spec/lineage_reads.py
ED = R.edition(2002)
SCHEMA = R.inventory_schema.load_schema()


def _normal_read(slice_name, case):
    """A planted 2002 read case, normalized at its record's edition."""
    return R.normal_read(slice_name, case, ED.year)


def case_00_the_one_normalization_reads_every_shape_and_refuses_what_it_cannot(expect):
    r = _normal_read("q-01", F.read("2002-GR-1.1.3-1", citations="§2002:1.1 GR1",
                                     **{"flags-answered": ["cross-reference 8.8.4: read in 2002, the same",
                                                           "changed-definition-term 'procedure': the division, not the term",
                                                           "changed-definition-term (class): the same sense"],
                                        "compiler-at-2002": "correct (probe p.cob)", "licence-2002": "§2002:B.3 item 1"}))
    expect.equal("a list of flags-answered lines: each trigger with the detail its head names, several per trigger",
                 r.answers, {"cross-reference": [("8.8.4", "read in 2002, the same")],
                             "changed-definition-term": [("procedure", "the division, not the term"),
                                                         ("class", "the same sense")]})
    expect("a string citation, the compiler's judgment and the licence are read",
           r.compiler == R.Judgment("correct", "correct (probe p.cob)") and r.licence == "§2002:B.3 item 1")
    expect("a licence is kept as written; the brief's `none` is no licence (licence_problem)",
           R.licence_problem(ED, _normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"licence-2002": "none"})).licence))
    m = _normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"flags-answered": {"normalized: a b": "x", "ambiguous": "y"}}))
    expect.equal("a map of flags-answered: its key's detail, or none", m.answers,
                 {"normalized": [("a b", "x")], "ambiguous": [(None, "y")]})
    expect("R1-4: an empty `to` (a rule with no pointer) is read", _normal_read("t-annexA", {**F.read("2002-GR-A.1-1"),
                                                                                              "to": []}).to == ())
    odd = _normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"compiler-at-2002": "outcome correct but on another ground"}))
    expect("a judgment naming none of correct, wrong, not probed is kept as written, its kind None",
           odd.compiler.kind is None)
    for label, case, needle in (
            ("a verdict outside the brief's", F.read("2002-GR-1.1.3-1", verdict="MAYBE"), "verdict"),
            ("a `to` that is no list of ids", {**F.read("2002-GR-1.1.3-1"), "to": "GR-1"}, "`to`"),
            ("a citations field of another type", {**F.read("2002-GR-1.1.3-1"), "citations": 7}, "citations"),
            ("a flags-answered line with no answer", F.read("2002-GR-1.1.3-1", **{"flags-answered": ["ambiguous"]}),
             "flags-answered"),
            ("one trigger and detail answered twice", F.read("2002-GR-1.1.3-1", **{"flags-answered": ["a: x", "a: y"]}),
             "twice")):
        expect(f"MALFORMED, refused loudly by slice and rule: {label}",
               F.raises(lambda: _normal_read("q-07", case), "q-07", "2002-GR-1.1.3-1", needle))
    expect("a refute of a rule the reader did not decide SAME is refused",
           F.raises(lambda: R.normal_slice("p-02", F.record("p-02", [F.read("2002-GR-1.1.3-1", "DIFFERENT")],
                                                            [F.refute("2002-GR-1.1.3-1")])), "p-02", "did not decide SAME"))
    expect("a rule the reader decides twice is refused",
           F.raises(lambda: R.normal_slice("p-02", F.record("p-02", [F.read("2002-GR-1.1.3-1")] * 2)), "twice"))


def case_01_what_a_read_becomes(expect):
    ids = [f"2002-GR-1.1.3-{n}" for n in range(1, 11)]
    s = R.normal_slice("p-01", F.record("p-01", [
        F.read(ids[0]),                                                          # SAME, upheld, unflagged: KEPT
        F.read(ids[1]),                                                          # SAME, no refuter record
        F.read(ids[2]),                                                          # SAME, OVERTURNED, compiler correct
        F.read(ids[3], "DIFFERENT", **{"compiler-at-2002": "wrong: p.cob prints 34 digits"}),
        F.read(ids[4], "DOUBTFUL"),
        F.read(ids[5], "DIFFERENT", **{"compiler-at-2002": "outcome correct on another ground"}),
        F.read(ids[6]),                                                          # SAME, flagged, unanswered
        F.read(ids[7], **{"flags-answered": ["cross-reference: 8.4.2 says the same"]}),   # answered as asked
        F.read(ids[8], **{"flags-answered": ["cross-reference 9.9: another clause"]}),    # another detail
        F.read(ids[9]),                                                          # SAME, upheld, no witness at 2002
    ], [F.refute(ids[n]) for n in (0, 6, 7, 8, 9)]
       + [F.refute(ids[2], "OVERTURNED", probe="cd.cob", **{"compiler-at-2002": "correct"})],
        asked={ids[7]: {"cross-reference": "8.4.2"}}))
    screen = {"marks": {ids[6]: {"band-formats": "[1, 2] against [1]"}, ids[7]: {"cross-reference": "8.4.2"},
                        ids[8]: {"cross-reference": "8.4.2"}}}
    w = F.witnesses({F.twin(r): ["2002", "2023"] for r in ids[:9]} | {F.twin(ids[9]): ["2023"]})
    got = {d.rule_id.rsplit("-", 1)[1]: d for d in R.decide(ED, [s], screen, w, F.lineage(ids))}
    expect("a SAME its refuter UPHELD, unflagged, its pairing's targets witnessed at 2002 is KEPT, sourced to its record",
           got["1"].kept and got["1"].source == "lineage-reads-2002/p-01.json")
    expect.equal("a SAME with NO REFUTER RECORD is refused", got["2"].refused, ((R.NO_REFUTER, "no refuter has attacked the SAME"),))
    expect("an OVERTURNED SAME whose refuter showed the compiler correct is needs-own-witness, with the refuter's probe",
           got["3"].work == R.NEEDS_OWN_WITNESS and got["3"].probe == "cd.cob" and not got["3"].kept)
    expect.equal("a DIFFERENT whose reader showed the compiler wrong is a defect", got["4"].work, R.DEFECT)
    expect.equal("a DOUBTFUL read is undecided", got["5"].work, R.UNDECIDED)
    expect.equal("a judgment no reading interprets is listed as such, never defaulted", got["6"].work, R.UNINTERPRETABLE)
    expect("an UNANSWERED FLAG refuses the row, naming the trigger and its detail",
           got["7"].refused == ((R.FLAG_UNANSWERED, "band-formats: [1, 2] against [1]"),))
    expect.equal("an answer naming no detail answers the flag the slice asked, keyed by that detail", got["8"].answers,
                 {"cross-reference": {"flag": "8.4.2", "answer": "8.4.2 says the same"}})
    expect("an answer for ANOTHER detail of the trigger is no answer", [r for r, _ in got["9"].refused] == [R.FLAG_UNANSWERED])
    expect("a witness whose band lacks the edition refuses the row: no own-edition evidence",
           [r for r, _ in got["10"].refused] == [R.NO_OWN_WITNESS] and "GR-1.1.3-10" in got["10"].refused[0][1])
    sm = R.summary(list(got.values()))
    expect.equal("the summary counts kept, refused by reason, and work by kind",
                 (sm["kept"], sm["refused-by-reason"], sm["work"]),
                 (2, {R.FLAG_UNANSWERED: 2, R.NO_OWN_WITNESS: 1, R.NO_REFUTER: 1},
                  {R.DEFECT: 1, R.NEEDS_OWN_WITNESS: 1, R.UNDECIDED: 1, R.UNINTERPRETABLE: 1}))


def case_02_the_evidence_is_of_the_kind_the_rule_needs(expect):
    sr, gr, dns, dns_bare, acc = "2002-SR-2.1.2-1", "2002-GR-2.1.3-1", "2002-GR-2.1.3-2", "2002-GR-2.1.3-3", "2002-GR-2.1.3-4"
    ids = [sr, gr, dns, dns_bare, acc]
    s = R.normal_slice("p-01", F.record("p-01", [F.read(sr), F.read(gr), F.read(dns, **{"licence-2002": "§2002:B.3 item 1"}),
                                                  F.read(dns_bare, **{"licence-2002": "none"}), F.read(acc)],
                                        [F.refute(r) for r in ids]))
    w = F.witnesses(accepted={F.twin(acc): ["2002"]},
                    diagnosed={F.twin(r): ["2002"] for r in (sr, gr, dns, dns_bare)},
                    verdicts={F.twin(dns): "DOCUMENTED-NON-SUPPORT", F.twin(dns_bare): "DOCUMENTED-NON-SUPPORT"})
    got = {d.rule_id: d for d in R.decide(ED, [s], None, w, F.lineage(ids))}
    expect("R1-1: a refusal at the edition witnesses a SYNTAX rule (the catalog's kind is one the table names)", got[sr].kept)
    expect("R1-1: a refusal at the edition is NO witness of a general rule: it needs a case that runs and passes there",
           [r for r, _ in got[gr].refused] == [R.NO_OWN_WITNESS] and "only a diagnosis" in got[gr].refused[0][1])
    expect("R1-1: a refusal witnesses a DECLINED facility (DOCUMENTED-NON-SUPPORT) when the read states the 2002 licence",
           got[dns].kept)
    expect("R1-1: and without a stated licence the row is refused as having none",
           [r for r, _ in got[dns_bare].refused] == [R.NO_LICENCE])
    expect("a case accepted at the edition witnesses a general rule", got[acc].kept)
    as_gr = {d.rule_id: d for d in R.decide(ED, [s], None, w, F.lineage(ids, kinds={sr: "GR"}))}
    expect("the kind is the catalog's `kind` field, not the id: the same rule catalogued GR is refused",
           not as_gr[sr].kept)
    expect("the evidence table refuses a kind the catalog does not have",
           F.raises(lambda: R.EvidenceTable.load(kinds=["GR"]), "SR"))


def case_03_the_pairing_decides_which_rows_may_be_kept(expect):
    paired, wrong, pointer, empty = "2002-GR-3.1.3-1", "2002-GR-3.1.3-2", "2002-GR-3.1.3-3", "2002-GR-3.1.3-4"
    s = R.normal_slice("t-near", F.record("t-near", [
        F.read(paired), {**F.read(wrong), "to": ["GR-9.9.9-9"]}, F.read(pointer), {**F.read(empty, "DIFFERENT",
                                                                                            **{"compiler-at-2002": "correct"}),
                                                                                    "to": []}],
        [F.refute(paired), F.refute(wrong), F.refute(pointer)]))
    w = F.witnesses({F.twin(r): ["2002"] for r in (paired, wrong, pointer)} | {"GR-9.9.9-9": ["2002"]})
    got = {d.rule_id: d for d in R.decide(ED, [s], None, w, F.lineage([paired, wrong, pointer, empty],
                                                                       unpaired=(pointer, empty)))}
    expect("a read into exactly the pairing's targets is KEPT", got[paired].kept)
    expect("R1-2: a read into targets the pairing does not name is refused by name, never KEPT",
           [r for r, _ in got[wrong].refused] == [R.NOT_THE_PAIRING] and "GR-9.9.9-9" in got[wrong].refused[0][1])
    expect("R1-2: a SAME of a rule with NO candidate pair (a pointer) is the own-verdict-from-pointer work item",
           got[pointer].work == R.OWN_VERDICT_FROM_POINTER and not got[pointer].kept)
    expect("a DIFFERENT of an unpaired rule with an empty `to` is the usual work item", got[empty].work == R.NEEDS_OWN_WITNESS)


def case_04_the_import_writes_the_durable_records_verbatim_or_nothing(expect):
    d = scratch_dir("lineage-reads")
    src, asked, out = d / "out", d / "in", d / "records"
    src.mkdir()
    asked.mkdir()
    (src / "p-01-read.json").write_text(json.dumps({"slice": "p-01", "cases": [F.read("2002-GR-1.1.3-1")]}),
                                        encoding="utf-8", newline="\n")
    (src / "p-01-refute.json").write_text(json.dumps({"slice": "p-01", "cases": [F.refute("2002-GR-1.1.3-1")],
                                                      "disputed-different": []}), encoding="utf-8", newline="\n")
    (src / "t-annexA-read.json").write_text(json.dumps({"slice": "t-annexA", "cases": [
        {**F.read("2002-GR-1.1.3-2", "DIFFERENT"), "to": []}]}), encoding="utf-8", newline="\n")
    (asked / "t-annexA.json").write_text(json.dumps({"ids": ["2002-GR-1.1.3-2"], "flags": {}}), encoding="utf-8",
                                         newline="\n")
    expect.equal("R1-4: every slice that exists is imported, whatever its label (t-annexA), an empty `to` and a missing "
                 "refuter record included", R.import_records(2002, src, asked, out), ["p-01", "t-annexA"])
    doc = json.loads((out / "p-01.json").read_text(encoding="utf-8"))
    expect("the record holds the sources verbatim with each one's content digest",
           doc["read"]["cases"][0]["rule-id"] == "2002-GR-1.1.3-1" and set(doc["imported"]) == {"read", "refute"}
           and doc["imported"]["read"] == R.digest(json.loads((src / "p-01-read.json").read_text(encoding="utf-8"))))
    expect.equal("an import of unchanged sources rewrites nothing", R.import_records(2002, src, asked, out), [])
    expect.equal("load normalizes every record", sorted(s.name for s in R.load(2002, out)), ["p-01", "t-annexA"])
    before = {p.name: p.read_text(encoding="utf-8") for p in out.iterdir()}
    (src / "q-02-read.json").write_text(json.dumps({"slice": "q-02", "cases": [F.read("2002-GR-1.1.3-3", "NEW")]}),
                                        encoding="utf-8", newline="\n")
    expect("a malformed slice refuses the whole import, naming slice and rule, and writes nothing",
           F.raises(lambda: R.import_records(2002, src, asked, out), "q-02", "2002-GR-1.1.3-3")
           and {p.name: p.read_text(encoding="utf-8") for p in out.iterdir()} == before)
    (src / "q-02-read.json").write_text(json.dumps({"slice": "q-02", "cases": [F.read("2002-GR-9.9.9-9")]}),
                                        encoding="utf-8", newline="\n")
    expect("a read of a rule the edition's catalog does not hold refuses the import, and writes nothing",
           F.raises(lambda: R.import_records(2002, src, asked, out, {"2002-GR-1.1.3-1", "2002-GR-1.1.3-2"}),
                    "q-02", "2002-GR-9.9.9-9", "no rule of the 2002 catalog")
           and {p.name: p.read_text(encoding="utf-8") for p in out.iterdir()} == before)
    (src / "q-02-read.json").write_text(json.dumps({"slice": "q-02", "cases": [F.read("2002-GR-1.1.3-1")]}),
                                        encoding="utf-8", newline="\n")
    expect("R1-5: a rule two slices read is refused BEFORE any write",
           F.raises(lambda: R.import_records(2002, src, asked, out), "q-02", "read again")
           and {p.name: p.read_text(encoding="utf-8") for p in out.iterdir()} == before)
    (src / "q-02-read.json").unlink()
    (src / "q-03-refute.json").write_text("{}", encoding="utf-8", newline="\n")
    expect("a refuter record with no reader's record is refused",
           F.raises(lambda: R.import_records(2002, src, asked, out), "q-03-refute.json"))
    (src / "q-03-refute.json").unlink()
    edited = json.loads((out / "p-01.json").read_text(encoding="utf-8"))
    edited["refute"]["cases"][0]["verdict"] = "OVERTURNED"
    (out / "p-01.json").write_text(json.dumps(edited), encoding="utf-8", newline="\n")
    expect("R1-6: a record edited after its import no longer matches its digest and is refused at load",
           F.raises(lambda: R.load(2002, out), "p-01", "refute no longer matches"))


def case_05_the_audit_holds_a_stored_record_to_its_read(expect):
    ids = ["2002-GR-1.1.3-1", "2002-GR-1.1.3-2", "2002-GR-1.1.3-8", "2002-GR-1.1.3-9"]
    s = R.normal_slice("p-01", F.record("p-01", [F.read(ids[0]), F.read(ids[1])],
                                        [F.refute(ids[0]), F.refute(ids[1], "OVERTURNED")]))

    def stored(rid, to=None, src="lineage-reads-2002/p-01.json", kind="KEPT", own=True):
        t = (to or [F.twin(rid)])[0]
        return {"rule-id": rid, "next": {"kind": kind, "to": to or [F.twin(rid)], "source": src},
                **({"test-ref": f"conformance:cases/X/{t}"} if own else {})}

    lin = F.lineage(ids)
    w = F.witnesses({"GR-1.1.3-1": ["2002", "2023"], "GR-1.1.3-2": ["2002", "2023"], "GR-1.1.3-9": ["2002", "2023"],
                     "GR-1.1.3-8": ["2023"]})
    expect.equal("a stored KEPT record that is its read's decision stands, and one of another source witnessed at 2002",
                 R.stored_errors(ED, [stored(ids[0]), stored(ids[3], src="audit-2002/x")], [s], w, lin), [])
    other = R.stored_errors(ED, [stored(ids[2], src="audit-2002/x")], [s], w, lin)
    expect("a stored KEPT row whose own witness does not run at its edition is red, naming the row and the case",
           len(other) == 1 and R.NO_OWN_WITNESS in other[0] and ids[2] in other[0] and "cases/X/GR-1.1.3-8" in other[0])
    expect.equal("kb/Work PB5675: a stored KEPT row with NO witness of its own is no audit error (its state is GAP)",
                 R.stored_errors(ED, [stored(ids[2], src="audit-2002/x", own=False)], [s], w, lin), [])
    errs = R.stored_errors(ED, [stored(ids[1]), stored("2002-GR-1.1.3-3"), stored(ids[0], to=["GR-9.9.9-9"])], [s], w, lin)
    expect("a record whose read was overturned, whose source holds no read of it, or that carries into other rules is red",
           len(errs) == 3 and "OVERTURNED" in errs[0] and "holds no read" in errs[1] and "carries into" in errs[2])
    narrowed = F.witnesses({"GR-1.1.3-1": ["2023"]})
    lost = R.stored_errors(ED, [stored(ids[0])], [s], narrowed, lin)
    expect("kb/Work PB5675: a LOSS is loud: a witness whose band is later narrowed past the edition turns the stored "
           "record red, naming the row and the case",
           len(lost) == 1 and ids[0] in lost[0] and "cases/X/GR-1.1.3-1" in lost[0] and "does not run at 2002" in lost[0])
    moved = R.stored_errors(ED, [dict(stored(ids[0]), **{"test-ref": "conformance:cases/X/gone"})], [s], w, lin)
    expect("a witness whose case moved or was deleted is red by name", len(moved) == 1 and "names no conformance" in moved[0])
    expect.equal("kb/Work PB5675: the audit of a STORED record asks no pairing: a record into targets the pairing does not "
                 "name, witnessed of its own, stands (the pairing is the writer's test of a NEW record)",
                 R.stored_errors(ED, [stored(ids[3], to=["GR-1.1.3-1"], src="audit-2002/x")], [s], w, lin), [])


def case_05b_one_rule_at_every_older_edition(expect):
    """kb/Work PB5675: the same evidence test at 1985 as at 2002, with no edition key; the writer's refusal at 1985."""
    e85 = R.edition(1985)
    rid, t = "1985-SR-VI.5.8.3-1", "SR-13.18.38.3-1"
    w = R.Witnesses({f"cases/X/{t}": R.CaseInfo(f"cases/X/{t}", R.REPO, ("85", "2023"), ("85", "2023"), (), (t,), None)},
                    {t: [f"conformance:cases/X/{t}"]}, {t: ["85", "2002", "2014", "2023"]})
    lin85 = R.Lineage({rid: "SR"}, {}, R.EvidenceTable.load(), {})
    kept = {"rule-id": rid, "next": {"kind": "KEPT", "to": [t], "source": "audit-1985/x"}}
    expect.equal("a 1985 KEPT row's own witness that runs at 85 stands", R.stored_errors(
        e85, [dict(kept, **{"test-ref": f"conformance:cases/X/{t}"})], [], w, lin85), [])
    narrowed = R.Witnesses({f"cases/X/{t}": R.CaseInfo(f"cases/X/{t}", R.REPO, ("2023",), ("2023",), (), (t,), None)},
                           {t: [f"conformance:cases/X/{t}"]}, {t: ["2023"]})
    expect("and the same loss is loud at 1985 as at 2002 (no edition is exempt)",
           any("does not run at 85" in e for e in R.stored_errors(
               e85, [dict(kept, **{"test-ref": f"conformance:cases/X/{t}"})], [], narrowed, lin85)))
    expect("a unit test is no own witness at an older edition",
           any("runs at no older edition" in e for e in R.stored_errors(
               e85, [dict(kept, **{"test-ref": "unit:T.M"})], [], w, lin85)))
    found, problems = R.kept_witnesses(e85, rid, [t], None, lin85, w)
    expect("the WRITER keeps the whole test at 1985: the 1985 pairing is empty (word-identical text), so a new KEPT record "
           "is refused there, though the evidence part finds its witness",
           [r for r, _ in problems] == [R.NOT_THE_PAIRING] and found == [f"conformance:cases/X/{t}"])
    real = R.Lineage.of(e85)
    expect.equal("the repository's 1985 pairing pairs no rule: every new 1985 KEPT record is refused (1985 rows are "
                 "recorded with their own verdicts)", len(real.pairs), 0)
    paired = R.Lineage({rid: "SR"}, {rid: (t,)}, R.EvidenceTable.load(), {})
    expect.equal("with a pairing, the writer's search at 1985 returns the witness it records as the row's test-ref",
                 R.kept_witnesses(e85, rid, [t], None, paired, w), ([f"conformance:cases/X/{t}"], []))


def case_05c_a_row_closed_on_its_own_derivation_is_planned_no_witness(expect):
    """kb/Work PB5822: the planners' population (`bare_kept`, so `witness-batch` and `widen`) leaves out a KEPT row closed
    on a derivation of its own, as `lineage.unwitnessed` does, and keeps one whose derivation does not stand. The states
    are the state rule's (`lineage.state_of`) on a planted schema whose derivation reads a fabricated register."""
    schema = R.inventory_schema.Schema(json.loads(R.inventory_schema.SCHEMA_PATH.read_text(encoding="utf-8")))
    d, e85, t = schema.derivation, R.edition(1985), "GR-1"
    stands, falls, bare = "1985-GR-XIV.1-1", "1985-GR-XIV.1-2", "1985-GR-XIV.1-3"
    text = "If the operands overlap, the result of the transfer is undefined."
    schema._catalog = {stands: (1985, text), falls: (1985, text), bare: (1985, text), t: (2023, "Other text.")}
    schema._register_items = set()
    d._undefined, d._refuters = {}, {}
    d._register = {f"DRV-{rid}": R.inventory_schema.DerivationRow(f"DRV-{rid}", "undefined-in-rule", f'"{quote}"', "",
                                                                  "claude: 2026-11-12", "—")
                   for rid, quote in ((stands, "the result of the transfer is undefined"),
                                      (falls, "the result of the move is undefined"))}
    targets = {t: {"rule-id": t, "state": "OK", "editions": "85,2002,2014,2023"}}
    rows = [_stored(rid, [t], **({"derivation": d.anchor_template.replace("{rule-id}", rid)} if rid != bare else {}))
            for rid in (stands, falls, bare)]
    for r in rows:
        r["state"] = R.lineage.state_of(r, e85, schema, targets)
    expect.equal("the state rule closes only the row whose own derivation stands",
                 [r["state"] for r in rows], ["OK", "GAP", "GAP"])
    expect.equal("bare_kept leaves out the derivation-closed row and keeps one whose derivation does not stand",
                 [r["rule-id"] for r in R.bare_kept(rows, schema)[1]], [falls, bare])
    expect.equal("... and agrees with lineage.unwitnessed, the views' count of the same population",
                 [r["rule-id"] for r in rows if R.lineage.unwitnessed(r, e85, schema, targets)], [falls, bare])


def case_05d_a_rule_that_withholds_a_property_is_witnessed_by_a_refusal(expect):
    """kb/Work PB5783, option 1: a rule of any kind that WITHHOLDS a property ("does not imply") is witnessed by the
    refusal of a program that relies on it; a granting word in the same rule still sends it to an accepted case."""
    t = R.EvidenceTable.load()
    withholds = "Use of the EXTERNAL clause does not imply that the associated file-name or data-name is a global name."
    expect("a general rule that says a clause 'does not imply' a property is diagnosable", t.diagnosable("GR", withholds))
    expect("... but the same text granting a form ('may be accessed') still needs an accepted case (the heuristic "
           "fails closed and wins)",
           not t.diagnosable("GR", withholds + " The data item may be accessed by any program that describes it."))
    expect("a general rule with neither is not diagnosable",
           not t.diagnosable("GR", "The EXTERNAL clause makes the record available to every program describing it."))
    expect("a syntax rule is unchanged: a prohibition diagnosable, a grant not",
           t.diagnosable("SR", "The clause shall not be specified twice.")
           and not t.diagnosable("SR", "The clause may be specified twice."))
    for y in R.inventory_schema.HELD:
        rules = R.edition(y).rules()
        made = sorted(r["id"] for r in rules if r.get("kind") not in t.diagnosed
                      and t.diagnosable(r.get("kind", ""), r.get("text", "")))
        print(f"    INFO  {y}: the withholding pattern makes diagnosable {made or 'no rule'}")
    expect.equal("on the 1985 catalog it makes diagnosable exactly 1985-GR-X.4.5.4-3 (kb/Work PB5783)",
                 sorted(r["id"] for r in R.edition(1985).rules() if r.get("kind") not in t.diagnosed
                        and t.diagnosable(r.get("kind", ""), r.get("text", ""))), ["1985-GR-X.4.5.4-3"])
    bad = pathlib.Path(scratch_dir("withholding")) / "table.json"
    doc = json.loads(R.EVIDENCE_TABLE.read_text(encoding="utf-8"))
    doc["withholding-rules"] = [{"pattern": "x"}]
    bad.write_text(json.dumps(doc), encoding="utf-8", newline="\n")
    expect("a withholding pattern without its why is refused at load",
           F.raises(lambda: R.EvidenceTable.load(bad), "withholding-rules"))


def case_06_the_widening_plans_and_edits_only_what_can_witness_the_rule(expect):
    order = ["85", "2002", "2014", "2023"]
    cases = {"cases/X/a": R.CaseInfo("cases/X/a", R.REPO, ("2023",), ("2023",), (), ("GR-1",), None),
             "cases/X/b": R.CaseInfo("cases/X/b", R.REPO, ("2023",), ("2023",), (), ("GR-2", "GR-LATE"), None),
             "cases/X/c": R.CaseInfo("cases/X/c", R.REPO, ("2023",), (), (), ("GR-3",), None, ("2023",)),
             "cases/X/s": R.CaseInfo("cases/X/s", R.REPO, ("2023",), (), (), ("SR-5",), None, ("2023",)),
             "cases/X/d": R.CaseInfo("cases/X/d", R.REPO, ("2002",), ("2002",), (), ("GR-4",), None)}
    w = R.Witnesses(cases, {"GR-1": ["conformance:cases/X/a"], "GR-2": ["conformance:cases/X/b", "unit:T.M"],
                            "GR-3": ["conformance:cases/X/c"], "SR-5": ["conformance:cases/X/s"],
                            "GR-4": ["conformance:cases/X/d"]},
                    {r: ["2002", "2023"] for r in ("GR-1", "GR-2", "GR-3", "SR-5", "GR-4")} | {"GR-LATE": ["2023"]})
    ds = [R.Decision(f"2002-{t}", "p-01", (t,), "s", refused=((R.NO_OWN_WITNESS, "x"),)) for t in ("GR-1", "GR-2", "GR-3",
                                                                                                  "SR-5")]
    ds.append(R.Decision("2002-GR-4", "p-01", ("GR-4",), "s", refused=((R.FLAG_UNANSWERED, "x"),)))
    plan = R.widen_plan(ED, R.unwitnessed([], ds, SCHEMA), w, F.lineage([d.rule_id for d in ds]))
    expect.equal("a case whose every cited rule exists at the edition is widenable, a refusal test too; one citing a later "
                 "rule is held by its band; a row refused for anything but its evidence is not planned",
                 {k: c["status"] for k, c in plan["cases"].items()},
                 {"cases/X/a": R.WIDEN, "cases/X/b": R.BAND, "cases/X/c": R.WIDEN_REFUSAL, "cases/X/s": R.WIDEN_REFUSAL})
    expect.equal("R1-1: widening a refusal helps a syntax rule, never a general rule: that row needs an own witness",
                 {r["rule-id"]: r["state"] for r in plan["rows"]},
                 {"2002-GR-1": "widenable", "2002-GR-2": "needs-own-witness", "2002-GR-3": "needs-own-witness",
                  "2002-SR-5": "widenable"})
    expect.equal("kb/Work PB5730: each needs-own-witness row says why, per lacking target, from the closed set",
                 {r["rule-id"]: r.get("reasons") for r in plan["rows"]},
                 {"2002-GR-1": None, "2002-GR-2": {"GR-2": [R.BAND_HELD]}, "2002-GR-3": {"GR-3": [R.NOT_EVIDENCE]},
                  "2002-SR-5": None})
    expect.equal("and the counts give rows by state, by reason and cases by status",
                 plan["counts"], {"rows": {"needs-own-witness": 2, "widenable": 2},
                                  "reasons": {R.BAND_HELD: 1, R.NOT_EVIDENCE: 1},
                                  "cases": {R.BAND: 1, R.WIDEN: 1, R.WIDEN_REFUSAL: 2}})
    d = scratch_dir("lineage-reads-widen")
    cob = d / "a.cob"
    text = ("      *>--- WiseOwl test\n      *> id: a\n      *> accept: [2023]\n      *> flag: {kind: refuse, at: [2014]}\n"
            "      *>---\n       IDENTIFICATION DIVISION.\n")
    cob.write_text(text, encoding="utf-8", newline="")
    edits = R.widen_case(cob, "2002", order)
    expect("the header's accept and flag lists gain the edition in the editions' order, and nothing else changes",
           cob.read_text(encoding="utf-8") == text.replace("accept: [2023]", "accept: [2002, 2023]")
           .replace("at: [2014]", "at: [2002, 2014]") and len(edits) == 2)
    R.revert(cob, edits)
    expect("a revert restores the bytes", cob.read_text(encoding="utf-8") == text)
    side = d / "c.json"
    side.write_text('{"refuse-at": ["2023"], "expect": "COBOL0900"}', encoding="utf-8", newline="")
    e2 = R.widen_refusal(side, "2002", order)
    expect("a refusal entry's refuse-at gains the edition",
           json.loads(side.read_text(encoding="utf-8")) == {"refuse-at": ["2002", "2023"], "expect": "COBOL0900"})
    R.revert(side, e2)
    expect("and its revert restores it", side.read_text(encoding="utf-8") == '{"refuse-at": ["2023"], "expect": "COBOL0900"}')


def _stored(rid, to, src="audit-1985/state-route", **kw):
    """A planted STORED KEPT inventory row of `rid` carrying into `to` (no test-ref: no witness of its own)."""
    return {"rule-id": rid, "next": {"kind": "KEPT", "to": list(to), "source": src}, **kw}


def case_06b_the_stored_kept_rows_are_planned_proven_and_written(expect):
    """kb/Work PB5730: an edition whose KEPT records are STORED (1985: no read records) has a widening path, through the
    one population (`unwitnessed`) and the one classifier (`widen_plan`)."""
    e85 = R.edition(1985)
    d = scratch_dir("lineage-reads-stored")
    text = "      *>--- WiseOwl test\n      *> id: {0}\n      *> accept: [2023]\n      *>---\n"
    (d / "a.cob").write_text(text.format("a"), encoding="utf-8", newline="")
    cases = {"cases/X/a": R.CaseInfo("cases/X/a", d / "a.cob", ("2023",), ("2023",), (), ("GR-1",), None),
             "cases/X/b": R.CaseInfo("cases/X/b", R.REPO, ("2023",), ("2023",), (), ("GR-2", "GR-LATE"), None),
             "cases/X/c": R.CaseInfo("cases/X/c", R.REPO, ("85", "2023"), ("2023",), (), ("GR-3",), None, ("85", "2023"))}
    w = R.Witnesses(cases, {"GR-1": ["conformance:cases/X/a"], "GR-2": ["conformance:cases/X/b"],
                            "GR-3": ["conformance:cases/X/c"], "GR-4": ["unit:T.M"]},
                    {r: ["85", "2002", "2023"] for r in ("GR-1", "GR-2", "GR-3", "GR-4")} | {"GR-LATE": ["2023"]})
    w.entries = lambda keys, sp: ({k: [{"id": k.rsplit("/", 1)[1]}] for k in keys}, {})
    rows = [_stored("1985-GR-1", ["GR-1"]), _stored("1985-GR-2", ["GR-2"]), _stored("1985-GR-3", ["GR-3"]),
            _stored("1985-GR-4", ["GR-4"]), _stored("1985-GR-5", ["GR-1"], **{"test-ref": "conformance:cases/X/a"}),
            {"rule-id": "1985-GR-6", "verdict": "CONFORMS"}]
    pop = R.unwitnessed(rows, [], SCHEMA)
    expect.equal("the population is the stored KEPT rows with no witness of their own (a witnessed row and an unchained "
                 "row are not in it), with no read decision at all",
                 [(u.rule_id, u.to) for u in pop], [(f"1985-GR-{i}", (f"GR-{i}",)) for i in (1, 2, 3, 4)])
    plan = R.widen_plan(e85, pop, w, F.lineage([u.rule_id for u in pop]))
    expect.equal("a stored row whose target has a widenable case is planned widenable; a band case, a diagnosis that runs "
                 "at 85 for a general rule and a target with no suite case each need an own witness, saying why",
                 {r["rule-id"]: (r["state"], r.get("reasons")) for r in plan["rows"]},
                 {"1985-GR-1": (R.WIDENABLE, None), "1985-GR-2": (R.NEEDS_OWN_WITNESS, {"GR-2": [R.BAND_HELD]}),
                  "1985-GR-3": (R.NEEDS_OWN_WITNESS, {"GR-3": [R.DIAGNOSIS_ONLY]}),
                  "1985-GR-4": (R.NEEDS_OWN_WITNESS, {"GR-4": [R.NO_CASE]})})
    got = R.widen_apply(plan, w, e85, d / "cobol", d / "scratch", _harness({"a": "PASS"}, results_edition="85"))
    expect("the planted harness proves the stored row's case at 85 and only then is it written",
           got["widened"] == ["cases/X/a"] and not got["reverted"]
           and (d / "a.cob").read_text(encoding="utf-8") == text.format("a").replace("[2023]", "[85, 2023]"))


def case_06c_stored_and_read_rows_share_the_one_classifier(expect):
    """kb/Work PB5730: the stored and the read population are ONE population; a rule planned as either is classified
    identically (this fails the day either source gets a planner of its own)."""
    rid, t = "2002-GR-1", "GR-1"
    w = R.Witnesses({"cases/X/a": R.CaseInfo("cases/X/a", R.REPO, ("2023",), (), (), (t,), None, ("2023",))},
                    {t: ["conformance:cases/X/a"]}, {t: ["2002", "2023"]})
    lin = F.lineage([rid])
    read = R.Decision(rid, "p-01", (t,), "lineage-reads-2002/p-01.json", refused=((R.NO_OWN_WITNESS, "x"),))
    from_read = R.widen_plan(ED, R.unwitnessed([], [read], SCHEMA), w, lin)
    from_store = R.widen_plan(ED, R.unwitnessed([_stored(rid, [t], src="audit-2002/x")], [], SCHEMA), w, lin)
    strip = lambda p: [{k: v for k, v in r.items() if k != "source"} for r in p["rows"]]  # noqa: E731
    expect("the same rule and targets plan the same row from either source (only the source differs)",
           strip(from_read) == strip(from_store) and from_read["cases"] == from_store["cases"]
           and from_read["counts"] == from_store["counts"] and len(strip(from_read)) == 1)
    expect.equal("a rule both sources name is planned once", len(R.unwitnessed(
        [_stored(rid, [t], src="audit-2002/x")], [read], SCHEMA)), 1)
    expect("and one whose two sources carry into different targets stops the tool, naming the rule",
           F.raises(lambda: R.unwitnessed([_stored(rid, ["GR-9"])], [read], SCHEMA), rid, "two populations"))
    expect.equal("a stored row the search already witnesses at every target is witness-batch's, not a widening",
                 R.widen_plan(ED, R.unwitnessed([_stored(rid, [t])], [], SCHEMA), F.witnesses({t: ["2002"]}),
                              lin)["rows"][0]["state"], R.WITNESSED)


def case_06d_an_edition_with_no_reads_plans_without_a_state(expect):
    """kb/Work PB5730: `--state` is needed only where read records exist, and its absence there is said plainly."""
    d = scratch_dir("lineage-reads-nostate")
    expect.equal("an edition with no read records plans from its stored rows alone, without --state",
                 R.widen_reads(1985, None, d / "none"), [])
    held = d / "held"
    held.mkdir()
    sr = "2002-SR-2.1.2-9"
    (held / "p-01.json").write_text(json.dumps(F.record("p-01", [F.read(sr)], [F.refute(sr)])), encoding="utf-8",
                                    newline="\n")
    expect("an edition WITH read records and no --state stops the tool, saying it needs the audit run",
           F.raises(lambda: R.widen_reads(2002, None, held), "1 read record(s)", "--state RUN"))


def _planted_widening(root):
    """Two accepted cases (p passes at 2002, q fails) and a refusal test, on files in `root`; the plan widens all three.
    The witnesses' `entries` is planted (the generator's own is exercised by the dry runs)."""
    text = "      *>--- WiseOwl test\n      *> id: {0}\n      *> accept: [2023]\n      *>---\n"
    cases = {}
    for name in ("p", "q"):
        (root / f"{name}.cob").write_text(text.format(name), encoding="utf-8", newline="")
        cases[f"cases/X/{name}"] = R.CaseInfo(f"cases/X/{name}", root / f"{name}.cob", ("2023",), ("2023",), (),
                                              ("GR-1",), None)
    (root / "r.json").write_text('{"refuse-at": ["2023"], "expect": "COBOL0900"}', encoding="utf-8", newline="")
    cases["cases/X/r"] = R.CaseInfo("cases/X/r", root / "r.cob", ("2023",), (), (), ("SR-1",), None, ("2023",),
                                    root / "r.json", "COBOL0900")
    plan = {"cases": {"cases/X/p": {"status": R.WIDEN}, "cases/X/q": {"status": R.WIDEN},
                      "cases/X/r": {"status": R.WIDEN_REFUSAL}}}
    w = R.Witnesses(cases, {}, {})
    w.entries = lambda keys, sp: ({k: [{"id": k.rsplit("/", 1)[1]}] for k in keys}, {})
    files = {p.name: p.read_text(encoding="utf-8") for p in root.iterdir()}
    return w, plan, files


def _harness(results, code=1, refusal_out="error COBOL0900 here", results_edition="2002"):
    """A planted subprocess.run: run.py exits `code` writing `results` at `results_edition` (None: writes nothing); the
    compiler refuses with `refusal_out`."""
    def run(cmd, **kw):
        if cmd[1].endswith("run.py"):
            if results is not None:
                out = pathlib.Path(cmd[cmd.index("--out") + 1])
                out.mkdir(parents=True, exist_ok=True)
                (out / "results.json").write_text(json.dumps({"tests": [{"edition": results_edition, "id": i, "verdict": v}
                                                                        for i, v in results.items()]}),
                                                  encoding="utf-8", newline="\n")
            return F.completed(code)
        return F.completed(1, refusal_out)
    return run


def _tree(d):
    return {p.name: p.read_text(encoding="utf-8") for p in d.iterdir() if p.is_file()}


def case_07_the_widening_proves_first_and_writes_last(expect):
    d = scratch_dir("lineage-reads-apply")
    w, plan, files = _planted_widening(d)
    seen = {}

    def watching(results):
        inner = _harness(results)

        def run(cmd, **kw):
            seen.setdefault("tree-while-proving", _tree(d))
            return inner(cmd, **kw)
        return run

    got = R.widen_apply(plan, w, ED, d / "cobol", d / "scratch-1", watching({"p": "PASS", "q": "FAIL"}))
    expect("R2-1: while the cases are proved the tracked files are untouched (the proof runs on in-memory bands)",
           seen["tree-while-proving"] == files)
    expect.equal("a case that PASSED at the edition is written; one that failed is never written and is listed as "
                 "needing an own witness", (got["widened"], sorted(got["reverted"])), (["cases/X/p", "cases/X/r"],
                                                                                       ["cases/X/q"]))
    expect("q's file is untouched, p's widened, and the proof and the applied record are in the scratch",
           (d / "q.cob").read_text(encoding="utf-8") == files["q.cob"] and "2002" in (d / "p.cob").read_text(encoding="utf-8")
           and (d / "scratch-1" / R.PROOF).is_file() and (d / "scratch-1" / R.APPLIED).is_file())
    for p, t in files.items():
        (d / p).write_text(t, encoding="utf-8", newline="")
    got = R.widen_apply(plan, w, ED, d / "cobol", d / "scratch-2", _harness(None, code=2))
    expect("R1-3: a runner that exits 2 and writes no results proves nothing it was to judge: nothing of it is written",
           got["widened"] == ["cases/X/r"] and sorted(got["reverted"]) == ["cases/X/p", "cases/X/q"]
           and (d / "p.cob").read_text(encoding="utf-8") == files["p.cob"])
    for p, t in files.items():
        (d / p).write_text(t, encoding="utf-8", newline="")
    stale = d / "scratch-3"
    (stale / "results").mkdir(parents=True)
    (stale / "results" / "results.json").write_text('{"tests": []}', encoding="utf-8", newline="\n")
    expect("a scratch holding an earlier run's results (and no proof) is refused before anything is run or written",
           F.raises(lambda: R.widen_apply(plan, w, ED, d / "cobol", stale, _harness({"p": "PASS"})), "empty --scratch")
           and _tree(d) == files)
    got = R.widen_apply(plan, w, ED, d / "cobol", d / "scratch-5", _harness({"p": "PASS", "q": "PASS"},
                                                                             refusal_out="error COBOL0001 elsewhere"))
    expect("a refusal test refused without its expected diagnostic is not written",
           "cases/X/r" in got["reverted"] and (d / "r.json").read_text(encoding="utf-8") == files["r.json"])


def case_07b_a_refusal_drawn_only_with_a_diagnosis_the_case_does_not_expect_is_not_widened(expect):
    """Review R1-1 of records-2002-reads-2: message-tag-refused-strong-group-ordering expects COBOL1535 and draws it at
    2002, but 2002 also refuses it with the COBOL0900 introduction gate (USAGE MESSAGE-TAG is new in 2023) and COBOL1567
    (a 33-character word); its 2002 refusal witnessed no 2002 rule, and the widening wrote it."""
    d = scratch_dir("lineage-reads-refusal-diagnoses")
    out = {"2023": "  p.cob(24,12): error COBOL1535: ordering\n  p.cob(9,1): warning COBOL0400: a warning\n",
           "2002": "  p.cob(24,12): error COBOL1535: ordering\n  p.cob(14,20): error COBOL1567: 33 characters\n"
                   "  p.cob(19,11): error COBOL0900: USAGE MESSAGE-TAG requires COBOL-2023\n"}
    compiled = []

    def run(cmd, **kw):
        sp = cmd[cmd.index("--std") + 1]
        compiled.append(sp)
        return F.completed(65, out[sp])
    (d / "r.json").write_text('{"refuse-at": ["2023"], "expect": "COBOL1535"}', encoding="utf-8", newline="")
    case = R.CaseInfo("cases/X/r", d / "r.cob", ("2023",), (), (), ("SR-1",), None, ("2023",), d / "r.json", "COBOL1535")
    w = R.Witnesses({"cases/X/r": case}, {}, {})
    got = R.widen_apply({"cases": {"cases/X/r": {"status": R.WIDEN_REFUSAL}}}, w, ED, d / "cobol", d / "scratch-1", run)
    why = got["reverted"].get("cases/X/r", "")
    expect("refused at the edition with its expected code AND an introduction gate and a limit it does not draw at its "
           "own edition: not widened, the reason naming both codes", got["widened"] == [] and "COBOL0900" in why
           and "COBOL1567" in why and "COBOL1535" not in why.split("which")[0]
           and json.loads((d / "r.json").read_text(encoding="utf-8"))["refuse-at"] == ["2023"])
    expect.equal("the proof compiles the case at the edition and then at its newest refusing edition", compiled,
                 ["2002", "2023"])
    out["2002"] = "  p.cob(24,12): error COBOL1535: ordering\n  p.cob(30,1): error COBOL1288: a cascade\n"
    out["2023"] = out["2002"]
    expect("an extra error the case draws at its own edition too (a cascade its gate row passes with) is expected",
           R.refused_at(case, "2002", d / "cobol", d / "scratch-2", run, "2023") is None)
    out["2002"] = "  p.cob(24,12): error COBOL1535: ordering\n  p.cob(9,1): warning COBOL0400: a warning\n"
    compiled.clear()
    expect("a warning is no diagnosis of the program, and nothing unexpected needs no second compile",
           R.refused_at(case, "2002", d / "cobol", d / "scratch-3", run, "2023") is None and compiled == ["2002"])
    gate = R.CaseInfo("cases/X/g", d / "g.cob", ("85",), (), (), ("SR-1",), None, ("85",), d / "g.json",
                      "COBOL0900: USAGE MESSAGE-TAG")
    out["2002"] = "  p.cob(19,11): error COBOL0900: USAGE MESSAGE-TAG requires COBOL-2023\n"
    expect("an introduction-gate test that EXPECTS the gate is refused by the code its entry names",
           R.refused_at(gate, "2002", d / "cobol", d / "scratch-4", run) is None)


#: The child a KILL is planted in (review R2-1): it plants the widening in argv[1] and dies (os._exit, no cleanup can
#: run) at the point argv[2] names: while run.py proves (`prove`) or after the first proven case is written (`write`).
KILLED_CHILD = r"""
import json, os, pathlib, sys, types
sys.path[:0] = [sys.argv[3], sys.argv[4]]
import lineage_reads as R
root, where = pathlib.Path(sys.argv[1]), sys.argv[2]
text = "      *>--- WiseOwl test\n      *> id: {0}\n      *> accept: [2023]\n      *>---\n"
cases = {}
for name in ("p", "q"):
    cases[f"cases/X/{name}"] = R.CaseInfo(f"cases/X/{name}", root / f"{name}.cob", ("2023",), ("2023",), (), ("GR-1",), None)
w = R.Witnesses(cases, {}, {})
w.entries = lambda keys, sp: ({k: [{"id": k.rsplit("/", 1)[1]}] for k in keys}, {})
plan = {"cases": {k: {"status": R.WIDEN} for k in cases}}
def run(cmd, **kw):
    if where == "prove":
        os._exit(9)
    out = pathlib.Path(cmd[cmd.index("--out") + 1]); out.mkdir(parents=True, exist_ok=True)
    (out / "results.json").write_text(json.dumps({"tests": [{"edition": "2002", "id": i, "verdict": "PASS"} for i in "pq"]}),
                                      encoding="utf-8", newline="\n")
    return types.SimpleNamespace(returncode=0, stdout="", stderr="")
R.widen_apply(plan, w, R.edition(2002), root / "cobol", root / "scratch", run, on_write=lambda k: os._exit(9))
"""


def case_08_a_killed_widening_leaves_no_unproven_band(expect):
    d = scratch_dir("lineage-reads-kill")
    text = "      *>--- WiseOwl test\n      *> id: {0}\n      *> accept: [2023]\n      *>---\n"
    for name in ("p", "q"):
        (d / f"{name}.cob").write_text(text.format(name), encoding="utf-8", newline="")
    files = _tree(d)
    here = pathlib.Path(R.__file__).resolve().parent
    killed = subprocess.run([sys.executable, "-c", KILLED_CHILD, str(d), "prove", str(here), str(here.parent)],
                            capture_output=True, text=True)
    expect("R2-1: a run killed (os._exit) while it proves leaves the tracked tree untouched",
           killed.returncode == 9 and _tree(d) == files)
    shutil.rmtree(d / "scratch")
    killed = subprocess.run([sys.executable, "-c", KILLED_CHILD, str(d), "write", str(here), str(here.parent)],
                            capture_output=True, text=True)
    written = [n for n in ("p.cob", "q.cob") if (d / n).read_text(encoding="utf-8") != files[n]]
    expect("R2-1: a run killed after its first write leaves only a PROVEN widening, recorded in the scratch",
           killed.returncode == 9 and written == ["p.cob"]
           and json.loads((d / "scratch" / R.APPLIED).read_text(encoding="utf-8")) == ["cases/X/p"])
    w = R.Witnesses({f"cases/X/{n}": R.CaseInfo(f"cases/X/{n}", d / f"{n}.cob", ("2023",), ("2023",), (), ("GR-1",), None)
                     for n in "pq"}, {}, {})
    got = R.widen_apply({"cases": {}}, w, ED, d / "cobol", d / "scratch", _harness({}))
    expect("re-running --apply with that scratch reads the proof and FINISHES the writes, saying so",
           got["resumed"] and got["already-written"] == ["cases/X/p"] and got["widened"] == ["cases/X/p", "cases/X/q"]
           and all("2002" in (d / n).read_text(encoding="utf-8") for n in ("p.cob", "q.cob")))


def case_09_an_uncommitted_band_is_no_witness(expect):
    w = F.witnesses({"GR-1.1.3-1": ["2002"]})
    w.cases["cases/X/GR-1.1.3-1"] = dataclasses.replace(w.cases["cases/X/GR-1.1.3-1"], committed=False)
    expect("R2-1: a case whose band the working tree changed and no gate ran is no witness, even accepted at 2002",
           w.accepted_at("GR-1.1.3-1", "2002") == [] and w.at("GR-1.1.3-1", "2002") == []
           and R.case_status(w.cases["cases/X/GR-1.1.3-1"], "2002", {}) == R.UNCOMMITTED)


def case_10_a_licence_is_a_checked_citation_of_the_rows_own_edition(expect):
    for label, text, ok in (("a citation of the 2002 text, then its gloss", "§2002:3.1.5 with §2002:B.3 item 1 (gloss)", True),
                            ("the brief's none", "none", False),
                            ("a sentence that names a citation later", "not needed: §2002:3.1 would license it", False),
                            ("words that are no citation", "see above", False),
                            ("a citation of another edition", "§2023:4.2.10", False),
                            ("a clause the 2002 text does not have", "§2002:99.99.99", False),
                            ("absent", None, False)):
        got = R.licence_problem(ED, text)
        expect(f"R2-2: {label} -> {'a licence' if ok else 'refused, with why'}", (got is None) == ok and (ok or bool(got)))


def case_11_a_rule_that_grants_a_form_needs_an_accepted_case(expect):
    t = R.EvidenceTable.load()
    for text in ("The phrases may appear in any order.", "The FILLER clause is optional.", "They are written in any order.",
                 "A literal is permitted here.", "The clause can be specified once."):
        expect(f"R2-3: {text!r} grants a form: a refusal does not witness it", not t.diagnosable("SR", text))
    expect("a prohibition (\"may not\", \"shall not\") is diagnosable by a refusal",
           t.diagnosable("SR", "Identifier-1 may not be reference modified.")
           and t.diagnosable("SR", "The clause shall not be specified twice."))
    expect("a general rule that withholds nothing is not diagnosable",
           not t.diagnosable("GR", "The clause shall not be specified twice."))
    sr = "2002-SR-2.1.2-9"
    s = R.normal_slice("p-01", F.record("p-01", [F.read(sr)], [F.refute(sr)]))
    lin = F.lineage([sr])
    lin = dataclasses.replace(lin, texts={sr: "The clauses may be written in any order."})
    got = R.decide(ED, [s], None, F.witnesses(diagnosed={F.twin(sr): ["2002"]}), lin)[0]
    expect("a granting syntax rule witnessed only by a refusal at 2002 is refused: no own-edition witness",
           [r for r, _ in got.refused] == [R.NO_OWN_WITNESS])


def case_12_an_own_verdict_from_a_pointer_is_written_and_audited_like_a_kept_record(expect):
    ids = {n: f"2002-GR-4.1.3-{i}" for i, n in enumerate(
        ("ok", "half", "refusal", "overturned", "doubtful", "declined", "bare", "disagree", "late", "unrefuted"), 1)}
    two = {**F.read(ids["half"]), "to": ["GR-8.1-1", "GR-8.1-2"]}
    mixed = {**F.read(ids["disagree"]), "to": ["GR-8.1-1", "GR-8.1-3"]}
    s = R.normal_slice("u-01", F.record("u-01", [
        F.read(ids["ok"]), two, F.read(ids["refusal"]), F.read(ids["overturned"]), F.read(ids["doubtful"]),
        F.read(ids["declined"], **{"licence-2002": "none"}), F.read(ids["bare"], citations="§2023:1.1 GR1"), mixed,
        F.read(ids["late"]), F.read(ids["unrefuted"])],
        [F.refute(ids[n]) for n in ("ok", "half", "refusal", "declined", "bare", "disagree", "late")]
        + [F.refute(ids["overturned"], "OVERTURNED", **{"compiler-at-2002": "wrong: prints 3"}),
           F.refute(ids["doubtful"], "DOUBTFUL")]))
    twins = {F.twin(ids[n]): ["2002", "2023"] for n in ("ok", "overturned", "doubtful", "bare", "unrefuted")}
    w = F.witnesses(accepted=twins | {"GR-8.1-1": ["2002"], "GR-8.1-2": ["2023"], "GR-8.1-3": ["2002"],
                                      F.twin(ids["late"]): ["2023"]},
                    diagnosed={F.twin(ids["refusal"]): ["2002"], F.twin(ids["declined"]): ["2002"]},
                    verdicts={t: "CONFORMS" for t in [*twins, "GR-8.1-1", "GR-8.1-2", F.twin(ids["refusal"]),
                                                      F.twin(ids["late"])]}
                    | {F.twin(ids["declined"]): "DOCUMENTED-NON-SUPPORT", "GR-8.1-3": "DOCUMENTED-NON-SUPPORT"})
    register = next(iter(R._schema().anchored_files))   # the register file: a location there is a determination
    w.locations = {t: ["src/A.cs#M"] for t in w.verdicts} | {F.twin(ids["ok"]): ["src/A.cs#M", f"{register}#planted"]}
    lin = F.lineage(ids.values(), unpaired=tuple(ids.values()))
    got = {next(n for n, r in ids.items() if r == d.rule_id): d for d in R.decide(ED, [s], None, w, lin)}
    expect("an unpaired SAME its refuter UPHELD, its pointer accepted at 2002, is an own verdict with nothing blocking it",
           got["ok"].work == R.OWN_VERDICT_FROM_POINTER and got["ok"].refused == ())
    expect("a pointer with SEVERAL targets, one of them unwitnessed at 2002, blocks the own verdict by that target",
           [r for r, _ in got["half"].refused] == [R.NO_OWN_WITNESS] and "GR-8.1-2" in got["half"].refused[0][1])
    expect("a REFUSAL is no witness of a general (behaviour) rule's pointer: no own-edition witness",
           [r for r, _ in got["refusal"].refused] == [R.NO_OWN_WITNESS] and "only a diagnosis" in got["refusal"].refused[0][1])
    expect("an OVERTURNED SAME of an unpaired rule is its refuter's work item, never an own verdict (a defect here)",
           got["overturned"].work == R.DEFECT)
    expect.equal("a SAME its refuter calls DOUBTFUL is undecided, never an own verdict", got["doubtful"].work, R.UNDECIDED)
    expect("an unpaired SAME with NO REFUTER RECORD is refused for it, never own-verdict work (kb/Work PB5699: the "
           "refuter is read before the pairing)",
           got["unrefuted"].work is None and got["unrefuted"].refused == ((R.NO_REFUTER, "no refuter has attacked the SAME"),))
    expect("a DOCUMENTED-NON-SUPPORT pointer with no 2002 licence blocks the own verdict",
           R.NO_LICENCE in [r for r, _ in got["declined"].refused])
    expect("a read citing nothing of its own edition blocks the own verdict",
           [r for r, _ in got["bare"].refused] == [R.NO_OWN_CITATION])
    expect("pointers whose verdicts differ block the own verdict: it states one",
           R.POINTERS_DISAGREE in [r for r, _ in got["disagree"].refused])
    bare_w = F.witnesses(accepted={F.twin(ids["ok"]): ["2002"]}, verdicts={F.twin(ids["ok"]): "CONFORMS"})
    expect("a pointer that names no code location blocks a CONFORMS own verdict, which requires one",
           [r for r, _ in R.own_verdict_problems(ED, s.reads[ids["ok"]], lin, bare_w)] == [R.NO_CODE_LOCATION])
    rec = R.own_verdict(ED, s.reads[ids["ok"]], lin, w)
    expect.equal("the record: the pointer's verdict and code (not its register anchor), ONLY its cases accepted at 2002, "
                 "notes opening with the read record", (rec["verdict"], rec["code-location"], rec["test-ref"]),
                 ("CONFORMS", "src/A.cs#M", f"conformance:cases/X/{F.twin(ids['ok'])}"))
    expect("the notes name the read record and carry its 2002 citation",
           R.OWN_SOURCE.match(rec["notes"]).group(1) == "lineage-reads-2002/u-01.json" and "§2002:1.1 GR1" in rec["notes"])
    expect.equal("THE AUDIT: the stored own verdict stands", R.stored_errors(ED, [rec], [s], w, lin), [])
    narrowed = F.witnesses(accepted={F.twin(ids["ok"]): ["2023"]}, verdicts={F.twin(ids["ok"]): "CONFORMS"})
    expect("a stored own verdict whose case no longer runs at the edition is red",
           any(R.NO_OWN_WITNESS in e for e in R.stored_errors(ED, [rec], [s], narrowed, lin)))
    flipped = F.witnesses(accepted={F.twin(ids["ok"]): ["2002"]}, verdicts={F.twin(ids["ok"]): "DOCUMENTED-NON-SUPPORT"})
    expect.equal("the audit reads no pointer (kb/Work PB5675): a stored own verdict stands on its own witness at the "
                 "edition when its pointer's verdict later changes", R.stored_errors(ED, [rec], [s], flipped, lin), [])
    over = R.normal_slice("u-01", F.record("u-01", [F.read(ids["ok"])], [F.refute(ids["ok"], "OVERTURNED")]))
    expect("a stored own verdict whose read was since OVERTURNED is red",
           any("OVERTURNED" in e for e in R.stored_errors(ED, [rec], [over], w, lin)))
    expect("a stored own verdict of a rule that has a candidate pair is red: its read writes a KEPT record",
           any("candidate pair" in e for e in R.stored_errors(ED, [rec], [s], w, F.lineage([ids["ok"]]))))
    rewritten = {**rec, "notes": "A correction, then the read's reason: hand-written."}
    expect("review R1-4: an own verdict whose NOTES WERE REWRITTEN, of a rule whose tracked read decides "
           "own-verdict-from-pointer, is still audited: its witness no longer running at 2002 is reported",
           any(R.NO_OWN_WITNESS in e and "does not run at 2002" in e
               for e in R.stored_errors(ED, [rewritten], [s], narrowed, lin)))
    expect("an own verdict of such a rule with no test-ref of its own is reported",
           any("no test-ref of its own" in e for e in R.stored_errors(ED, [{**rewritten, "test-ref": ""}], [s], w, lin)))
    expect.equal("an own verdict of a rule whose read does NOT decide own-verdict-from-pointer, with notes naming no "
                 "read, stands on its own record: this audit does not hold it",
                 R.stored_errors(ED, [{**rewritten, "rule-id": ids["overturned"]}], [s], narrowed, lin), [])
    refusal = f"conformance:cases/X/{F.twin(ids['refusal'])}"
    expect.equal("a diagnosis cited BESIDE a case accepted at the edition is the restriction witnessed beside the grant: "
                 "the stored own verdict stands", R.stored_errors(ED, [{**rec, "test-ref": f"{rec['test-ref']}; {refusal}"}],
                                                                [s], w, lin), [])
    expect("a stored own verdict of a rule that grants a form, witnessed at the edition by a diagnosis ALONE, is reported",
           any("only a diagnosis" in e for e in R.stored_errors(ED, [{**rec, "test-ref": refusal}], [s], w, lin)))
    plan = R.widen_plan(ED, R.unwitnessed([], list(got.values()), SCHEMA), w, lin)
    expect.equal("widen plans the own verdicts blocked only by their evidence, as it plans the KEPT-refused rows",
                 sorted((r["rule-id"], r["for"]) for r in plan["rows"]),
                 sorted((ids[n], R.OWN_VERDICT_FROM_POINTER) for n in ("half", "refusal", "late")))


def _format_world():
    """Four planted cases of one syntax rule's target: a refusal test refused at 85 and 2002 in free form and one in
    fixed form, and a WiseOwl case accepted at 85 in free form and one in fixed form."""
    t = "SR-13.18.33.3-1"
    cases = {
        "cases/X/free-ref": R.CaseInfo("cases/X/free-ref", R.REPO, ("85", "2002"), (), (), (t,), None, ("85", "2002"),
                                       source_format="free"),
        "cases/X/fixed-ref": R.CaseInfo("cases/X/fixed-ref", R.REPO, ("85", "2002"), (), (), (t,), None, ("85", "2002")),
        "wiseowl/cases/X/free-acc": R.CaseInfo("wiseowl/cases/X/free-acc", R.REPO, ("85",), ("85",), (), (t,), None,
                                               source_format="free"),
        "wiseowl/cases/X/fixed-acc": R.CaseInfo("wiseowl/cases/X/fixed-acc", R.REPO, ("85",), ("85",), (), (t,), None)}
    return t, R.Witnesses(cases, {t: [f"conformance:{k}" for k in cases]}, {t: ["85", "2002", "2014", "2023"]})


def case_13_a_witness_at_an_edition_is_text_in_a_format_it_defines(expect):
    """kb/Work PB5754: one predicate (`format_problem`) asked by the audit, the writer and the planner."""
    e85 = R.edition(1985)
    t, w = _format_world()
    expect("the predicate: free form is no format of 85 and is one of 2002; fixed form is one of both",
           R.format_problem("free", "85") and not R.format_problem("free", "2002")
           and not R.format_problem("fixed", "85") and not R.format_problem("fixed", "2002"))
    probs = w.own_problems(e85, ["conformance:cases/X/free-ref"], True, None)
    expect("THE AUDIT: a free-form refusal test refused at 85 is no witness of a 1985 row",
           len(probs) == 1 and "free form" in probs[0] and "PB5754" in probs[0])
    expect.equal("and the same test is a witness of a 2002 row", w.own_problems(ED, ["conformance:cases/X/free-ref"],
                                                                               True, None), [])
    expect.equal("a fixed-form refusal test refused at 85 is a witness at both",
                 (w.own_problems(e85, ["conformance:cases/X/fixed-ref"], True, None),
                  w.own_problems(ED, ["conformance:cases/X/fixed-ref"], True, None)), ([], []))
    expect("a WiseOwl free-form case ACCEPTED at 85 is no witness at 1985 either",
           w.own_problems(e85, ["conformance:wiseowl/cases/X/free-acc"], False, None)
           and not w.own_problems(e85, ["conformance:wiseowl/cases/X/fixed-acc"], False, None))
    lin85 = R.Lineage({"1985-SR-VI.5.7.3-1": "SR"}, {}, R.EvidenceTable.load(), {})
    own = {"rule-id": "1985-SR-VI.5.7.3-9", "verdict": "CONFORMS", "test-ref": "conformance:cases/X/free-ref; unit:T.M"}
    expect("THE AUDIT holds a row with a verdict of its own to the same predicate (its unit test is its own fields')",
           [e for e in R.stored_errors(e85, [own], [], w, lin85) if "free form" in e]
           and not R.stored_errors(e85, [dict(own, **{"test-ref": "conformance:cases/X/fixed-ref"})], [], w, lin85))
    expect.equal("THE PLANNER: at 85 the search finds the fixed-form accepted case and only the fixed-form cases run "
                 "there; at 2002 both refusal tests",
                 (w.search(e85, True, [t], None, lin85.table)[0], w.at(t, "85"),
                  sorted(w.search(ED, True, [t], None, lin85.table)[0])),
                 (["wiseowl/cases/X/fixed-acc"], ["cases/X/fixed-ref", "wiseowl/cases/X/fixed-acc"],
                  ["cases/X/fixed-ref", "cases/X/free-ref"]))
    free_only = R.Witnesses({"cases/X/f": R.CaseInfo("cases/X/f", R.REPO, ("2002",), ("2002",), (), (t,), None,
                                                     source_format="free")}, {t: ["conformance:cases/X/f"]},
                            {t: ["85", "2002", "2014", "2023"]})
    plan = R.widen_plan(e85, [R.Unwitnessed("1985-GR-VI.5.7.3-2", (t,), "s")], free_only,
                        R.Lineage({"1985-GR-VI.5.7.3-2": "GR"}, {}, R.EvidenceTable.load(), {}))
    expect.equal("THE PLANNER never proposes a free-form case at 85: its status is FORMAT and the row says why",
                 ({k: c["status"] for k, c in plan["cases"].items()}, plan["rows"][0].get("reasons")),
                 ({"cases/X/f": R.FORMAT}, {t: [R.NOT_ITS_FORMAT]}))
    import record_verdicts  # noqa: PLC0415  (the writer)
    root = scratch_dir("lineage-reads-format")
    for name, fmt in (("free", "free"), ("fixed", "fixed")):
        lead = "" if fmt == "free" else "      "
        p = root / "cases" / "X" / f"{name}.cob"
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text("".join(f"{lead}{x}\n" for x in ("*>--- conformance-case", f"*> id: {name}", "*> rules: []",
                                                       f"*> source-format: {fmt}", "*>---")), encoding="utf-8", newline="")
    real = R.SUITE
    R.SUITE = root
    try:
        def errs(ref):
            row = {"rule-id": "1985-SR-VI.5.7.3-1", "next": {"kind": "KEPT", "to": [t]}}
            return [e for e in record_verdicts.validate([(root / "b.json", 0, {"rule-id": row["rule-id"], "test-ref": ref})],
                                                        SCHEMA, {row["rule-id"]: "SR"}, {row["rule-id"]: row}, e85,
                                                        {t: {"rule-digest": "d"}}) if "PB5754" in e or "cannot be told" in e]
        expect("THE WRITER refuses a 1985 record naming a free-form case, before it is written",
               len(errs("conformance:cases/X/free")) == 1)
        expect.equal("and writes one naming a fixed-form case", errs("conformance:cases/X/fixed"), [])
        (root / "cases" / "X" / "broken.cob").write_text("IDENTIFICATION DIVISION.\n", encoding="utf-8", newline="")
        expect("and refuses one whose header does not read (its format cannot be told)",
               len(errs("conformance:cases/X/broken")) == 1)
        expect.equal("a reference to no case on disk is the battery gate's to refuse, never guessed here",
                     errs("conformance:cases/X/absent"), [])
        expect.equal("a 2002 reference is not asked to be fixed form",
                     R.ref_format_problem("conformance:cases/X/free", ED), None)
    finally:
        R.SUITE = real


def case_14_a_read_speaks_of_its_own_edition_and_no_other(expect):
    """kb/Work PB5453: `compiler-at-<year>` and `licence-<year>` are read at the record's OWN edition. Until 2026-11-21 only
    `compiler-at-2002` was read, so a 1985 read's `compiler-at-1985` was dropped and a row proven correct imported
    `undecided`; a key that names another edition, or none, is refused by slice and rule."""
    ed85 = R.edition(1985)
    rid = "1985-GR-IV.4.1-p2"
    s = R.normal_slice("s-01", F.record("s-01", [
        {**F.read(rid, "DIFFERENT", citations="§1985:IV.4.1 p2"), "to": [],
         "compiler-at-1985": "correct: E:/probes/s-01/p.cob", "licence-1985": "§1985:I.1.5.2.5 item 1"}],
        edition=1985))
    r = s.reads[rid]
    expect("a 1985 read's compiler-at-1985 and licence-1985 are read",
           r.compiler == R.Judgment("correct", "correct: E:/probes/s-01/p.cob") and r.licence == "§1985:I.1.5.2.5 item 1")
    d = R.decide(ed85, [s], None, None, F.lineage([rid]))[0]
    expect.equal("so the row it proved correct is the work item needs-own-witness, never undecided", d.work,
                 R.NEEDS_OWN_WITNESS)
    for label, key, year in (("another edition's judgment", "compiler-at-2002", 1985),
                             ("another edition's licence", "licence-1985", 2002),
                             ("a judgment naming no edition", "compiler-at", 1985),
                             ("a two-digit edition", "compiler-at-85", 1985),
                             ("another edition's scope", "scope-2002", 1985)):
        case = {**F.read("1985-GR-IV.4.1-p3", "DIFFERENT"), key: "correct: p.cob"}
        expect(f"REFUSED loudly by slice and rule: {label} (`{key}` in a {year} read)",
               F.raises(lambda: R.normal_read("s-02", case, year), "s-02", "1985-GR-IV.4.1-p3", f"`{key}`"))
    expect("a refuter's case is held to the same edition",
           F.raises(lambda: R.normal_slice("s-03", F.record("s-03", [F.read("1985-GR-IV.4.1-p4")],
                                                            [F.refute("1985-GR-IV.4.1-p4", **{"compiler-at-2002": "wrong"})],
                                                            edition=1985)), "s-03", "compiler-at-2002"))
    kept = R.normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"compiler-at-2002-probe": "p.cob",
                                                               "answer-evidence-2002": "x", "pointer-1985": "y"}), 2002)
    expect("a suffix after the record's own year, and a key of no edition-keyed family, are the record's own",
           kept.rule_id == "2002-GR-1.1.3-1")
    expect("a record with no edition year is refused: its cases' fields are keyed by it",
           F.raises(lambda: R.normal_slice("s-04", {**F.record("s-04", [F.read("1985-GR-IV.4.1-p5")]), "edition": "85"}),
                    "s-04", "edition"))


def case_15_a_derivation_a_read_proposes_has_a_route(expect):
    """kb/Work PB5453: a DIFFERENT read that proposes a derivation (section 1.1) becomes the work item `derivation` once its
    derivation refuter UPHELD it (a mechanical arm needs none), and `derivation_record` gives its store record and its
    section 8 register row; until 2026-11-21 it landed `undecided` and nothing wrote the row."""
    ed85 = R.edition(1985)
    cite = "§1985:IV.2.1.1 p1"
    ids = {k: f"1985-SR-IV.2.1.{n}-p1" for n, k in enumerate(("upheld", "unrefuted", "mechanical", "overturned",
                                                               "doubtful", "shapeless", "informative"), start=1)}

    def proposal(rid, arm="definitional", names="defines the kinds of element a general format of the user's program "
                                                "is made of", **kw):
        return {**F.read(rid, "DIFFERENT", citations=cite), "to": [], "class": "definitional",
                "compiler-at-1985": "not probed: no program can show it", "derivation": {"arm": arm, "names": names, **kw}}

    reads = [proposal(ids["upheld"], **{"code-location": "src/WiseOwl.Cobol.Compiler/A.cs#M"}),
             proposal(ids["unrefuted"]),
             proposal(ids["mechanical"], "undefined-in-rule", '"the result is undefined in that case"'),
             proposal(ids["overturned"]), proposal(ids["doubtful"]),
             proposal(ids["shapeless"], names="defines it"),
             proposal(ids["informative"], "informative",
                      "describes the standard's general formats: \"the kinds of element a general format\"")]
    drefs = [F.refute(ids["upheld"], citations=cite, attempt="a.cob"), F.refute(ids["shapeless"], attempt="b.cob"),
             F.refute(ids["overturned"], "OVERTURNED", probe="w.cob", attempt="w.cob",
                      **{"compiler-at-1985": "correct: w.cob"}),
             F.refute(ids["doubtful"], "DOUBTFUL", attempt="d.cob")]
    s = R.normal_slice("s-05", F.record("s-05", reads, edition=1985, derivation_refutes=drefs))
    got = {d.rule_id: d for d in R.decide(ed85, [s], None, None, F.lineage(ids.values()))}
    expect("an upheld definitional derivation is the work item `derivation`, blocked by nothing",
           got[ids["upheld"]].work == R.DERIVATION and not got[ids["upheld"]].refused)
    expect("an argued arm no refuter attacked waits for its refuter (`no refuter record`)",
           got[ids["unrefuted"]].work == R.DERIVATION and got[ids["unrefuted"]].refused[0][0] == R.NO_REFUTER)
    expect("the informative arm is ARGUED, never mechanical: with no refuter record it waits for its refuter, its shape "
           "standing (kb/Work PB6012)",
           got[ids["informative"]].work == R.DERIVATION
           and [r for r, _ in got[ids["informative"]].refused] == [R.NO_REFUTER])
    expect("a mechanical arm needs no refuter: its checker is its reader",
           got[ids["mechanical"]].work == R.DERIVATION and not got[ids["mechanical"]].refused)
    expect.equal("an OVERTURNED derivation (a program can show the rule) is the refuter's work item",
                 got[ids["overturned"]].work, R.NEEDS_OWN_WITNESS)
    expect.equal("a DOUBTFUL refuter leaves it undecided", got[ids["doubtful"]].work, R.UNDECIDED)
    expect("a Names cell out of its arm's shape blocks it, with the shape",
           [r for r, _ in got[ids["shapeless"]].refused] == [R.NAMES_SHAPE])
    schema = SCHEMA
    record, cells, problems = R.derivation_record(ed85, s.reads[ids["upheld"]], s.derivation_refutes[ids["upheld"]],
                                                  {}, "claude: 2026-11-16", schema)
    anchor = schema.derivation.anchor_for({"rule-id": ids["upheld"]})
    expect("an unrecorded row takes CONFORMS, the read's code location and the COMPUTED anchor, its notes naming the read",
           not problems and record["verdict"] == R.DERIVED_VERDICT and record["derivation"] == anchor
           and record["code-location"] == "src/WiseOwl.Cobol.Compiler/A.cs#M"
           and record["notes"].startswith(f"{R.DERIVATION_LEAD} lineage-reads-1985/s-05.json"))
    expect("its register row: the key, the arm, the Names, the argument naming the read and the refutation, the signature "
           "and the refuter record file",
           cells[:3] == [f"DRV-{ids['upheld']}", "definitional", reads[0]["derivation"]["names"]]
           and "lineage-reads-1985/s-05.json" in cells[3] and "UPHELD" in cells[3] and cells[4] == "claude: 2026-11-16"
           and cells[5] == "tests/version-matrix/derivation-refutations/lineage-reads-1985-s-05.jsonl")
    _r, _c, missing = R.derivation_record(ed85, s.reads[ids["unrefuted"]], None, {}, "claude: 2026-11-16", schema)
    expect.equal("an unrecorded row whose read names no code location is refused (CONFORMS requires one)",
                 [r for r, _ in missing], [R.NO_CODE_LOCATION])
    chained, _c, ok = R.derivation_record(ed85, s.reads[ids["unrefuted"]], None,
                                          {"rule-id": ids["unrefuted"], "next": {"kind": "KEPT"}}, "claude: 2026-11-16",
                                          schema)
    expect("a chained KEPT row gains the derivation alone: it stands in for the row's own witness",
           not ok and chained == {"rule-id": ids["unrefuted"], "derivation": schema.derivation.anchor_for(
               {"rule-id": ids["unrefuted"]})})
    _r, _c, tested = R.derivation_record(ed85, s.reads[ids["upheld"]], None,
                                         {"rule-id": ids["upheld"], "verdict": "CONFORMS",
                                          "test-ref": "conformance:cases/X/a"}, "claude: 2026-11-16", schema)
    expect.equal("a row with a spec-derived test is refused (section 1.1 refusal 1)", [r for r, _ in tested], [R.HAS_TEST])
    lines = R.refutation_lines(1985, s).splitlines()
    expect("the slice's refuter records are one JSON line per derivation refute, in the form the Refuted cell's checker reads",
           len(lines) == 4 and json.loads(lines[0]) == {"rule-id": ids["upheld"], "outcome": "UPHELD", "citation": cite,
                                                        "attempt": "a.cob", "reason": "planted UPHELD",
                                                        "source": "lineage-reads-1985/s-05.json"})
    for label, case, needle in (
            ("a definitional class with no derivation", {**F.read("1985-SR-IV.2.1.9-p1", "DIFFERENT"),
                                                         "class": "definitional"}, "no `derivation`"),
            ("a derivation on a SAME read", {**F.read("1985-SR-IV.2.1.9-p1"), "derivation": {"arm": "definitional",
                                                                                            "names": "defines x"}},
             "DIFFERENT"),
            ("an arm the schema does not have", {**F.read("1985-SR-IV.2.1.9-p1", "DIFFERENT"),
                                                 "derivation": {"arm": "obvious", "names": "x"}}, "arm")):
        expect(f"REFUSED loudly: {label}", F.raises(lambda: R.normal_read("s-06", case, 1985), "s-06", needle))
    expect("a derivation refute of a read that proposes none is refused",
           F.raises(lambda: R.normal_slice("s-07", F.record("s-07", [F.read("1985-SR-IV.2.1.9-p1", "DIFFERENT")],
                                                            edition=1985,
                                                            derivation_refutes=[F.refute("1985-SR-IV.2.1.9-p1",
                                                                                         attempt="x.cob")])),
                    "s-07", "proposes no derivation"))
    d = scratch_dir("lineage-reads-derivation")
    src, out = d / "out", d / "records"
    src.mkdir()
    (src / "s-05-read.json").write_text(json.dumps({"slice": "s-05", "cases": reads}), encoding="utf-8", newline="\n")
    (src / "s-05-derivation-refute.json").write_text(json.dumps({"slice": "s-05", "cases": drefs}), encoding="utf-8",
                                                     newline="\n")
    expect.equal("the import takes the derivation refuter's file as a part of its slice, not an orphan refuter",
                 R.import_records(1985, src, None, out), ["s-05"])
    doc = json.loads((out / "s-05.json").read_text(encoding="utf-8"))
    expect("the part is held verbatim with its digest, and load reads it back",
           doc["imported"]["derivation-refute"] == R.digest(doc["derivation-refute"])
           and set(R.load(1985, out)[0].derivation_refutes) == {ids["upheld"], ids["shapeless"], ids["overturned"],
                                                                 ids["doubtful"]})
    repo = d / "repo"
    jsonl = repo / R.refutations_file(1985, "s-05")
    jsonl.parent.mkdir(parents=True)
    jsonl.write_text(R.refutation_lines(1985, s), encoding="utf-8", newline="\n")
    expect.equal("the audit holds a generated refuter record file to its read record", R.refutation_errors(1985, [s], repo), [])
    jsonl.write_text(R.refutation_lines(1985, s).replace("UPHELD", "OVERTURNED"), encoding="utf-8", newline="\n")
    expect("and a hand edit of it is red, naming the file and the read record",
           any("lineage-reads-1985/s-05.json" in e for e in R.refutation_errors(1985, [s], repo)))


def case_16_a_second_read_supersedes_the_first_and_the_refuter_brief_shapes_are_read(expect):
    """kb/Work PB5453 (the import landing): the fleet's second read (slice s-99) re-decides rules other slices read first,
    each case naming the slice it replaces (`supersedes`). Before the fix the import refused every such rule `read again`
    and nothing was written; a derivation refuter's case in its brief's shape (`attempt`, no citations) and a DOUBTFUL
    definitional read (a reader who found no arm that fits) were refused too."""
    rid, other = "1985-GR-IV.1-p1", "1985-GR-IV.1-p2"
    first = [F.read(rid, "DOUBTFUL", to=[]), F.read(other, "DIFFERENT", to=[])]
    second = [{**F.read(rid, "DIFFERENT", to=[]), "supersedes": "s-01",
               "compiler-at-1985": "correct: p.cob", "probe": "p.cob"}]
    d = scratch_dir("lineage-reads-supersede")
    src, out = d / "out", d / "records"
    src.mkdir()
    for name, cases in (("s-01", first), ("s-99", second)):
        (src / f"{name}-read.json").write_text(json.dumps({"slice": name, "cases": cases}), encoding="utf-8",
                                               newline="\n")
    expect.equal("both slices import: the second read names the slice it replaces",
                 R.import_records(1985, src, None, out), ["s-01", "s-99"])
    loaded = {s.name: s for s in R.load(1985, out)}
    expect("the newest read decides the rule; the first record keeps its other rules and stays verbatim",
           set(loaded["s-01"].reads) == {other} and set(loaded["s-99"].reads) == {rid}
           and json.loads((out / "s-01.json").read_text(encoding="utf-8"))["read"]["cases"][0]["rule-id"] == rid)
    got = {x.rule_id: x for x in R.decide(R.edition(1985), list(loaded.values()), None, None, F.lineage([rid, other]))}
    expect("the decision is the second read's, from its own slice",
           got[rid].slice == "s-99" and got[rid].work == R.NEEDS_OWN_WITNESS)
    for label, cases, needle in (
            ("a second read naming no slice", [F.read(rid, "DIFFERENT", to=[])], "no line of second reads"),
            ("a second read naming a slice that does not read the rule",
             [{**F.read("1985-GR-IV.1-p3", "DIFFERENT", to=[]), "supersedes": "s-01"}], "does not read the rule"),
            ("a slice superseding itself", [{**F.read(rid, "DIFFERENT", to=[]), "supersedes": "s-99"}], "supersedes")):
        (src / "s-99-read.json").write_text(json.dumps({"slice": "s-99", "cases": cases}), encoding="utf-8",
                                            newline="\n")
        before = {p.name: p.read_text(encoding="utf-8") for p in out.iterdir()}
        expect(f"REFUSED before any write: {label}",
               F.raises(lambda: R.import_records(1985, src, None, out), needle)
               and {p.name: p.read_text(encoding="utf-8") for p in out.iterdir()} == before)
    proposal = {**F.read("1985-SR-IV.2.1.1-p1", "DIFFERENT", citations="§1985:IV.2.1.1 p1"), "to": [],
                "class": "definitional", "derivation": {"arm": "definitional", "names": "defines the kinds of element"}}
    brief = {"rule-id": "1985-SR-IV.2.1.1-p1", "arm": "definitional", "verdict": "UPHELD", "attempt": "w.cob refused",
             "reason": "no program can show it", "probe": None}
    s = R.normal_slice("s-05", F.record("s-05", [proposal], edition=1985, derivation_refutes=[brief]))
    expect("a derivation refuter's case in the brief's shape is read: its attempt kept, its citations none",
           s.derivation_refutes["1985-SR-IV.2.1.1-p1"].attempt == "w.cob refused"
           and s.derivation_refutes["1985-SR-IV.2.1.1-p1"].citations == ()
           and '"attempt": "w.cob refused"' in R.refutation_lines(1985, s))
    for label, case, needle in (("a derivation refute with no attempt", {k: v for k, v in brief.items() if k != "attempt"},
                                 "attempt"),
                                ("a derivation refute of another arm", {**brief, "arm": "unpopulatable-antecedent"},
                                 "arm")):
        expect(f"REFUSED loudly: {label}",
               F.raises(lambda: R.normal_slice("s-05", F.record("s-05", [proposal], edition=1985,
                                                                derivation_refutes=[case])), "s-05", needle))
    doubt = R.normal_read("s-16", {**F.read("1985-SR-X.2.3-p1", "DOUBTFUL", to=[]), "class": "definitional",
                                   "derivation": None}, 1985)
    expect("a DOUBTFUL definitional read with no derivation (no arm fits) is read: it proposes none",
           doubt.derivation is None and doubt.verdict == "DOUBTFUL")
    keyed = R.normal_slice("s-05", F.record("s-05", [{**proposal, "reason": "the reel swap is DOC-A.1-71's"}],
                                            edition=1985, derivation_refutes=[brief]))
    _rec, cells, _p = R.derivation_record(R.edition(1985), keyed.reads["1985-SR-IV.2.1.1-p1"],
                                          keyed.derivation_refutes["1985-SR-IV.2.1.1-p1"], {}, "claude: 2026-11-21", SCHEMA)
    expect("a register row's argument cites an A.1 determination as `A.1 item N`, never its row key (the register "
           "anchors a row on the key by a word search over the whole file)",
           "A.1 item 71's" in cells[3] and "DOC-A.1-71" not in cells[3])


def case_17_a_read_record_follows_a_recorded_move_of_its_rule(expect):
    """kb/Work PB6020: a regeneration renumbers p4 to p3 and p5 to p4 (one batch, simultaneous) after slice r-01 read
    both, r-02 re-read the old p4, and a later slice r-03 re-reads the new p4 (r-01's old p5). A read record is sealed
    by its digests, so `load` reads r-01's and r-02's reads under the new ids through the recorded move, which names
    them, BEFORE the second reads are resolved; r-03, which the move does not name, keeps its id. Before PB6020 a move left no record the reads could follow, and
    `audit_batch.py import-reads` refused every later import of the edition ("read rule(s) ... are no rule of the
    catalog")."""
    d = scratch_dir("lineage-reads-moves")
    p4, p5, p3 = "2002-GR-1.1.3-p4", "2002-GR-1.1.3-p5", "2002-GR-1.1.3-p3"
    docs = {"r-01": F.record("r-01", [F.read(p4), F.read(p5)], [F.refute(p4)]),
            "r-02": F.record("r-02", [{**F.read(p4, "DIFFERENT"), "to": [], "supersedes": "r-01"}]),
            "r-03": F.record("r-03", [{**F.read(p4, "DIFFERENT"), "to": [], "supersedes": "r-01"}])}
    for name, doc in docs.items():
        (d / f"{name}.json").write_text(json.dumps(doc), encoding="utf-8", newline="\n")
    moves = [[(p4, p3, frozenset({"r-01", "r-02"})), (p5, p4, frozenset({"r-01"}))]]
    got = {s.name: s for s in R.load(2002, d, moves)}
    expect.equal("the named slices read the moved rules under their new ids, once each (no chained rename of p5 to p3): "
                 "r-02 supersedes r-01's read of p3 and r-03 its read of p4", {n: sorted(s.reads) for n, s in got.items()},
                 {"r-01": [], "r-02": [p3], "r-03": [p4]})
    expect("each read carries its new id, and the superseded read's refute left with it",
           got["r-02"].reads[p3].rule_id == p3 and got["r-03"].reads[p4].rule_id == p4 and not got["r-01"].refutes)
    expect("the records themselves are unchanged: their digests still verify, and with no move the old ids read",
           sorted(sorted(s.reads) for s in R.load(2002, d, [], superseded=True)) == [[p4], [p4], [p4, p5]])
    expect("the readers a move records are the slices that read the old id before any second read takes one out",
           sorted(s.name for s in R.load(2002, d, [], superseded=True) if p4 in s.reads) == ["r-01", "r-02", "r-03"])

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
