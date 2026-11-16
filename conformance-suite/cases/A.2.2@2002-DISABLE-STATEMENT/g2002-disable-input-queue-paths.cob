      *>--- conformance-case
      *> id: g2002-disable-input-queue-paths
      *> rules: [2002-GR-A.2.2.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.3 GR3 - DISABLE INPUT (no TERMINAL) deactivates the
      *> paths between the queue structure named by the CD's four names
      *> "and all the associated enabled sources". T1 feeds Q1/S1 (D1),
      *> Q1/S2 (D2) and Q2/S1 (D3). With Q1/S1 disabled a message sent
      *> to D1 and D2 reaches Q1/S2 only; disabling Q2 itself reaches
      *> its sub-queue S1, which a DISABLE of Q2/S1 then finds already
      *> disabled. Table A.1 (§2002:A.1.2.4) DISABLE INPUT: 00, 15
      *> already disabled, 20 queue unknown - the column printed between
      *> ENABLE OUTPUT and DISABLE INPUT I-O TERMINAL, misprinted
      *> "ENABLE INPUT" in the 2002 table. ENABLE INPUT of Q1/S1
      *> (§2002:A.2.3.3 GR2) lets the held message in. A path left open,
      *> the wrong sub-queue closed, or a message lost fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS03.
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
           MOVE "{""queues"":[{""name"":""Q1"",""subQueues"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""S1""},{""name"":""S2""}]},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""Q2"",""subQueues"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""S1""}]}],""sources"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""T1""}],""destinations"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D1"",""deliverTo"":""Q1/S1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """source"":""T1""},{""name"":""D2"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""Q1/S2"",""source"":""T1""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D3"",""deliverTo"":""Q2/S1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """source"":""T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "Q1" TO IN-Q.
           MOVE "S1" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q1 S1     " IN-STAT.
           MOVE 2 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND D1 D2    " OUT-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 S1 COUNT   " IN-CNT.
           MOVE "S2" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 S2 COUNT   " IN-CNT.
           MOVE "S1" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q1 S1 AGN " IN-STAT.
           MOVE "QZ" TO IN-Q.
           MOVE SPACES TO IN-S1.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT CIN.
           DISPLAY "DIS QZ        " IN-STAT.
           MOVE "Q2" TO IN-Q.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q2        " IN-STAT.
           MOVE 1 TO OUT-CNT.
           MOVE "D3" TO OUT-DEST (1).
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND D3       " OUT-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q2 COUNT      " IN-CNT.
           MOVE "S1" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q2 S1     " IN-STAT.
           MOVE "Q1" TO IN-Q.
           MOVE "XX" TO IN-STAT.
           ENABLE INPUT CIN.
           DISPLAY "ENA Q1 S1     " IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 S1 COUNT   " IN-CNT.
           STOP RUN.
