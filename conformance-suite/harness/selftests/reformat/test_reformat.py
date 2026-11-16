#!/usr/bin/env python3
"""The free-form to fixed-form converter on planted texts (kb/Work PB5754; DESIGN-conformance-suite.md section 3.6):
each transformation the 1985 layout asks for (the header, comment lines, area A and area B, a data entry's own column,
an inline comment, a marked line, a re-wrap at margin R, a library text), each refusal by its kind, the token-stream
equality that guards them all, and the refusal entry whose position moves with its text."""
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/reformat.py
G = T.generate
RESERVED = G.reserved_words()
LATER = ["2002", "2014", "2023"]
HEAD = "*>--- conformance-case\n*> id: {id}\n*> rules: [SR-1.1-1]\n*> source-format: free\n*> portable: unverified\n*>---\n"


def _convert(body, **kw):
    return T.convert_text(HEAD.format(id="t") + body, "t.cob", RESERVED, LATER, **kw)


def _refused(body, kind, **kw):
    try:
        _convert(body, **kw)
    except T.Refused as r:
        return r.kind == kind and r.path == "t.cob" and r.line > 0
    return False


PROGRAM = """\
*> a comment at column 1
IDENTIFICATION DIVISION.
PROGRAM-ID. P1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "f.dat".
DATA DIVISION.
FILE SECTION.
FD F.
01 F-REC PIC X.
WORKING-STORAGE SECTION.
77 N PIC 9.
01 G.
  05 E PIC X
     VALUE "A".
  88 E-A VALUE "A".
PROCEDURE DIVISION.
DECLARATIVES.
D-1 SECTION.
    USE AFTER ERROR PROCEDURE ON F.
END DECLARATIVES.
MAIN SECTION.
0100.
    MOVE 1 TO N
    DISPLAY N.
P-2.
    EXIT.
END PROGRAM P1.
"""

FIXED = """\
      *> a comment at column 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "f.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X.
       WORKING-STORAGE SECTION.
       77 N PIC 9.
       01 G.
         05 E PIC X
            VALUE "A".
         88 E-A VALUE "A".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-1 SECTION.
           USE AFTER ERROR PROCEDURE ON F.
       END DECLARATIVES.
       MAIN SECTION.
       0100.
           MOVE 1 TO N
           DISPLAY N.
       P-2.
           EXIT.
       END PROGRAM P1.
"""


def case_00_the_1985_layout_of_every_line_kind(expect):
    got = _convert(PROGRAM)
    head, _, body = got.text.partition("      *>---\n")
    expect.equal("the header moves to column 7 and says fixed; nothing else in it changes", head,
                 "".join("      " + line + "\n" for line in HEAD.format(id="t").replace("free", "fixed").splitlines()[:-1]))
    expect.equal("§1985:IV.7: headers, paragraph-names, FD and 01/77 in area A; sentences, entries and continued lines in "
                 "area B; a data entry keeps its own column right of margin A; a comment line's asterisk in column 7",
                 body, FIXED)
    expect("the converted text's tokens are the original's, comments aside",
           T._tokens(got.text, "fixed") == T._tokens(HEAD.format(id="t") + PROGRAM, "free"))
    already = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. P2.\n       PROCEDURE DIVISION.\n           STOP RUN.\n"
    expect.equal("a free-form text already in fixed columns keeps them", _convert(already).text.partition("*>---\n")[2],
                 already)
    misplaced = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. P2.\n       PROCEDURE DIVISION.\n       MAIN.\n" \
                "       STOP RUN.\n"
    expect("a sentence written in area A moves to area B",
           _convert(misplaced).text.endswith("       MAIN.\n           STOP RUN.\n"))


def case_01_an_inline_comment_and_a_marked_line(expect):
    body = ("IDENTIFICATION DIVISION.\nPROGRAM-ID. P3.\nPROCEDURE DIVISION.\n    DISPLAY \"*> no comment\" *> why\n"
            "    STOP RUN. *> <<\n")
    got = _convert(body).text.partition("*>---\n")[2]
    expect.equal("an inline comment becomes a comment line before its line, and a literal's `*>` stays in the literal; "
                 "the free mark `*> <<` becomes `<<` in the sequence number area",
                 got, "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. P3.\n       PROCEDURE DIVISION.\n"
                      "      *> why\n           DISPLAY \"*> no comment\"\n<<         STOP RUN.\n")


def case_02_a_line_past_margin_r_is_rewrapped_between_separators(expect):
    long_statement = "    IF N = 1 DISPLAY \"" + "A" * 30 + "\" ELSE DISPLAY \"" + "B" * 20 + "\".\n"
    two = "    MOVE 1 TO N. MOVE 2 TO N. MOVE 3 TO N. MOVE 4 TO N. MOVE 5 TO N. MOVE 6 TO NN.\n"
    body = "IDENTIFICATION DIVISION.\nPROGRAM-ID. P4.\nPROCEDURE DIVISION.\n" + long_statement + two
    got = _convert(body)
    lines = got.text.split("\n")[6:]
    expect("every line ends by column 72", all(len(x) <= 72 for x in lines))
    expect.equal("a continued phrase goes to area B four columns in; a line with a sentence boundary late enough breaks "
                 "there, the new sentence at the line's own column", lines[3:],
                 ['           IF N = 1 DISPLAY "' + "A" * 30 + '" ELSE',
                  '               DISPLAY "' + "B" * 20 + '".',
                  "           MOVE 1 TO N. MOVE 2 TO N. MOVE 3 TO N. MOVE 4 TO N.",
                  "           MOVE 5 TO N. MOVE 6 TO NN.", ""])
    expect.equal("a position moves with its text: the line's start, and a column inside its second part",
                 (got.position(10, 5), got.position(10, 5 + long_statement.index('"B') - 4)), ((10, 12), (11, 24)))


def case_03_each_refusal_names_its_kind_and_line(expect):
    pd = "IDENTIFICATION DIVISION.\nPROGRAM-ID. P5.\nPROCEDURE DIVISION.\n"
    for label, body, kind in (
            ("a compiler directive", pd + ">>D DISPLAY 1.\n", T.DIRECTIVE),
            ("a debugging line's directive inside a line", pd + "    >>SOURCE FORMAT FIXED\n", T.DIRECTIVE),
            ("a tab", pd + "\tSTOP RUN.\n", T.TAB),
            ("a literal open at the end of a line", pd + '    DISPLAY "ABC\n- "DEF".\n', T.CONTINUATION),
            ("a free-form literal continuation", pd + '    DISPLAY "ABC"-\n    "DEF".\n', T.CONTINUATION),
            ("a data entry that opens with no level-number", "IDENTIFICATION DIVISION.\nPROGRAM-ID. P5.\nDATA DIVISION.\n"
                                                             "WORKING-STORAGE SECTION.\nW PIC X.\n", T.UNDECIDED),
            ("a one-word sentence reserved at some editions only (GOBACK: not at 85)", pd + "    GOBACK.\n",
             T.UNDECIDED),
            ("a comment-entry paragraph", "IDENTIFICATION DIVISION.\nPROGRAM-ID. P5.\nAUTHOR. ME.\n", T.COMMENT_ENTRY),
            ("a literal no separator lets end by column 72", pd + '    DISPLAY "' + "X" * 70 + '".\n', T.TOO_LONG),
            ("a marked line whose comment is more than its mark", pd + "    STOP RUN. *> why *> <<\n", T.MARK)):
        expect(f"REFUSED, by kind and line: {label}",
               _refused(body, kind) if kind != T.UNDECIDED or "GOBACK" not in body
               else _refused_at(body, kind, ["85", "2002"]))
    fixed = HEAD.format(id="t").replace("*>", "      *>").replace("free", "fixed")
    try:
        T.convert_text(fixed + "       IDENTIFICATION DIVISION.\n", "t.cob", RESERVED, LATER)
        expect("a fixed-form case is refused as not free form", False)
    except T.Refused as r:
        expect.equal("a fixed-form case is refused as not free form", r.kind, T.NOT_FREE)
    real = T._breaks
    T._breaks = lambda code: list(range(1, len(code)))       # a re-wrap that would split a word
    try:
        expect("THE GUARD: a text whose fixed-form tokens are not the original's is refused, never written",
               _refused(pd + "    MOVE ALPHABETIC-UPPER-CASE-NAME TO ANOTHER-NAME-OF-SOME-LENGTH-ENOUGH-TO-WRAP-IT.\n",
                        T.ROUND_TRIP))
    finally:
        T._breaks = real


def _refused_at(body, kind, editions):
    try:
        T.convert_text(HEAD.format(id="t") + body, "t.cob", RESERVED, editions)
    except T.Refused as r:
        return r.kind == kind
    return False


def _suite(files):
    root = scratch_dir("reformat-")
    for rel, text in files.items():
        p = root / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8", newline="")
    return root


def case_04_a_case_with_its_library_text_and_its_refusal_entry(expect):
    prog = ("IDENTIFICATION DIVISION.\nPROGRAM-ID. P6.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\nCOPY LIB1.\n"
            "PROCEDURE DIVISION.\n    DISPLAY X *> moved\n    MOVE 1 TO Y.\n")
    head = HEAD.format(id="c6").replace("*> source-format", "*> files: [LIB1.cpy]\n*> source-format")
    root = _suite({"cases/1.1-X/c6.cob": head + prog, "cases/1.1-X/LIB1.cpy": "01 X PIC 9.\n01 Y PIC 9.\n",
                   "wiseowl/diagnostics/c6.json": json.dumps({"refuse-at": ["85", "2002"], "expect": "(14,5): error COBOL1"})})
    plan = T.plan_case(root / "cases/1.1-X/c6.cob", suite=root)
    expect.equal("the library text is converted with the case, in the division it is copied into",
                 plan.files[root / "cases/1.1-X/LIB1.cpy"], "       01 X PIC 9.\n       01 Y PIC 9.\n")
    expect.equal("the refusal entry's position moves with its text (a comment line inserted before it, the line to "
                 "area B)", json.loads(plan.entry[1])["expect"], "(15,12): error COBOL1")
    T.write(plan)
    expect("written in place: the case reads as fixed form", G.read_header(
        (root / "cases/1.1-X/c6.cob").read_text(encoding="utf-8"))[0].fields["source-format"] == "fixed")
    shared = _suite({"cases/1.1-X/c6.cob": head + prog, "cases/1.1-X/LIB1.cpy": "01 X PIC 9.\n",
                     "cases/1.1-X/c7.cob": HEAD.format(id="c7").replace("*> source-format",
                                                                        "*> files: [LIB1.cpy]\n*> source-format")})
    try:
        T.plan_case(shared / "cases/1.1-X/c6.cob", suite=shared)
        expect("a library text another case declares is refused", False)
    except T.Refused as r:
        expect.equal("a library text another case declares is refused", r.kind, T.LIBRARY)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
