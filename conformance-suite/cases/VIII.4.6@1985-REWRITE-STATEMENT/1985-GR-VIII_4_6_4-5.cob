      *>--- conformance-case
      *> id: 1985-GR-VIII_4_6_4-5
      *> rules: [1985-GR-VIII.4.6.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.4 GR5 (THE REWRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85RW05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-F ASSIGN TO "r85rw05.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-F.
       01  RL-REC PIC X(6).
       01  RL-NUM PIC 9(6).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  WS-SHORT PIC X(3) VALUE "ABC".
       01  WS-LONG PIC X(10) VALUE "LONGERTHAN".
       01  WS-N3 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       MAIN-1.
           OPEN OUTPUT REL-F.
           MOVE "111111" TO RL-REC.
           WRITE RL-REC.
           MOVE "222222" TO RL-REC.
           WRITE RL-REC.
           MOVE "333333" TO RL-REC.
           WRITE RL-REC.
           CLOSE REL-F.
           OPEN I-O REL-F.
           READ REL-F RECORD AT END DISPLAY "U1 AT END".
           MOVE "XX" TO FS.
           REWRITE RL-REC FROM WS-SHORT.
           DISPLAY "U1 " FS " [" WS-SHORT "]".
           READ REL-F RECORD AT END DISPLAY "U2 AT END".
           MOVE "XX" TO FS.
           REWRITE RL-REC FROM WS-LONG.
           DISPLAY "U2 " FS " [" WS-LONG "]".
           READ REL-F RECORD AT END DISPLAY "U3 AT END".
           MOVE "XX" TO FS.
           REWRITE RL-NUM FROM WS-N3.
           DISPLAY "U3 " FS " [" WS-N3 "]".
           CLOSE REL-F.
           OPEN INPUT REL-F.
           READ REL-F RECORD AT END DISPLAY "V1 AT END".
           DISPLAY "V1 [" RL-REC "]".
           READ REL-F RECORD AT END DISPLAY "V2 AT END".
           DISPLAY "V2 [" RL-REC "]".
           READ REL-F RECORD AT END DISPLAY "V3 AT END".
           DISPLAY "V3 [" RL-REC "]".
           READ REL-F RECORD AT END DISPLAY "V4 AT END".
           CLOSE REL-F.
           STOP RUN.
