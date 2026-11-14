#!/usr/bin/env python3
"""The one reader of docs/CONFORMANCE.md on planted documents (DESIGN-orchestrator-loop.md section 16.8, kb/Work PB5395): a
section is found by its number and ends at the next heading of its own or a shallower level, a table's first row is its
header, the escaped pipe stays in its cell, the register's rows and keys come out as the audits need them, and the live
document parses to the populations the readers rely on."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

cs = selftest_cases.subject(__file__)   # scripts/spec/conformance_source.py

DOC = (
    "# Conformance\n"
    "## 1. Summary\n\nprose\n\n"
    "## 2. Annex A.3 — dispositions\n\n"
    "| A.3 # | Element | § | Disposition | Note |\n|---|---|---|---|---|\n"
    "| 1 | e1 | 13.1 | **Claimed** | a \\| b |\n| 2 | e2 | 13.2 | Not claimed | n |\n| 3 | short |\n\n"
    "## 3. Determinations\n\n"
    "- **D-ONE first.** The one.\n  continued\n- **D-TWO second.** The two.\n\n"
    "### 3.1 Undefined cases\n\n- **D-UND a case.** text\n\n"
    "### 3.2 Extensions\n\n| `ext-a` | x |\n\n"
    "## 4. Documented non-support\n\n1. **Message control system** — declined\n2. **Screens** — declined\n\n"
    "## 5. Annex A.4 — modules\n\n| A.4 # | Module | Disposition | Note |\n|---|---|---|---|\n"
    "| A.4.1 | m1 | Not claimed | n1 |\n| A.4.2 | m2 | **Claimed** | n2 |\n\n"
    "## 7. Annex A.1 — register\n\n| A.1 item | Element | What we do | Pinned by |\n|---|---|---|---|\n"
    "| DOC-A.1-1 | el | **Not provided.** because | — |\n| DOC-A.1-82 | abs | rounds \\|v\\| up | `T` |\n\n"
    "## 8. Derivations\n\n| Rule | Arm | Names | Argument | Signed | Refuted |\n|---|---|---|---|---|---|\n"
    "| DRV-GR-1-1 | undefined-A.2 | A.2 item 4 | because | owner: 2026-09-03 | — |\n")


def case_00_a_section_runs_to_the_next_heading_of_its_level_or_shallower(expect):
    conf = cs.parse(DOC)
    three = conf.section("3")
    expect.equal("section 3 holds its subsections", ("### 3.1 Undefined cases" in three.body, "D-TWO" in three.body), (True, True))
    expect.equal("section 3 ends at section 4", "Message control" in three.body, False)
    expect.equal("section 3.1 ends at 3.2, which is a heading of its own level",
                 ("D-UND" in conf.section("3.1").body, "ext-a" in conf.section("3.1").body), (True, False))
    expect.equal("a section's heading line and its line number", (conf.section("5").heading, conf.section("5").line),
                 ("## 5. Annex A.4 — modules", DOC.split("\n").index("## 5. Annex A.4 — modules") + 1))
    expect.equal("a number is exact: section 3 is not section 3.1", conf.section("3.1").number, "3.1")
    expect.equal("the number a heading text names", [cs.number_of("## 8. Rules with"), cs.number_of("### 3.2 Nonstandard")], ["8", "3.2"])


def case_01_a_missing_section_is_an_empty_answer_or_a_named_refusal(expect):
    conf = cs.parse("# x\n## 7. Annex A.1\n| A.1 item | E | D | P |\n|---|---|---|---|\n| DOC-A.1-9 | e | d | p |\n")
    expect.equal("a body carrying one section has no rows of another", (conf.rows("2"), conf.body_of("2"), conf.find_section("2")), ([], "", None))
    try:
        conf.section("2")
        refused = ""
    except SystemExit as e:
        refused = str(e)
    expect("`section` names the number it could not find", "has no §2" in refused, refused)
    try:
        cs.number_of("not a heading")
        malformed = False
    except ValueError:
        malformed = True
    expect("a heading with no number is refused", malformed)


def case_02_a_table_has_a_header_row_a_separator_and_cells_split_on_unescaped_pipes(expect):
    conf = cs.parse(DOC)
    a3 = conf.a3_dispositions()
    expect.equal("the header and separator are no rows, a row of two cells is none of A.3's five", [r["items"] for r in a3], ["1", "2"])
    expect.equal("an escaped pipe stays in its cell", a3[0]["note"], "a | b")
    expect.equal("a row of the table with its line", [line for line, _ in conf.rows_at("2")][:2],
                 [DOC.split("\n").index("| 1 | e1 | 13.1 | **Claimed** | a \\| b |") + 1,
                  DOC.split("\n").index("| 2 | e2 | 13.2 | Not claimed | n |") + 1])
    expect.equal("cells of a bare line", cs.cells("| a | b \\| c |"), ["a", "b | c"])


def case_03_the_register_sections_read_as_the_audits_need_them(expect):
    conf = cs.parse(DOC)
    expect.equal("section 7's rows, escaped pipes kept in the determination",
                 [(r.key, r.determination) for r in conf.a1_rows()],
                 [("DOC-A.1-1", "**Not provided.** because"), ("DOC-A.1-82", "rounds |v| up")])
    expect.equal("the map of a row's key to its determination", sorted(conf.a1_determinations()), ["DOC-A.1-1", "DOC-A.1-82"])
    expect.equal("section 8 is not section 7's rows", [r.key for r in conf.derivation_rows("8")], ["DRV-GR-1-1"])
    expect.equal("a derivation row's cells by name", conf.derivation_rows("8")[0].refuter, "—")
    dup = cs.parse(DOC.replace("DOC-A.1-82", "DOC-A.1-1"))
    try:
        dup.a1_determinations()
        raised = ""
    except ValueError as e:
        raised = str(e)
    expect("a repeated §7 key is refused, naming it", "DOC-A.1-1" in raised and "repeats" in raised, raised)
    expect.equal("A.4's dispositions by name", [(r["clause"], r["module"], r["disposition"]) for r in conf.a4_dispositions()],
                 [("A.4.1", "m1", "Not claimed"), ("A.4.2", "m2", "**Claimed**")])


def case_04_a_determination_is_found_by_its_key_and_ends_where_the_next_begins(expect):
    conf = cs.parse(DOC)
    expect.equal("a bullet runs to the next top-level bullet, its continuation line included",
                 conf.bullet("D-ONE"), "- **D-ONE first.** The one.\n  continued")
    expect.equal("a bullet that is not there", conf.bullet("D-NONE"), None)
    expect.equal("a paragraph runs to the first blank line", conf.paragraph("D-TWO"), "**D-TWO second.** The two.\n\n")
    expect.equal("the row of a key anywhere in the document", conf.row_line("A.4.2"), "| A.4.2 | m2 | **Claimed** | n2 |")
    expect.equal("a row that is not there", conf.row_line("A.4.9"), None)
    expect.equal("section 4's facilities, in the order written", conf.facility_titles(), ["Message control system", "Screens"])


def case_05_the_live_document_parses_to_the_populations_the_readers_rely_on(expect):
    conf = cs.load()
    expect("the A.3 table holds its 40 rows and A.4 its 13", len(conf.a3_dispositions()) >= 40 and len(conf.a4_dispositions()) >= 13,
           str((len(conf.a3_dispositions()), len(conf.a4_dispositions()))))
    expect("section 7 holds more than 200 rows, every key unique", len(conf.a1_determinations()) > 200, str(len(conf.a1_determinations())))
    expect("section 8 holds derivations, each keyed DRV-", conf.derivation_rows("8") != [] and all(r.key.startswith("DRV-") for r in conf.derivation_rows("8")))
    expect("sections 1 to 8 are all found", all(conf.find_section(n) is not None for n in "12345678"), "")
    expect("sections 3.1 to 3.10 are subsections of 3", all(conf.find_section(f"3.{i}") is not None for i in range(1, 11)), "")
    expect("the facility titles are the four whole facilities", len(conf.facility_titles()) == 4, str(conf.facility_titles()))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
