      *>--- conformance-case
      *> id: g2002-disable-portion-survives
      *> rules: [2002-GR-A.2.2.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.3 GR1 - "The logical path for the transfer of
      *> data between the COBOL programs and the MCS is not affected by
      *> the DISABLE statement." The program releases HELLO, a portion
      *> (format 1, §2002:A.2.6.3 GR6), disables D1 (which delivers to
      *> Q1), then releases WORLD with EMI, the remaining portion
      *> (§2002:A.2.6.3 GR8), and enables D1 again. The partly released
      *> message survives the DISABLE: Q1 holds one message, HELLOWORLD,
      *> end of message (END KEY 2, TEXT LENGTH 10: §2002:A.1.2.3 GR12a,
      *> GR11). A message cut at the DISABLE (HELLO alone, two messages,
      *> or none) fails a line. The SEND status after DISABLE is not
      *> displayed. This case does not witness §2002:A.2.2.3 GR2: no
      *> transmission to or from a terminal is in progress at the
      *> DISABLE (GR6 holds the portion until the message is complete),
      *> and the disconnection timing is not observed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  PART1 PIC X(5) VALUE "HELLO".
       01  PART2 PIC X(5) VALUE "WORLD".
       01  BUF PIC X(20).
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
           MOVE 5 TO OUT-LEN.
           MOVE "XX" TO OUT-STAT.
           SEND COUT FROM PART1.
           DISPLAY "SEND PART   " OUT-STAT.
           MOVE "XX" TO OUT-STAT.
           DISABLE OUTPUT COUT.
           DISPLAY "DIS D1      " OUT-STAT.
           SEND COUT FROM PART2 WITH EMI.
           MOVE "XX" TO OUT-STAT.
           ENABLE OUTPUT COUT.
           DISPLAY "ENA D1      " OUT-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 COUNT    " IN-CNT.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "RCV " BUF " " IN-END " " IN-LEN.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1 COUNT    " IN-CNT.
           STOP RUN.
