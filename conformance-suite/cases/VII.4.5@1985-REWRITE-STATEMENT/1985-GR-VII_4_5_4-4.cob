      *>--- conformance-case
      *> id: 1985-GR-VII_4_5_4-4
      *> rules: [1985-GR-VII.4.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR4 (THE REWRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "w85c3.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(8).
       01  SQ-NUM PIC 9(8).
       01  SQ-JUST PIC X(8) JUSTIFIED RIGHT.
       WORKING-STORAGE SECTION.
       01  SQ-STATUS PIC XX VALUE "??".
       01  WS-SHORT PIC X(3) VALUE "ABC".
       01  WS-LONG PIC X(12) VALUE "ABCDEFGHIJKL".
       01  WS-NUM PIC 9(3) VALUE 123.
       01  WS-GROUP.
           05  WS-G1 PIC 99 VALUE 45.
           05  WS-G2 PIC X(2) VALUE "XY".
       PROCEDURE DIVISION.
       P-CREATE.
           OPEN OUTPUT SQ-FILE.
           MOVE "11111111" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "22222222" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "33333333" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "44444444" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "55555555" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
       P-REWRITE.
           OPEN I-O SQ-FILE.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           REWRITE SQ-REC FROM WS-SHORT.
           DISPLAY "REWRITE 1 STATUS " SQ-STATUS " FROM " WS-SHORT.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           REWRITE SQ-REC FROM WS-LONG.
           DISPLAY "REWRITE 2 STATUS " SQ-STATUS " FROM " WS-LONG.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           REWRITE SQ-NUM FROM WS-NUM.
           DISPLAY "REWRITE 3 STATUS " SQ-STATUS " FROM " WS-NUM.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           REWRITE SQ-JUST FROM WS-SHORT.
           DISPLAY "REWRITE 4 STATUS " SQ-STATUS " FROM " WS-SHORT.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           REWRITE SQ-REC FROM WS-GROUP.
           DISPLAY "REWRITE 5 STATUS " SQ-STATUS " FROM " WS-GROUP.
           CLOSE SQ-FILE.
       P-CHECK.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "RECORD 1 [" SQ-REC "]".
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "RECORD 2 [" SQ-REC "]".
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "RECORD 3 [" SQ-REC "]".
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "RECORD 4 [" SQ-REC "]".
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "RECORD 5 [" SQ-REC "]".
           READ SQ-FILE AT END DISPLAY "AT END " SQ-STATUS.
           CLOSE SQ-FILE.
           STOP RUN.
