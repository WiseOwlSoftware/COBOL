      *>--- conformance-case
      *> id: pb5286_enable_input_reopens_source_path
      *> rules: [1985-GR-XIV.3.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.3.4 GR2 (THE ENABLE STATEMENT). An output test.
      * X3.23-1985 XIV 3.3.4 general rule 2: ENABLE INPUT of a queue
      * activates the logical paths between the queue and its sources,
      * including a path DISABLE INPUT TERMINAL closed. The source stays
      * closed to the queue not named.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENAQPA.
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
       01  KEYX PIC X(10) VALUE SPACES.
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
           MOVE "{""queues"":[{""name"":""Q1""},{""name"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """Q2""}],""sources"":[{""name"":""T1""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""Q1"",""source"":""T1""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D2"",""deliverTo"":""Q2"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """source"":""T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO IN-Q IN-S1 IN-S2 IN-S3 IN-SRC.
           MOVE "T1" TO IN-SRC.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DISABLE T1 " IN-STAT.
           MOVE "Q1" TO IN-Q.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE Q1  " IN-STAT.
           MOVE 2 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           MOVE "Q1" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1      " IN-CNT.
           MOVE "Q2" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q2      " IN-CNT.
           STOP RUN.
