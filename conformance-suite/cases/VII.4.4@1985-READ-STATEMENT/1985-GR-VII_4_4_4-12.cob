      *>--- conformance-case
      *> id: 1985-GR-VII_4_4_4-12
      *> rules: [1985-GR-VII.4.4.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR12 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "w85c2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  SQ-STATUS PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       SQ-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SQ-FILE.
       SQ-ERR-1.
           DISPLAY "USE PROCEDURE STATUS " SQ-STATUS.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-CREATE.
           OPEN OUTPUT SQ-FILE.
           MOVE "REC-0001" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "REC-0002" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
       P-READ.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE
               AT END DISPLAY "READ 1 AT END"
               NOT AT END DISPLAY "READ 1 " SQ-REC
           END-READ.
           DISPLAY "READ 1 STATUS " SQ-STATUS.
           READ SQ-FILE
               AT END DISPLAY "READ 2 AT END"
               NOT AT END DISPLAY "READ 2 " SQ-REC
           END-READ.
           DISPLAY "READ 2 STATUS " SQ-STATUS.
           READ SQ-FILE
               AT END DISPLAY "READ 3 AT END"
               NOT AT END DISPLAY "READ 3 " SQ-REC
           END-READ.
           DISPLAY "READ 3 STATUS " SQ-STATUS.
           READ SQ-FILE
               AT END DISPLAY "READ 4 AT END"
               NOT AT END DISPLAY "READ 4 NOT AT END"
           END-READ.
           DISPLAY "READ 4 STATUS " SQ-STATUS.
           READ SQ-FILE
               AT END DISPLAY "READ 5 AT END"
               NOT AT END DISPLAY "READ 5 NOT AT END"
           END-READ.
           DISPLAY "READ 5 STATUS " SQ-STATUS.
           CLOSE SQ-FILE.
       P-REOPEN.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE
               AT END DISPLAY "READ 6 AT END"
               NOT AT END DISPLAY "READ 6 " SQ-REC
           END-READ.
           DISPLAY "READ 6 STATUS " SQ-STATUS.
           CLOSE SQ-FILE.
           STOP RUN.
