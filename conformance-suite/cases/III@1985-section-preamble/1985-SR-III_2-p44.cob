      *>--- conformance-case
      *> id: 1985-SR-III_2-p44
      *> rules: [1985-SR-III.2-p44]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p44 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-III.2-p44: condition-1 of a general format is a simple
      * condition, optionally parenthesized, or a combined condition.
      * Expect: K1 OK / K2 OK / K3 OK / K4 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P44CN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9 VALUE 1.
       01  B   PIC 9 VALUE 2.
       01  C   PIC 9 VALUE 5.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P0.
           IF A = 1 DISPLAY "K1 OK" ELSE DISPLAY "K1 BAD".
           IF ((A = 1)) DISPLAY "K2 OK" ELSE DISPLAY "K2 BAD".
           IF (A = 1) AND (B = 3 OR C = 5) AND NOT B = 9
               DISPLAY "K3 OK" ELSE DISPLAY "K3 BAD".
           PERFORM UNTIL (N > 2 AND A = 1) OR B = 0
               ADD 1 TO N
           END-PERFORM.
           IF N = 3 DISPLAY "K4 OK" ELSE DISPLAY "K4 BAD".
           STOP RUN.
