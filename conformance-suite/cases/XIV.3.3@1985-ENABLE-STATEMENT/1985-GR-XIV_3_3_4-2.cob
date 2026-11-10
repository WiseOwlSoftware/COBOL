      *>--- conformance-case
      *> id: 1985-GR-XIV_3_3_4-2
      *> rules: [1985-GR-XIV.3.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.3.4 GR2 (THE ENABLE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.3.4 general rule 2: ENABLE INPUT opens the
      * paths between the queue and sub-queue named and their sources.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENAGR2.
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
           MOVE """name"":""S1"",""enabled"":false},{""name"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """S2"",""enabled"":false}]}],""sources"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""T1"",""enabled"":true}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"",""enabled"""
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE ":true,""deliverTo"":""Q1/S1"",""source"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """T1""},{""name"":""D2"",""enabled"":true,"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""Q1/S2"",""source"":""T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "S1" TO IN-S1.
           MOVE 2 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 S1   " IN-CNT.
           MOVE "XX" TO IN-STAT.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE  " IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 S1   " IN-CNT.
           MOVE "S2" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 S2   " IN-CNT.
           MOVE "S1" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE  " IN-STAT.
           STOP RUN.
