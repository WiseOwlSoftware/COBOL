      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-6
      *> rules: [1985-GR-XIV.3.6.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR6 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.6.4 general rule 6: format 1 releases a
      * portion; format 2 never more than one segment or one message;
      * and no portion of a message reaches a device until the entire
      * message has been released.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDGR6.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS LF-CH IS 11 CR-CH IS 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT DEVICE ASSIGN TO "DEV.out".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       FD  DEVICE.
       01  DEV-CH PIC X.
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       01  T-A PIC X(2) VALUE "AB".
       01  T-B PIC X(2) VALUE "CD".
       01  T-C PIC X(2) VALUE "EF".
       01  T-X PIC X(2) VALUE "XX".
       01  T-Y PIC X(2) VALUE "YY".
       01  T-Z PIC X(2) VALUE "ZZ".
       01  DEV-LINE PIC X(10).
       01  DEV-POS PIC 99.
       01  DEV-EOF PIC X.
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
           MOVE "{""queues"":[{""name"":""HOLD""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""LOOP"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""HOLD""},{""name"":""DEV""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "HOLD" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE 1 TO OUT-CNT.
           MOVE "LOOP" TO OUT-DEST (1).
           MOVE 2 TO OUT-LEN.
           SEND COUT FROM T-A.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "FORMAT 1 " OUT-STAT " " IN-CNT.
           SEND COUT FROM T-B WITH ESI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "WITH ESI " OUT-STAT " " IN-CNT.
           SEND COUT FROM T-C WITH EMI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "WITH EMI " OUT-STAT " " IN-CNT.
           PERFORM RCV-SEG 2 TIMES.
           MOVE "DEV" TO OUT-DEST (1).
           SEND COUT FROM T-X WITH EMI.
           SEND COUT FROM T-Y WITH ESI.
           DISPLAY "SEGMENT RELEASED " OUT-STAT.
           PERFORM SHOW-DEVICE.
           SEND COUT FROM T-Z WITH EMI.
           DISPLAY "MESSAGE RELEASED " OUT-STAT.
           PERFORM SHOW-DEVICE.
           STOP RUN.
       RCV-MSG.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "MESSAGE " IN-LEN " " IN-END
                   " [" BUF "]".
       RCV-SEG.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN SEGMENT INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "SEGMENT " IN-LEN " " IN-END
                   " [" BUF "]".
       SHOW-DEVICE.
           OPEN INPUT DEVICE.
           MOVE SPACES TO DEV-LINE.
           MOVE 0 TO DEV-POS.
           MOVE "N" TO DEV-EOF.
           PERFORM SHOW-CHAR UNTIL DEV-EOF = "Y".
           CLOSE DEVICE.
       SHOW-CHAR.
           READ DEVICE AT END MOVE "Y" TO DEV-EOF.
           IF DEV-EOF = "N"
               IF DEV-CH = LF-CH
                   DISPLAY "DEVICE " DEV-POS " [" DEV-LINE "]"
                   MOVE SPACES TO DEV-LINE
                   MOVE 0 TO DEV-POS
               ELSE
                   IF DEV-CH NOT = CR-CH
                       ADD 1 TO DEV-POS
                       MOVE DEV-CH TO DEV-LINE (DEV-POS:1).
