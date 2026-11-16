#!/usr/bin/env python3
"""The code reads of an older edition's rules on planted reads (kb/Work PB5759; DESIGN-spec-conformance-review.md section
10.6.1): every refusal of the import, each naming the slice, the rule and the field, and nothing written when any slice
is refused; each outcome of the one outcome function; the work and adjudication writers; the plan; and the audit of a
stored record (a hand edit, a digest's shape, a contradicted record with no note)."""
import json
import sys

import fixtures as F
import selftest_cases
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/code_reads.py


def case_00_a_well_formed_read_is_no_problem(expect):
    repo, _ = F.tree()
    ctx = F.context(repo.path)
    expect.equal("a CONFORMS read as the brief writes it", C.reader_problems(F.read(F.GR8), ctx), [])
    expect.equal("a PARTIAL read with its defect fields", C.reader_problems(F.read(F.GR12, "PARTIAL"), ctx), [])
    expect.equal("an amendment rule cites the amendment's own text (as the catalog's citation does)",
                 C.reader_problems(F.read(F.AR1), ctx), [])
    expect.equal("an empty edition-consulted that says how the reader looked",
                 C.reader_problems(F.read(F.GR8, **{"edition-consulted": [], "edition-search": "grep Edition"}), ctx), [])
    expect.equal("an upheld refutation", C.refuter_problems(F.refute(F.GR8)), [])


def case_01_every_malformed_reader_field_is_named(expect):
    repo, _ = F.tree()
    ctx = F.context(repo.path)
    for label, kw, needle in (
            ("a verdict outside the inventory's vocabulary (the brief's UNDECIDED)", {"verdict": "UNDECIDED"}, "`verdict`"),
            ("a code-location with a parenthetical note (the sample's shape)",
             {"code-location": "src/A.cs#B (the arm)"}, "`code-location`"),
            ("an empty code-location on a CONFORMS read", {"code-location": ""}, "`code-location` is empty"),
            ("no requirement", {"requirement": ""}, "`requirement`"),
            ("no notes", {"notes": None}, "`notes`"),
            ("no probe", {"probes": []}, "`probes`"),
            ("a probe without its file", {"probes": [{"class": "a", "printed": "", "expected": ""}]}, "a probe"),
            ("an edition-consulted entry with no boolean arm",
             {"edition-consulted": [{"site": "s", "switches": "w", "same-arm-as-latest": "yes"}]}, "edition-consulted"),
            ("an empty edition-consulted and no search", {"edition-consulted": []}, "edition-search"),
            ("seeded as text", {"seeded": "true"}, "`seeded`"),
            ("no citation", {"citations": []}, "`citations`"),
            ("a citation that does not parse", {"citations": [{"cite": "§XII.2.4", "check": "x"}]}, "does not parse"),
            ("a citation of another edition", {"citations": [{"cite": "§2023:7.2.2.5 GR3", "check": "x"}]},
             "not of the rule's own text"),
            ("a citation that does not check", {"citations": [{"cite": "§1985:XII.2.4 GR99", "check": "x"}]},
             "does not check"),
            ("a rule of no catalog row", {"rule-id": "1985-GR-IX.99.9-1"}, "no rule of the 1985 catalog")):
        got = C.reader_problems(F.read(F.GR8, **kw), ctx)
        expect(f"refused, naming the field: {label}", any(needle in e for e in got), str(got))
    expect("the amendment's §1989 is no citation of a 1985 rule",
           any("not of the rule's own text" in e for e in C.reader_problems(
               F.read(F.GR8, citations=[{"cite": "§1989:A.2.2 AR1", "check": "x"}]), ctx)))
    for label, kw, needle in (("no cause", {"cause": ""}, "`cause`"), ("no repro", {"repro": ""}, "`repro`"),
                              ("cause (b) and nothing said of the newest edition", {"cause": "b"}, "at-2023")):
        got = C.reader_problems(F.read(F.GR12, "PARTIAL", **kw), ctx)
        expect(f"a non-conforming read is refused: {label}", any(needle in e for e in got), str(got))
    for label, kw, needle in (("a refutation outside the vocabulary", {"refutation": "MAYBE"}, "`refutation`"),
                              ("an overturn that does not say the right verdict", {"refutation": "OVERTURNED",
                                                                                     "corrected": ""}, "right verdict"),
                              ("no evidence", {"evidence": ""}, "`evidence`"),
                              ("missed sites that are no list", {"edition-sites-missed": "none"}, "edition-sites-missed")):
        got = C.refuter_problems(F.refute(F.GR8, **kw))
        expect(f"a refuter's case is refused: {label}", any(needle in e for e in got), str(got))


def case_02_the_import_takes_every_slice_or_none(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR8), F.read(F.GR12, "PARTIAL")], [F.refute(F.GR8)])
    F.plant(reads, "s-02", [F.read(F.SR1)], [F.refute(F.SR1, "OVERTURNED")])
    F.plant(reads, "t-01", [F.read("2002-GR-1.1-1")], [], edition=2002)   # another edition's slice: not this import's
    records, imported, replaced = C.import_reads(ctx, reads, store, pin)
    C.write(records, store)
    got = C.load(ctx, store)
    expect.equal("the edition's slices only, one record per rule", sorted(got), sorted([F.GR8, F.GR12, F.SR1]))
    expect.equal("each outcome", {r: C.outcome(got[r], ctx) for r in got},
                 {F.GR8: C.CONFIRMED, F.GR12: C.CONTRADICTED, F.SR1: C.UNDECIDED})
    r = got[F.GR8]
    expect("the record holds the read verbatim, its pin, slice and the digests of its source files and parts",
           r["reader"] == F.read(F.GR8) and r["refuter"] == F.refute(F.GR8) and r["read"]["pin"] == pin
           and r["read"]["slice"] == "s-01" and set(r["read"]["sources"]) == {"reader", "refuter"}
           and r["read"]["imported"] == {"reader": C.lineage_reads.digest(r["reader"]),
                                         "refuter": C.lineage_reads.digest(r["refuter"])})
    expect("a non-conforming read holds no refuter's part", "refuter" not in got[F.GR12])
    expect("the file is the store's one spelling, LF", (store / f"{F.GR8}.json").read_bytes()
           == C.inventory_store.render_record(r).encode("utf-8"))
    expect("a second read of a recorded rule is refused without --replace",
           F.refused(lambda: C.import_reads(ctx, reads, store, pin), F.GR8, "already recorded"))
    _, _, replaced = C.import_reads(ctx, reads, store, pin, replace=True)
    expect.equal("--replace says what it replaced", replaced, sorted([F.GR8, F.GR12, F.SR1]))
    bad = repo.path / "bad"
    F.plant(bad, "s-01", [F.read(F.GR8), F.read(F.OWN)], [F.refute(F.GR8), F.refute("1985-GR-XII.2.4-77")])
    F.plant(bad, "s-02", [F.read(F.GR12), F.read(F.SR1, "PARTIAL"), F.read(F.GR8)], [F.refute(F.SR1)])
    (bad / "s-03-refute.json").write_text('{"cases": []}', encoding="utf-8")
    empty = repo.path / "empty-store"
    expect("every problem of every slice is named together, and nothing is written",
           F.refused(lambda: C.import_reads(ctx, bad, empty, pin),
                     f"s-01: {F.OWN}: its row is not", "attacks a rule the reader did not read","s-02: " + F.GR12 + ": a CONFORMS read with no refuter",
                     "attacks only CONFORMS", f"s-02: {F.GR8}: read again", "s-03-refute.json: a refuter's file with no reader")
           and not empty.exists())
    expect("a pin that is no commit of the repository is refused",
           F.refused(lambda: C.import_reads(ctx, reads, empty, "0" * 40), "no commit"))


def case_03_the_outcome_is_one_function(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    rec = C.record_of(F.read(F.GR8), F.refute(F.GR8), "0b2e58f8a250", pin, "s-01", {"reader": "0" * 64})
    expect.equal("CONFORMS upheld is confirmed", C.outcome(rec, ctx), C.CONFIRMED)
    for ref in ("OVERTURNED", "DOUBTFUL"):
        o = C.outcome({**rec, "refuter": F.refute(F.GR8, ref)}, ctx)
        expect.equal(f"CONFORMS {ref} with no adjudication is undecided", o, C.UNDECIDED)
    over = {**rec, "refuter": F.refute(F.GR8, "OVERTURNED")}
    expect.equal("an adjudication that is not CONFORMS contradicts",
                 C.outcome({**over, "adjudication": {"verdict": "PARTIAL", "reason": "r", "ref": "x"}}, ctx), C.CONTRADICTED)
    expect.equal("an adjudication of CONFORMS confirms",
                 C.outcome({**over, "adjudication": {"verdict": "CONFORMS", "reason": "r", "ref": "x"}}, ctx), C.CONFIRMED)
    expect.equal("a read whose rule's text moved is stale, whatever it says",
                 C.outcome(rec, F.context(repo.path, digest="111111111111")), C.STALE)


def case_04_work_and_adjudication_are_written_by_the_module(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR8), F.read(F.GR12, "PARTIAL")], [F.refute(F.GR8, "OVERTURNED")])
    C.write(C.import_reads(ctx, reads, store, pin)[0], store)
    expect("an adjudication that is not CONFORMS names its note",
           F.refused(lambda: C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/PB1/adj.md", None), "--work"))
    expect("an adjudication's ref is a file of the tree",
           F.refused(lambda: C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/none.md", "PB1"), "no file"))
    expect("a note that is not in kb/Work is refused",
           F.refused(lambda: C.attach_work(ctx, store, F.GR12, "PB2"), "no note"))
    C.write(C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/PB1/adj.md", "PB1"), store)
    C.write(C.attach_work(ctx, store, F.GR12, "PB1"), store)
    got = C.load(ctx, store)
    expect("the adjudication and the note are recorded; both rows are contradicted",
           got[F.GR8]["adjudication"] == {"verdict": "PARTIAL", "reason": "why", "ref": "kb/Work/PB1/adj.md"}
           and got[F.GR8]["work"] == got[F.GR12]["work"] == "PB1"
           and [C.outcome(got[r], ctx) for r in (F.GR8, F.GR12)] == [C.CONTRADICTED] * 2)
    expect("a second adjudication is refused without --replace",
           F.refused(lambda: C.adjudicate(ctx, store, F.GR8, "CONFORMS", "w", "kb/Work/PB1/adj.md", None), "already"))
    expect.equal("the audit finds nothing wrong with them",
                 {r: C.record_problems(got[r], ctx) for r in got}, {F.GR8: [], F.GR12: []})


def case_05_the_audit_holds_a_stored_record_to_its_import(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    rec = C.record_of(F.read(F.GR12, "PARTIAL"), None, "0b2e58f8a250", pin, "s-01", {"reader": "a" * 64})
    expect("a contradicted record with no note is red",
           any("`work`" in e for e in C.record_problems(rec, ctx)))
    ok = {**rec, "work": "PB1"}
    expect.equal("with its note it stands", C.record_problems(ok, ctx), [])
    for label, broken, needle in (
            ("a held part edited by hand", {**ok, "reader": {**ok["reader"], "notes": "rewritten"}}, "edited since"),
            ("a source digest of the wrong shape", {**ok, "read": {**ok["read"], "sources": {"reader": "abc"}}},
             "read.sources"),
            ("a pin that is no commit id", {**ok, "read": {**ok["read"], "pin": "HEAD"}}, "read.pin"),
            ("a rule of no catalog row", {**ok, "rule-id": "1985-GR-IX.99.9-1"}, "no rule"),
            ("a key no record holds", {**ok, "state": "OK"}, "no record holds"),
            ("a refuter's case on a non-conforming read", {**ok, "refuter": F.refute(F.GR12)}, "attacks only CONFORMS"),
            ("a CONFORMS read with no refuter",
             C.record_of(F.read(F.GR8), None, "0b2e58f8a250", pin, "s-01", {"reader": "a" * 64}), "no refuter"),
            ("an adjudication whose ref is no file", {**ok, "adjudication": {"verdict": "PARTIAL", "reason": "r",
                                                                             "ref": "kb/Work/none.md"}}, "no file")):
        got = C.record_problems(broken, ctx)
        expect(f"red: {label}", any(needle in e for e in got), str(got))


def case_06_the_plan_is_the_kept_rows_no_read_decides(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    confirmed = C.record_of(F.read(F.GR8), F.refute(F.GR8), "0b2e58f8a250", pin, "s", {"reader": "a" * 64})
    undecided = C.record_of(F.read(F.GR12), F.refute(F.GR12, "OVERTURNED"), "0b2e58f8a250", pin, "s",
                            {"reader": "a" * 64})
    records = {F.GR8: confirmed, F.GR12: undecided}
    expect.equal("every KEPT row but the confirmed one (an undecided read is still owed; a row with its own verdict "
                 "is none of the plan's)", C.owed(ctx, records), [F.GR12, F.SR1, F.AR1])
    expect.equal("--witnessed keeps the rows with a test-ref of their own", C.owed(ctx, records, witnessed=True),
                 [F.GR12, F.AR1])
    expect.equal("a stale confirmed read is owed again", C.owed(F.context(repo.path, "111111111111"), records),
                 [F.GR8, F.GR12, F.SR1, F.AR1])
    expect.equal("the counts", C.counts(ctx, records),
                 {C.CONFIRMED: 1, C.CONTRADICTED: 0, C.UNDECIDED: 1, C.STALE: 0, "owed": 3})


def case_07_the_newest_edition_has_no_store(expect):
    expect("the newest held edition is refused",
           F.refused(lambda: C.Context.of(F.SCHEMA.newest.year, rows=[]), "newest held edition"))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
