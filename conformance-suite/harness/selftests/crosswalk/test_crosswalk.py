#!/usr/bin/env python3
"""The NIST-85 crosswalk on planted programs and a planted catalog (DESIGN-conformance-suite.md section 6.3, kb/Work PB6049):
the units a program holds, the reference in effect at each, the reference's tokens, the mechanical map (mapped, ambiguous,
unmapped, excluded), the flag units, the audit of the files and the NIST term."""
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/crosswalk.py


def _row(locator, kind, ordinal, subject, folio="VI-1", status="normal", rid=None):
    return {"rule-id": rid or f"1985-{kind}-{locator}-{ordinal}", "locator": locator, "kind": kind, "ordinal": ordinal,
            "subject": subject, "folio": folio, "status": status, "document": "X3.23-1985"}


def _catalog():
    rows = [_row("VI.6.20.2", "FMT", None, "THE MULTIPLY STATEMENT", rid="1985-FMT-VI.6.20.2"),
            _row("VI.6.20.4", "GR", 1, "THE MULTIPLY STATEMENT"), _row("VI.6.20.4", "GR", 2, "THE MULTIPLY STATEMENT"),
            _row("VI.6.19.4", "GR", 1, "THE MOVE STATEMENT"), _row("VI.6.19.4", "GR", 2, "THE MOVE STATEMENT"),
            _row("VI.5.12.4", "GR", 1, "THE SIGN CLAUSE"), _row("VI.5.12.4", "GR", 2, "THE SIGN CLAUSE"),
            _row("VI.6.7.1", "GR", 1, "THE ALTER STATEMENT", status="obsolete: C.1(10)", rid="1985-GR-VI.6.7.1-p1"),
            _row("VI.4.4.1", "GR", 1, "THE OBJECT-COMPUTER PARAGRAPH", status="obsolete: C.1(4)", rid="1985-GR-VI.4.4.1-p1"),
            _row("VI.6.12.1", "GR", 1, "THE ENTER STATEMENT", status="obsolete: C.1(13)", rid="1985-GR-VI.6.12.1-p1"),
            _row("VI.6.12.4", "GR", 1, "THE ENTER STATEMENT", status="obsolete: C.1(13)"),
            _row("A.2.22", "GR", 1, "LOWER-CASE", folio="A-51")]
    return T.Catalog(rows)


def _line(n, text, indicator=" "):
    return f"{n * 100:06d}{indicator}{text}".ljust(72) + "NC1014.2"


def _program(*lines):
    return "\n".join(_line(i, t, "*" if t.startswith("*") else " ") if not t.startswith("*") else
                     _line(i, t[1:], "*") for i, t in enumerate(lines, 1))


def case_01_a_program_holds_one_unit_per_distinct_paragraph_name_with_the_references_in_effect(expect):
    text = _program(
        'MOVE "MULTIPLY BY" TO FEATURE.', 'MOVE "VI-106 6.19.4 GR1" TO ANSI-REFERENCE.',
        'MPY-TEST-F1-1.', 'MOVE "MPY-TEST-F1-1 " TO PAR-NAME.', 'MPY-TEST-F1-1-0.',
        '*MOVE "NOT-A-UNIT" TO PAR-NAME.',
        'MOVE "VI-42 5.12.4" TO ANSI-REFERENCE.', 'MOVE "SIGN" TO FEATURE.',
        'MOVE "MPY-TEST-F1-1" TO PAR-NAME.', 'MOVE "TWO" TO FEATURE, PAR-NAME.',
        'IF REC-CT EQUAL TO ZERO MOVE SPACE TO PAR-NAME.', 'MOVE TAB-LOC TO PAR-NAME.')
    prog = T.parse_program("X", text)
    expect.equal("a name once, the trailing blank trimmed, a commented move no unit and a SPACE reset no unit",
                 list(prog.units), ["MPY-TEST-F1-1", "TWO"])
    expect.equal("the first move's lines, feature and reference", prog.units["MPY-TEST-F1-1"], ([4, 9], "MULTIPLY BY", "VI-106 6.19.4 GR1"))
    expect.equal("the reference and feature in effect at a later unit", prog.units["TWO"], ([10], "TWO", "VI-42 5.12.4"))
    expect.equal("a PAR-NAME move naming no literal is listed, not dropped", prog.unenumerated, [(12, "MOVE TAB-LOC TO PAR-NAME")])


def case_02_a_unit_before_any_reference_has_none(expect):
    prog = T.parse_program("X", _program('MOVE "A-TEST" TO PAR-NAME.'))
    expect.equal("no reference", prog.units["A-TEST"], ([1], "", None))


def case_03_a_flag_comment_is_a_unit_of_the_statement_it_follows(expect):
    text = _program("AUTHOR. SOMEONE.", "*Message expected for above statement: OBSOLETE",
                    "ALTER A TO B.", "*MESSAGE EXPECTED FOR ABOVE STATEMENT: NON-CONFORMING STANDARD",
                    "*Message expected for following statement: NON-CONFORMING", "STOP RUN.")
    expect.equal("each flag is a unit: its line, its kind, the statement flagged",
                 T.parse_program("X", text).flags,
                 [(2, "OBSOLETE", "AUTHOR SOMEONE"), (4, "NON-CONFORMING", "ALTER A TO B"), (5, "NON-CONFORMING", "")])


def case_04_a_reference_is_read_as_page_and_section_tokens(expect):
    def tokens(ref):
        return [(c.roman, c.page, c.section, c.rules) for c in T.parse_reference(ref)]
    expect.equal("page, section and rule", tokens("VI-106 6.19.4 GR1"), [("VI", 106, "6.19.4", [1])])
    expect.equal("a digit typed for a letter of the numeral", tokens("V1-89 6.15.4 GR2"), [("VI", 89, "6.15.4", [2])])
    expect.equal("a range of rules", tokens("V1-71 6.5.4 GR1-5"), [("VI", 71, "6.5.4", [1, 2, 3, 4, 5])])
    expect.equal("a parenthesised rule number", tokens("VII-51,4.6.4(5)"), [("VII", 51, "4.6.4", [5])])
    expect.equal("a second section after a slash", tokens("VI-89 6.15.3/4"),
                 [("VI", 89, "6.15.3", []), ("VI", None, "6.15.4", [])])
    expect.equal("a page alone and an annex section", tokens("VI-31"), [("VI", 31, None, [])])
    expect.equal("an annex reference", tokens("See ref. A-65 2.65"), [("A", 65, "2.65", [])])
    expect.equal("a reference naming nothing", tokens("FILE STATUS"), [])


def case_05_a_unit_maps_to_its_rows_only_when_the_reference_and_its_words_agree(expect):
    catalog = _catalog()
    mapped = T.map_unit(catalog, "SIGN-TEST-1", [4], "SIGN", "VI-42 5.12.4 GR2")
    expect.equal("the rule it names, the section's subject naming the unit",
                 (mapped["disposition"], mapped["rows"]), ("rows", ["1985-GR-VI.5.12.4-2"]))
    keyed = T.map_unit(catalog, "SIGN-TEST-F1-1", [4], "SIGN", "VI-42 5.12.4 GR2")
    expect.equal("a format-keyed name keeps its format", keyed.get("format"), 1)
    fmt = T.map_unit(catalog, "MPY-TEST-F1-1", [4], "MULTIPLY BY", "VI-107 6.20.4 GR1")
    expect.equal("a format-keyed name adds the statement's FMT row", fmt["rows"], ["1985-GR-VI.6.20.4-1", "1985-FMT-VI.6.20.2"])
    section_only = T.map_unit(catalog, "SIGN-TEST-2", [4], "SIGN", "VI-42 5.12.4")
    expect.equal("a section with no rule named is ambiguous: every row of it is a candidate",
                 (section_only["disposition"], section_only["resolution"], section_only["literal"]),
                 ("open", "ambiguous", ["1985-GR-VI.5.12.4-1", "1985-GR-VI.5.12.4-2"]))


def case_06_numbering_one_section_short_is_never_believed_but_offers_the_section_the_words_name(expect):
    unit = T.map_unit(_catalog(), "MPY-TEST-F1-1", [4], "MULTIPLY BY", "VI-106 6.19.4 GR1")
    expect.equal("the literal reference names MOVE: a word of the unit does not agree, so the unit stays open",
                 (unit["disposition"], unit["resolution"], unit["literal"], unit["literal-agrees"]),
                 ("open", "ambiguous", ["1985-GR-VI.6.19.4-1"], False))
    expect.equal("MULTIPLY is the one section of the chapter whose subject names the unit: its rule 1 is offered",
                 unit["by-name"], ["1985-GR-VI.6.20.4-1"])


def case_07_a_unit_with_no_usable_reference_is_unmapped(expect):
    catalog = _catalog()
    for ref in (None, "FILE STATUS", "VI-943"):
        unit = T.map_unit(catalog, "X-TEST", [1], "UNRELATED", ref)
        expect.equal(f"reference {ref!r}", (unit["disposition"], unit["resolution"]), ("open", "unmapped"))


def case_08_an_annex_section_unit_agrees_by_its_function_name(expect):
    unit = T.map_unit(_catalog(), "F-LOWCASE-04", [1], "LOWER-CASE Function", "See ref. A-51 2.22")
    expect.equal("one row at the section and its subject names the function", (unit["disposition"], unit["rows"]),
                 ("rows", ["1985-GR-A.2.22-1"]))


def case_09_a_flag_maps_through_its_appendix_c1_item(expect):
    catalog = _catalog()
    one = T.map_flag(catalog, 5, "OBSOLETE", "OBJECT-COMPUTER. MEMORY SIZE 100 CHARACTERS", False)
    expect.equal("an element whose item marks one row maps to it", (one["disposition"], one["rows"], one["element"]),
                 ("rows", ["1985-GR-VI.4.4.1-p1"], "C.1(4)"))
    many = T.map_flag(catalog, 6, "OBSOLETE", "ENTER COBOL", False)
    expect.equal("an element whose item marks several is ambiguous", (many["disposition"], many["resolution"]),
                 ("open", "ambiguous"))
    expect.equal("with every row as a candidate", many["candidates"], ["1985-GR-VI.6.12.1-p1", "1985-GR-VI.6.12.4-1"])
    none = T.map_flag(catalog, 7, "OBSOLETE", "SOMETHING ELSE", False)
    expect.equal("a statement naming no element is unmapped", (none["disposition"], none["resolution"], none["element"]),
                 ("open", "unmapped", None))
    fips = T.map_flag(catalog, 8, "NON-CONFORMING", "STOP RUN", True)
    expect.equal("a FIPS flag is excluded with its reason", (fips["disposition"], fips["reason"]), ("excluded", "fips-pub-21-1"))


def case_10_a_fips_program_holds_only_excluded_flag_units(expect):
    text = _program("STOP RUN.", "*MESSAGE EXPECTED FOR ABOVE STATEMENT: NON-CONFORMING STANDARD")
    obj = T.build_program(_catalog(), "FIPS1", text)
    expect.equal("one excluded unit", [(u["unit"], u["disposition"]) for u in obj["units"]], [("NON-CONFORMING@2", "excluded")])


def _world(programs):
    directory = scratch_dir("crosswalk-")
    catalog = _catalog()
    for name, obj in T.build_all(catalog, programs).items():
        (directory / f"{name}.json").write_bytes(T.dump(obj).encode("utf-8"))
    return catalog, directory


def _edit(directory, name, change):
    path = directory / f"{name}.json"
    obj = json.loads(path.read_text(encoding="utf-8"))
    change(obj)
    path.write_bytes(T.dump(obj).encode("utf-8"))


def case_11_the_audit_is_silent_on_the_files_the_tool_writes(expect):
    programs = {"A": _program('MOVE "SIGN" TO FEATURE.', 'MOVE "VI-42 5.12.4 GR1" TO ANSI-REFERENCE.', 'MOVE "SIGN-TEST-1" TO PAR-NAME.')}
    catalog, directory = _world(programs)
    expect.equal("no problem", T.audit(catalog, programs, directory), [])


def case_12_the_audit_names_each_way_a_file_disagrees_with_its_program_or_the_catalog(expect):
    programs = {"A": _program('MOVE "SIGN" TO FEATURE.', 'MOVE "VI-42 5.12.4 GR1" TO ANSI-REFERENCE.', 'MOVE "SIGN-TEST-1" TO PAR-NAME.'),
                "B": _program('MOVE "B-TEST" TO PAR-NAME.')}
    catalog, directory = _world(programs)
    (directory / "B.json").unlink()
    (directory / "Z.json").write_text("{}", encoding="utf-8", newline="\n")
    _edit(directory, "A", lambda o: o["units"][0].update(rows=["1985-GR-NOPE-1"]))
    got = "\n".join(T.audit(catalog, programs, directory))
    expect("a program with no file", "B: no crosswalk file" in got, got)
    expect("a file of no program", "Z: a crosswalk file of no program" in got, got)
    expect("a row the catalog lacks", "row 1985-GR-NOPE-1 is no row of the 1985 catalog" in got, got)
    expect("a mechanical entry that no longer equals the map", "a mechanical entry differs" in got, got)
    (directory / "Z.json").unlink()
    _edit(directory, "A", lambda o: o["units"][0].update(rows=["1985-GR-VI.5.12.4-1"], basis="adjudicated"))
    expect.equal("an adjudicated entry may differ from the map while its rows exist",
                 [p for p in T.audit(catalog, programs, directory) if p.startswith("A:")], [])
    _edit(directory, "A", lambda o: o["units"][0].update(disposition="excluded", reason="nope"))
    expect("an unknown exclusion reason", "exclusion reason 'nope' is unknown" in "\n".join(T.audit(catalog, programs, directory)))
    _edit(directory, "A", lambda o: o["units"][0].update(unit="OTHER", disposition="open", resolution="unmapped"))
    got = "\n".join(T.audit(catalog, programs, directory))
    expect("a unit with no entry and an entry of no unit", "unit SIGN-TEST-1 (line 3) has no entry" in got and "entry OTHER is no unit" in got, got)
    _edit(directory, "A", lambda o: o["units"][0].update(unit="SIGN-TEST-1", feature="CHANGED"))
    expect("a stored fact the program contradicts", "feature is 'CHANGED'" in "\n".join(T.audit(catalog, programs, directory)))


def case_13_the_term_counts_open_units_unenumerated_sites_and_rows_no_cleared_test_witnesses(expect):
    crosswalk = {"A": {"program": "A", "units": [
        {"unit": "a", "disposition": "open", "resolution": "unmapped"},
        {"unit": "b", "disposition": "rows", "rows": ["r1", "r2"]},
        {"unit": "c", "disposition": "rows", "rows": ["r1"]},
        {"unit": "d", "disposition": "excluded", "reason": "fips-pub-21-1"}],
        "unenumerated": [{"line": 1, "text": "x", "reason": "y"}]}}
    witness = {"r1": "suite", "r2": "accepted-uncleared"}
    expect.equal("open 1, unenumerated 1, unsubsumed 1 (b: r2 is not cleared)", T.owed(crosswalk, witness),
                 {"open": 1, "unenumerated": 1, "unsubsumed": 1})
    expect.equal("every row met leaves only the open unit and the site", T.owed(crosswalk, {"r1": "suite", "r2": "derivation"}),
                 {"open": 1, "unenumerated": 1, "unsubsumed": 0})
    try:
        T.owed(crosswalk, {"r1": "suite"})
        expect("a row the catalog lacks fails loudly", False, "no error")
    except T.CrosswalkError:
        expect("a row the catalog lacks fails loudly", True)


def case_14_a_missing_file_or_another_edition_fails_loudly(expect):
    directory = scratch_dir("crosswalk-")
    try:
        T.load_crosswalk(directory, {"A": "x"})
        expect("a program with no file fails loudly", False, "no error")
    except T.CrosswalkError:
        expect("a program with no file fails loudly", True)
    try:
        T.unmet("2002")
        expect("an edition but 85 fails loudly", False, "no error")
    except T.CrosswalkError:
        expect("an edition but 85 fails loudly", True)


def case_15_the_element_table_is_held_to_appendix_c1_of_the_transcription(expect):
    items = "\n".join(f"({n}) <u>{words} clause (1 NUC)</u>. text." for n, words in sorted(T.C1_HEADINGS.items()))
    spec = f"## 1. <u>OBSOLETE LANGUAGE ELEMENT LIST</u>\n\n{items}\n\n## 2. <u>NEXT</u>\n"
    expect.equal("a list whose headings say the elements is sound", T.audit_elements(spec), [])
    bad = spec.replace("(10) <u>ALTER clause", "(10) <u>PERFORM clause")
    got = "\n".join(T.audit_elements(bad))
    expect("a heading that names another element", "item 10 is 'PERFORM clause (1 NUC)'" in got, got)
    expect("a transcription with no Appendix C.1", "is not in the 1985 transcription" in "\n".join(T.audit_elements("x")))


def case_16_a_reference_moved_after_the_name_in_the_same_init_paragraph_is_the_units_own(expect):
    text = _program(
        'MOVE "VI-72 6.5.4 GR7" TO ANSI-REFERENCE.', 'MOVE "ACCEPT DATE" TO FEATURE.',
        'ACC-TEST-F2-1.', 'ACCEPT X FROM DATE.',
        'ACC-INIT-F2-2.', 'MOVE "ACC-TEST-F2-2" TO PAR-NAME.', 'MOVE "VI-72 6.5.4 GR8" TO ANSI-REFERENCE.',
        'MOVE "ACCEPT DAY" TO FEATURE.', 'ACC-TEST-F2-2.', 'ACCEPT X FROM DAY.',
        'ACC-INIT-F2-3.', 'MOVE "ACC-TEST-F2-3" TO PAR-NAME.', 'ACC-TEST-F2-3.')
    prog = T.parse_program("X", text)
    expect.equal("the reference and feature set after the name and before its test paragraph", prog.units["ACC-TEST-F2-2"],
                 ([6], "ACCEPT DAY", "VI-72 6.5.4 GR8"))
    expect.equal("a unit with none of its own keeps the one in effect", prog.units["ACC-TEST-F2-3"],
                 ([12], "ACCEPT DAY", "VI-72 6.5.4 GR8"))


def case_17_a_unit_binds_its_feature_and_reference_once_and_a_later_move_is_the_next_units(expect):
    # a second FEATURE line of the same test, after the name and the first FEATURE move
    two_lines = T.parse_program("X", _program(
        'ACC-INIT-F1-1.', 'MOVE "ACC-TEST-F1-1" TO PAR-NAME.', 'MOVE "VI-72 6.5.4 GR7" TO ANSI-REFERENCE.',
        'MOVE "ACCEPT DATE" TO FEATURE.', 'MOVE "CONTINUED" TO FEATURE.', 'ACC-TEST-F1-1.'))
    expect.equal("a second FEATURE move before the test paragraph does not replace the first", two_lines.units["ACC-TEST-F1-1"],
                 ([2], "ACCEPT DATE", "VI-72 6.5.4 GR7"))
    # a FEATURE move in a failure branch that follows the unit's own moves with no header between
    failure = T.parse_program("X", _program(
        'MOVE "ACCEPT DAY" TO FEATURE.', 'MOVE "VI-72 6.5.4 GR8" TO ANSI-REFERENCE.',
        'MOVE "ACC-TEST-F1-2" TO PAR-NAME.', 'IF X EQUAL TO 1 MOVE "FAILED" TO FEATURE.', 'ACC-TEST-F1-2.'))
    expect.equal("a move after the unit's own, in its window, is not the unit's", failure.units["ACC-TEST-F1-2"],
                 ([3], "ACCEPT DAY", "VI-72 6.5.4 GR8"))
    # the next unit's FEATURE moved just before its PAR-NAME move, in the previous unit's paragraph
    chained = T.parse_program("X", _program(
        'ACC-INIT-F1-3.', 'MOVE "ACC-TEST-F1-3" TO PAR-NAME.', 'MOVE "VI-72 6.5.4 GR1" TO ANSI-REFERENCE.',
        'MOVE "ACCEPT TIME" TO FEATURE.', 'MOVE "SIGN" TO FEATURE.', 'MOVE "VI-42 5.12.4" TO ANSI-REFERENCE.',
        'MOVE "ACC-TEST-F1-4" TO PAR-NAME.', 'ACC-TEST-F1-3.'))
    expect.equal("the first unit keeps its own", chained.units["ACC-TEST-F1-3"], ([2], "ACCEPT TIME", "VI-72 6.5.4 GR1"))
    expect.equal("the next unit takes the moves made before its name", chained.units["ACC-TEST-F1-4"],
                 ([7], "SIGN", "VI-42 5.12.4"))


_SIGN = ('MOVE "SIGN" TO FEATURE.', 'MOVE "VI-42 5.12.4 GR1" TO ANSI-REFERENCE.', 'MOVE "SIGN-TEST-1" TO PAR-NAME.',
         'MOVE "MPY-TEST-F1-1" TO PAR-NAME.')
_DECIDED = {"disposition": "rows", "rows": ["1985-GR-VI.5.12.4-2"], "basis": "adjudicated: planted (selftest)"}


def case_18_a_rebuild_keeps_an_adjudicated_entry_verbatim_and_rebuilds_the_mechanical_ones(expect):
    programs = {"A": _program(*_SIGN)}
    catalog, directory = _world(programs)
    _edit(directory, "A", lambda o: (o["units"][0].update(_DECIDED), o["units"][1].update(feature="STALE")))
    before = json.loads((directory / "A.json").read_text(encoding="utf-8"))
    files, kept, conflicts = T.build(catalog, programs, directory)
    after = json.loads((directory / "A.json").read_text(encoding="utf-8"))
    expect.equal("one file, one adjudication kept, no conflict", (files, kept, conflicts), (1, 1, []))
    expect.equal("the adjudicated entry is written back verbatim, in its place", after["units"][0], before["units"][0])
    expect.equal("the mechanical entry is rebuilt", after["units"][1], T.build_program(catalog, "A", programs["A"])["units"][1])
    expect.equal("the audit of the rebuilt file is silent", T.audit(catalog, programs, directory), [])
    raw = (directory / "A.json").read_bytes()
    expect("the file is LF, never CRLF", b"\n" in raw and b"\r" not in raw, repr(raw[:80]))


def case_19_a_rebuild_reports_an_adjudication_the_program_no_longer_supports_and_still_keeps_it(expect):
    catalog, directory = _world({"A": _program(*_SIGN)})
    _edit(directory, "A", lambda o: (o["units"][0].update(_DECIDED), o["units"][1].update(_DECIDED)))
    kept = json.loads((directory / "A.json").read_text(encoding="utf-8"))["units"]
    # the program moved: SIGN-TEST-1 has another FEATURE, MPY-TEST-F1-1 is gone
    moved = {"A": _program('MOVE "SIGNS" TO FEATURE.', 'MOVE "VI-42 5.12.4 GR1" TO ANSI-REFERENCE.',
                           'MOVE "SIGN-TEST-1" TO PAR-NAME.')}
    files, count, conflicts = T.build(catalog, moved, directory)
    got = "\n".join(conflicts)
    expect.equal("two adjudications kept, two conflicts", (count, len(conflicts)), (2, 2))
    expect("a changed fact is named", "A:SIGN-TEST-1: feature is 'SIGN', the program says 'SIGNS' (adjudicated entry kept" in got, got)
    expect("a vanished unit is named", "A:MPY-TEST-F1-1: an adjudicated entry of no unit of the program (kept" in got, got)
    expect.equal("both entries are kept verbatim", json.loads((directory / "A.json").read_text(encoding="utf-8"))["units"], kept)
    audit = "\n".join(T.audit(catalog, moved, directory))
    expect("the audit names the stale fact and the vanished unit",
           "feature is 'SIGN', the program says 'SIGNS'" in audit and "entry MPY-TEST-F1-1 is no unit" in audit, audit)
    expect("and no rebuild would change either entry", "a rebuild would change" not in audit, audit)


def case_20_a_unit_adjudicated_twice_fails_loudly_and_writes_nothing(expect):
    programs = {"A": _program(*_SIGN)}
    catalog, directory = _world(programs)
    _edit(directory, "A", lambda o: o.update(units=[o["units"][0] | _DECIDED, o["units"][0] | _DECIDED | {"rows": []}]))
    before = (directory / "A.json").read_bytes()
    try:
        T.build(catalog, programs, directory)
        expect("a unit adjudicated twice fails loudly", False, "no error")
    except T.CrosswalkError as e:
        expect("a unit adjudicated twice fails loudly", "adjudicated twice" in str(e), str(e))
    expect.equal("the file is untouched", (directory / "A.json").read_bytes(), before)
    expect("the audit names it", "adjudicated twice" in "\n".join(T.audit(catalog, programs, directory)))


def case_21_the_audit_refuses_a_file_a_rebuild_would_change_an_adjudicated_entry_of(expect):
    programs = {"A": _program(*_SIGN)}
    catalog, directory = _world(programs)
    _edit(directory, "A", lambda o: o["units"][0].update(_DECIDED))
    stored = json.loads((directory / "A.json").read_text(encoding="utf-8"))
    fresh = T.build_program(catalog, "A", programs["A"])
    expect.equal("a sound file", T.rebuild_problems(fresh, stored), [])
    real = T.merge
    try:   # a merge that drops adjudications, as the builder before PB6131 did
        T.merge = lambda f, s: (f, 0, [])
        expect.equal("is named when the rebuild would replace its adjudicated entry", T.rebuild_problems(fresh, stored),
                     ["A:SIGN-TEST-1: a rebuild would change this adjudicated entry"])
    finally:
        T.merge = real


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
