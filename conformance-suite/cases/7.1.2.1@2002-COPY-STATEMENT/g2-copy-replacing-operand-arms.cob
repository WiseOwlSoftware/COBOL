      *>--- conformance-case
      *> id: g2-copy-replacing-operand-arms
      *> rules: [2002-FMT-7.1.2.1]
      *> accept: [2002]
      *> files: [G2CPF.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.1 - the 2002 REPLACING operand is {==pseudo-text-1==
      *> | text-1 | literal-3 | word-1} BY {==pseudo-text-2== | text-2 |
      *> literal-4 | word-2}, or LEADING/TRAILING ==partial-word-1== BY
      *> ==partial-word-2==, repeated. One COPY writes every arm.
      *> §2002:7.1.2.3 GR8 - "Each matched occurrence of pseudo-text-1,
      *> text-1, word-1, literal-3, or partial-word-1 in the library text
      *> is replaced by the corresponding pseudo-text-2, text-2, word-2,
      *> literal-4, or partial-word-2"; GR9 - "For purposes of matching,
      *> text-1, word-1, and literal-3 are treated as pseudo-text".
      *> §2002:7.1.2.2 SR9 - text-1/text-2 are "function-identifier,
      *> qualified-data-name-with-subscripts, reference-modification"
      *> (or the counters); SR10 - "Word-1 or word-2 may be any single
      *> COBOL word except" COPY and the indicators.
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *> [BBBNEWLT] text-1 OLDLIT BY NEWLIT, literal-3 "AAA" BY "BBB":
      *>            MOVE NEWLIT; DISPLAY "BBB" W-OUT. Either arm
      *>            unreplaced prints AAA or OLD.
      *> [B22]      text-1 subscripted W-A (1) BY W-B (2) (A11 if not).
      *> [WORD-1]   word-1 XSHOW BY word-2 DISPLAY, a reserved word: an
      *>            unreplaced XSHOW fails the compile.
      *> [yz]       text-1 reference-modification W-S (1:2) BY W-T (4:2)
      *>            ("vwxyz" positions 4-5; ab if not).
      *> [edcba]    text-1 function-identifier FUNCTION UPPER-CASE (W-S)
      *>            BY FUNCTION REVERSE (W-S) (ABCDE if not).
      *> [G2Q]      text-1 qualified W-Q OF G1 BY W-Q OF G2 (G1Q if not).
      *> [[PT]]     ==ZZZ== BY =="PT"== (ZZZ is undeclared if not).
      *> [N1]       LEADING ==PFX-== BY ==NEW-==: PFX-ONE -> NEW-ONE.
      *> [OT]       TRAILING ==-SFX== BY ==-TWO==: ONE-SFX -> ONE-TWO.
      *> GR10f: the replacement is not rescanned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-OUT   PIC X(5).
       01 OLDLIT  PIC X(5) VALUE "OLDLT".
       01 NEWLIT  PIC X(5) VALUE "NEWLT".
       01 T.
          05 W-A  PIC X(3) OCCURS 2.
          05 W-B  PIC X(3) OCCURS 2.
       01 W-S     PIC X(5) VALUE "abcde".
       01 W-T     PIC X(5) VALUE "vwxyz".
       01 G1.
          05 W-Q  PIC X(3) VALUE "G1Q".
       01 G2.
          05 W-Q  PIC X(3) VALUE "G2Q".
       01 NEW-ONE PIC X(2) VALUE "N1".
       01 ONE-TWO PIC X(2) VALUE "OT".
       PROCEDURE DIVISION.
       P1.
           MOVE "A11" TO W-A (1).
           MOVE "B22" TO W-B (2).
           COPY G2CPF REPLACING OLDLIT BY NEWLIT
                                "AAA" BY "BBB"
                                W-A (1) BY W-B (2)
                                XSHOW BY DISPLAY
                                W-S (1:2) BY W-T (4:2)
                                FUNCTION UPPER-CASE (W-S)
                                  BY FUNCTION REVERSE (W-S)
                                W-Q OF G1 BY W-Q OF G2
                                ==ZZZ== BY =="PT"==
                                LEADING ==PFX-== BY ==NEW-==
                                TRAILING ==-SFX== BY ==-TWO==.
           STOP RUN.
