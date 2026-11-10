      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-14
      *> rules: [1985-GR-VIII.4.9.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR14 (THE WRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85WR14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-F ASSIGN TO "r85wr14.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS RK-1
               FILE STATUS IS FS.
           SELECT RND-F ASSIGN TO "r85wr14.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK-R
               FILE STATUS IS FS.
           SELECT DYN-F ASSIGN TO "r85wr14.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK-D
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-F.
       01  SQ-REC PIC X(4).
       FD  RND-F.
       01  RN-REC PIC X(4).
       FD  DYN-F.
       01  DY-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK-1 PIC 9.
       01  RK-R PIC 9(4).
       01  RK-D PIC 9(4).
       01  WS-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       SEQ-PART.
           OPEN OUTPUT SEQ-F.
           PERFORM WRITE-ONE 9 TIMES.
           DISPLAY "C9 " FS " " RK-1.
           MOVE "TENT" TO SQ-REC.
           MOVE "XX" TO FS.
           WRITE SQ-REC
               INVALID KEY DISPLAY "C10 INVALID KEY " FS
               NOT INVALID KEY DISPLAY "C10 NOT INVALID KEY " FS
           END-WRITE.
           DISPLAY "C10 REC " SQ-REC.
           CLOSE SEQ-F.
       RND-PART.
           OPEN I-O RND-F.
           MOVE 10 TO RK-R.
           READ RND-F RECORD INVALID KEY DISPLAY "A10 INVALID KEY " FS.
           MOVE 3 TO RK-R.
           MOVE "DUPL" TO RN-REC.
           MOVE "XX" TO FS.
           WRITE RN-REC
               INVALID KEY DISPLAY "A3 INVALID KEY " FS
               NOT INVALID KEY DISPLAY "A3 NOT INVALID KEY " FS
           END-WRITE.
           DISPLAY "A3 REC " RN-REC.
           READ RND-F RECORD INVALID KEY DISPLAY "A3 READ INVALID KEY".
           DISPLAY "A3 READ " FS " " RN-REC.
           MOVE 10 TO RK-R.
           MOVE "NEW1" TO RN-REC.
           MOVE "XX" TO FS.
           WRITE RN-REC
               INVALID KEY DISPLAY "A10 INVALID KEY " FS
               NOT INVALID KEY DISPLAY "A10 NOT INVALID KEY " FS
           END-WRITE.
           CLOSE RND-F.
       DYN-PART.
           OPEN I-O DYN-F.
           MOVE 10 TO RK-D.
           MOVE "DUP2" TO DY-REC.
           MOVE "XX" TO FS.
           WRITE DY-REC
               INVALID KEY DISPLAY "D10 INVALID KEY " FS
               NOT INVALID KEY DISPLAY "D10 NOT INVALID KEY " FS
           END-WRITE.
           DISPLAY "D10 REC " DY-REC.
           READ DYN-F RECORD INVALID KEY DISPLAY "D10 READ INVALID KEY".
           DISPLAY "D10 READ " FS " " DY-REC.
           MOVE 11 TO RK-D.
           MOVE "NEW2" TO DY-REC.
           MOVE "XX" TO FS.
           WRITE DY-REC
               INVALID KEY DISPLAY "D11 INVALID KEY " FS
               NOT INVALID KEY DISPLAY "D11 NOT INVALID KEY " FS
           END-WRITE.
           CLOSE DYN-F.
           STOP RUN.
       WRITE-ONE.
           ADD 1 TO WS-N.
           MOVE WS-N TO SQ-REC.
           WRITE SQ-REC INVALID KEY DISPLAY "C INVALID KEY AT " WS-N.
