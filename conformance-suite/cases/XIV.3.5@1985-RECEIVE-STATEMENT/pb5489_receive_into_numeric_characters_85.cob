      *>--- conformance-case
      *> id: pb5489_receive_into_numeric_characters_85
      *> rules: [1985-GR-XIV.3.5.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.5.4 GR3: the message "is transferred to the
      * receiving character positions of the area referenced by
      * identifier-1 aligned to the left without space fill", so the
      * characters AB land in the first two positions of a numeric
      * display item holding 1234 and leave the item AB34, whether
      * identifier-1 is the item or a reference-modified slice of it
      * (kb/Work PB5489). Each leg can fail: a numeric carrier that
      * keeps only digits prints 0034 for either leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5489A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  T-A PIC X(2) VALUE "AB".
       01  K-NUM PIC 9(4) VALUE 1234.
       01  K-REF PIC 9(4) VALUE 1234.
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
           RECEIVE CIN MESSAGE INTO K-NUM
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "WHOLE [" K-NUM "]".
           SEND COUT FROM T-A WITH EMI.
           RECEIVE CIN MESSAGE INTO K-REF (1:2)
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "SLICE [" K-REF "]".
           STOP RUN.
