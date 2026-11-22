#!/usr/bin/env python3
"""The code reads of an older edition's rules on planted reads (kb/Work PB5759; DESIGN-spec-conformance-review.md section
10.6.1): every refusal of the import, each naming the slice, the rule and the field, and nothing written when any slice
is refused; each outcome of the one outcome function; the work and adjudication writers; the plan; and the audit of a
stored record (a hand edit, a digest's shape, a contradicted record with no note)."""
import contextlib
import inspect
import io
import json
import sys

import fixtures as F
import selftest_cases
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/code_reads.py
import code_read_landing as L  # noqa: E402  (the landing check, kb/Work PB6056; scripts/spec is on the path `subject` set)


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
            ("a verdict outside the inventory's vocabulary and not UNDECIDED", {"verdict": "Undecided"}, "`verdict`"),
            ("a verdict outside the vocabulary (a refuter's word)", {"verdict": "UPHELD"}, "`verdict`"),
            ("a code-location with a parenthetical note left in it (the import splits it; a stored record holds none)",
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
            ("a read that cites only another edition (PB5842)",
             {"citations": [{"cite": "§2023:13.18.8.4 GR3", "check": "x"}]}, "no citation is of the rule's own text"),
            ("another edition's citation that does not check (PB5842)",
             {"citations": [{"cite": "§1985:XII.2.4 GR8", "check": "x"}, {"cite": "§2023:13.18.8.4 GR99", "check": "x"}]},  # citation-audit: example
             "does not check"),
            ("a citation of an edition the repository does not hold", {"citations": [
                {"cite": "§1985:XII.2.4 GR8", "check": "x"}, {"cite": "§2014:13.18.8.4 GR3", "check": "x"}]}, "does not hold"),
            ("a citation that does not check", {"citations": [{"cite": "§1985:XII.2.4 GR99", "check": "x"}]},  # citation-audit: example
             "does not check"),
            ("a rule of no catalog row", {"rule-id": "1985-GR-IX.99.9-1"}, "no rule of the 1985 catalog")):
        got = C.reader_problems(F.read(F.GR8, **kw), ctx)
        expect(f"refused, naming the field: {label}", any(needle in e for e in got), str(got))
    expect.equal("the amendment's §1989 anchors a read (PB5842)", C.reader_problems(
        F.read(F.GR8, citations=[{"cite": "§1989:A.2.2 AR1", "check": "x"}]), ctx), [])
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
    C.store.write(records, store)
    got = C.load(ctx, store)
    expect.equal("the edition's slices only, one record per rule", sorted(got), sorted([F.GR8, F.GR12, F.SR1]))
    expect.equal("each outcome", {r: C.outcome(got[r], ctx) for r in got},
                 {F.GR8: C.CONFIRMED, F.GR12: C.CONTRADICTED, F.SR1: C.store.UNDECIDED})
    r = got[F.GR8]
    expect("the record holds the read verbatim, its pin, slice and the digests of its source files and parts",
           r["reader"] == F.read(F.GR8) and r["refuter"] == F.refute(F.GR8) and r["read"]["pin"] == pin
           and r["read"]["slice"] == "s-01" and set(r["read"]["sources"]) == {"reader", "refuter"}
           and r["read"]["imported"] == {"reader": C.lineage_reads.digest(r["reader"]),
                                         "refuter": C.lineage_reads.digest(r["refuter"])})
    expect("a non-conforming read holds no refuter's part", "refuter" not in got[F.GR12])
    expect("the file is the store's one spelling, LF", (store / f"{F.GR8}.json").read_bytes()
           == C.store.inventory_store.render_record(r).encode("utf-8"))
    expect("a second read of a recorded rule is refused without --replace",
           F.refused(lambda: C.import_reads(ctx, reads, store, pin), F.GR8, "already recorded"))
    _, _, replaced = C.import_reads(ctx, reads, store, pin, replace=True)
    expect.equal("--replace says what it replaced", replaced, sorted([F.GR8, F.GR12, F.SR1]))
    bad = repo.path / "bad"
    F.plant(bad, "s-01", [F.read(F.GR8), F.read(F.OWN)], [F.refute(F.GR8), F.refute("1985-GR-XII.2.4-77")])
    F.plant(bad, "s-02", [F.read(F.GR12), F.read(F.SR1, "PARTIAL"), F.read(F.GR8)], [F.refute(F.SR1)])
    (bad / "s-03-refute.json").write_text('{"cases": []}', encoding="utf-8", newline="\n")
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
        expect.equal(f"CONFORMS {ref} with no adjudication is undecided", o, C.store.UNDECIDED)
    over = {**rec, "refuter": F.refute(F.GR8, "OVERTURNED")}
    expect.equal("an adjudication that is not CONFORMS contradicts",
                 C.outcome({**over, "adjudication": {"verdict": "PARTIAL", "reason": "r", "ref": "x"}}, ctx), C.CONTRADICTED)
    expect.equal("an adjudication of CONFORMS confirms",
                 C.outcome({**over, "adjudication": {"verdict": "CONFORMS", "reason": "r", "ref": "x"}}, ctx), C.CONFIRMED)
    expect.equal("a read whose rule's text moved is stale, whatever it says",
                 C.outcome(rec, F.context(repo.path, digest="111111111111")), C.store.STALE)


def case_04_work_and_adjudication_are_written_by_the_module(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR8), F.read(F.GR12, "PARTIAL")], [F.refute(F.GR8, "OVERTURNED")])
    C.store.write(C.import_reads(ctx, reads, store, pin)[0], store)
    expect("an adjudication that is not CONFORMS names its note",
           F.refused(lambda: C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/PB1/adj.md", None), "--work"))
    expect("an adjudication's ref is a file of the tree",
           F.refused(lambda: C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/none.md", "PB1"), "no file"))
    expect("a note that is not in kb/Work is refused",
           F.refused(lambda: C.attach_work(ctx, store, F.GR12, "PB2"), "no note"))
    C.store.write(C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/PB1/adj.md", "PB1"), store)
    C.store.write(C.attach_work(ctx, store, F.GR12, "PB1"), store)
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
    expect.equal("the counts", C.store.counts(ctx.rows, ctx.schema, ctx.shared, records),
                 {C.CONFIRMED: 1, C.CONTRADICTED: 0, C.store.UNDECIDED: 1, C.store.STALE: 0, "owed": 3})


def case_07_the_newest_edition_has_no_store(expect):
    expect("the newest held edition is refused",
           F.refused(lambda: C.Context.of(F.SCHEMA.newest.year, rows=[]), "newest held edition"))


def case_08_a_remark_after_a_location_is_kept_beside_it(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    a, b, c = "src/A.cs#B", "src/C.cs#D", "src/E.cs"
    ok = {
        f"{a} (the 85 arm)": ({a: "the 85 arm"}, [a]),
        f"{a}; {b} (TAN row: Fixed 1..1, 'n'); {c}": ({b: "TAN row: Fixed 1..1, 'n'"}, [a, b, c]),
        f"{c} (FD clause loop; reportClause arm)": ({c: "FD clause loop; reportClause arm"}, [c]),
        f"{a} (a (nested) remark)": ({a: "a (nested) remark"}, [a]),
        f"{a}\t(tab before it)": ({a: "tab before it"}, [a]),
        f"{a}; {b}": ({}, [a, b]),
    }
    for text, (remarks, sites) in ok.items():
        got, problems = C.take_remarks(F.read(F.GR8, **{"code-location": text}), ctx)
        expect.equal(f"{text!r}: no problem", problems, [])
        expect.equal(f"{text!r}: the locations alone, in the inventory's shape",
                     ctx.schema.split(got["code-location"], ctx.schema.code_location_sep), sites)
        expect.equal(f"{text!r}: the remarks by location", got.get(C.REMARKS_KEY, {}), remarks)
        if remarks:
            expect.equal(f"{text!r}: the stored form stands", C.reader_problems(got, ctx), [])
    for label, text, needle in (
            ("a remark with no location", "(the arm)", "a remark with no location"),
            ("a remark with no location after a good one", f"{a}; (the arm)", "a remark with no location"),
            ("two remarks", f"{a} (one) (two)", "text after the closing parenthesis"),
            ("text after the closing parenthesis", f"{a} (one) and more", "text after the closing parenthesis"),
            ("an unclosed parenthesis", f"{a} (one", "never closed"),
            ("a closing parenthesis alone", f"{a} one)", "no '('"),
            ("an empty remark", f"{a} ( )", "empty remark"),
            ("a remark glued to its location", f"{a}(one)", "white space"),
            ("the same location with two remarks", f"{a} (one); {a} (two)", "two remarks")):
        got, problems = C.take_remarks(F.read(F.GR8, **{"code-location": text}), ctx)
        expect(f"refused, naming the text: {label}", any(needle in e and ("(" in e or ")" in e) for e in problems),
               str(problems))
    got, problems = C.take_remarks(F.read(F.GR8, **{"code-location": f"{a} (x)", C.REMARKS_KEY: {a: "y"}}), ctx)
    expect("a reader's file that already holds the derived key is refused", any("derived" in e for e in problems),
           str(problems))
    # the stored form is held to its own shape: a remark for a location the list does not hold, and an empty remark
    for label, remarks in (("a remark of no listed location", {"src/Z.cs#Q": "x"}), ("an empty remark", {
            "src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs#BindEntry": " "}), ("a map that is no map", ["x"])):
        got = C.reader_problems(F.read(F.GR8, **{C.REMARKS_KEY: remarks}), ctx)
        expect(f"the audit refuses a stored `location-remarks`: {label}", any(C.REMARKS_KEY in e for e in got), str(got))
    # through the import: a remark is kept, a refusal names the row and the text, and nothing is written
    reads, store, bad_store = repo.path / "reads", repo.path / "store", repo.path / "bad-store"
    loc = "src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs#BindEntry"
    F.plant(reads, "s-01", [F.read(F.GR8, **{"code-location": f"{loc} (the 85 arm)"})], [F.refute(F.GR8)])
    records, _, _ = C.import_reads(ctx, reads, store, pin)
    r = records[F.GR8]
    expect("the record holds the location and, beside it, the remark",
           r["reader"]["code-location"] == loc and r["reader"][C.REMARKS_KEY] == {loc: "the 85 arm"})
    expect.equal("the record stands in the audit", C.record_problems(r, ctx), [])
    expect("the digest of the held reader is the digest of the part the record holds",
           r["read"]["imported"]["reader"] == C.lineage_reads.digest(r["reader"]))
    expect("the remark decides nothing: the outcome is the one without it",
           C.outcome(r, ctx) == C.CONFIRMED == C.outcome(C.record_of(F.read(F.GR8), F.refute(F.GR8), r["rule-digest"],
                                                                   pin, "s-01", r["read"]["sources"]), ctx))
    bad = repo.path / "bad"
    F.plant(bad, "s-01", [F.read(F.GR8, **{"code-location": f"{loc} (one) (two)"})], [F.refute(F.GR8)])
    expect("an import refuses the row, naming it and the text, and writes nothing",
           F.refused(lambda: C.import_reads(ctx, bad, bad_store, pin), f"s-01: {F.GR8}", "(one) (two)")
           and not bad_store.exists())
    F.plant(bad, "s-01", [F.read(F.GR8, **{"code-location": "src/Nowhere Such (the arm)"})], [F.refute(F.GR8)])
    expect("a location of no shape is refused whatever its remark",
           F.refused(lambda: C.import_reads(ctx, bad, bad_store, pin), f"s-01: {F.GR8}", "`code-location`"))


def case_09_a_reader_who_did_not_decide_is_undecided(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    und = F.read(F.GR8, "UNDECIDED")
    expect.equal("an UNDECIDED reader owes no defect fields", C.reader_problems(und, ctx), [])
    expect.equal("nor a location", C.reader_problems({**und, "code-location": ""}, ctx), [])
    expect("every other field is still required",
           any("`requirement`" in e for e in C.reader_problems({**und, "requirement": ""}, ctx)))
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [und, F.read(F.GR12, "UNDECIDED"), F.read(F.SR1, "UNDECIDED")],
            [F.refute(F.GR12), F.refute(F.SR1, "OVERTURNED")])
    records, _, _ = C.import_reads(ctx, reads, store, pin)
    expect.equal("UNDECIDED whatever the refuter says (none, UPHELD, OVERTURNED)",
                 [C.outcome(records[r], ctx) for r in (F.GR8, F.GR12, F.SR1)], [C.store.UNDECIDED] * 3)
    expect.equal("the stored record stands in the audit", [C.record_problems(records[r], ctx) for r in records],
                 [[], [], []])
    expect.equal("counted where undecided records are counted",
                 C.store.counts(ctx.rows, ctx.schema, ctx.shared, records),
                 {C.CONFIRMED: 0, C.CONTRADICTED: 0, C.store.UNDECIDED: 3, C.store.STALE: 0, "owed": 4})
    expect.equal("plan still lists each row as owed a read", C.owed(ctx, records), [F.GR8, F.GR12, F.SR1, F.AR1])
    expect("an adjudication still decides the record",
           C.outcome({**records[F.GR8], "adjudication": {"verdict": "CONFORMS", "reason": "r", "ref": "x"}}, ctx)
           == C.CONFIRMED)
    bad, empty = repo.path / "bad", repo.path / "empty-store"
    F.plant(bad, "s-01", [F.read(F.GR8, "Undecided")], [])
    expect("any other word outside the vocabulary is still refused",
           F.refused(lambda: C.import_reads(ctx, bad, empty, pin), f"s-01: {F.GR8}", "`verdict`") and not empty.exists())
    F.plant(bad, "s-01", [F.read(F.GR8, "UNDECIDED")], [F.refute(F.GR8, "UPHELD", **{"rule-id": F.GR12})])
    expect("a refuter's case of a rule the reader did not read is still refused",
           F.refused(lambda: C.import_reads(ctx, bad, empty, pin), "attacks a rule the reader did not read"))


def case_10_a_citation_of_another_held_edition_is_accepted_and_checked(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    both = F.read(F.GR12, "PARTIAL", cause="b", **{"at-2023": "the same at 2023", "citations": [
        {"cite": "§1985:XII.2.4 GR12", "check": "x"}, {"cite": "§2023:13.18.8.4 GR3", "check": "x"}]})
    expect.equal("a read citing its own edition and 2023 stands", C.reader_problems(both, ctx), [])
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [both])
    records, imported, _ = C.import_reads(ctx, reads, store, pin)
    expect.equal("it is imported", imported, [F.GR12])
    only = F.read(F.GR12, "PARTIAL", cause="b", **{"at-2023": "same", "citations": [
        {"cite": "§2023:13.18.8.4 GR3", "check": "x"}]})
    got = C.reader_problems(only, ctx)
    expect("a read citing only 2023 is refused, naming the rule", any(
        "no citation is of the rule's own text" in e and F.GR12 in e for e in got), str(got))
    bad = F.read(F.GR12, "PARTIAL", cause="b", **{"at-2023": "same", "citations": [
        {"cite": "§1985:XII.2.4 GR12", "check": "x"}, {"cite": "§2023:13.18.8.4 GR99", "check": "x"}]})  # citation-audit: example
    got = C.reader_problems(bad, ctx)
    expect("a 2023 citation that does not check is refused beside a good 1985 one", any(
        "GR99" in e and "does not check" in e for e in got), str(got))
    # an import through the slice refuses the read that cites only another edition and writes nothing
    refused_store = repo.path / "refused-store"
    F.plant(reads, "s-02", [only])
    expect("the import refuses a read that cites only another edition, naming the slice and the rule",
           F.refused(lambda: C.import_reads(ctx, reads, refused_store, pin), "s-02", F.GR12, "no citation is of the rule's own")
           and not refused_store.exists())


def case_11_withdraw_removes_a_record_and_the_row_is_owed_again(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR8), F.read(F.GR12, "PARTIAL")], [F.refute(F.GR8)])
    C.store.write(C.import_reads(ctx, reads, store, pin)[0], store)
    expect("the confirmed row is not owed", F.GR8 not in C.owed(ctx, C.load(ctx, store)))
    records, removed = C.withdraw(ctx, store, F.GR8, "its location resolves nowhere")
    C.store.write(records, store)
    expect("the record removed is returned whole", removed["rule-id"] == F.GR8 and removed["reader"]["verdict"] == "CONFORMS")
    left = C.load(ctx, store)
    expect.equal("the store holds the other record only", sorted(left), [F.GR12])
    expect("the row is owed a read again", F.GR8 in C.owed(ctx, left))
    expect("the file is gone", not (store / f"{F.GR8}.json").exists())
    expect("a rule with no record is refused", F.refused(lambda: C.withdraw(ctx, store, F.GR8, "r"), "no code read"))
    expect("an empty reason is refused", F.refused(lambda: C.withdraw(ctx, store, F.GR12, "  "), "--reason is empty"))
    expect("a refused withdrawal removed nothing", sorted(C.load(ctx, store)) == [F.GR12])


def case_12_the_plan_leaves_out_a_row_its_own_derivation_closed(expect):
    repo, _ = F.tree()
    base = F.rows()
    closed = {**base[0], "rule-id": "1985-GR-DRV-1", "state": "OK", "test-ref": ""}
    open_ = {**base[0], "rule-id": "1985-GR-DRV-2", "state": "GAP", "test-ref": ""}
    ok_witnessed = {**base[0], "rule-id": "1985-GR-DRV-3", "state": "OK", "test-ref": "conformance:cases/x/y"}
    rows = {r["rule-id"]: r for r in base + [closed, open_, ok_witnessed]}
    got = C.store.owed(rows, F.SCHEMA, {}, {})
    expect("a chained row OK with no test-ref of its own is not owed", "1985-GR-DRV-1" not in got)
    expect("a chained row whose derivation does not stand (GAP) is owed", "1985-GR-DRV-2" in got)
    expect("a chained row OK on a test-ref of its own is still owed", "1985-GR-DRV-3" in got)
    expect.equal("the count of rows owed follows the plan", C.store.counts(rows, F.SCHEMA, {}, {})["owed"], len(got))
    expect("--witnessed leaves the closed row out too", "1985-GR-DRV-1" not in C.store.owed(rows, F.SCHEMA, {}, {}, True))


def case_13_a_contradicted_read_whose_fix_has_landed_is_owed_a_re_read(expect):
    """kb/Work PB5854: `fixed_since` and the refusal that makes it total, on planted notes at a pin and in the tree."""
    repo, _ = F.tree()   # PB1 open
    repo.commit("a note landed at the pin, and one open", {"kb/Work/PB3.md": F.note("PB3", "landed"),
                                                           "kb/Work/PB4.md": F.note("PB4")})
    pin = repo.git("rev-parse", "HEAD").strip()   # the read's pin: PB1 open, PB3 landed, PB4 open, PB2 and PB5 absent
    repo.commit("the fixes land", {"kb/Work/PB1.md": F.note("PB1", "landed"), "kb/Work/PB2.md": F.note("PB2", "landed"),
                                   "kb/Work/PB5.md": F.note("PB5", "wontfix")})
    ctx = F.context(repo.path)

    def contradicted(rid, work):
        return {**C.record_of(F.read(rid, "PARTIAL"), None, "0b2e58f8a250", pin, "s", {"reader": "a" * 64}), "work": work}
    landed_since = contradicted(F.GR12, "PB1")
    expect("a contradicted read whose note is landed in the tree and was open at the pin is fixed since",
           C.fixed_since(landed_since, ctx))
    expect("... a note ABSENT at the pin and landed in the tree is fixed since too", C.fixed_since(contradicted(F.GR12, "PB2"), ctx))
    expect("a note still open is not", not C.fixed_since(contradicted(F.GR12, "PB4"), ctx))
    expect("a note in some other status (not landed) is not", not C.fixed_since(contradicted(F.GR12, "PB5"), ctx))
    confirmed = {**C.record_of(F.read(F.GR8), F.refute(F.GR8), "0b2e58f8a250", pin, "s", {"reader": "a" * 64}), "work": "PB1"}
    expect("a CONFIRMED record that names a landed note is not", not C.fixed_since(confirmed, ctx))
    expect("a contradicted record that names a note landed AT its pin is not fixed 'since'",
           not C.fixed_since(contradicted(F.GR12, "PB3"), ctx))
    records = {F.GR12: landed_since, F.GR8: confirmed, F.SR1: contradicted(F.SR1, "PB4")}
    expect.equal("`reread` lists the fixed-since rows, with the note", [(r.rule_id, r.fixed, r.moved) for r in C.reread(ctx, records)],
                 [(F.GR12, "PB1", [])])
    expect.equal("`--witnessed` keeps those with a test-ref of their own (SR1 has none)",
                 [r.rule_id for r in C.reread(ctx, {**records, F.SR1: contradicted(F.SR1, "PB1")}, witnessed=True)], [F.GR12])
    expect.equal("the rows owed a read are the plan's other group: a contradicted row is decided, fixed since or not",
                 C.owed(ctx, records), [F.AR1])
    expect.equal("the store's counts still count the fixed-since record as contradicted",
                 C.store.counts(ctx.rows, ctx.schema, ctx.shared, records)[C.CONTRADICTED], 2)
    # the refusal: a note already landed at the read's pin is no mechanism of a contradiction read after it
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR12, "PARTIAL"), F.read(F.GR8, "PARTIAL")], [])
    C.store.write(C.import_reads(ctx, reads, store, pin)[0], store)
    expect("`work` refuses a note landed at the pin, saying to file a new note",
           F.refused(lambda: C.attach_work(ctx, store, F.GR12, "PB3"), "already `landed`", "NEW kb/Work note"))
    expect("`adjudicate` refuses it too",
           F.refused(lambda: C.adjudicate(ctx, store, F.GR8, "PARTIAL", "why", "kb/Work/PB1/adj.md", "PB3"),
                     "already `landed`"))
    expect("a note landed since the read is accepted by `work`", C.attach_work(ctx, store, F.GR12, "PB1")[F.GR12]["work"] == "PB1")
    expect("the audit's question is red on a stored record that holds a note landed at its pin",
           any("already `landed`" in e for e in C.record_problems(contradicted(F.GR12, "PB3"), ctx)))
    expect.equal("... and silent on one fixed since (information, never red)", C.record_problems(landed_since, ctx), [])
    expect("a pin that is no commit is no answer of `absent`: it refuses",
           F.refused(lambda: C.status_at("PB1", "0" * 40, repo.path), "no commit"))


def case_14_an_import_resolves_every_location_at_its_pin(expect):
    """kb/Work PB5863: a read is evidence AT ITS PIN, so the import resolves each `code-location` entry in the tree of the
    pin, by the gate's own rule (`code_location`), and refuses the slice when one does not resolve there."""
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    bad, empty = repo.path / "bad", repo.path / "empty-store"
    for label, loc, needle in (
            ("a member the file does not hold", f"{F.CODE_FILE}#NoSuchMember", "'NoSuchMember' is not in that file's code"),
            ("a member only a comment holds", f"{F.CODE_FILE}#OnlyInAComment", "'OnlyInAComment' is not in that file's code"),
            ("a file the pin does not hold", "src/Gone.cs#BindEntry", "no such file"),
            ("a member of a type that does not declare it", f"{F.CODE_FILE}#Other.BindEntry", "is not in that file's code")):
        F.plant(bad, "s-01", [F.read(F.GR8, **{"code-location": f"{F.LOCATION}; {loc}"})], [F.refute(F.GR8)])
        expect(f"refused, naming the slice, the rule, the entry and the pin: {label}",
               F.refused(lambda: C.import_reads(ctx, bad, empty, pin), f"s-01: {F.GR8}", loc, needle, pin[:10])
               and not empty.exists())
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR8, **{"code-location": f"{F.LOCATION} (the 85 arm)"})], [F.refute(F.GR8)])
    expect("a remark after the entry is no part of the location the pin resolves",
           F.GR8 in C.import_reads(ctx, reads, store, pin)[0])
    repo.commit("a member added after the read", {F.CODE_FILE: F.CODE.replace(
        "BindEntry() { }", "BindEntry() { }\n    internal void BindLater() { }")})
    F.plant(bad, "s-01", [F.read(F.GR8, **{"code-location": f"{F.CODE_FILE}#BindLater"})], [F.refute(F.GR8)])
    expect("a member the working tree holds and the pin does not is refused: the read cannot have read it",
           F.refused(lambda: C.import_reads(ctx, bad, empty, pin), f"s-01: {F.GR8}", "'BindLater'", pin[:10]))


def case_15_a_read_whose_code_moved_is_owed_a_re_read_never_a_red(expect):
    """kb/Work PB5863: the tree moving under a read is no failure of the read; `moved_since` lists it in the plan's re-read
    group, beside a read whose fix landed, each with its reason, and the audit stays green."""
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    reads, store = repo.path / "reads", repo.path / "store"
    F.plant(reads, "s-01", [F.read(F.GR8), F.read(F.GR12, "PARTIAL"), F.read(F.SR1)],
            [F.refute(F.GR8), F.refute(F.SR1, "OVERTURNED")])
    C.store.write(C.import_reads(ctx, reads, store, pin)[0], store)
    C.store.write(C.attach_work(ctx, store, F.GR12, "PB1"), store)
    records = C.load(ctx, store)
    expect.equal("nothing has moved and nothing is fixed: no re-read is owed", C.reread(ctx, records), [])
    repo.commit("a fix renames the member and lands its note", {
        F.CODE_FILE: F.CODE.replace("BindEntry", "BindItem"), "kb/Work/PB1.md": F.note("PB1", "landed")})
    expect("a read made at the pin still imports: its entry resolves where the code was read",
           F.GR8 in C.import_reads(ctx, reads, repo.path / "store-2", pin)[0])
    got = C.reread(ctx, records)
    expect.equal("the confirmed and the contradicted read are owed a re-read, each with its reasons; the undecided one is "
                 "in the owed group already", [(r.rule_id, r.fixed, len(r.moved)) for r in got],
                 [(F.GR8, None, 1), (F.GR12, "PB1", 1)])
    expect("the moved reason names the entry and the member", all(F.LOCATION in r.moved[0] and "'BindEntry'" in r.moved[0]
                                                                  for r in got), str(got))
    expect.equal("`moved_since` is the predicate: the unresolved entries in the working tree",
                 [bool(C.moved_since(records[r], ctx)) for r in (F.GR8, F.GR12, F.SR1)], [True, True, True])
    expect("the undecided row is in the owed group, not the re-read group", F.SR1 in C.owed(ctx, records))
    doc = C.plan_doc(ctx, records)
    expect.equal("`plan --json` carries each re-read row with why it is there",
                 doc["re-read"], [{"rule-id": F.GR8, "fixed": None, "moved": got[0].moved},
                                  {"rule-id": F.GR12, "fixed": "PB1", "moved": got[1].moved}])
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        red = C.check_edition(ctx, records)
    expect("the audit is green with moved records and prints their count as information",
           not red and "2 read(s) owed a re-read" in out.getvalue() and "1 whose fix has landed" in out.getvalue()
           and "2 whose code has moved" in out.getvalue(), out.getvalue())


def case_16_the_resolver_answers_the_parity_fixture(expect):
    """The planted cases both resolvers answer (`tests/doc-facts/code-location-parity-cases.json`): the C# gate compares
    the two engines and the fixture; this runs the Python one on its own, so a self-test run names a case it breaks."""
    doc = json.loads(C.code_location.PARITY_FIXTURE.read_text(encoding="utf-8"))
    got = C.code_location.parity(C.code_location.PARITY_FIXTURE)
    expect("the fixture carries cases that resolve and cases that do not",
           {c["resolves"] for c in doc["cases"]} == {True, False})
    for c in doc["cases"]:
        expect.equal(f"{c['name']} ({c['location']})", got[c["name"]], c["resolves"])


def case_17_verify_pins_lists_a_stored_read_that_does_not_resolve_at_its_own_pin(expect):
    repo, pin = F.tree()
    ctx = F.context(repo.path)
    good = C.record_of(F.read(F.GR8), F.refute(F.GR8), "0b2e58f8a250", pin, "s", {"reader": "a" * 64})
    wrong = C.record_of(F.read(F.GR12, **{"code-location": f"{F.CODE_FILE}#NoSuchMember"}), F.refute(F.GR12),
                        "0b2e58f8a250", pin, "s", {"reader": "a" * 64})
    lost = {**good, "rule-id": F.SR1, "read": {**good["read"], "pin": "0" * 40}}
    got = C.verify_pins(ctx, {F.GR8: good, F.GR12: wrong, F.SR1: lost})
    expect.equal("the records that do not resolve at their own pin, and only those", sorted(got), [F.GR12, F.SR1])
    expect("each names the entry and the pin", any("'NoSuchMember'" in e and pin[:10] in e for e in got[F.GR12]), str(got))
    expect("a pin that is no commit of the repository is named as such", any("no commit" in e for e in got[F.SR1]), str(got))


def case_18_a_decline_read_is_judged_against_the_verdict_its_row_shares(expect):
    """kb/Work PB5865: a documented decline carries no defect fields; a DOUBTFUL needs no `corrected`; the outcome is the
    one function's, against the verdict the row shares with its chain."""
    repo, pin = F.tree()
    dns = "DOCUMENTED-NON-SUPPORT"
    declined = F.context(repo.path, shared={F.GR8: dns})
    conforming = F.context(repo.path, shared={F.GR8: "CONFORMS"})
    nothing = F.context(repo.path)
    expect.equal("a decline read with no defect fields is a read", C.reader_problems(F.read(F.GR8, dns), declined), [])
    expect("a PARTIAL read still owes its defect fields",
           any("`harm`" in e for e in C.reader_problems(F.read(F.GR12, "PARTIAL", harm=""), declined)))
    expect.equal("a DOUBTFUL with no corrected verdict is a case", C.refuter_problems(F.refute(F.GR8, "DOUBTFUL", corrected="")), [])
    expect("an OVERTURNED with no corrected verdict is refused",
           any("right verdict" in e for e in C.refuter_problems(F.refute(F.GR8, "OVERTURNED", corrected=""))))
    rec = C.record_of(F.read(F.GR8, dns), None, "0b2e58f8a250", pin, "s", {"reader": "a" * 64})
    expect.equal("a decline read of a declined row is confirmed", C.outcome(rec, declined), C.CONFIRMED)
    expect.equal("a decline read of a conforming row is contradicted", C.outcome(rec, conforming), C.CONTRADICTED)
    expect.equal("a decline read of a row whose chain shares no verdict is undecided", C.outcome(rec, nothing), C.store.UNDECIDED)
    expect("a contradicted decline read names its work, as any contradicted record",
           "names its mechanism's kb/Work note" in "".join(C.record_problems(rec, conforming)))
    partial = C.record_of(F.read(F.GR8, "PARTIAL"), None, "0b2e58f8a250", pin, "s", {"reader": "a" * 64})
    expect.equal("a PARTIAL read stays contradicted whatever the row shares", C.outcome(partial, F.context(repo.path, shared={F.GR8: "PARTIAL"})), C.CONTRADICTED)
    doubt = C.record_of(F.read(F.GR8), F.refute(F.GR8, "DOUBTFUL", corrected=""), "0b2e58f8a250", pin, "s", {"reader": "a" * 64})
    expect.equal("a DOUBTFUL CONFORMS with no corrected verdict is undecided", C.outcome(doubt, declined), C.store.UNDECIDED)
    expect.equal("the shared verdict is the chain's: every target of a chained row, the same verdict",
                 C.store.shared_verdicts({F.GR8: F.rows()[0]}, F.SCHEMA, {"GR-7.2.2.5-3": {"verdict": dns}}), {F.GR8: dns})
    expect.equal("targets that differ, or hold none, share nothing",
                 C.store.shared_verdicts({F.GR8: {**F.rows()[0], "next": {"kind": F.KEPT, "to": ["A", "B"]}}}, F.SCHEMA,
                                         {"A": {"verdict": dns}, "B": {"verdict": "CONFORMS"}}), {})


def _moving_change(carry_reread=False):
    """A planted store of two confirmed reads made at the commit `base`, and the `head` of a change that renames the member
    both name (`BindEntry` to `BindItem`); `carry_reread` makes the store the change's own re-read: records made at `head`,
    whose entries resolve there."""
    repo, base = F.tree()
    ctx = F.context(repo.path)
    first = repo.path / "reads"
    F.plant(first, "s-01", [F.read(F.GR8), F.read(F.GR12)], [F.refute(F.GR8), F.refute(F.GR12)])
    records = C.import_reads(ctx, first, repo.path / "store", base)[0]
    repo.commit("the change renames the member the reads name", {F.CODE_FILE: F.CODE.replace("BindEntry", "BindItem")})
    head = repo.git("rev-parse", "HEAD").strip()
    if carry_reread:
        moved = {"code-location": F.LOCATION.replace("BindEntry", "BindItem")}
        again = repo.path / "reads-2"
        F.plant(again, "s-01", [F.read(F.GR8, **moved), F.read(F.GR12, **moved)], [F.refute(F.GR8), F.refute(F.GR12)])
        records = C.import_reads(ctx, again, repo.path / "store-2", head)[0]
    return repo, ctx, records, base, head


def case_19_moved_by_names_the_reads_a_change_moved_and_no_other(expect):
    """kb/Work PB6056: `moved_by` is the change from a base to a head (`moved_since` is the working tree against the pin)."""
    repo, ctx, records, base, head = _moving_change()
    with C.pins_open(repo.path) as pins:
        got = C.moved_by(ctx, records, pins.at(head), pins.at(base))
        expect.equal("the change moved the member both reads name: each read, with the entry that no longer resolves",
                     [(r.rule_id, r.fixed, len(r.moved)) for r in got], [(F.GR8, None, 1), (F.GR12, None, 1)])
        expect("the reason names the entry and the member",
               all(F.LOCATION in r.moved[0] and "'BindEntry'" in r.moved[0] for r in got), str(got))
        expect.equal("a change that moved nothing (head = base) moves no read", C.moved_by(ctx, records, pins.at(base),
                                                                                         pins.at(base)), [])
        expect.equal("code that moved BEFORE the base is `moved_since`'s, never this change's (base = head = the moved tree)",
                     C.moved_by(ctx, records, pins.at(head), pins.at(head)), [])


def case_20_the_landing_check_refuses_a_move_whatever_the_editions_status_on_main(expect):
    """kb/Work PB6056, decision 2026-11-22: an edition that does NOT read COMPLETE on main is refused too. The base of the
    planted change holds an open note (`F.tree`'s PB1), so its edition is not COMPLETE there, and the move is refused; the
    check has no input of main's status at all, so no status can let a move through."""
    repo, ctx, records, base, head = _moving_change()
    lines = []
    rc = L.check(repo.path, head, base, lines.append, lambda root, rev: [(ctx, records)], lambda y, r, v: ctx)
    text = "\n".join(lines)
    expect("refused (exit 1) though the edition is open on main (an open note at the base)", rc == 1, text)
    expect("the refusal names the edition, both rows, the moved member and the remedy",
           "edition 1985" in text and F.GR8 in text and F.GR12 in text and "'BindEntry'" in text
           and "import --replace" in text and "re-read" in text, text)
    expect("the check reads no status of main: no parameter of it and no reader of `edition_status.py` in the module",
           list(inspect.signature(L.check).parameters) == ["root", "rev", "base", "say", "reads", "context"]
           and "edition_status.py --" not in inspect.getsource(L) and not hasattr(L, "complete_on_main"),
           str(inspect.signature(L.check)))


def case_21_the_landing_check_passes_a_carried_reread_and_a_change_that_moves_nothing(expect):
    repo, ctx, records, base, head = _moving_change()
    lines = []
    expect.equal("a change that moves no read's code passes",
                 L.check(repo.path, base, base, lines.append, lambda root, rev: [(ctx, records)], lambda y, r, v: ctx), 0)
    repo, ctx, carried, base, head = _moving_change(carry_reread=True)
    expect.equal("a change that carries the re-read passes (its records' entries resolve at the head)",
                 L.check(repo.path, head, base, lines.append, lambda root, rev: [(ctx, carried)], lambda y, r, v: ctx), 0)
    try:
        L.check(repo.path, "0" * 40, base, lines.append, lambda root, rev: [(ctx, carried)], lambda y, r, v: ctx)
        raised = False
    except L.NotRun:
        raised = True
    expect("a revision that is no commit is NOT RUN, never a pass", raised)


def case_22_the_landing_check_reads_the_store_at_the_revision_it_judges(expect):
    """kb/Work PB6056 branch review R1-2: `stored_reads` reads the store as committed at `--rev`, the revision `moved_by`
    resolves in, so a dirty working-tree store does not change the verdict, either way."""
    repo, ctx, stale, base, head = _moving_change()
    _, _, carried, _, _ = _moving_change(carry_reread=True)
    rel = C.store.store_rel(1985)
    C.store.write(stale, repo.path / rel)
    repo.commit("the head commits the stale store (records read at the base)")
    committed_stale = repo.git("rev-parse", "HEAD").strip()

    def reads(root, rev):
        return L.stored_reads(root, rev, F.SCHEMA, lambda year, r, v: ctx)

    def at(year, root, rev):
        return ctx
    lines = []
    expect.equal("the committed store is read: the stale reads, refused", L.check(repo.path, committed_stale, base,
                                                                                  lines.append, reads, at), 1)
    C.store.write(carried, repo.path / rel)   # the working tree now holds the re-read, uncommitted
    expect("the working tree's store is dirty", bool(repo.git("status", "--porcelain", "--", rel)))
    expect.equal("an uncommitted re-read does not pass the committed stale store", L.check(repo.path, committed_stale, base,
                                                                                           lines.append, reads, at), 1)
    repo.commit("the head commits the re-read")
    committed_carried = repo.git("rev-parse", "HEAD").strip()
    C.store.write(stale, repo.path / rel)     # and back: the working tree holds the stale store, uncommitted
    expect.equal("an uncommitted stale store does not refuse the committed re-read", L.check(repo.path, committed_carried,
                                                                                             base, lines.append, reads, at), 0)
    expect.equal("at a revision with no store, no edition is read", list(L.stored_reads(repo.path, base, F.SCHEMA,
                                                                                         lambda year, r, v: ctx)), [])


NEW_DIGEST = "5c0ffee5ba5e"


def _retexted_change(carry_reread=False):
    """Two confirmed reads made at the commit `base` against the digest `0b2e58f8a250`, and the change that corrects the text of
    both rules, so the rows' digests are `NEW_DIGEST` at the head (kb/Work PB6090: PR #1953). Returns the repository, the base's
    and the head's contexts, the store the head holds (the stale records, or with `carry_reread` the change's own re-read, made
    against the new text), and the two commits. The change moves no code."""
    repo, base = F.tree()
    base_ctx, head_ctx = F.context(repo.path), F.context(repo.path, NEW_DIGEST)
    first = repo.path / "reads"
    F.plant(first, "s-01", [F.read(F.GR8), F.read(F.GR12)], [F.refute(F.GR8), F.refute(F.GR12)])
    records = C.import_reads(base_ctx, first, repo.path / "store", base)[0]
    repo.commit("the change corrects the text of two rules", {"docs/x.md": "the corrected text\n"})
    head = repo.git("rev-parse", "HEAD").strip()
    if carry_reread:
        again = repo.path / "reads-2"
        F.plant(again, "s-01", [F.read(F.GR8), F.read(F.GR12)], [F.refute(F.GR8), F.refute(F.GR12)])
        records = C.import_reads(head_ctx, again, repo.path / "store-2", head)[0]
    return repo, base_ctx, head_ctx, records, base, head


def case_23_staled_by_names_the_reads_a_changed_rule_text_made_stale_and_no_other(expect):
    """kb/Work PB6090: a read that decided its row at the base and leaves it owed at the head (the audit's `outcome` and `owed`)."""
    _, base_ctx, head_ctx, stale, _, _ = _retexted_change()
    _, _, _, carried, _, _ = _retexted_change(carry_reread=True)
    expect.equal("both rows' reads went stale: each row, the digest the read covers and the digest of the row now",
                 C.staled_by(head_ctx, base_ctx, stale),
                 [(F.GR8, "0b2e58f8a250", NEW_DIGEST), (F.GR12, "0b2e58f8a250", NEW_DIGEST)])
    expect.equal("a change that changed no rule text (head context = base context) stales no read",
                 C.staled_by(base_ctx, base_ctx, stale), [])
    expect.equal("a record made against the new text covers its row: the carried re-read stales none",
                 C.staled_by(head_ctx, base_ctx, carried), [])
    expect.equal("a read already stale on main (the base holds the new text too) is not this change's",
                 C.staled_by(head_ctx, head_ctx, stale), [])


def case_24_the_landing_check_refuses_a_changed_rule_text_that_stales_a_read(expect):
    """kb/Work PB6090: a digest change is refused, naming the rows, both digests and the remedy, whatever main's status."""
    repo, base_ctx, head_ctx, stale, base, head = _retexted_change()
    lines = []
    rc = L.check(repo.path, head, base, lines.append, lambda root, rev: [(head_ctx, stale)], lambda y, r, v: base_ctx)
    text = "\n".join(lines)
    expect("refused (exit 1) though the change moves no code", rc == 1, text)
    expect("names the edition, both rows, both digests and the remedy",
           "edition 1985" in text and F.GR8 in text and F.GR12 in text and "0b2e58f8a250" in text and NEW_DIGEST in text
           and "import --replace" in text and "rule text" in text, text)


def case_25_the_landing_check_passes_a_carried_reread_of_the_new_text_and_a_change_of_no_text(expect):
    repo, base_ctx, head_ctx, carried, base, head = _retexted_change(carry_reread=True)
    lines = []
    expect.equal("a change that carries the re-read of the corrected text passes",
                 L.check(repo.path, head, base, lines.append, lambda root, rev: [(head_ctx, carried)],
                         lambda y, r, v: base_ctx), 0)
    repo, base_ctx, _, records, base, head = _retexted_change()
    expect.equal("a change that changes no rule text (the head's rows are the base's) passes",
                 L.check(repo.path, head, base, lines.append, lambda root, rev: [(base_ctx, records)],
                         lambda y, r, v: base_ctx), 0)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
