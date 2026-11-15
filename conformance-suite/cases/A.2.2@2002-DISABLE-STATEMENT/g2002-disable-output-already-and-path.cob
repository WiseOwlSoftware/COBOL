      *>--- conformance-case
      *> id: g2002-disable-output-already-and-path
      *> rules: [2002-GR-A.2.2.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.3 GR1 - DISABLE disconnects the MCS from the named
      *> destination; one "already disconnected" takes no action
      *> "except that the value in the status key data item indicates
      *> this condition"; and the path between the program and the MCS
      *> is not affected. Table A.1 (§2002:A.1.2.4), DISABLE OUTPUT: 00,
      *> 15 already disabled, 20 destination unknown with action for the
      *> known one, 80 for 15 with 20; Table A.2 (§2002:A.1.2.5) error
      *> keys 5 already disabled, 1 unknown, 0 none; error keys are set
      *> when an error occurs (§2002:A.1.2.3 GR22). D1 delivers to Q1
      *> (this MCS's mcs.json). A SEND to disabled D1 still reaches the
      *> MCS: 10 "Action completed", error key 2, and Q1 holds nothing
      *> until ENABLE OUTPUT (§2002:A.2.3.3 GR5) reactivates D1. Each
      *> line fails if the second DISABLE acts or reports 00, if the
      *> SEND is refused or delivered while D1 is disabled, or if the
      *> message is lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS01.
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
           MOVE "{""queues"":[{""name"":""Q1""}],""destinations"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""D1"",""deliverTo"":""Q1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D9" TO OUT-DEST (2).
           MOVE 5 TO OUT-LEN.
           MOVE "XX" TO OUT-STAT.
           DISABLE OUTPUT COUT.
           DISPLAY "DIS D1      " OUT-STAT.
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           DISABLE OUTPUT COUT.
           DISPLAY "DIS D1 AGN  " OUT-STAT " " OUT-ERR (1).
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND D1     " OUT-STAT " " OUT-ERR (1).
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 COUNT    " IN-CNT.
           MOVE 2 TO OUT-CNT.
           MOVE "X" TO OUT-ERR (1) OUT-ERR (2).
           DISABLE OUTPUT COUT.
           DISPLAY "DIS D1 D9   " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           MOVE 1 TO OUT-CNT.
           ENABLE OUTPUT COUT.
           DISPLAY "ENA D1      " OUT-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 COUNT    " IN-CNT.
           MOVE 2 TO OUT-CNT.
           MOVE "X" TO OUT-ERR (1) OUT-ERR (2).
           DISABLE OUTPUT COUT.
           DISPLAY "DIS D1 D9 2 " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           STOP RUN.
