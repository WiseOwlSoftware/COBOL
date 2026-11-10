      *>--- conformance-case
      *> id: 1985-GR-XI_4_2_4-3
      *> rules: [1985-GR-XI.4.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.2.4 GR3 (THE RELEASE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SR-KEY  PIC X(3).
           05  SR-REST PIC X(2).
       WORKING-STORAGE SECTION.
       01  WS-SRC PIC X(7).
       PROCEDURE DIVISION.
       MAIN-PARA SECTION.
       M1.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           MOVE "ZZZ99XX" TO WS-SRC.
           RELEASE SORT-REC FROM WS-SRC.
           MOVE "AAA11YY" TO WS-SRC.
           RELEASE SORT-REC FROM WS-SRC.
       OUT-PROC SECTION.
       O1.
           RETURN SORT-FILE AT END GO TO O9.
           DISPLAY SORT-REC.
           GO TO O1.
       O9.
           EXIT.
