      *>--- conformance-case
      *> id: pb5278_receive_send_any_category_85
      *> rules: [1985-SR-XIV.3.5.3-1, 1985-SR-XIV.3.6.3-6,
      *>     1985-GR-XIV.3.5.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.5.3 has one syntax rule, SR1 on cd-name-1,
      * and none on RECEIVE's identifier-1; XIV 3.6.3 restricts
      * SEND's identifier-1 only when it is a function-identifier
      * (SR6). So at 85 a numeric, numeric edited or alphabetic item
      * is legal after SEND FROM and RECEIVE INTO (the category
      * rules are ISO 2002's A.2.5.2 SR2 and A.2.6.2 SR2; kb/Work
      * PB5278). GR3 moves the message "to the receiving character
      * positions ... aligned to the left without space fill".
      * Each leg can fail: a refusal fails the translation; NUM2
      * shows 12345 only if the numeric SEND sent its five digits;
      * NUM3 shows 12999 only if the two characters land left and
      * the rest of NUM3 is kept; the edited leg shows "  7" then
      * the kept asterisks; the alphabetic leg WXYZ then two kept
      * spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5278A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       01  NUM PIC 9(5) VALUE 12345.
       01  NUM2 PIC 9(5) VALUE 0.
       01  NUM3 PIC 9(5) VALUE 99999.
       01  ED PIC ZZ9.
       01  AL PIC A(4) VALUE "WXYZ".
       01  AL2 PIC A(6) VALUE SPACES.
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
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM NUM WITH EMI.
           RECEIVE CIN MESSAGE INTO NUM2
               NO DATA DISPLAY "NO DATA".
           DISPLAY "NUMERIC " OUT-STAT " " IN-LEN " " NUM2.
           MOVE 2 TO OUT-LEN.
           SEND COUT FROM NUM WITH EMI.
           RECEIVE CIN MESSAGE INTO NUM3
               NO DATA DISPLAY "NO DATA".
           DISPLAY "SHORT " IN-LEN " " NUM3.
           MOVE 7 TO ED.
           MOVE 3 TO OUT-LEN.
           SEND COUT FROM ED WITH EMI.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "EDITED [" BUF "]".
           MOVE 4 TO OUT-LEN.
           SEND COUT FROM AL WITH EMI.
           RECEIVE CIN MESSAGE INTO AL2
               NO DATA DISPLAY "NO DATA".
           DISPLAY "ALPHABETIC [" AL2 "]".
           STOP RUN.
