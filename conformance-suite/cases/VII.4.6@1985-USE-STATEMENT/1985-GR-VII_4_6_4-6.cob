      *>--- conformance-case
      *> id: 1985-GR-VII_4_6_4-6
      *> rules: [1985-GR-VII.4.6.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.4 GR6 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "w85c4.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  SQ-STATUS PIC XX VALUE "??".
       01  USE-COUNT PIC 9 VALUE ZERO.
       01  WS-FLAG PIC X(4) VALUE "INIT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       SQ-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SQ-FILE.
       SQ-ERR-1.
           ADD 1 TO USE-COUNT.
           DISPLAY "USE PROCEDURE " USE-COUNT " STATUS " SQ-STATUS.
       SQ-ERR-2.
           DISPLAY "USE PROCEDURE " USE-COUNT " ENDS".
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-CREATE.
           OPEN OUTPUT SQ-FILE.
           MOVE "REC-0001" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
       P-READ.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE.
           DISPLAY "AFTER READ 1 " SQ-REC " STATUS " SQ-STATUS.
           IF SQ-STATUS = "00"
               READ SQ-FILE
               DISPLAY "AFTER READ 2 STATUS " SQ-STATUS
               MOVE "NEXT" TO WS-FLAG
           ELSE
               DISPLAY "ELSE BRANCH".
           DISPLAY "AFTER IF " WS-FLAG.
           CLOSE SQ-FILE.
       P-AGAIN.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE.
           PERFORM P-AT-END.
           DISPLAY "AFTER PERFORM STATUS " SQ-STATUS.
           CLOSE SQ-FILE.
           DISPLAY "USE PROCEDURE RAN " USE-COUNT " TIMES".
           STOP RUN.
       P-AT-END.
           READ SQ-FILE.
       P-NOT-REACHED.
           DISPLAY "FELL THROUGH THE PERFORMED PARAGRAPH".
