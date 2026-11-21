      *>--- conformance-case
      *> id: 1985-GR-III_2-p312
      *> rules: [1985-GR-III.2-p312]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p312 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TABCONSEC.
      * 1985 III.2 Table: "a set of logically consecutive items of
      * data ... defined ... by means of the OCCURS clause": the
      * elements of W-T follow one another in order, so the group
      * W-T holds E(1) E(2) E(3) and E(n) is the n-th position.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-T.
           05  W-E      PIC XX OCCURS 3 TIMES.
       01  W-OK         PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE "AABBCC" TO W-T.
           IF W-E (1) = "AA" ADD 1 TO W-OK.
           IF W-E (2) = "BB" ADD 1 TO W-OK.
           IF W-E (3) = "CC" ADD 1 TO W-OK.
           MOVE "XY" TO W-E (2).
           IF W-T = "AAXYCC" ADD 1 TO W-OK.
           IF W-OK = 4 DISPLAY "TABCONSEC PASS"
               ELSE DISPLAY "TABCONSEC FAIL " W-OK.
           STOP RUN.
