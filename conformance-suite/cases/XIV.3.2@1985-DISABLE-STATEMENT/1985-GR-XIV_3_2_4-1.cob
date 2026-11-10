      *>--- conformance-case
      *> id: 1985-GR-XIV_3_2_4-1
      *> rules: [1985-GR-XIV.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR1 (THE DISABLE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.2.4 general rule 1: DISABLE disconnects; a
      * second DISABLE only sets the status key; SEND still reaches
      * the MCS.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISGR1.
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
           MOVE "{""queues"":[{""name"":""Q1""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"",""enabled"""
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE ":true,""deliverTo"":""Q1""}]}"
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
           DISABLE OUTPUT COUT.
           DISPLAY "DISABLE " OUT-STAT.
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           DISABLE OUTPUT COUT.
           DISPLAY "DISABLE " OUT-STAT " " OUT-ERR (1).
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND    " OUT-STAT " " OUT-ERR (1).
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "COUNT   " IN-CNT.
           ENABLE OUTPUT COUT.
           DISPLAY "ENABLE  " OUT-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "COUNT   " IN-CNT.
           STOP RUN.
