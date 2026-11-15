      *>--- conformance-case
      *> id: g2002-disable-output-count-range
      *> rules: [2002-GR-A.2.2.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.3 GR6 - DISABLE OUTPUT deactivates the paths "for
      *> all destinations specified by the content of each occurrence
      *> of data-name-5 up to and including the occurrence specified by
      *> the content of data-name-1": with D1 D2 D3 in the table and a
      *> DESTINATION COUNT of 2, D1 and D2 are disabled and D3 is not.
      *> Table A.1 (§2002:A.1.2.4): DISABLE OUTPUT 00 when no error;
      *> SEND output-cd 10 "One or more destinations disabled. Action
      *> completed"; DISABLE OUTPUT 15 when destinations are already
      *> disabled. Table A.2 (§2002:A.1.2.5): error key 2 "Symbolic
      *> destination disabled" (SEND), 5 "already enabled/disabled"
      *> (DISABLE OUTPUT), 0 "No error"; each occurrence up to the
      *> count is updated when an error occurs (§2002:A.1.2.3 GR22).
      *> Each destination delivers to its own queue (this MCS's
      *> mcs.json). Legs: the SEND line fails if the DISABLE stopped
      *> short of occurrence 2 (error key 0 for D2) or went past it
      *> (error key 2 for D3, or status other than 10); the queue
      *> counts fail if D3's path was deactivated (Q3 0) or D1's was
      *> not (Q1 1); the second DISABLE fails if D3 had already been
      *> disabled (error key 5 for D3) or D1/D2 had not (0).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS20.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
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
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 3 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1""},{""name"":""Q3""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"",""deliverTo"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """Q1""},{""name"":""D2"",""deliverTo"":""Q1""},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""D3"",""deliverTo"":""Q3""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE "D3" TO OUT-DEST (3).
           MOVE 5 TO OUT-LEN.
           MOVE 2 TO OUT-CNT.
           MOVE "XX" TO OUT-STAT.
           DISABLE OUTPUT COUT.
           DISPLAY "DIS CNT 2   " OUT-STAT.
           MOVE 3 TO OUT-CNT.
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND CNT 3  " OUT-STAT " "
               OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           MOVE "Q1" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 COUNT    " IN-CNT.
           MOVE "Q3" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q3 COUNT    " IN-CNT.
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           DISABLE OUTPUT COUT.
           DISPLAY "DIS CNT 3   " OUT-STAT " "
               OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           STOP RUN.
