#!/usr/bin/env python3
"""The canonicalizer of citation designators on planted files (kb/Work PB4976, PB6000, PB6035): each designator with one
canonical form rewritten and what it leaves; the three ways the first run over C# broke 61 cases (a later path level
written without its space, a label's `)` that also closed the message's parenthesis, a designator completed by an
interpolation hole), each planted and seen handled; and a message string rewritten only with every expectation that
quotes it, or not at all."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

R = selftest_cases.subject(__file__)   # scripts/spec/citation_rewrite.py
DIAG = "conformance-suite/wiseowl/diagnostics/x.json"


def _run(scope: dict, around: dict | None = None):
    plan = R.plan_texts(scope, around or {})
    return R.applied(plan, {**(around or {}), **scope}), plan


def _reasons(plan) -> dict:
    return {(r.file, r.line): r.reason for r in plan.refusals}


def case_00_a_document_line_takes_each_designator_with_one_form(expect):
    rewrites = [  # (written, canonical or None: left as written)
        ("(ISO §2023:14.9.37.4 GR3 c) 2) falls", "(ISO §2023:14.9.37.4 GR3c.2) falls"),  # citation-audit: example
        ("(COBOL2199, §2023:13.18.35.3 SR6 e)) an", "(COBOL2199, §2023:13.18.35.3 SR6e) an"),  # citation-audit: example
        ("the receiver (§2023:14.9.25.4 rule 6) is", "the receiver (§2023:14.9.25.4 GR6) is"),  # citation-audit: example
        ("the receiver §2023:14.9.25.4 6) b) is", "the receiver §2023:14.9.25.4 GR6b is"),  # citation-audit: example
        ("the SORT statement (§1985:XI.4.4.4 rule 7)", "the SORT statement (§1985:XI.4.4.4 GR7)"),  # citation-audit: example
        ("an unlimited capacity (§2023:8.5.1.9.1 item (2))", "an unlimited capacity (§2023:8.5.1.9.1 GR2)"),  # citation-audit: example
        ("status 24 (§1985:VIII.1.3.4 item (3) c)) is", "status 24 (§1985:VIII.1.3.4 GRL2.3c) is"),  # citation-audit: example
        ("the composite (§1985:VI.6.4.4 item 2) of", "the composite (§1985:VI.6.4.4 SR2) of"),  # citation-audit: example
        ("the list (§1985:C.2 item (48)) of", "the list (§1985:C.2 item 48) of"),  # citation-audit: example
        ("a text word (§1985:III.2 item 3) of", None),  # citation-audit: example
        ("I-O status (§1985:VIII.1.3.4 item (3)) of", None),  # citation-audit: example
        ("the regions (§2023:14.9.22.4 GR9 a) b)) of", None),  # citation-audit: example
        ("the keys (§2023:14.9.51.4 37) 39)) of", None),  # citation-audit: example
        ("the operands (§2023:14.9.18.3 SR4 a)/c)) of", None),  # citation-audit: example
        ("identity (§2023:9.3.8.2.3 rules 2, 3, 6 and 7) of", None),  # citation-audit: example
        ("the length (§2023:8.8.2 rule 9/10) of", None),  # citation-audit: example
        ("the modes (§2023:14.7.4.3 rule 3 to 10; a MOVE", None),  # citation-audit: example
        ("a phantom's (§2023:14.9.25.4 rule 6)", None),  # citation-audit: example
    ]
    got = [_run({"docs/x.md": a})[0]["docs/x.md"] for a, _ in rewrites]
    expect.equal("a spaced path is glued, a rule named by number takes its catalog designator, an item the one rule that "
                 "holds it; a list, an item several lists print and a line that reports a wrong citation stay as written",
                 got, [b if b is not None else a for a, b in rewrites])
    _, plan = _run({"docs/x.md": "the regions (§2023:14.9.22.4 GR9 a) b)) of"})  # citation-audit: example
    expect.equal("a designator left as written carries its reason", [r.reason for r in plan.refusals],
                 ["a list of designators: no one designator says it"])


def case_01_a_later_path_level_without_its_space_is_read_whole(expect):
    # breakage 1 of kb/Work PB6035: `SR12 a)2.` was glued as `SR12a` and its `2.` left attached: `SR12a2.`
    src = 'class A {\n  string m = "class S is not O (ISO §2023:14.9.39.3 SR12 a)2.)";\n}\n'  # citation-audit: example
    out, _ = _run({"src/A.cs": src})
    expect.equal("the whole path is glued, its number after a dot", out["src/A.cs"].split("\n")[1],
                 '  string m = "class S is not O (ISO §2023:14.9.39.3 SR12a.2)";')
    doc = "the SET (§2023:14.9.39.3 SR12a2.) admits it, and §2023:13.18.60.4 GR22 d)2.a. names it"  # citation-audit: example
    expect.equal("the glued form the first run wrote is itself rewritten, and a three-level path read whole",
                 _run({"docs/x.md": doc})[0]["docs/x.md"],
                 "the SET (§2023:14.9.39.3 SR12a.2.) admits it, and §2023:13.18.60.4 GR22d.2.a names it")


def case_02_a_label_closer_that_closes_the_message_is_kept(expect):
    # breakage 2: `(ISO §2023:14.9.40.3 SR14 e)` lost the `)` that also closed the message's `(`
    src = ('class A {\n'
           '  string n = "TIN: (ISO §2023:14.9.40.3 SR14 e)";\n'  # citation-audit: example
           '  string m = "a key (COBOL2199, §2023:13.18.35.3 SR6 e)) is refused";\n'  # citation-audit: example
           '  string o = "(ISO " +\n    "§2023:14.9.40.3 SR14 e)";\n'  # citation-audit: example
           '}\n')
    lines = _run({"src/A.cs": src})[0]["src/A.cs"].split("\n")
    expect.equal("the closer the message needs is kept, a label's own is dropped, and the count is read over a "
                 "concatenation of literals", [lines[1], lines[2], lines[4]],
                 ['  string n = "TIN: (ISO §2023:14.9.40.3 SR14e)";',
                  '  string m = "a key (COBOL2199, §2023:13.18.35.3 SR6e) is refused";',
                  '    "§2023:14.9.40.3 SR14e)";'])
    open_chain = 'class A {\n  string p = "(ISO " + x + "§2023:14.9.40.3 SR14 e)";\n}\n'  # citation-audit: example
    out, plan = _run({"src/A.cs": open_chain})
    expect("a closer whose opening parenthesis may lie in a non-literal operand is refused, never guessed",
           out["src/A.cs"] == open_chain and "non-literal operand" in _reasons(plan).get(("src/A.cs", 2), ""))
    comment = "// the key (§2023:14.9.40.3 SR14 e)\n"  # citation-audit: example
    expect.equal("a comment keeps the closer its line needs too", _run({"src/B.cs": comment})[0]["src/B.cs"],
                 "// the key (§2023:14.9.40.3 SR14e)\n")


def case_03_a_designator_an_interpolation_or_concatenation_may_complete_is_refused(expect):
    # breakage 3: `rule 9 {arm}))` became `GR9 {arm}))`, printed `GR9 a))`
    src = ('class A {\n'
           '  string q = $"(ISO §2023:9.3.8.2.3 rule 9 {arm}))";\n'  # citation-audit: example
           '  string f = string.Format("(ISO §2023:9.3.8.2.3 rule 9 {0}))", arm);\n'  # citation-audit: example
           '  string c = "(ISO §2023:9.3.8.2.3 rule 9 " + arm + "))";\n'  # citation-audit: example
           '}\n')
    out, plan = _run({"src/A.cs": src})
    why = _reasons(plan)
    expect("an interpolation hole, a format item and a concatenation after the designator each leave it as written",
           out["src/A.cs"] == src and "interpolation hole" in why.get(("src/A.cs", 2), "")
           and "format item" in why.get(("src/A.cs", 3), "") and "concatenated" in why.get(("src/A.cs", 4), ""))


def case_04_a_message_changes_with_every_expectation_that_quotes_it(expect):
    scope = {"src/A.cs": 'class A { string n = "TIN: (ISO §2023:14.9.40.3 SR14 e)"; '  # citation-audit: example
                         'string r = "(ISO §2023:9.3.8.2.3 rule 8)"; }\n',  # citation-audit: example
             "tests/T.cs": 'class T { void F() { Has("SR14 e"); Has("rule 8"); } // SR14 e) is refused\n}\n',
             DIAG: '{"refuse-at": ["2023"], "expect": "SR14 e)"}\n'}
    out, plan = _run(scope)
    expect.equal("the message, its golden's fragment, a test's fragment without the closer and a rule named in words are "
                 "rewritten together; a test's comment is not an expectation",
                 [out["src/A.cs"], out["tests/T.cs"], out[DIAG]],
                 ['class A { string n = "TIN: (ISO §2023:14.9.40.3 SR14e)"; string r = "(ISO §2023:9.3.8.2.3 GR8)"; }\n',
                  'class T { void F() { Has("SR14e"); Has("GR8"); } // SR14 e) is refused\n}\n',
                  '{"refuse-at": ["2023"], "expect": "SR14e)"}\n'])
    expect.equal("the report names each message and each expectation rewritten with it",
                 (len(R.report(plan)["messages"]), sorted({e["file"] for e in R.report(plan)["expectations"]})),
                 (2, [DIAG, "tests/T.cs"]))


def case_05_a_quoted_designator_that_means_two_things_is_refused(expect):
    scope = {"src/A.cs": 'class A { string a = "(ISO §2023:9.3.8.2.3 rule 2)"; '  # citation-audit: example
                         'string b = "(ISO §2023:15.8.3 rule 2)"; }\n',  # citation-audit: example
             "tests/T.cs": 'class T { void F() { Has("rule 2"); } }\n'}
    out, plan = _run(scope)
    expect("a fragment two messages rewrite differently (GR2, AR2) and an expectation quotes is left in both",
           out == scope and all("two ways" in r.reason for r in plan.refusals) and len(plan.refusals) == 2)
    unquoted = {"src/A.cs": scope["src/A.cs"]}
    expect("unquoted, each takes its own designator", "GR2" in _run(unquoted)[0]["src/A.cs"]
           and "AR2" in _run(unquoted)[0]["src/A.cs"])


def case_06_a_quoted_designator_another_message_keeps_is_refused(expect):
    scope = {"src/A.cs": 'class A { string q = $"(ISO §2023:9.3.8.2.3 rule 9 {arm}))"; '  # citation-audit: example
                         'string r = "(ISO §2023:9.3.8.2.3 rule 9)"; }\n',  # citation-audit: example
             "tests/T.cs": 'class T { void F() { Has("rule 9"); } }\n'}
    out, plan = _run(scope)
    expect("a fragment an expectation quotes and a message this run leaves still holds is rewritten nowhere",
           out == scope and any("does not rewrite" in r.reason for r in plan.refusals))
    other = dict(scope, **{"src/A.cs": 'class A { string r = "(ISO §2023:9.3.8.2.3 rule 9)"; }\n',  # citation-audit: example
                           "tests/data.txt": "rule 9\n"})
    out, plan = _run(other)
    expect("a fragment a file no expectation reader rewrites quotes is rewritten nowhere",
           out == other and any("no expectation reader" in r.reason for r in plan.refusals))


def case_07_a_test_literal_and_a_generated_file_are_not_rewritten_by_themselves(expect):
    scope = {"tests/T.cs": 'class T { string s = "(ISO §2023:14.9.40.3 SR14 e)"; }\n',  # citation-audit: example
             "src/R.g.cs": '// (§2023:14.9.40.3 SR14 e)\n'}  # citation-audit: example
    out, plan = _run(scope)
    expect("a test's own literal is left, with its reason, and a generated file is not read",
           out == scope and [r.reason for r in plan.refusals]
           == ["a test's string literal: an expectation changes only with the message it quotes"])


def case_08_a_tail_after_another_clause_is_that_clauses(expect):
    # the dry run of 2026-11-21 read `3) a)` of §2023:6.1 in a test's message as the `3) a)` of §2023:8.4.6.2.1
    scope = {"src/A.cs": 'class A { string m = "hidden (ISO §2023:8.4.6.2.1 3) a))"; }\n',  # citation-audit: example
             "tests/T.cs": 'class T { void F() { Ok("over §2023:6.1 3) a)\'s 255"); Has("§2023:8.4.6.2.1 3) a)"); } }\n'}  # citation-audit: example
    out, _ = _run(scope)
    expect.equal("a quotation of the same tail after another clause is left; one after the rewritten clause is rewritten",
                 out["tests/T.cs"],
                 'class T { void F() { Ok("over §2023:6.1 3) a)\'s 255"); Has("§2023:8.4.6.2.1 GR3a"); } }\n')


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
