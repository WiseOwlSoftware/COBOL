#!/usr/bin/env python3
"""Is edition N complete (kb/Work PB5924): the verdict is COMPLETE only when all four parts are measured and zero, and
NOT COMPLETE when any one is nonzero or unmeasured; a reader's refusal is an unmeasured part and never a zero, for the
real readers too (an unheld edition, an unreadable register); the work part reads the notes' declared `editions:` and
nothing else, and an open note that declares none leaves it unmeasured (kb/Work PB5995); and the prose holes are measured on a planted 1985 transcription through the catalog's own walk:
every printed paragraph in no row AT OR UNDER its locator counts (R1-2, R1-4), the deontic sentences are a floor inside
it, a catalog row the walk does not reach refuses (R1-3), and an edition no reader here reads is refused."""
import sys

import fixtures as F
import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/edition_status.py

def case_00_complete_only_when_all_four_are_zero(expect):
    expect.equal("four zeros", F.line(0, 0, 0, 0), (True, "=== EDITION 1985: COMPLETE ==="))


def case_01_any_one_nonzero_is_not_complete_and_named(expect):
    for at, name in enumerate(F.NAMES):
        counts = [0, 0, 0, 0]
        counts[at] = 3
        done, text = F.line(*counts)
        expect.equal(f"{name} = 3 is not complete", done, False)
        expect.equal(f"{name} = 3 is named", text, f"=== EDITION 1985: NOT COMPLETE — 3 {name} ===")


def case_02_an_unmeasured_part_is_never_a_zero(expect):
    done, text = F.line(0, 0, 0, None)
    expect.equal("a refusing reader is not complete", done, False)
    expect.equal("and says which part", text, "=== EDITION 1985: NOT COMPLETE — catalog prose holes not measured ===")
    parts = C.status(1985, F.planted(0, 0, 0, None))
    expect.equal("the refusal's text is kept", parts[3].why, "catalog prose holes: planted refusal")
    expect.equal("it has no count", parts[3].count, None)
    expect.equal("the json says unmeasured", C.as_doc(1985, parts)["parts"]["catalog prose holes"]["measured"], False)


def case_03_the_work_part_reads_the_declared_editions(expect):
    d = scratch_dir("edition-status-work-")
    files = {
        "PB1": F.note("PB1", wrong_answer="true", editions="[1985]"),                       # HARM
        "PB2": F.note("PB2", kind="analysis", editions="[1985, 2002]"),                     # QUESTION
        "PB3": F.note("PB3", process_only="true", process_only_reason='"x"', editions="[1985]"),   # TOOLING
        "PB4": F.note("PB4", editions="[1985, 2014]"),                                      # OTHER
        "PB5": F.note("PB5", "landed", editions="[1985]"),                                  # terminal: excluded
        "PB6": F.note("PB6", title='"PB6 - the 1985 thing"', editions="[2002]"),            # the TEXT is not read
        "PB7": F.note("PB7", "blocked", editions="[1985]"),                                 # blocked is open
        "PB8": F.note("PB8", body="Under --std 2023 the rule GR-14.9.1-3 holds.", editions="[]"),   # blocks none
        "PB9": F.note("PB9", "retired"),                                                    # terminal, undeclared: fine
    }
    for name, text in files.items():
        (d / f"{name}.md").write_text(text, encoding="utf-8", newline="\n")
    p = C.work_part(1985, d)
    expect.equal("PB1 HARM", p.data["HARM"], ["PB1"])
    expect.equal("PB2 QUESTION", p.data["QUESTION"], ["PB2"])
    expect.equal("PB3 TOOLING", p.data["TOOLING"], ["PB3"])
    expect.equal("PB4 and PB7 OTHER; PB6's text names 1985 but it declares 2002", p.data["OTHER"], ["PB4", "PB7"])
    expect.equal("the total counts every category", p.count, 5)
    expect.equal("2002 reads the field", (C.work_part(2002, d).data["QUESTION"], C.work_part(2002, d).data["OTHER"]),
                 (["PB2"], ["PB6"]))
    expect.equal("2014 reads the field", C.work_part(2014, d).data["OTHER"], ["PB4"])
    expect.equal("2023: a note citing it but declaring [] blocks none", C.work_part(2023, d).count, 0)


def case_03b_an_open_note_that_declares_none_is_unmeasured(expect):
    d = scratch_dir("edition-status-undeclared-")
    (d / "PB1.md").write_text(F.note("PB1", editions="[1985]"), encoding="utf-8", newline="\n")
    (d / "PB2.md").write_text(F.note("PB2", title='"PB2 - the 1985 thing"'), encoding="utf-8", newline="\n")
    (d / "PB3.md").write_text(F.note("PB3", editions="[1977]"), encoding="utf-8", newline="\n")
    p = C.work_part(1985, d)
    expect.equal("never a count", p.count, None)
    expect("the reason names each undeclared note", "2 non-terminal note(s) declare no editions" in p.why
           and "PB2 PB3" in p.why, p.why)
    expect.equal("so the edition is not complete",
                 C.verdict(1985, [*(C.Part(n, 0) for n in F.NAMES[:2]), p, C.Part(F.NAMES[3], 0)])[0], False)

def case_04_an_unreadable_register_is_unmeasured(expect):
    d = scratch_dir("edition-status-unreadable-")
    (d / "PB1.md").write_text(F.note("PB1", title='"PB1 - 1985"'), encoding="utf-8", newline="\n")
    (d / "PB2").mkdir()   # addenda of a note that does not exist: work.load refuses the register
    (d / "PB2" / "2026-11-20-x.md").write_text("## x\n", encoding="utf-8", newline="\n")
    parts = C.status(1985, (*F.planted(0, 0)[:2], lambda y: C.work_part(y, d), F.planted(0, 0, 0, 0)[3]))
    expect.equal("the register's refusal is no count", parts[2].count, None)
    expect("and says why", "unreadable" in parts[2].why)
    expect.equal("so the edition is not complete", C.verdict(1985, parts)[0], False)


def case_05_the_real_readers_of_a_held_and_an_unheld_edition(expect):
    gap = C.inventory_part(1985)
    expect("1985's inventory is measured", gap.measured and gap.data["rows"] > 0)
    expect.equal("2023 is the newest: no store, zero owed", (C.code_reads_part(2023).count,
                                                             C.code_reads_part(2023).data["store"]), (0, False))
    parts = C.status(2014, (C.inventory_part, C.code_reads_part, F.planted(0, 0, 0, 0)[2], C.holes_part))
    expect.equal("2014's inventory is not measured", parts[0].measured, False)
    expect.equal("2014's code reads are not measured", parts[1].measured, False)
    expect.equal("2014's verdict names each", C.verdict(2014, parts)[1],
                 "=== EDITION 2014: NOT COMPLETE — inventory GAP not measured · code reads not measured · "
                 "catalog prose holes not measured ===")


def case_05b_a_bounded_inventory_is_never_zero(expect):
    rows = [{"state": "OK"}, {"state": "OK"}]
    whole = C.inventory_count(rows, {})
    expect.equal("no GAP and no bound is zero", (whole.count, whole.text), (0, "0 inventory GAP"))
    bounded = C.inventory_count(rows, {"clauses pending adjudication": 89})
    expect.equal("no GAP with clauses pending is not zero", bounded.count, 89)
    parts = [bounded, *(C.Part(n, 0) for n in F.NAMES[1:])]
    expect.equal("and the verdict names the bound beside the GAP", C.verdict(2002, parts),
                 (False, "=== EDITION 2002: NOT COMPLETE — 0 inventory GAP + 89 clauses pending adjudication ==="))
    both = C.inventory_count([{"state": "GAP"}], {"clauses pending adjudication": 2, "rule-shaped headings untyped": 3})
    expect.equal("every pending item counts", (both.count, both.data["gap"]), (6, 1))
    real = C.inventory_part(2002)
    expect.equal("the real part carries the edition's bound, read by the one reader",
                 real.data["bound"], C.inventory_view.pending(C.inventory_schema.load_schema().edition(2002)))


def case_06_the_prose_holes_of_a_planted_1985_transcription(expect):
    root = F.planted_1985(scratch_dir("edition-status-holes-"),
                          [{"locator": "VI.1.1", "text": "The compiler must reject every undeclared name in the source text."}])
    h = C.catalog_prose_holes.holes(1985, root)
    expect.equal("the region's printed paragraphs, the appendix and Section II outside it", h.printed_paragraphs, 3)
    expect.equal("each paragraph in no row at or under its locator", [u.locator for u in h.uncovered],
                 ["VI.1.1", "VI.1.2", "VI.1.3"])
    expect.equal("the count a verdict uses is the paragraphs", h.count, 3)
    expect.equal("the deontic floor inside it", h.deontic, 2)
    expect.equal("a sentence of another clause's row is no cover (R1-4), and is the lead",
                 [u.locator for u in h.uncovered if u.elsewhere], ["VI.1.3"])
    expect.equal("the numbered paragraphs with no row", h.rowless, ("VI.1.2", "VI.1.3"))


def case_07_a_catalog_row_the_walk_does_not_reach_refuses(expect):
    root = F.planted_1985(scratch_dir("edition-status-drift-"), [{"locator": "VI.9.9", "text": "A row of no paragraph."}])
    try:
        C.catalog_prose_holes.holes(1985, root)
        expect.equal("refused", "measured", "refused")
    except SystemExit as e:
        expect("the refusal names the drift", "VI.9.9" in str(e.code))
    expect.equal("and the part reads it as not measured",
                 C.status(1985, (*F.planted(0, 0, 0)[:3], lambda y: C.catalog_prose_holes.holes(y, root)))[3].measured,
                 False)


def case_07b_a_paragraph_leaves_the_count_only_on_a_disposition_and_its_record(expect):
    # kb/Work PB6001: the measure reads the one disposition store through the one segmentation and the one test of a
    # record; a disposition with no UPHELD record of the unit's text, a pending unit and a stale block stay counted.
    import json  # noqa: PLC0415
    H = C.catalog_prose_holes
    E, U = H.extract_rule_catalog, H.text_units
    root = F.planted_1985(scratch_dir("edition-status-disposed-"), [], F.DISPOSED_1985)
    lines = F.DISPOSED_1985.splitlines()
    manifest = root / E.UNHARVESTED_OF[1985].relative_to(E.REPO)
    manifest.parent.mkdir(parents=True, exist_ok=True)
    records_at = "docs/planted-refute.jsonl"
    scan = H.rule_catalog_1985.parse(lines).unharvested
    blocks = {c: {**{k: scan[c][k] for k in ("title", "ordinals", "lists", "longest")}, "disposition": "not-rules",
                  "why": "planted", "digest": scan[c]["digest"] if c.endswith("2.5") else "000000000000",
                  "refuted": records_at} for c in ("§1985:VI.2.5", "§1985:VI.2.6")}
    manifest.write_text(json.dumps({"blocks": blocks}), encoding="utf-8", newline="\n")
    _cat, units, _a, _m = E.units_1985(lines, root)
    unit = {c: us[0] for c, us in units.items()}
    records = [{"unit": c, "clause": c, "digest": b["digest"], "outcome": "UPHELD"} for c, b in blocks.items()]
    entries = {}
    for c, (d, outcome, read) in {"§1985:VI.2.1": ("restates", "UPHELD", None),
                                  "§1985:VI.2.2": ("not-a-rule", "OVERTURNED", None),
                                  "§1985:VI.2.3": ("restates", "UPHELD", "0123456789ab"),
                                  "§1985:VI.2.4": ("pending", None, None),
                                  "§1985:VI.2.5": ("not-a-rule", "UPHELD", None),
                                  "§1985:VI.2.6": ("pending", None, None)}.items():
        u = unit[c]
        # each record upholds the disposition its entry states (text_units.disposition_refusal)
        judged = {"restates": {"rules": ["1985-GR-VI.2.1-1"]}, "not-a-rule": {"class": "lead-in"}}.get(d, {})
        entries[c] = {"units": [{"unit": u.label, "digest": u.digest, "order": 0, "disposition": d,
                                 "refuted": records_at, **judged}]}
        if outcome:
            records.append({"unit": u.label, "clause": c, "digest": read or u.digest, "outcome": outcome,
                            "disposition": d, **judged})
    manifest.write_text(json.dumps({"blocks": blocks, "paragraph-blocks": entries}), encoding="utf-8", newline="\n")
    (root / records_at).write_text("".join(json.dumps(r) + "\n" for r in records), encoding="utf-8", newline="\n")
    expect.equal("the planted units are the clauses' paragraphs, the held lists none of them",
                 {c: [u.label for u in us] for c, us in units.items() if c != "§1985:VI.2.7"},
                 {f"§1985:VI.2.{n}": ["p1"] for n in range(1, 7)})
    h = H.holes(1985, root)
    expect.equal("counted: an OVERTURNED record, a record of other words, a pending unit, and a block whose text moved "
                 "(its lead-in pending, its two items)", [u.locator for u in h.uncovered],
                 ["VI.2.2", "VI.2.3", "VI.2.4", "VI.2.6", "VI.2.6", "VI.2.6"])
    expect.equal("covered by cause: a restates and a not-a-rule on their records, a not-rules block's two items",
                 h.discounted, {"row": 0, "restates": 1, "not-a-rule": 1, "not-rules block": 2})
    expect.equal("an HTML table, a caption below it, a figure's caption and the repeated section title are no prose",
                 h.printed_paragraphs, 10)


def case_07c_an_examples_clause_is_units_and_its_code_is_no_prose(expect):
    # kb/Work PB6001 (wave 20262132 group a): a clause headed "Examples" is text like any other, so its preamble, its
    # label and its table rows are units disposed on their own records; its fenced code is a display (text_units
    # .display_mask), no unit and so no hole. On the base the clause had no units and its code lines were holes.
    import json  # noqa: PLC0415
    H = C.catalog_prose_holes
    E = H.extract_rule_catalog
    lines = F.EXAMPLES_1985.splitlines()
    root = F.planted_1985(scratch_dir("edition-status-examples-"), [], F.EXAMPLES_1985)
    _cat, units, _a, _m = E.units_1985(lines, root)
    expect.equal("the Examples clause is units: its preamble, its label and its table's row; the code is none",
                 [(u.label, u.shape) for u in units.get("§1985:VI.3.1", [])],
                 [("p1", "paragraph"), ("p2", "paragraph"), ("T1.1", "table-row")])
    h = H.holes(1985, root)
    expect.equal("only the pending preamble is a hole: a fenced code line is no prose", [u.line for u in h.uncovered],
                 [7])
    p1 = units["§1985:VI.3.1"][0]
    records_at = "docs/planted-refute.jsonl"
    manifest = root / E.UNHARVESTED_OF[1985].relative_to(E.REPO)
    manifest.parent.mkdir(parents=True, exist_ok=True)
    manifest.write_text(json.dumps({"blocks": {}, "paragraph-blocks": {"§1985:VI.3.1": {"units": [
        {"unit": "p1", "digest": p1.digest, "order": 0, "disposition": "restates", "rules": ["1985-GR-VI.3.1-1"],
         "refuted": records_at}]}}}), encoding="utf-8", newline="\n")
    (root / records_at).write_text(json.dumps({"unit": "p1", "clause": "§1985:VI.3.1", "digest": p1.digest,
                                               "outcome": "UPHELD", "disposition": "restates",
                                               "rules": ["1985-GR-VI.3.1-1"]}) + "\n", encoding="utf-8", newline="\n")
    h = H.holes(1985, root)
    expect.equal("disposed on its UPHELD record, the preamble leaves the count", (h.count, h.discounted["restates"]),
                 (0, 1))


def case_08_an_edition_no_reader_reads_is_refused(expect):
    for year in (2002, 2014, 2023):
        try:
            C.catalog_prose_holes.holes(year)
            expect.equal(f"{year} refused", "measured", "refused")
        except C.catalog_prose_holes.NotMeasured:
            expect(f"{year} refused", True)


def case_09_the_one_reader_of_the_field(expect):
    expect.equal("the editions", C.work.EDITIONS, (1985, 2002, 2014, 2023))
    expect.equal("a list", C.work.declared_editions({"editions": ["1985", "2023"]}), frozenset({1985, 2023}))
    expect.equal("an empty list blocks none", C.work.declared_editions({"editions": []}), frozenset())
    expect.equal("absent is no declaration", C.work.declared_editions({}), None)
    expect.equal("an unknown edition is no declaration", C.work.declared_editions({"editions": ["1977"]}), None)
    expect.equal("a scalar is no declaration", C.work.declared_editions({"editions": "1985"}), None)
    expect("the text patterns are gone", not hasattr(C, "EDITION_PATTERNS") and not hasattr(C, "touches"))

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
