#!/usr/bin/env python3
"""test_fuzz_undefined.py — the self-test of scripts/fuzz_undefined.py (kb/Work PB3862).

    python scripts/test_fuzz_undefined.py        # no build, no compiler, no network

Each shape of an undefined result has a program that must be found and the nearest program that must NOT be (a finding hides a
difference from the triage, so a false one is worse than a missed one):
  STORAGE    an item no statement wrote and no VALUE initialises, DISPLAYed: found; an item with a VALUE (or a group above it with
             one), written by a MOVE of a literal, a figurative constant, a function or a DEFINED item, or named by any other statement
             (ADD, COMPUTE, ACCEPT, INITIALIZE, SET, PERFORM VARYING), or aliased by REDEFINES / EXTERNAL: not. A MOVE or a
             MOVE CORRESPONDING from storage that is itself unwritten writes nothing defined (the three shapes of PB3468 class 2:
             a MOVE CORRESPONDING whose names do not correspond, an item never targeted, and a copy of an unwritten group).
  SUBSCRIPT  an integer-literal subscript outside 1 .. OCCURS (every dimension): found; inside, or a variable: not.
  INCOMPAT   an unprefixed alphanumeric literal with a letter MOVEd to a numeric item: found; a digit literal, an alphanumeric
             receiver, a national or hexadecimal literal, a reference-modified receiver: not.
  ABSTAIN    a program the analysis cannot follow (COPY, OPTIONS, more than one program) has no finding.
The STORAGE arms are the minimal programs of the shapes the fifth night's PB3468 class 2 met (fz01110, fz01111, fz01282, fz01933,
fz01980, fz02818, fz04400, fz00704), named in each arm's text.
"""
from __future__ import annotations

import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import fuzz_undefined as fu  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

NL = chr(10)
FAILS: list[str] = []


def expect(what: str, ok: bool, got=None) -> None:
    print(("PASS  " if ok else "FAIL  ") + what + ("" if ok else f"  (got {got!r})"))
    if not ok:
        FAILS.append(what)


def program(data: str, proc: str) -> str:
    return NL.join(["IDENTIFICATION DIVISION.", "PROGRAM-ID. FUZZ.", "DATA DIVISION.", "WORKING-STORAGE SECTION.", data,
                    "PROCEDURE DIVISION.", "MAIN-PARA.", proc, "STOP RUN."]) + NL


def kinds(data: str, proc: str) -> list[str]:
    return [f.kind for f in fu.undefined_results(program(data, proc))]


def details(data: str, proc: str) -> list[str]:
    return [f.detail for f in fu.undefined_results(program(data, proc))]


GRP = NL.join(["01 A.", "   05 A1 PIC 9(3) VALUE 123.", "   05 A2 PIC X(4) VALUE 'ABCD'.",
               "01 B.", "   05 B1 PIC 9(3).", "   05 B2 PIC X(4).",
               "01 C.", "   05 C1 PIC 9(3).", "   05 C2 PIC X(4)."])


def main() -> int:
    U, S, I = fu.UNWRITTEN_STORAGE, fu.SUBSCRIPT_RANGE, fu.INCOMPATIBLE_DATA

    # ── STORAGE: the shapes the fifth night met, each the smallest program of its shape
    expect("STORAGE: an item never targeted, DISPLAYed (fz02818 TA)", kinds("01 X PIC 9(2)." , 'DISPLAY "X=" X') == [U])
    expect("STORAGE: a MOVE CORRESPONDING whose names do not correspond writes nothing (fz01111 GC)",
           kinds(GRP, "MOVE CORRESPONDING B TO C DISPLAY C1 C2") == [U, U])
    expect("STORAGE: a group copied from an unwritten group is unwritten (fz01110, fz04400 GB)",
           details(GRP, "MOVE B TO C DISPLAY C1") == ["DISPLAY reads C1, which has no VALUE clause and no statement writes it"])
    expect("STORAGE: a MOVE CORRESPONDING copying unwritten items writes nothing (fz00704, fz01933)",
           kinds(NL.join(["01 P.", "   05 F PIC 9(4).", "01 Q.", "   05 F PIC 9(4)."]), "MOVE CORRESPONDING P TO Q DISPLAY F OF Q") == [U])
    expect("STORAGE: the group is named when one of its members is the gap",
           details("01 G." + NL + "   05 G1 PIC 9 VALUE 1." + NL + "   05 G2 PIC 9.", "DISPLAY G")
           == ["DISPLAY reads G (its member G2), which has no VALUE clause and no statement writes it"])
    expect("STORAGE: the chain follows a copy of a copy",
           kinds("01 X PIC 9.\n01 Y PIC 9.\n01 Z PIC 9.", "MOVE X TO Y MOVE Y TO Z DISPLAY Z") == [U])

    # ── STORAGE: storage the runtime or a description elsewhere defines (PB3862 review R1-1, R1-2, R1-4)
    def full(env: str, data: str, proc: str) -> str:
        return NL.join(["IDENTIFICATION DIVISION.", "PROGRAM-ID. FUZZ.", env, "DATA DIVISION.", "WORKING-STORAGE SECTION.", data,
                        "PROCEDURE DIVISION.", "MAIN-PARA.", proc, "STOP RUN."]) + NL

    fenv = 'ENVIRONMENT DIVISION. INPUT-OUTPUT SECTION. FILE-CONTROL. SELECT F ASSIGN TO "nofile.dat" FILE STATUS IS WS-FS.'
    renv = 'ENVIRONMENT DIVISION. INPUT-OUTPUT SECTION. FILE-CONTROL. SELECT F ASSIGN TO "r.dat" ORGANIZATION IS RELATIVE RELATIVE KEY IS WS-K.'
    senv = "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. CRT STATUS IS WS-CRT."
    plain = "ENVIRONMENT DIVISION. INPUT-OUTPUT SECTION. FILE-CONTROL."
    expect("STORAGE/not: a FILE STATUS item the program only DISPLAYs is set by OPEN (ISO 9.1.13.1)",
           fu.undefined_results(full(fenv, "01 WS-FS PIC XX.", 'OPEN INPUT F DISPLAY "STATUS " WS-FS')) == [])
    expect("STORAGE/not: a RELATIVE KEY item and a CRT STATUS item likewise",
           fu.undefined_results(full(renv, "01 WS-K PIC 9(4).", "DISPLAY WS-K")) == []
           and fu.undefined_results(full(senv, "01 WS-CRT PIC 9(4).", "DISPLAY WS-CRT")) == [])
    expect("STORAGE: the same item with no clause naming it in the ENVIRONMENT DIVISION is found",
           [f.kind for f in fu.undefined_results(full(plain, "01 WS-FS PIC XX.", "DISPLAY WS-FS"))] == [U])
    expect("STORAGE/not: SAME AS carries the VALUE of the entry it names (ISO 13.18.49.4 1), a group's members too",
           kinds("01 WS-T PIC X(5) VALUE 'ABCDE'.\n01 WS-C SAME AS WS-T.", "DISPLAY WS-C") == []
           and kinds("01 WS-G.\n   05 G1 PIC 9 VALUE 1.\n   05 G2 PIC X VALUE 'Y'.\n01 WS-H SAME AS WS-G.", "DISPLAY WS-H G1 OF WS-H") == [])
    expect("STORAGE: SAME AS a source with no VALUE has none either",
           kinds("01 WS-T PIC X(5).\n01 WS-C SAME AS WS-T.", "DISPLAY WS-C") == [U])
    expect("STORAGE: SAME AS a group with one unvalued member names that member",
           details("01 WS-G.\n   05 G1 PIC 9 VALUE 1.\n   05 G2 PIC X.\n01 WS-H SAME AS WS-G.", "DISPLAY WS-H")
           == ["DISPLAY reads WS-H (its member G2), which has no VALUE clause and no statement writes it"])
    expect("STORAGE/not: TYPE TO carries the VALUE of the type (ISO 13.18.57.4 1), elementary and group",
           kinds("01 T-AMT TYPEDEF PIC 9(3) VALUE 7.\n01 WS-A TYPE TO T-AMT.", "DISPLAY WS-A") == []
           and kinds("01 T-REC TYPEDEF.\n   05 R1 PIC 9 VALUE 1.\n01 WS-R TYPE T-REC.", "DISPLAY WS-R") == [])
    expect("STORAGE: TYPE of a typedef with no VALUE has none; an unresolved type reads as defined",
           kinds("01 T-AMT TYPEDEF PIC 9(3).\n01 WS-A TYPE TO T-AMT.", "DISPLAY WS-A") == [U]
           and kinds("01 WS-A TYPE TO T-ELSEWHERE.", "DISPLAY WS-A") == [])
    expect("STORAGE/not: a VALUE with no integer part (.5) does not end the entry",
           kinds("01 G.\n   05 A PIC V9 VALUE .5.\n   05 B PIC X VALUE 'Y'.", "DISPLAY G") == []
           and kinds("01 G.\n   05 A PIC SV9 VALUE -.25.\n   05 B PIC X VALUE 'Y'.", "DISPLAY G") == [])

    # ── STORAGE: the nearest programs that must not be found
    expect("STORAGE/not: a VALUE clause initialises", kinds("01 X PIC 9(2) VALUE 5.", "DISPLAY X") == [])
    expect("STORAGE/not: a VALUE on the group initialises its members", kinds("01 G VALUE SPACES." + NL + "   05 G1 PIC X.", "DISPLAY G1") == [])
    expect("STORAGE/not: a MOVE of a literal, a figurative constant or a function writes",
           kinds("01 X PIC 9(2).\n01 Y PIC X(2).\n01 Z PIC X(2).", 'MOVE 5 TO X MOVE SPACES TO Y MOVE FUNCTION REVERSE("AB") TO Z DISPLAY X Y Z') == [])
    expect("STORAGE/not: a MOVE from a defined item writes, group and CORRESPONDING alike",
           kinds(GRP + NL + "01 D." + NL + "   05 A1 PIC 9(3)." + NL + "   05 A2 PIC X(4).",
                 "MOVE A TO B MOVE CORRESPONDING A TO D DISPLAY B1 B2 A1 OF D A2 OF D") == [])
    expect("STORAGE/not: a CORRESPONDING MOVE of the names that do correspond writes those and only those",
           kinds(NL.join(["01 P.", "   05 F PIC 9 VALUE 1.", "   05 G PIC 9.", "01 Q.", "   05 F PIC 9.", "   05 G PIC 9."]),
                 "MOVE CORRESPONDING P TO Q DISPLAY F OF Q") == [])
    expect("STORAGE/not: every statement that names an item before the DISPLAY may have written it",
           kinds("01 A PIC 9.\n01 B PIC 9.\n01 C PIC 9.\n01 D PIC X.\n01 E PIC 9.\n01 F PIC 9.\n01 G PIC 9.\n01 H PIC 9.\n01 FLAG PIC 9.\n   88 ON1 VALUE 1.",
                 "ADD 1 TO A COMPUTE B = 2 ACCEPT D INITIALIZE C SET ON1 TO TRUE "
                 "PERFORM VARYING E FROM 1 BY 1 UNTIL E > 2 CONTINUE END-PERFORM "
                 "READ F INTO G CALL 'P' USING H DISPLAY A B C D E F G H FLAG") == [])
    expect("STORAGE/not: REDEFINES and EXTERNAL alias storage this analysis does not follow",
           kinds("01 R1 PIC 9(2) VALUE 5.\n01 R2 REDEFINES R1 PIC XX.\n01 EXT PIC X EXTERNAL.", "DISPLAY R2 EXT") == [])
    expect("STORAGE/not: a subscripted or reference-modified receiver writes the item",
           kinds("01 T.\n   05 TE PIC 9 OCCURS 3.\n01 W PIC X(4).", "MOVE 1 TO TE (2) MOVE 'AB' TO W (1:2) DISPLAY T W") == [])
    expect("STORAGE/not: a write before and a display after, or a write after, are both a write (the analysis is flow-insensitive)",
           kinds("01 X PIC 9.", "DISPLAY X MOVE 1 TO X") == [])
    expect("STORAGE/not: DISPLAY operands that are not data items (literals, function results, UPON, NO ADVANCING)",
           kinds("01 X PIC 9 VALUE 1.", 'DISPLAY "A" X UPON CONSOLE WITH NO ADVANCING DISPLAY FUNCTION LENGTH(X) DISPLAY 5') == [])
    expect("STORAGE/not: names in comments and strings are not references",
           kinds("01 X PIC 9.", 'DISPLAY "MOVE 1 TO X" X *> MOVE 1 TO X') == [U])

    # ── SUBSCRIPT
    two = NL.join(["01 TAB.", "   05 ROW OCCURS 3.", "      10 CELL PIC 9 OCCURS 2 VALUE 1."])
    expect("SUBSCRIPT: a literal above the OCCURS, below 1, in either dimension",
           kinds(two, "DISPLAY CELL (4, 1) CELL (1, 3) CELL (0, 1)") == [S, S, S])
    expect("SUBSCRIPT/not: a subscript inside the bounds, a variable, and a subscript count that is not the dimension count",
           kinds(two + NL + "01 I PIC 9 VALUE 1.", "DISPLAY CELL (3, 2) CELL (I, I) CELL (9)") == [])
    expect("SUBSCRIPT: the OCCURS maximum of a variable-length table is the highest permissible (ISO 8.4.2.3.4 2)",
           kinds("01 N PIC 9 VALUE 2.\n01 V PIC 9 OCCURS 1 TO 5 DEPENDING ON N VALUE 1.", "DISPLAY V (5) V (6)") == [S])

    # ── INCOMPATIBLE DATA
    num = "01 N PIC 9(5) VALUE 0.\n01 SN PIC S9(3)V9 VALUE 0.\n01 BN PIC S9(4) BINARY VALUE 0.\n01 BL USAGE BINARY-LONG VALUE 0.\n01 X PIC X(5) VALUE 'ABCDE'.\n01 E PIC Z(4)9 VALUE 0."
    expect("INCOMPAT: a letter MOVEd to a numeric item of any usage",
           kinds(num, 'MOVE "ABC" TO N MOVE "AB" TO SN MOVE "A" TO BN MOVE "Z" TO BL') == [I, I, I, I])
    expect("INCOMPAT/not: a digit literal, an alphanumeric or edited receiver, a national or hex literal, a reference-modified receiver",
           kinds(num, 'MOVE "123" TO N MOVE "ABC" TO X MOVE "AB" TO E MOVE N"AB" TO N MOVE X"4142" TO N MOVE "AB" TO N (1:2) MOVE 7 TO N') == [])

    # ── ABSTAIN
    expect("ABSTAIN: a COPY member, an OPTIONS paragraph or a second program hides what the analysis needs",
           fu.undefined_results(program("COPY BOOK.\n01 X PIC 9.", "DISPLAY X")) == []
           and fu.undefined_results(program("01 X PIC 9.", "DISPLAY X") + "IDENTIFICATION DIVISION." + NL + "PROGRAM-ID. TWO." + NL) == []
           and fu.undefined_results(program("01 X PIC 9.", "DISPLAY X").replace("DATA DIVISION.", "OPTIONS. DEFAULT ROUNDED MODE IS TRUNCATION." + NL + "DATA DIVISION.")) == [])
    expect("ABSTAIN: no WORKING-STORAGE, or no PROCEDURE DIVISION, is no finding",
           fu.undefined_results("IDENTIFICATION DIVISION." + NL + "PROGRAM-ID. FUZZ." + NL) == [])
    expect("CITATIONS: every kind carries its clause", set(fu.CITATIONS) == {U, S, I}, sorted(fu.CITATIONS))
    print(f"=== test_fuzz_undefined: {'GREEN' if not FAILS else 'RED'} ({len(FAILS)} failure(s)) ===")
    return 1 if FAILS else 0


if __name__ == "__main__":
    ensure_utf8()
    raise SystemExit(main())
