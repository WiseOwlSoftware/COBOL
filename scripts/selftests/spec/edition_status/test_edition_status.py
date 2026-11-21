#!/usr/bin/env python3
"""Is edition N complete (kb/Work PB5924): the verdict is COMPLETE only when all four parts are measured and zero, and
NOT COMPLETE when any one is nonzero or unmeasured; a reader's refusal is an unmeasured part and never a zero, for the
real readers too (an unheld edition, an unreadable register); the work part reads a planted register, body and addenda
included (review R1-1); and the prose holes are measured on a planted 1985 transcription through the catalog's own walk:
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


def case_03_the_work_part_reads_a_planted_register(expect):
    d = scratch_dir("edition-status-work-")
    files = {
        "PB1": F.note("PB1", wrong_answer="true", tags="[std85]"),                        # HARM, std85
        "PB2": F.note("PB2", kind="analysis", spec_refs="['§1985:IV.7.1']"),              # QUESTION
        "PB3": F.note("PB3", process_only="true", process_only_reason='"x"', title='"PB3 - 1985 listing"'),   # TOOLING
        "PB4": F.note("PB4", title='"PB4 - the 1985 thing"'),                              # OTHER
        "PB5": F.note("PB5", "landed", title='"PB5 - 1985 landed"'),                       # terminal: excluded
        "PB6": F.note("PB6", title='"PB6 - the 2002 thing"'),                              # another edition
        "PB7": F.note("PB7", "blocked", title='"PB7 - a --std 85 refusal"'),               # blocked is open
        "PB8": F.note("PB8", title='"PB8 - 19850 is no year"'),                             # digits around the year
        "PB9": F.note("PB9", body="The listing of §1989:A.2 is owed."),                    # the BODY names it (R1-1)
        "PB10": F.note("PB10", body="Under --std 2023 the rule GR-14.9.1-3 holds."),       # 2023 by citation shape
        "PB11": F.note("PB11", title='"PB11 - ISO/IEC 1989:2023 and PB2014"'),             # neither 2023 nor 2014
    }
    for name, text in files.items():
        (d / f"{name}.md").write_text(text, encoding="utf-8", newline="\n")
    (d / "PB4").mkdir()
    (d / "PB4" / "2026-11-20-addendum.md").write_text("## 2026-11-20 - an addendum\n\nThe 2014 text differs.\n",
                                                      encoding="utf-8", newline="\n")
    p = C.work_part(1985, d)
    expect.equal("PB1 HARM", p.data["HARM"], ["PB1"])
    expect.equal("PB2 QUESTION", p.data["QUESTION"], ["PB2"])
    expect.equal("PB3 TOOLING", p.data["TOOLING"], ["PB3"])
    expect.equal("PB4, PB7 and the body-only PB9 OTHER", p.data["OTHER"], ["PB4", "PB7", "PB9"])
    expect.equal("the total counts every category", p.count, 6)
    expect.equal("2002 sees only its own", C.work_part(2002, d).data["OTHER"], ["PB6"])
    expect.equal("2014 is read from an addendum, never from a note id", C.work_part(2014, d).data["OTHER"], ["PB4"])
    expect.equal("2023 by citation and --std, never by the standard's number", C.work_part(2023, d).data["OTHER"], ["PB10"])


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


def case_08_an_edition_no_reader_reads_is_refused(expect):
    for year in (2002, 2014, 2023):
        try:
            C.catalog_prose_holes.holes(year)
            expect.equal(f"{year} refused", "measured", "refused")
        except C.catalog_prose_holes.NotMeasured:
            expect(f"{year} refused", True)


def case_09_the_one_pattern_table_covers_every_edition(expect):
    expect.equal("an edition per row", sorted(C.EDITION_PATTERNS), list(C.EDITIONS))
    expect("a --std 85 spelling", C.touches({"title": "x --std 85 y"}, 1985))
    expect("a section citation", C.touches({"spec_refs": ["§1985:VI.5.3"]}, 1985))
    expect("a rule id", C.touches({"inventory_rows": ["1985-GR-XII.2.4-8"]}, 1985))
    expect("the body is read", C.touches({"_body": "see X3.23-1985"}, 1985))
    expect("a note id is no year", not C.touches({"title": "PB1985 and PB2014"}, 1985))
    expect("the reader's other keys are not text", not C.touches({"_file": "1985.md"}, 1985))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
