      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-9
      *> rules: [1985-GR-XIV.3.6.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR9 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.6.4 general rule 9: identifier-2 is 1 (ESI),
      * 2 (EMI), 3 (EGI); any other character is 0, no indicator;
      * without identifier-1 only the indicator is sent, and then a
      * value other than 1, 2 or 3 is an error and nothing is
      * transferred.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDGR9.
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
       01  T-A PIC X(2) VALUE "AA".
       01  T-B PIC X(2) VALUE "BB".
       01  T-C PIC X(2) VALUE "CC".
       01  T-D PIC X(2) VALUE "DD".
       01  T-E PIC X(2) VALUE "EE".
       01  T-F PIC X(2) VALUE "FF".
       01  T-G PIC X(2) VALUE "GG".
       01  T-H PIC X(2) VALUE "HH".
       01  IND PIC 9.
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
           MOVE 1 TO IND.
           SEND COUT FROM T-A WITH IND.
           MOVE 2 TO IND.
           SEND COUT FROM T-B WITH IND.
           MOVE 3 TO IND.
           SEND COUT FROM T-C WITH IND.
           MOVE 0 TO IND.
           SEND COUT FROM T-D WITH IND.
           MOVE 2 TO IND.
           SEND COUT FROM T-E WITH IND.
           MOVE 7 TO IND.
           SEND COUT FROM T-F WITH IND.
           MOVE 2 TO IND.
           SEND COUT FROM T-G WITH IND.
           SEND COUT FROM T-H.
           SEND COUT WITH IND.
           DISPLAY "INDICATOR ONLY " OUT-STAT.
           PERFORM RCV-SEG 6 TIMES.
           MOVE 0 TO IND.
           SEND COUT WITH IND.
           DISPLAY "NO IDENTIFIER-1, 0 " OUT-STAT.
           MOVE 7 TO IND.
           SEND COUT WITH IND.
           DISPLAY "NO IDENTIFIER-1, 7 " OUT-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "MESSAGES " IN-CNT.
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
