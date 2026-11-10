      *>--- conformance-case
      *> id: 1985-GR-XIV_3_4_4-3
      *> rules: [1985-GR-XIV.3.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.4.4 GR3 (THE PURGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.4.4 general rule 3: PURGE updates the status
      * key and the error key of the output CD's area.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PURGR3.
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
           MOVE "{""destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """enabled"":true}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG.
           MOVE "XX" TO OUT-STAT.
           PURGE COUT.
           DISPLAY "PARTIAL " OUT-STAT.
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           PURGE COUT.
           DISPLAY "NONE    " OUT-STAT " " OUT-ERR (1).
           MOVE "NOPE" TO OUT-DEST (1).
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           PURGE COUT.
           DISPLAY "UNKNOWN " OUT-STAT " " OUT-ERR (1).
           MOVE ZERO TO OUT-CNT.
           MOVE "XX" TO OUT-STAT.
           PURGE COUT.
           DISPLAY "COUNT   " OUT-STAT.
           STOP RUN.
