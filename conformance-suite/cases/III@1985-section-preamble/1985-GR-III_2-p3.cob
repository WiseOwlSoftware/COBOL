      *>--- conformance-case
      *> id: 1985-GR-III_2-p3
      *> rules: [1985-GR-III.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p3 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p3: an actual decimal point is the period in an
      * edited item. Expect: POINT 12.34 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E   PIC 99.99.
       PROCEDURE DIVISION.
       P0.
           MOVE 12.34 TO E.
           IF E = "12.34"
               DISPLAY "POINT " E " OK"
           ELSE
               DISPLAY "POINT " E " BAD"
           END-IF.
           STOP RUN.
