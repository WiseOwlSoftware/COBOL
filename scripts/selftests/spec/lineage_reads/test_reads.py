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


def case_00_the_one_normalization_reads_every_shape_and_refuses_what_it_cannot(expect):
    r = R.normal_read("q-01", F.read("2002-GR-1.1.3-1", citations="§2002:1.1 GR1",
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
           R.licence_problem(ED, R.normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"licence-2002": "none"})).licence))
    m = R.normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"flags-answered": {"normalized: a b": "x", "ambiguous": "y"}}))
    expect.equal("a map of flags-answered: its key's detail, or none", m.answers,
                 {"normalized": [("a b", "x")], "ambiguous": [(None, "y")]})
    expect("R1-4: an empty `to` (a rule with no pointer) is read", R.normal_read("t-annexA", {**F.read("2002-GR-A.1-1"),
                                                                                              "to": []}).to == ())
    odd = R.normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"compiler-at-2002": "outcome correct but on another ground"}))
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
               F.raises(lambda: R.normal_read("q-07", case), "q-07", "2002-GR-1.1.3-1", needle))
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
    plan = R.widen_plan(ED, ds, w, F.lineage([d.rule_id for d in ds]))
    expect.equal("a case whose every cited rule exists at the edition is widenable, a refusal test too; one citing a later "
                 "rule is held by its band; a row refused for anything but its evidence is not planned",
                 {k: c["status"] for k, c in plan["cases"].items()},
                 {"cases/X/a": R.WIDEN, "cases/X/b": R.BAND, "cases/X/c": R.WIDEN_REFUSAL, "cases/X/s": R.WIDEN_REFUSAL})
    expect.equal("R1-1: widening a refusal helps a syntax rule, never a general rule: that row needs an own witness",
                 {r["rule-id"]: r["state"] for r in plan["rows"]},
                 {"2002-GR-1": "widenable", "2002-GR-2": "needs-own-witness", "2002-GR-3": "needs-own-witness",
                  "2002-SR-5": "widenable"})
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


def _harness(results, code=1, refusal_out="error COBOL0900 here"):
    """A planted subprocess.run: run.py exits `code` writing `results` (None: writes nothing); the compiler refuses
    with `refusal_out`."""
    def run(cmd, **kw):
        if cmd[1].endswith("run.py"):
            if results is not None:
                out = pathlib.Path(cmd[cmd.index("--out") + 1])
                out.mkdir(parents=True, exist_ok=True)
                (out / "results.json").write_text(json.dumps({"tests": [{"edition": "2002", "id": i, "verdict": v}
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
    expect("a general rule is never diagnosable", not t.diagnosable("GR", "The clause shall not be specified twice."))
    sr = "2002-SR-2.1.2-9"
    s = R.normal_slice("p-01", F.record("p-01", [F.read(sr)], [F.refute(sr)]))
    lin = F.lineage([sr])
    lin = dataclasses.replace(lin, texts={sr: "The clauses may be written in any order."})
    got = R.decide(ED, [s], None, F.witnesses(diagnosed={F.twin(sr): ["2002"]}), lin)[0]
    expect("a granting syntax rule witnessed only by a refusal at 2002 is refused: no own-edition witness",
           [r for r, _ in got.refused] == [R.NO_OWN_WITNESS])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
