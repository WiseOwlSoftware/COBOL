      *>--- conformance-case
      *> id: 1985-GR-XIV_1_1-p1
      *> rules: [1985-GR-XIV.1.1-p1, 1985-SR-XIV.2.1.1-p1, 1985-SR-XIV.2.1.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.1.1 p1; §1985:XIV.2.1.1 p1; §1985:XIV.2.1.2 p2
      *> (INTRODUCTION TO THE COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-18.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 XIV.2.1.1 / XIV.2.1.2: the Communication Section holds
      * a CD entry (CD, cd-name, independent clauses, period) and
      * a record description of hierarchical data description
      * entries (level-number, data-name or FILLER, clauses) that
      * describes the CD's area. Q1 holds two messages; the queue
      * name is moved through the record, the count read through it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       01  CIN-REC.
           02  CIN-QUEUES.
               03  CIN-Q       PIC X(12).
               03  CIN-SUBS.
                   04  CIN-SUB PIC X(12) OCCURS 3 TIMES.
           02  FILLER          PIC X(30).
           02  CIN-TAIL.
               03  CIN-END     PIC X.
               03  CIN-ST      PIC XX.
               03  CIN-CNT     PIC 9(6).
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """messages"":[""ONE"",""TWO""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO CIN-QUEUES.
           MOVE "Q1" TO CIN-Q.
           MOVE "XX" TO IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "REC " CIN-ST " " CIN-CNT.
           DISPLAY "CD  " IN-STAT " " IN-CNT.
           STOP RUN.
