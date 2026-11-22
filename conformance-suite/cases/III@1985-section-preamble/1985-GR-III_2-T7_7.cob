      *>--- conformance-case
      *> id: 1985-GR-III_2-T7_7
      *> rules: [1985-GR-III.2-T7.7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 Table 7 row 7 ((section preamble)). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DECCOMMA.
      * 1985 III.2 Table 7 rows 7, 9: comma as decimal point,
      * period as insertion character under DECIMAL-POINT IS COMMA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-AMT        PIC 9(4)V99 VALUE 1234,5.
       01  W-ED         PIC Z.ZZ9,99.
       PROCEDURE DIVISION.
       P1.
           MOVE W-AMT TO W-ED.
           DISPLAY "ED=" W-ED.
           IF W-ED = "1.234,50" DISPLAY "DECCOMMA PASS"
               ELSE DISPLAY "DECCOMMA FAIL".
           STOP RUN.
