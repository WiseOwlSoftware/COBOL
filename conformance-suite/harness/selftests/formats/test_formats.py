#!/usr/bin/env python3
"""A printed general format read into the tree of its alternatives (DESIGN-conformance-suite.md section 3.4, kb/Work PB6050):
each mark of the notation planted on its own in a drawing, and the hand-read figures held to the words of their drawing."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/formats.py


def _tree(*lines):
    return T.read(list(lines)).as_json()


def case_01_a_required_word_and_an_operand(expect):
    expect.equal("the tree", _tree("<u>CONTINUE</u> identifier-1"), [T.SEQ, [T.WORD, "CONTINUE"], [T.OPERAND, "identifier"]])


def case_02_a_one_row_bracket_holds_an_optional_word(expect):
    expect.equal("the tree", _tree("<u>EXIT</u> [ NEXT ]"), [T.SEQ, [T.WORD, "EXIT"], [T.OPT, [T.OPTIONAL, "NEXT"]]])


def case_03_a_brace_of_stacked_rows_is_an_alternation(expect):
    expect.equal("the tree", _tree("<u>ADD</u> ╭ identifier-1 ╮", "    ┤              ├", "    ╰ literal-1    ╯"),
                 [T.SEQ, [T.WORD, "ADD"], [T.ALT, [T.OPERAND, "identifier"], [T.OPERAND, "literal"]]])


def case_04_an_ellipsis_repeats_the_group_before_it(expect):
    expect.equal("the tree", _tree("<u>GO</u> { procedure-name-1 } ..."),
                 [T.SEQ, [T.WORD, "GO"], [T.REP, [T.OPERAND, "procedure-name"]]])


def case_05_a_bracket_of_stacked_rows_is_at_most_one_of_them(expect):
    expect.equal("the tree", _tree("┌ <u>AREA</u>  ┐", "│       │", "└ <u>AREAS</u> ┘"), [T.OPT, [T.ALT, [T.WORD, "AREA"], [T.WORD, "AREAS"]]])


def case_06_a_bracket_spanning_rows_round_a_sequence_and_a_brace(expect):
    expect.equal("the tree",
                 _tree("┌           ╭ <u>BEFORE</u> ╮ ┐", "│ WITH <u>TEST</u> ┤        ├ │", "└           ╰ <u>AFTER</u>  ╯ ┘"),
                 [T.OPT, [T.SEQ, [T.OPTIONAL, "WITH"], [T.WORD, "TEST"], [T.ALT, [T.WORD, "BEFORE"], [T.WORD, "AFTER"]]]])


def case_07_a_typed_bracket_across_lines_holds_the_rows_between(expect):
    expect.equal("the tree", _tree("<u>OBJECT-COMPUTER</u>. [ computer-name", "", "[ <u>WORDS</u> ] . ]"),
                 [T.SEQ, [T.WORD, "OBJECT-COMPUTER"], [T.OPT, [T.SEQ, [T.OPERAND, "computer-name"], [T.OPT, [T.WORD, "WORDS"]]]]])


def case_08_a_bracket_whose_rows_begin_in_different_columns_is_one_wrapped_phrase(expect):
    got = _tree("┌ <u>AFTER</u> identifier-1 ┐", "│" + " " * 20 + "│", "└    <u>BY</u> literal-1    ┘")
    expect("a sequence, not two alternatives", got[1][0] == T.SEQ, str(got))


def case_09_a_brace_is_alternatives_whatever_the_columns(expect):
    got = _tree("╭ <u>INPUT</u> file-name-1    ╮", "┤" + " " * 22 + "├", "╰   <u>OUTPUT</u> file-name-2 ╯")
    expect("an alternation", got[0] == T.ALT, str(got))


def case_10_a_level_2_box_holding_only_an_ellipsis_repeats_the_portion_before_it(expect):
    expect.equal("the tree", _tree("{ identifier-1 } ║ … ║"), [T.REP, [T.OPERAND, "identifier"]])


def case_11_a_delimiter_with_no_partner_is_refused_not_guessed_past(expect):
    try:
        _tree("<u>OPEN</u> ┌ file-name-1")
        expect("an unpaired bracket glyph is refused", False, "no error")
    except T.Unreadable:
        expect("an unpaired bracket glyph is refused", True)


def case_12_a_decided_reading_must_carry_the_words_of_its_figure(expect):
    reading = T.from_json(T.DECIDED_READINGS[("1985-FMT-XIII.3.17.2", 1)])
    figure = ["[<u>SIGN</u> IS]  {<u>LEADING</u>  } <u>SEPARATE</u> CHARACTER", "         { <u>TRAILING</u> }"]
    expect("the report group SIGN clause's reading agrees with its drawing", T.agrees(figure, reading))
    expect("a figure with another word is refused", not T.agrees([figure[0].replace("LEADING", "HEADING"), figure[1]], reading))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
