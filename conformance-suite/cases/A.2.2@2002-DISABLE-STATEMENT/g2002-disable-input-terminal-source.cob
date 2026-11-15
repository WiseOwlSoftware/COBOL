      *>--- conformance-case
      *> id: g2002-disable-input-terminal-source
      *> rules: [2002-GR-A.2.2.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.3 GR4 - DISABLE INPUT TERMINAL deactivates the
      *> paths between the source named by SYMBOLIC SOURCE "and all of
      *> its associated queues and sub-queues are deactivated". T1
      *> feeds QA (D1) and QB (D2), T2 feeds QA (D3). With T1 disabled a
      *> message to D1, D2, D3 reaches QA once (from T2) and QB not at
      *> all. Table A.1 (§2002:A.1.2.4) DISABLE INPUT TERMINAL: 00, 15
      *> already disabled, 21 source unknown. ENABLE INPUT TERMINAL
      *> (§2002:A.2.3.3 GR3) lets T1's held messages in. A path of T2
      *> closed, a T1 path left open, or a lost message fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS04.
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
           MOVE "{""queues"":[{""name"":""QA""},{""name"":""QB""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """sources"":[{""name"":""T1""},{""name"":""T2""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""QA"",""source"":""T1""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D2"",""deliverTo"":""QB"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """source"":""T1""},{""name"":""D3"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""QA"",""source"":""T2""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO IN-Q IN-S1 IN-S2 IN-S3.
           MOVE "T1" TO IN-SRC.
           MOVE 3 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE "D3" TO OUT-DEST (3).
           MOVE 5 TO OUT-LEN.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DIS T1      " IN-STAT.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND        " OUT-STAT.
           MOVE "QA" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "QA COUNT    " IN-CNT.
           MOVE "QB" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "QB COUNT    " IN-CNT.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DIS T1 AGN  " IN-STAT.
           MOVE "TX" TO IN-SRC.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DIS TX      " IN-STAT.
           MOVE "T1" TO IN-SRC.
           MOVE "XX" TO IN-STAT.
           ENABLE INPUT TERMINAL CIN.
           DISPLAY "ENA T1      " IN-STAT.
           MOVE "QA" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "QA COUNT    " IN-CNT.
           MOVE "QB" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "QB COUNT    " IN-CNT.
           STOP RUN.
