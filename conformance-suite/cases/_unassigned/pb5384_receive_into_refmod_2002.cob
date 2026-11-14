      *>--- conformance-case
      *> id: pb5384_receive_into_refmod_2002
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5384 - ISO/IEC 1989:2002 A.2.5.2 SR2: "Identifier-1
      *> shall reference a data item of category alphanumeric." A
      *> reference modification of a numeric item is one: its unique
      *> data item is "class and category alphanumeric" unless the usage
      *> is national (8.4.2.3.3 GR6 of 2002, §2023:8.4.3.3.4 GR6). The
      *> RECEIVE below was refused because the screen read the base item
      *> K-NUM through the reference modification. Each leg can fail: a
      *> refusal fails the translation, and the message 56 lands in the
      *> two character positions the reference names, aligned left
      *> (A.2.5.3 GR3), so K-NUM holds 5634 only if it was received
      *> into the slice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5384G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  T-A PIC X(2) VALUE "56".
       01  K-NUM PIC 9(4) VALUE 1234.
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
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""HOLD""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""LOOP"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""HOLD""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "HOLD" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE 1 TO OUT-CNT.
           MOVE "LOOP" TO OUT-DEST (1).
           MOVE 2 TO OUT-LEN.
           SEND COUT FROM T-A WITH EMI.
           RECEIVE CIN MESSAGE INTO K-NUM (1:2)
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "MESSAGE " IN-LEN " " IN-END
                   " [" K-NUM "]".
           STOP RUN.
