      *>--- conformance-case
      *> id: 1985-GR-III_2-p148
      *> rules: [1985-GR-III.2-p148]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p148 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p148: the invalid key condition arises when a
      * key value of an indexed or relative file is invalid: a READ
      * of a record that does not exist takes INVALID KEY on both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P148.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "p148.idx"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-K
               FILE STATUS IS FS1.
           SELECT RL ASSIGN TO "p148.rel"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-R.
           05  IX-K PIC X(3).
           05  IX-D PIC X(3).
       FD  RL.
       01  RL-R PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       01  RK PIC 99.
       01  HITS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IX.
           MOVE "K01" TO IX-K. MOVE "AAA" TO IX-D.
           WRITE IX-R INVALID KEY DISPLAY "W1 INVALID".
           CLOSE IX.
           OPEN INPUT IX.
           MOVE "K99" TO IX-K.
           READ IX INVALID KEY ADD 1 TO HITS
               DISPLAY "IX INVALID KEY " FS1.
           CLOSE IX.
           OPEN OUTPUT RL.
           MOVE 1 TO RK. MOVE "BBB" TO RL-R.
           WRITE RL-R INVALID KEY DISPLAY "W2 INVALID".
           CLOSE RL.
           OPEN INPUT RL.
           MOVE 7 TO RK.
           READ RL INVALID KEY ADD 1 TO HITS
               DISPLAY "RL INVALID KEY " FS2.
           CLOSE RL.
           IF HITS = 2
               DISPLAY "P148 OK"
           ELSE
               DISPLAY "P148 FAIL " HITS.
           STOP RUN.
