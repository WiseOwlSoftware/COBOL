      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-11
      *> rules: [1985-GR-VIII.4.9.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR11 (THE WRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85WR11.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-A ASSIGN TO "r85wr11a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS RK-SA
               FILE STATUS IS FS.
           SELECT RND-A ASSIGN TO "r85wr11a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK-RA
               FILE STATUS IS FS.
           SELECT RND-B ASSIGN TO "r85wr11b.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK-RB
               FILE STATUS IS FS.
           SELECT SEQ-B ASSIGN TO "r85wr11b.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS RK-SB
               FILE STATUS IS FS.
           SELECT DYN-C ASSIGN TO "r85wr11c.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK-DC
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-A.
       01  SA-REC PIC X(4).
       FD  RND-A.
       01  RA-REC PIC X(4).
       FD  RND-B.
       01  RB-REC PIC X(4).
       FD  SEQ-B.
       01  SB-REC PIC X(4).
       FD  DYN-C.
       01  DC-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK-SA PIC 9(4).
       01  RK-RA PIC 9(4).
       01  RK-RB PIC 9(4).
       01  RK-SB PIC 9(4).
       01  RK-DC PIC 9(4).
       PROCEDURE DIVISION.
       SEQ-OUT.
           OPEN OUTPUT SEQ-A.
           MOVE 7 TO RK-SA.
           MOVE "AAAA" TO SA-REC.
           WRITE SA-REC INVALID KEY DISPLAY "A1 INVALID KEY".
           DISPLAY "A1 " FS " " RK-SA.
           MOVE 7 TO RK-SA.
           MOVE "BBBB" TO SA-REC.
           WRITE SA-REC INVALID KEY DISPLAY "A2 INVALID KEY".
           DISPLAY "A2 " FS " " RK-SA.
           MOVE 7 TO RK-SA.
           MOVE "CCCC" TO SA-REC.
           WRITE SA-REC INVALID KEY DISPLAY "A3 INVALID KEY".
           DISPLAY "A3 " FS " " RK-SA.
           CLOSE SEQ-A.
       SEQ-CHECK.
           OPEN INPUT RND-A.
           MOVE 2 TO RK-RA.
           READ RND-A RECORD INVALID KEY DISPLAY "B2 INVALID KEY".
           DISPLAY "B2 " FS " " RA-REC.
           MOVE 1 TO RK-RA.
           READ RND-A RECORD INVALID KEY DISPLAY "B1 INVALID KEY".
           DISPLAY "B1 " FS " " RA-REC.
           MOVE 3 TO RK-RA.
           READ RND-A RECORD INVALID KEY DISPLAY "B3 INVALID KEY".
           DISPLAY "B3 " FS " " RA-REC.
           MOVE 4 TO RK-RA.
           READ RND-A RECORD INVALID KEY DISPLAY "B4 INVALID KEY " FS.
           MOVE 7 TO RK-RA.
           READ RND-A RECORD INVALID KEY DISPLAY "B7 INVALID KEY " FS.
           CLOSE RND-A.
       RND-OUT.
           OPEN OUTPUT RND-B.
           MOVE 5 TO RK-RB.
           MOVE "R5R5" TO RB-REC.
           WRITE RB-REC INVALID KEY DISPLAY "C5 INVALID KEY".
           DISPLAY "C5 " FS " " RK-RB.
           MOVE 2 TO RK-RB.
           MOVE "R2R2" TO RB-REC.
           WRITE RB-REC INVALID KEY DISPLAY "C2 INVALID KEY".
           DISPLAY "C2 " FS " " RK-RB.
           CLOSE RND-B.
       RND-CHECK.
           OPEN INPUT SEQ-B.
           MOVE 9999 TO RK-SB.
           READ SEQ-B RECORD AT END DISPLAY "D1 AT END".
           DISPLAY "D1 " RK-SB " " SB-REC.
           MOVE 9999 TO RK-SB.
           READ SEQ-B RECORD AT END DISPLAY "D2 AT END".
           DISPLAY "D2 " RK-SB " " SB-REC.
           READ SEQ-B RECORD AT END DISPLAY "D3 AT END".
           CLOSE SEQ-B.
       DYN-OUT.
           OPEN OUTPUT DYN-C.
           MOVE 6 TO RK-DC.
           MOVE "D6D6" TO DC-REC.
           WRITE DC-REC INVALID KEY DISPLAY "E6 INVALID KEY".
           DISPLAY "E6 " FS " " RK-DC.
           MOVE 3 TO RK-DC.
           MOVE "D3D3" TO DC-REC.
           WRITE DC-REC INVALID KEY DISPLAY "E3 INVALID KEY".
           DISPLAY "E3 " FS " " RK-DC.
           CLOSE DYN-C.
       DYN-CHECK.
           OPEN INPUT DYN-C.
           MOVE 9999 TO RK-DC.
           READ DYN-C NEXT RECORD AT END DISPLAY "F1 AT END".
           DISPLAY "F1 " RK-DC " " DC-REC.
           MOVE 9999 TO RK-DC.
           READ DYN-C NEXT RECORD AT END DISPLAY "F2 AT END".
           DISPLAY "F2 " RK-DC " " DC-REC.
           READ DYN-C NEXT RECORD AT END DISPLAY "F3 AT END".
           MOVE 6 TO RK-DC.
           READ DYN-C RECORD INVALID KEY DISPLAY "F6 INVALID KEY".
           DISPLAY "F6 " FS " " DC-REC.
           CLOSE DYN-C.
           STOP RUN.
