      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_3-p5
      *> rules: [1985-GR-IV.6.4.3-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.3 p5 (PROCEDURE DIVISION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * Refuter probe s-07: 1985 IV.6.4.3 p4 (END-RECEIVE ends the most
      * recent unterminated RECEIVE) and p5 (WITH DATA binds to the
      * nearest RECEIVE that has none) in the Communication module.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R02NRCV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """ONE""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
      * L1: outer gets ONE; inner finds the queue empty.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "L1-O-ND"
               WITH DATA DISPLAY "L1-O-WD"
                   RECEIVE CIN MESSAGE INTO BUF
                       NO DATA DISPLAY "L1-I-ND"
                       WITH DATA DISPLAY "L1-I-WD"
                   END-RECEIVE
                   DISPLAY "L1-O-TAIL"
           END-RECEIVE.
      * L2: inner END-RECEIVE ends the inner; WITH DATA is the outer's.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "L2-O-ND"
                   RECEIVE CIN MESSAGE INTO BUF
                       NO DATA DISPLAY "L2-I-ND"
                   END-RECEIVE
                   DISPLAY "L2-O-TAIL"
               WITH DATA DISPLAY "L2-O-WD"
           END-RECEIVE.
      * L3: WITH DATA is the inner's (it has none yet).
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "L3-O-ND"
                   RECEIVE CIN MESSAGE INTO BUF
                       NO DATA DISPLAY "L3-I-ND"
                       WITH DATA DISPLAY "L3-I-WD"
                   END-RECEIVE
                   DISPLAY "L3-O-TAIL"
           END-RECEIVE.
           DISPLAY "DONE".
           STOP RUN.
