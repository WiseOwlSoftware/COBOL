#!/usr/bin/env python3
"""The read records of an older edition's rules on planted reads (kb/Work PB5597; DESIGN-spec-conformance-review.md
section 10.6): the one normalization and its loud refusals, what a read becomes (a kept record only from a SAME its
refuter upheld, every screen flag answered for its detail, a witness at the row's own edition; a work item for every
other verdict), the import's durable records, the audit of stored records, and the band widening's plan and edits."""
import json
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
                                      "compiler-at-2002": "correct (probe p.cob)"}))
    expect.equal("a list of flags-answered lines: each trigger with the detail its head names, several per trigger",
                 r.answers, {"cross-reference": [("8.8.4", "read in 2002, the same")],
                             "changed-definition-term": [("procedure", "the division, not the term"),
                                                         ("class", "the same sense")]})
    expect("a string citation and the compiler's judgment are read", r.compiler == R.Judgment("correct", "correct (probe p.cob)"))
    m = R.normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"flags-answered": {"normalized: a b": "x", "ambiguous": "y"}}))
    expect.equal("a map of flags-answered: its key's detail, or none", m.answers,
                 {"normalized": [("a b", "x")], "ambiguous": [(None, "y")]})
    odd = R.normal_read("q-01", F.read("2002-GR-1.1.3-1", **{"compiler-at-2002": "outcome correct but on another ground"}))
    expect("a judgment naming none of correct, wrong, not probed is kept as written, its kind None",
           odd.compiler.kind is None)
    for label, case, needle in (
            ("a verdict outside the brief's", F.read("2002-GR-1.1.3-1", verdict="MAYBE"), "verdict"),
            ("no `to`", {**F.read("2002-GR-1.1.3-1"), "to": []}, "`to`"),
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
    s = R.normal_slice("p-01", F.record("p-01", [
        F.read("2002-GR-1.1.3-1"),                                                   # SAME, upheld, unflagged: KEPT
        F.read("2002-GR-1.1.3-2"),                                                   # SAME, no refuter record
        F.read("2002-GR-1.1.3-3"),                                                   # SAME, OVERTURNED, compiler correct
        F.read("2002-GR-1.1.3-4", "DIFFERENT", **{"compiler-at-2002": "wrong: p.cob prints 34 digits"}),
        F.read("2002-GR-1.1.3-5", "DOUBTFUL"),
        F.read("2002-GR-1.1.3-6", "DIFFERENT", **{"compiler-at-2002": "outcome correct on another ground"}),
        F.read("2002-GR-1.1.3-7"),                                                   # SAME, flagged, unanswered
        F.read("2002-GR-1.1.3-8", **{"flags-answered": ["cross-reference: 8.4.2 says the same"]}),   # answered as asked
        F.read("2002-GR-1.1.3-9", **{"flags-answered": ["cross-reference 9.9: another clause"]}),    # another detail
        F.read("2002-GR-1.1.3-10"),                                                  # SAME, upheld, no witness at 2002
    ], [F.refute(f"2002-GR-1.1.3-{n}") for n in (1, 7, 8, 9, 10)]
       + [F.refute("2002-GR-1.1.3-3", "OVERTURNED", probe="cd.cob", **{"compiler-at-2002": "correct"})],
        asked={"2002-GR-1.1.3-8": {"cross-reference": "8.4.2"}}))
    screen = {"marks": {"2002-GR-1.1.3-7": {"band-formats": "[1, 2] against [1]"},
                        "2002-GR-1.1.3-8": {"cross-reference": "8.4.2"},
                        "2002-GR-1.1.3-9": {"cross-reference": "8.4.2"}}}
    w = F.witnesses({f"GR-1.1.3-{n}": ["2002", "2023"] for n in range(1, 10)} | {"GR-1.1.3-10": ["2023"]})
    got = {d.rule_id.rsplit("-", 1)[1]: d for d in R.decide(ED, [s], screen, w)}
    expect("a SAME its refuter UPHELD, unflagged, witnessed at 2002 is KEPT, its source the read record",
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


def case_02_the_import_writes_the_durable_records_verbatim_or_nothing(expect):
    d = scratch_dir("lineage-reads")
    src, asked, out = d / "out", d / "in", d / "records"
    src.mkdir()
    asked.mkdir()
    (src / "p-01-read.json").write_text(json.dumps(F.record()["read"] | {"cases": [F.read("2002-GR-1.1.3-1")]}), encoding="utf-8", newline="\n")
    (src / "p-01-refute.json").write_text(json.dumps({"slice": "p-01", "cases": [F.refute("2002-GR-1.1.3-1")],
                                                      "disputed-different": []}), encoding="utf-8", newline="\n")
    (src / "q-01-read.json").write_text(json.dumps({"slice": "q-01", "cases": [F.read("2002-GR-1.1.3-2")]}), encoding="utf-8", newline="\n")
    (asked / "q-01.json").write_text(json.dumps({"ids": ["2002-GR-1.1.3-2"], "flags": {}}), encoding="utf-8", newline="\n")
    expect.equal("every slice that exists is imported, a missing refuter record is no error",
                 R.import_records(2002, src, asked, out), ["p-01", "q-01"])
    doc = json.loads((out / "p-01.json").read_text(encoding="utf-8"))
    expect("the record holds the sources verbatim with each one's digest",
           doc["read"]["cases"][0]["rule-id"] == "2002-GR-1.1.3-1" and set(doc["imported"]) == {"read", "refute"}
           and doc["imported"]["read"] == R.digest((src / "p-01-read.json").read_bytes()))
    expect.equal("an import of unchanged sources rewrites nothing", R.import_records(2002, src, asked, out), [])
    expect.equal("load normalizes every record", sorted(s.name for s in R.load(2002, out)), ["p-01", "q-01"])
    (src / "q-02-read.json").write_text(json.dumps({"slice": "q-02", "cases": [F.read("2002-GR-1.1.3-3", "NEW")]}),
                                        encoding="utf-8", newline="\n")
    before = sorted(p.name for p in out.iterdir())
    expect("a malformed slice refuses the whole import, naming slice and rule, and writes nothing",
           F.raises(lambda: R.import_records(2002, src, asked, out), "q-02", "2002-GR-1.1.3-3")
           and sorted(p.name for p in out.iterdir()) == before)
    (src / "q-02-read.json").write_text(json.dumps({"slice": "q-02", "cases": [F.read("2002-GR-9.9.9-9")]}),
                                        encoding="utf-8", newline="\n")
    expect("a read of a rule the edition's catalog does not hold refuses the import, and writes nothing",
           raises_unknown := F.raises(lambda: R.import_records(2002, src, asked, out, {"2002-GR-1.1.3-1", "2002-GR-1.1.3-2"}),
                                      "q-02", "2002-GR-9.9.9-9", "no rule of the 2002 catalog"))
    expect("(nothing written)", raises_unknown and sorted(p.name for p in out.iterdir()) == before)
    (src / "q-02-read.json").unlink()
    (src / "q-03-refute.json").write_text("{}", encoding="utf-8", newline="\n")
    expect("a refuter record with no reader's record is refused", F.raises(lambda: R.import_records(2002, src, asked, out),
                                                                         "q-03-refute.json"))
    (src / "q-03-refute.json").unlink()
    (out / "q-09.json").write_text(json.dumps(F.record("q-09", [F.read("2002-GR-1.1.3-1")])), encoding="utf-8", newline="\n")
    expect("a rule two slices read is refused at load", F.raises(lambda: R.load(2002, out), "read again"))


def case_03_the_audit_holds_a_stored_record_to_its_read(expect):
    s = R.normal_slice("p-01", F.record("p-01", [F.read("2002-GR-1.1.3-1"), F.read("2002-GR-1.1.3-2")],
                                      [F.refute("2002-GR-1.1.3-1"), F.refute("2002-GR-1.1.3-2", "OVERTURNED")]))

    def stored(rid, to=None, src="lineage-reads-2002/p-01.json", kind="KEPT"):
        return {"rule-id": rid, "next": {"kind": kind, "to": to or [rid.split("-", 1)[1]], "source": src}}

    w = F.witnesses({"GR-1.1.3-1": ["2002", "2023"], "GR-1.1.3-2": ["2002", "2023"], "GR-1.1.3-9": ["2002", "2023"],
                   "GR-1.1.3-8": ["2023"]})
    expect.equal("a stored KEPT record that is its read's decision stands, and one of another source witnessed at 2002",
                 R.stored_errors(ED, [stored("2002-GR-1.1.3-1"), stored("2002-GR-1.1.3-9", src="audit-2002/x")], [s], w), [])
    other = R.stored_errors(ED, [stored("2002-GR-1.1.3-8", src="audit-2002/x")], [s], w)
    expect("a stored KEPT record of ANY source with no witness at its own edition is red (2002 names own-edition-evidence)",
           len(other) == 1 and "own edition" in other[0])
    expect.equal("an edition whose records predate the rule (1985) is not held to it",
                 R.stored_errors(R.edition(1985), [stored("1985-GR-1.1.3-8", src="audit-1985/x")], [s], w), [])
    errs = R.stored_errors(ED, [stored("2002-GR-1.1.3-2"), stored("2002-GR-1.1.3-3"),
                                stored("2002-GR-1.1.3-1", to=["GR-9.9.9-9"])], [s], w)
    expect("a record whose read was overturned, whose source holds no read of it, or that carries into other rules is red",
           len(errs) == 3 and "OVERTURNED" in errs[0] and "holds no read" in errs[1] and "carries into" in errs[2])
    narrowed = F.witnesses({"GR-1.1.3-1": ["2023"]})
    expect("a witness whose band is later narrowed past the edition turns the stored record red",
           any("own edition" in e for e in R.stored_errors(ED, [stored("2002-GR-1.1.3-1")], [s], narrowed)))


def case_04_the_widening_plans_and_edits_only_what_can_run_at_the_edition(expect):
    order = ["85", "2002", "2014", "2023"]
    cases = {"cases/X/a": R.CaseInfo("cases/X/a", R.REPO, ("2023",), ("2023",), (), ("GR-1",), None),
             "cases/X/b": R.CaseInfo("cases/X/b", R.REPO, ("2023",), ("2023",), (), ("GR-2", "GR-LATE"), None),
             "cases/X/c": R.CaseInfo("cases/X/c", R.REPO, ("2023",), (), (), ("GR-3",), None, ("2023",)),
             "cases/X/d": R.CaseInfo("cases/X/d", R.REPO, ("2002",), ("2002",), (), ("GR-4",), None)}
    w = R.Witnesses(cases, {"GR-1": ["conformance:cases/X/a"], "GR-2": ["conformance:cases/X/b", "unit:T.M"],
                            "GR-3": ["conformance:cases/X/c"], "GR-4": ["conformance:cases/X/d"]},
                    {"GR-1": ["2002", "2023"], "GR-2": ["2002", "2023"], "GR-LATE": ["2023"], "GR-3": ["2002", "2023"],
                     "GR-4": ["2002", "2023"]})
    ds = [R.Decision(f"2002-GR-{n}", "p-01", (f"GR-{n}",), "s", refused=((R.NO_OWN_WITNESS, "x"),)) for n in (1, 2, 3)]
    ds.append(R.Decision("2002-GR-4", "p-01", ("GR-4",), "s", refused=((R.FLAG_UNANSWERED, "x"),)))
    plan = R.widen_plan(ED, ds, w)
    expect.equal("a case whose every cited rule exists at the edition is widenable, a refusal test too; one citing a later "
                 "rule is held by its band; a row refused for anything but its evidence is not planned",
                 {k: c["status"] for k, c in plan["cases"].items()},
                 {"cases/X/a": R.WIDEN, "cases/X/b": R.BAND, "cases/X/c": R.WIDEN_REFUSAL})
    expect.equal("rows: widenable, or needs an own witness when no case can run there", plan["counts"]["rows"],
                 {"needs-own-witness": 1, "widenable": 2})
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


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
