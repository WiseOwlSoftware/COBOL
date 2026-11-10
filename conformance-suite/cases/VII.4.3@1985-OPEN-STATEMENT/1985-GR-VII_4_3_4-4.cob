      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-4
      *> rules: [1985-GR-VII.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR4 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "w85c1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  SQ-STATUS PIC XX VALUE "??".
       01  USE-COUNT PIC 99 VALUE ZERO.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SQ-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SQ-FILE.
       SQ-ERR-1.
           ADD 1 TO USE-COUNT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-NOT-OPEN.
           READ SQ-FILE.
           DISPLAY "NOT OPEN READ    " SQ-STATUS.
           MOVE "REC-0000" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "NOT OPEN WRITE   " SQ-STATUS.
       P-OUTPUT.
           OPEN OUTPUT SQ-FILE.
           DISPLAY "OPEN OUTPUT      " SQ-STATUS.
           READ SQ-FILE.
           DISPLAY "OUTPUT READ      " SQ-STATUS.
           MOVE "REC-0001" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "OUTPUT WRITE     " SQ-STATUS.
           CLOSE SQ-FILE.
       P-INPUT.
           OPEN INPUT SQ-FILE.
           DISPLAY "OPEN INPUT       " SQ-STATUS.
           MOVE "REC-0002" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "INPUT WRITE      " SQ-STATUS.
           READ SQ-FILE.
           DISPLAY "INPUT READ       " SQ-STATUS.
           REWRITE SQ-REC.
           DISPLAY "INPUT REWRITE    " SQ-STATUS.
           CLOSE SQ-FILE.
       P-I-O.
           OPEN I-O SQ-FILE.
           DISPLAY "OPEN I-O         " SQ-STATUS.
           READ SQ-FILE.
           DISPLAY "I-O READ         " SQ-STATUS.
           MOVE "REC-0003" TO SQ-REC.
           REWRITE SQ-REC.
           DISPLAY "I-O REWRITE      " SQ-STATUS.
           MOVE "REC-0004" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "I-O WRITE        " SQ-STATUS.
           CLOSE SQ-FILE.
       P-EXTEND.
           OPEN EXTEND SQ-FILE.
           DISPLAY "OPEN EXTEND      " SQ-STATUS.
           READ SQ-FILE.
           DISPLAY "EXTEND READ      " SQ-STATUS.
           MOVE "REC-0005" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "EXTEND WRITE     " SQ-STATUS.
           CLOSE SQ-FILE.
       P-CHECK.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE.
           DISPLAY "RECORD 1 " SQ-REC " " SQ-STATUS.
           READ SQ-FILE.
           DISPLAY "RECORD 2 " SQ-REC " " SQ-STATUS.
           READ SQ-FILE AT END DISPLAY "AT END           " SQ-STATUS.
           CLOSE SQ-FILE.
           DISPLAY "USE PROCEDURE RAN " USE-COUNT " TIMES".
           STOP RUN.
