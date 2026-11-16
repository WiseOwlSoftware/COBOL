      *>--- conformance-case
      *> id: g85_x42
      *> rules: [1989-GR-A.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.3 GR2 (1989-GR-A.2.3-2). An output test, the
      *> accepted half beside the refusal g85_x03: 'A numeric function
      *> is always considered to have an operational sign.' NUMVAL and
      *> REM are numeric functions (§1989:A.2.29.1, §1989:A.2.37.1),
      *> used here only in arithmetic expressions. Derived: NUMVAL
      *> ("-2.5") is -2.5, so the relation is true and the value keeps
      *> its sign; REM (-7 2) = -7 - (2 * INTEGER-PART (-3.5)) = -7 -
      *> (2 * -3) = -1 (§1989:A.2.37.4 RV1). An unsigned result would
      *> print NOT NEGATIVE, 2.5 and 1.0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X42.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S PIC S9V9.
       01  E PIC -9.9.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           IF FUNCTION NUMVAL ("-2.5") < 0
               DISPLAY "NUMVAL NEGATIVE"
           ELSE
               DISPLAY "NUMVAL NOT NEGATIVE"
           END-IF.
           COMPUTE S = FUNCTION NUMVAL ("-2.5").
           MOVE S TO E.
           DISPLAY "NUMVAL " E.
           COMPUTE S = FUNCTION REM (-7 2).
           MOVE S TO E.
           DISPLAY "REM " E.
           STOP RUN.
