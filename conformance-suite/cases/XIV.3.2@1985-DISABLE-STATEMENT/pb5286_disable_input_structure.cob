      *>--- conformance-case
      *> id: pb5286_disable_input_structure
      *> rules: [1985-GR-XIV.3.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR3 (THE DISABLE STATEMENT). An output test.
      * X3.23-1985 XIV 3.2.4 general rule 3: DISABLE INPUT of a queue
      * named with no sub-queue deactivates the logical paths between
      * the queue and its sub-queues, so a message the MCS would deliver
      * to a sub-queue is not delivered, and a DISABLE INPUT of the sub-
      * queue then finds it already disabled.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISSTR.
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
           MOVE "{""queues"":[{""name"":""Q1"",""subQueues"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[{""name"":""S1""}]}],""sources"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""T1""}],""destinations"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D1"",""deliverTo"":""Q1/S1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """source"":""T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE Q1 " IN-STAT.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 COUNT   " IN-CNT.
           MOVE "S1" TO IN-S1.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE Q1/S1 " IN-STAT.
           STOP RUN.
