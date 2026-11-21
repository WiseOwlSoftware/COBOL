      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_8-p3
      *> rules: [1985-GR-IV.4.3.8-p3, 1985-GR-IV.4.3.8.3.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8 p3; §1985:IV.4.3.8.3.1 p1 (CONCEPT OF COMPUTER
      *> INDEPENDENT DATA DESCRIPTION). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-05.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.8-p3, 1985-GR-IV.4.3.8.3.1-p1: a function's
      * value is determined by its arguments for that reference; a
      * function-identifier references the resulting data item, and a
      * reference-modifier applies to it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05FUNC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  N           PIC 99.
       77  W           PIC X(5) VALUE "abcde".
       PROCEDURE DIVISION.
       P1.
           COMPUTE N = FUNCTION MOD (11 3).
           DISPLAY "MOD11=" N.
           COMPUTE N = FUNCTION MOD (17 5).
           DISPLAY "MOD17=" N.
           COMPUTE N = FUNCTION MAX (4 9 2).
           DISPLAY "MAX=" N.
           DISPLAY "UP=" FUNCTION UPPER-CASE (W).
           DISPLAY "UPR=" FUNCTION UPPER-CASE (W) (2:3).
           STOP RUN.
