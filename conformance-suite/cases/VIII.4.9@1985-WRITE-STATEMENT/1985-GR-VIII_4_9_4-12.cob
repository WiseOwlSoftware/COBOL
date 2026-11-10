      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-12
      *> rules: [1985-GR-VIII.4.9.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR12 (THE WRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85WR12.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RND-F ASSIGN TO "r85wr12.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK-R
               FILE STATUS IS FS.
           SELECT SEQ-F ASSIGN TO "r85wr12.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS RK-S
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RND-F.
       01  RN-REC PIC X(4).
       FD  SEQ-F.
       01  SQ-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK-R PIC 9(4).
       01  RK-S PIC 9(4).
       PROCEDURE DIVISION.
       BUILD-1.
           OPEN OUTPUT RND-F.
           MOVE 2 TO RK-R.
           MOVE "R2R2" TO RN-REC.
           WRITE RN-REC INVALID KEY DISPLAY "W2 INVALID KEY".
           MOVE 5 TO RK-R.
           MOVE "R5R5" TO RN-REC.
           WRITE RN-REC INVALID KEY DISPLAY "W5 INVALID KEY".
           CLOSE RND-F.
       EXTEND-1.
           OPEN EXTEND SEQ-F.
           DISPLAY "X0 " FS.
           MOVE 1 TO RK-S.
           MOVE "E6E6" TO SQ-REC.
           WRITE SQ-REC INVALID KEY DISPLAY "X1 INVALID KEY".
           DISPLAY "X1 " FS " " RK-S.
           MOVE 1 TO RK-S.
           MOVE "E7E7" TO SQ-REC.
           WRITE SQ-REC INVALID KEY DISPLAY "X2 INVALID KEY".
           DISPLAY "X2 " FS " " RK-S.
           CLOSE SEQ-F.
       CHECK-1.
           OPEN I-O RND-F.
           MOVE 6 TO RK-R.
           READ RND-F RECORD INVALID KEY DISPLAY "Y6 INVALID KEY".
           DISPLAY "Y6 " FS " " RN-REC.
           MOVE 7 TO RK-R.
           READ RND-F RECORD INVALID KEY DISPLAY "Y7 INVALID KEY".
           DISPLAY "Y7 " FS " " RN-REC.
           MOVE 1 TO RK-R.
           READ RND-F RECORD INVALID KEY DISPLAY "Y1 INVALID KEY " FS.
           MOVE 3 TO RK-R.
           READ RND-F RECORD INVALID KEY DISPLAY "Y3 INVALID KEY " FS.
           MOVE 8 TO RK-R.
           READ RND-F RECORD INVALID KEY DISPLAY "Y8 INVALID KEY " FS.
           MOVE 7 TO RK-R.
           DELETE RND-F RECORD INVALID KEY DISPLAY "Z7 INVALID KEY".
           DISPLAY "Z7 " FS.
           MOVE 6 TO RK-R.
           DELETE RND-F RECORD INVALID KEY DISPLAY "Z6 INVALID KEY".
           DISPLAY "Z6 " FS.
           CLOSE RND-F.
       EXTEND-2.
           OPEN EXTEND SEQ-F.
           MOVE 1 TO RK-S.
           MOVE "N6N6" TO SQ-REC.
           WRITE SQ-REC INVALID KEY DISPLAY "X3 INVALID KEY".
           DISPLAY "X3 " FS " " RK-S.
           CLOSE SEQ-F.
       CHECK-2.
           OPEN INPUT SEQ-F.
           MOVE 9999 TO RK-S.
           READ SEQ-F RECORD AT END DISPLAY "S1 AT END".
           DISPLAY "S1 " RK-S " " SQ-REC.
           MOVE 9999 TO RK-S.
           READ SEQ-F RECORD AT END DISPLAY "S2 AT END".
           DISPLAY "S2 " RK-S " " SQ-REC.
           MOVE 9999 TO RK-S.
           READ SEQ-F RECORD AT END DISPLAY "S3 AT END".
           DISPLAY "S3 " RK-S " " SQ-REC.
           READ SEQ-F RECORD AT END DISPLAY "S4 AT END".
           CLOSE SEQ-F.
           STOP RUN.
