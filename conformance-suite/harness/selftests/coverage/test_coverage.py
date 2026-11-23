#!/usr/bin/env python3
"""Which coverage points a parse credits (DESIGN-conformance-suite.md section 3.4, decision 8; kb/Work PB6050): the matcher on
planted formats and token streams, the operand kinds, the frames of a row, the record's encoding and the loud failures of `unmet`."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/coverage.py
F = T.formats
P = T.points


def _credit(tree_json, tokens):
    """The point ids (after the row and format) a node's tokens credit on a planted format, [] when the format derives none."""
    tree = F.from_json(tree_json)
    pts = P.points_of("1985-FMT-X.1.1", 1, tree)
    m = T.Matcher(tree, tokens)
    if not m.credit():
        return []
    ids = {id(n): i for i, n in enumerate(P.number(tree))}
    got = {(ids[i], e) for i, e in m.events}
    return [p["id"].split("/", 1)[1] for p in pts if (p["node"], p["event"]) in got]


def _kw(*words):
    return [(w, "kw") for w in words]


ADD = [F.SEQ, [F.WORD, "ADD"], [F.OPERAND, "identifier"], [F.WORD, "TO"], [F.OPERAND, "identifier"], [F.OPT, [F.WORD, "ROUNDED"]]]


def case_01_a_bracket_left_out_credits_the_omission_and_written_the_other(expect):
    expect.equal("omitted", _credit(ADD, _kw("ADD") + [("", "id")] + _kw("TO") + [("", "id")]), ["f1", "f1/omit:ROUNDED"])
    expect.equal("written", _credit(ADD, _kw("ADD") + [("", "id")] + _kw("TO") + [("", "id")] + _kw("ROUNDED")), ["f1", "f1/written:ROUNDED"])


def case_02_a_node_the_format_does_not_derive_credits_nothing(expect):
    expect.equal("an unlisted word", _credit(ADD, _kw("ADD") + [("", "id")] + _kw("INTO") + [("", "id")]), [])
    expect.equal("text left over", _credit(ADD, _kw("ADD") + [("", "id")] + _kw("TO") + [("", "id")] + _kw("ROUNDED", "ROUNDED")), [])


def case_03_only_the_alternative_taken_is_credited(expect):
    tree = [F.SEQ, [F.WORD, "OPEN"], [F.ALT, [F.WORD, "INPUT"], [F.WORD, "OUTPUT"]], [F.OPERAND, "file-name"]]
    expect.equal("INPUT", _credit(tree, _kw("OPEN", "INPUT") + [("", "id")]), ["f1", "f1/written:INPUT"])


def case_04_a_portion_is_repeated_only_when_written_twice(expect):
    tree = [F.SEQ, [F.WORD, "GO"], [F.REP, [F.OPERAND, "procedure-name"]]]
    expect.equal("once", _credit(tree, _kw("GO") + [("", "id")]), ["f1"])
    expect.equal("twice", _credit(tree, _kw("GO") + [("", "id")] * 2), ["f1", "f1/rep:opnd:procedure-name"])


def case_05_choice_indicators_credit_one_alone_and_two_out_of_order(expect):
    tree = [F.SEQ, [F.WORD, "SET"], [F.BAR, [F.WORD, "A"], [F.WORD, "B"]]]
    expect.equal("one alone", _credit(tree, _kw("SET", "B")), ["f1", "f1/alone:B"])
    expect.equal("out of order", _credit(tree, _kw("SET", "B", "A")), ["f1", "f1/order:A+B"])


def case_06_an_optional_word_is_credited_by_its_presence_and_by_its_absence(expect):
    tree = [F.SEQ, [F.WORD, "MOVE"], [F.OPERAND, "identifier"], [F.OPTIONAL, "TO"], [F.OPERAND, "identifier"]]
    expect.equal("written", _credit(tree, _kw("MOVE") + [("", "id")] + _kw("TO") + [("", "id")]), ["f1", "f1/written:TO"])
    expect.equal("omitted", _credit(tree, _kw("MOVE") + [("", "id")] * 2), ["f1", "f1/omit:TO"])


def case_07_an_operand_is_one_item_of_its_kind(expect):
    ok = T.operand_ok
    expect("a user word is an identifier", ok("identifier", ("", "id")))
    expect("a literal is no identifier... but a compound reference is", ok("identifier", ("dataReference", "ph")) and not ok("literal", ("", "id")))
    expect("an integer is only an integer", ok("integer", ("", "int")) and not ok("integer", ("", "str")))
    expect("a nested statement is no operand", not ok("identifier", (T.STATEMENT, "ph")))
    expect("a figurative constant is a literal", ok("literal", ("ZERO", "kw")))
    expect("a division is its own placeholder", ok("data-division", ("dataDivision", "ph")) and not ok("data-division", ("", "id")))


def case_08_statements_nested_in_a_phrase_are_a_run_of_one_or_more(expect):
    tree = [F.SEQ, [F.WORD, "IF"], [F.OPERAND, "condition"], [F.OPERAND, "statement"], [F.OPT, [F.SEQ, [F.WORD, "ELSE"], [F.OPERAND, "statement"]]]]
    s = (T.STATEMENT, "ph")
    expect.equal("a branch of two statements", _credit(tree, _kw("IF") + [("condition", "ph"), s, s]), ["f1", "f1/omit:ELSE"])
    expect.equal("an ELSE", _credit(tree, _kw("IF") + [("condition", "ph"), s] + _kw("ELSE") + [s]), ["f1", "f1/written:ELSE"])


def case_09_the_period_and_the_comma_are_no_token(expect):
    expect("a period", T.token_of(".", "DOT") is None)
    expect("a comma", T.token_of(",", "COMMA") is None)
    expect.equal("a user word is erased unless a format prints it", (T.token_of("X", "IDENTIFIER"), T.token_of("COS", "IDENTIFIER", {"COS"})),
                 (("", "id"), ("COS", "id")))


def case_10_the_record_is_point_numbers_as_ranges(expect):
    expect.equal("encoded", T.encode([5, 3, 4, 9, 3, 12, 11]), "3-5,9,11-12")
    expect.equal("round trip", T.decode(T.encode([0, 1, 2, 7])), [0, 1, 2, 7])
    expect.equal("empty", (T.encode([]), T.decode("")), ("", []))


def case_11_a_row_with_no_frame_fails_loudly_and_one_without_a_parse_node_says_why(expect):
    rules = T.grammar_rules()
    try:
        T.frames_of("1985-FMT-Z.9.9.2", "THE UNHEARD-OF PIECE", rules)
        expect("an unplaceable row is refused", False, "no error")
    except T.points.CatalogError:
        expect("an unplaceable row is refused", True)
    expect.equal("COPY has no node", T.frames_of("1985-FMT-XII.2.2", "THE COPY STATEMENT", rules), None)
    expect.equal("MOVE is its statement", T.frames_of("1985-FMT-VI.6.19.2", "THE MOVE STATEMENT", rules), ["moveStatement"])
    expect.equal("a function", T.frames_of("1989-FMT-A.2.10.2", "THE COS FUNCTION", rules), ["functionCall"])


def case_12_every_row_of_the_committed_catalog_has_a_frame_the_grammar_has(expect):
    rules = T.grammar_rules()
    cat = T.Catalog(P.load())
    expect("every frame is a grammar rule", all(f in rules for f in cat.frames), str(sorted(set(cat.frames) - rules)))
    expect("the rows with no parse node are the named ones", len(cat.no_frame) == len(T.NO_FRAME), str(sorted(cat.no_frame)))


def case_13_the_committed_catalog_credits_a_move_a_corresponding_move_and_a_function(expect):
    cat = T.Catalog(P.load())
    move = cat.credit("moveStatement", _kw("MOVE") + [("", "id")] + _kw("TO") + [("", "id")])
    expect("MOVE format 1", "1985-FMT-VI.6.19.2/f1" in move and "1985-FMT-VI.6.19.2/f2" not in move, str(sorted(move)[:4]))
    corr = cat.credit("moveStatement", _kw("MOVE", "CORRESPONDING") + [("", "id")] + _kw("TO") + [("", "id")])
    expect("MOVE CORRESPONDING", "1985-FMT-VI.6.19.2/f2/written:CORRESPONDING" in corr, str(sorted(corr)[:4]))
    call = cat.credit("functionCall", [("FUNCTION", "kw"), ("COS", "id"), ("(", "kw"), ("arithmeticExpression", "ph"), (")", "kw")])
    expect("FUNCTION COS (x)", "1989-FMT-A.2.10.2/f1" in call, str(sorted(call)[:4]))


def case_14_a_file_description_credits_without_the_record_descriptions_after_it(expect):
    cat = T.Catalog(P.load())
    toks = _kw("FD") + [("", "id")] + [("dataDescriptionEntry", "ph")] * 2
    got = cat.credit("fileDescriptionEntry", toks)
    expect("the sequential FD format", "1985-FMT-VII.3.2.2/f1" in got, str(sorted(got)[:3]))


def case_15_a_clause_is_credited_without_the_context_the_page_prints_before_it(expect):
    cat = T.Catalog(P.load())
    got = cat.credit("redefinesClause", _kw("REDEFINES") + [("", "id")])
    expect("REDEFINES data-name-2", "1985-FMT-VI.5.10.2/f1" in got, str(sorted(got)[:3]))


def case_16_a_record_that_is_not_a_measure_of_the_tests_fails_loudly(expect):
    class Case:
        test_id = "t1"
        path = "none"
    saved = (T.accepted, T.read_record)
    T.accepted = lambda edition: [Case()]
    T.read_record = lambda edition: {}
    try:
        T.unmet("85")
        expect("a test with no record is refused", False, "no error")
    except RuntimeError as e:
        expect("a test with no record is refused", "no points record" in str(e), str(e))
    finally:
        T.accepted, T.read_record = saved


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
