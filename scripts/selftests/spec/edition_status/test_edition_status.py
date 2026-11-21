#!/usr/bin/env python3
"""Is edition N complete (kb/Work PB5924): the verdict is COMPLETE only when all four parts are measured and zero, and
NOT COMPLETE when any one is nonzero or unmeasured; a reader's refusal is an unmeasured part and never a zero; the work
part reads a planted register (the edition's patterns, the terminal statuses, the four categories); and the prose holes
are measured on a planted 1985 transcription and refused for an edition no reader here reads."""
import json
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
    }
    for name, text in files.items():
        (d / f"{name}.md").write_text(text, encoding="utf-8", newline="\n")
    p = C.work_part(1985, d)
    expect.equal("PB1 HARM", p.data["HARM"], ["PB1"])
    expect.equal("PB2 QUESTION", p.data["QUESTION"], ["PB2"])
    expect.equal("PB3 TOOLING", p.data["TOOLING"], ["PB3"])
    expect.equal("PB4 and PB7 OTHER", p.data["OTHER"], ["PB4", "PB7"])
    expect.equal("the total counts every category", p.count, 5)
    expect.equal("2002 sees only its own", C.work_part(2002, d).data["OTHER"], ["PB6"])


def case_04_the_prose_holes_of_a_planted_1985_transcription(expect):
    root = scratch_dir("edition-status-holes-")
    spec = C.catalog_prose_holes.spec_pdf.markdown(1985, root)
    spec.parent.mkdir()
    (root / "docs" / "rearchitecture").mkdir(parents=True)
    spec.write_text(
        "## Page 1\n\n# SECTION I: INTRODUCTION\n\n### 1.1 Scope\n\n"
        "The compiler must reject every undeclared name in the source text. "
        "The compiler shall report each rejected name once per program.\n\n"
        "### 1.2 Notes\n\nThis paragraph states a description with no requirement at all in it.\n", encoding="utf-8", newline="\n")
    (root / "docs" / "rearchitecture" / "spec-rule-catalog-1985.json").write_text(
        json.dumps({"rules": [{"locator": "I.1.1", "text": "The compiler must reject every undeclared name in the source text."}]}),
        encoding="utf-8", newline="\n")
    h = C.catalog_prose_holes.holes(1985, root)
    expect.equal("two normative sentences", h.normative, 2)
    expect.equal("one is a row's text", h.covered, 1)
    expect.equal("one hole, the second", [s[0] for hole in h.holes for s in hole.sentences],
                 ["The compiler shall report each rejected name once per program."])
    expect.equal("the count a verdict uses", h.count, 1)


def case_05_an_edition_no_reader_reads_is_refused(expect):
    for year in (2002, 2014, 2023):
        try:
            C.catalog_prose_holes.holes(year)
            expect.equal(f"{year} refused", "measured", "refused")
        except C.catalog_prose_holes.NotMeasured:
            expect(f"{year} refused", True)


def case_06_the_one_pattern_table_covers_every_edition(expect):
    expect.equal("an edition per row", sorted(C.EDITION_PATTERNS), list(C.EDITIONS))
    expect("a --std 85 spelling", C.touches({"title": "x --std 85 y"}, 1985))
    expect("a section citation", C.touches({"spec_refs": ["§1985:VI.5.3"]}, 1985))
    expect("a rule id", C.touches({"inventory_rows": ["1985-GR-XII.2.4-8"]}, 1985))
    expect("the reader's own keys are not frontmatter text", not C.touches({"_body": "1985"}, 1985))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
