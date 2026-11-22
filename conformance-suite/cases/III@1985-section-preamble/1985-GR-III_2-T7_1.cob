      *>--- conformance-case
      *> id: 1985-GR-III_2-T7_1
      *> rules: [1985-GR-III.2-T7.1, 1985-GR-III.2-T7.2, 1985-GR-III.2-T7.3, 1985-GR-III.2-T7.4, 1985-GR-III.2-T7.5, 1985-GR-III.2-p249, 1985-GR-III.2-p270, 1985-GR-III.2-p296]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 Table 7 row 1; §1985:III.2 Table 7 row 2;
      *> §1985:III.2 Table 7 row 3; §1985:III.2 Table 7 row 4;
      *> §1985:III.2 Table 7 row 5; §1985:III.2 p249; §1985:III.2 p270;
      *> §1985:III.2 p296 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SPECCHAR.
      * 1985 III.2 Special Character set: + - * / = as operators,
      * PICTURE symbols and relation character; resultant identifiers.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A          PIC S9(4) VALUE 7.
       01  B          PIC S9(4) VALUE 3.
       01  R1         PIC S9(4).
       01  R2         PIC S9(4).
       01  E1         PIC +9999.
       01  E2         PIC -9999.
       01  E3         PIC ***99.
       01  E4         PIC 99/99/99.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R1 = A + B * 2 - 8 / 4.
           DISPLAY "R1=" R1.
           ADD A TO B GIVING R2.
           DISPLAY "R2=" R2.
           MOVE R1 TO E1.
           DISPLAY "E1=" E1.
           MOVE -12 TO E2.
           DISPLAY "E2=" E2.
           MOVE 5 TO E3.
           DISPLAY "E3=" E3.
           MOVE 311285 TO E4.
           DISPLAY "E4=" E4.
           IF R1 = 11 DISPLAY "EQ-OK" ELSE DISPLAY "EQ-BAD".
           IF A > B DISPLAY "GT-OK" ELSE DISPLAY "GT-BAD".
           IF B < A DISPLAY "LT-OK" ELSE DISPLAY "LT-BAD".
           IF A NOT < B DISPLAY "NLT-OK" ELSE DISPLAY "NLT-BAD".
           IF A IS GREATER THAN B AND B IS LESS THAN A
               AND R1 IS EQUAL TO 11 DISPLAY "WORDS-OK"
           ELSE DISPLAY "WORDS-BAD".
           STOP RUN.
