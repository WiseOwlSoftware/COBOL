      *>--- conformance-case
      *> id: 1985-GR-XVI_1_4_3-2
      *> rules: [1985-GR-XVI.1.4.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.4.3 GR2 (ORGANIZATION). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the 1985 audit's drafts pass (a draft that
      *> mismatched on an older build, re-run to MATCH with its
      *> expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGSORT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X(5).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       MAIN-1.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       OUT-PROC SECTION.
       OP-1.
           RETURN SORT-FILE AT END GO TO OP-EXIT.
           GO TO OP-1.
       OP-EXIT.
           EXIT.
       IN-PROC SECTION 60.
       IP-START.
           GO TO IP-FIRST.
       IP-FIRST.
           DISPLAY "FIRST".
           MOVE "AAAAA" TO SORT-KEY.
           RELEASE SORT-REC.
           ALTER IP-START TO PROCEED TO IP-LATER.
           GO TO IP-EXIT.
       IP-LATER.
           DISPLAY "LATER".
           MOVE "BBBBB" TO SORT-KEY.
           RELEASE SORT-REC.
       IP-EXIT.
           EXIT.
