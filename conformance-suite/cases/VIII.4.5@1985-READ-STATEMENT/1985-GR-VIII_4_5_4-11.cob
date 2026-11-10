      *>--- conformance-case
      *> id: 1985-GR-VIII_4_5_4-11
      *> rules: [1985-GR-VIII.4.5.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR11 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85RD11.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-F ASSIGN TO "r85rd11.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-F.
       01  RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK PIC 9(4).
       01  WS-INTO PIC X(6).
       PROCEDURE DIVISION.
       DECLARATIVES.
       RL-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-F.
       RL-ERR-1.
           DISPLAY "USE " FS.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-1.
           OPEN OUTPUT REL-F.
           MOVE 1 TO RK.
           MOVE "AAAA" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE 2 TO RK.
           MOVE "BBBB" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE 4 TO RK.
           MOVE "DDDD" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "W4 INVALID".
           CLOSE REL-F.
           OPEN INPUT REL-F.
           MOVE "XX" TO FS.
           MOVE SPACES TO WS-INTO.
           READ REL-F NEXT RECORD INTO WS-INTO
               AT END DISPLAY "R1 AT END"
               NOT AT END DISPLAY "R1 NOT AT END"
           END-READ.
           DISPLAY "R1 " FS " " RL-REC " " WS-INTO ".".
           MOVE "XX" TO FS.
           READ REL-F NEXT RECORD
               AT END DISPLAY "R2 AT END"
           END-READ.
           DISPLAY "R2 " FS " " RL-REC.
           MOVE 1 TO RK.
           MOVE "XX" TO FS.
           MOVE SPACES TO WS-INTO.
           READ REL-F RECORD INTO WS-INTO
               INVALID KEY DISPLAY "R3 INVALID KEY"
               NOT INVALID KEY DISPLAY "R3 NOT INVALID KEY"
           END-READ.
           DISPLAY "R3 " FS " " RL-REC " " WS-INTO ".".
           MOVE "XX" TO FS.
           READ REL-F NEXT RECORD
               AT END DISPLAY "R4 AT END"
           END-READ.
           DISPLAY "R4 " FS " " RL-REC.
           CLOSE REL-F.
           MOVE "XX" TO FS.
           READ REL-F NEXT RECORD
               AT END DISPLAY "R5 AT END"
               NOT AT END DISPLAY "R5 NOT AT END"
           END-READ.
           DISPLAY "R5 RETURNED " FS.
           STOP RUN.
