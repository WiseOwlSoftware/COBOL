      *>--- conformance-case
      *> id: 1985-FMT-XI_4_3_2
      *> rules: [1985-FMT-XI.4.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.2 (THE RETURN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RETTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SORT-KEY PIC X.
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SORT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "DONE".
           STOP RUN.
       IN-PROC SECTION.
       IN-PARA.
           MOVE "C" TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE "A" TO SORT-KEY.
           RELEASE SORT-REC.
           MOVE "B" TO SORT-KEY.
           RELEASE SORT-REC.
       OUT-PROC SECTION.
       OUT-PARA.
           RETURN SORT-FILE RECORD INTO WS-REC
               AT END MOVE "Y" TO WS-EOF
               NOT AT END DISPLAY WS-REC
           END-RETURN.
           IF WS-EOF = "N" GO TO OUT-PARA.
       OUT-EXIT.
           EXIT.
