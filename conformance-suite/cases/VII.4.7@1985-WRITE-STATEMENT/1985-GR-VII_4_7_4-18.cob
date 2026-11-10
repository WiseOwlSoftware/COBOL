      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-18
      *> rules: [1985-GR-VII.4.7.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR18 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-FILE ASSIGN TO "w85c6-f.prt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT N-FILE ASSIGN TO "w85c6-n.prt"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS N-STATUS.
           SELECT X-FILE ASSIGN TO "w85c6-x.prt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F-FILE LINAGE IS 5 LINES WITH FOOTING AT 4.
       01  F-REC PIC X(4).
       FD  N-FILE LINAGE IS 5 LINES WITH FOOTING AT 4.
       01  N-REC PIC X(4).
       FD  X-FILE LINAGE IS 5 LINES.
       01  X-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  N-STATUS PIC XX VALUE "??".
       01  USE-COUNT PIC 9 VALUE ZERO.
       01  WS-N PIC 9 VALUE ZERO.
       01  WS-LC PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       N-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON N-FILE.
       N-ERR-1.
           ADD 1 TO USE-COUNT.
           DISPLAY "USE PROCEDURE STATUS " N-STATUS.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-MAIN.
           OPEN OUTPUT F-FILE.
           MOVE LINAGE-COUNTER OF F-FILE TO WS-LC.
           DISPLAY "F OPEN                    LC " WS-LC.
           PERFORM P-F-AFTER-1 7 TIMES.
           PERFORM P-F-BEFORE-2 2 TIMES.
           CLOSE F-FILE.
           MOVE ZERO TO WS-N.
           OPEN OUTPUT N-FILE.
           PERFORM P-N 6 TIMES.
           CLOSE N-FILE.
           DISPLAY "N USE PROCEDURE RAN " USE-COUNT " TIMES".
           MOVE ZERO TO WS-N.
           OPEN OUTPUT X-FILE.
           PERFORM P-X 6 TIMES.
           CLOSE X-FILE.
           STOP RUN.
       P-F-AFTER-1.
           ADD 1 TO WS-N.
           MOVE "FFFF" TO F-REC.
           WRITE F-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE
                   MOVE LINAGE-COUNTER OF F-FILE TO WS-LC
                   DISPLAY "F WRITE " WS-N " END-OF-PAGE     LC " WS-LC
               NOT AT END-OF-PAGE
                   MOVE LINAGE-COUNTER OF F-FILE TO WS-LC
                   DISPLAY "F WRITE " WS-N " NOT END-OF-PAGE LC " WS-LC
           END-WRITE.
       P-F-BEFORE-2.
           ADD 1 TO WS-N.
           MOVE "GGGG" TO F-REC.
           WRITE F-REC BEFORE ADVANCING 2 LINES
               AT END-OF-PAGE
                   MOVE LINAGE-COUNTER OF F-FILE TO WS-LC
                   DISPLAY "F WRITE " WS-N " END-OF-PAGE     LC " WS-LC
               NOT AT END-OF-PAGE
                   MOVE LINAGE-COUNTER OF F-FILE TO WS-LC
                   DISPLAY "F WRITE " WS-N " NOT END-OF-PAGE LC " WS-LC
           END-WRITE.
       P-N.
           ADD 1 TO WS-N.
           MOVE "NNNN" TO N-REC.
           WRITE N-REC AFTER ADVANCING 1 LINE.
           MOVE LINAGE-COUNTER OF N-FILE TO WS-LC.
           DISPLAY "N WRITE " WS-N " STATUS " N-STATUS " LC " WS-LC.
       P-X.
           ADD 1 TO WS-N.
           MOVE "XXXX" TO X-REC.
           WRITE X-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE
                   MOVE LINAGE-COUNTER OF X-FILE TO WS-LC
                   DISPLAY "X WRITE " WS-N " END-OF-PAGE     LC " WS-LC
               NOT AT END-OF-PAGE
                   MOVE LINAGE-COUNTER OF X-FILE TO WS-LC
                   DISPLAY "X WRITE " WS-N " NOT END-OF-PAGE LC " WS-LC
           END-WRITE.
