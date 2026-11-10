      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-3
      *> rules: [1985-GR-XIV.3.6.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR3 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.6.4 general rule 3: the text length is the
      * number of leftmost positions of identifier-1 sent; zero sends
      * no character; outside zero through the size is an error and no
      * data is transferred.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDGR3.
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
       01  MSG PIC X(10) VALUE "HELLOWORLD".
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
           MOVE 3 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "LENGTH 03 " OUT-STAT.
           PERFORM RCV-MSG.
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG.
           MOVE 0 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "LENGTH 05 THEN 00".
           PERFORM RCV-MSG.
           MOVE 10 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "LENGTH 10 " OUT-STAT.
           PERFORM RCV-MSG.
           MOVE 11 TO OUT-LEN.
           SEND COUT FROM MSG.
           DISPLAY "LENGTH 11 " OUT-STAT.
           MOVE 2 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "LENGTH 02 " OUT-STAT.
           PERFORM RCV-MSG.
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
