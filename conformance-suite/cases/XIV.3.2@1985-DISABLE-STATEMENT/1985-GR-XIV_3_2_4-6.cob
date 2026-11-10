      *>--- conformance-case
      *> id: 1985-GR-XIV_3_2_4-6
      *> rules: [1985-GR-XIV.3.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR6 (THE DISABLE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.2.4 general rule 6: DISABLE OUTPUT closes the
      * destinations of the table up to the destination count.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISGR6.
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
           MOVE """enabled"":true},{""name"":""D2"",""enabled"""
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE ":true},{""name"":""D3"",""enabled"":true}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 3 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE "D3" TO OUT-DEST (3).
           MOVE 5 TO OUT-LEN.
           MOVE 2 TO OUT-CNT.
           MOVE "XX" TO OUT-STAT.
           DISABLE OUTPUT COUT.
           DISPLAY "DISABLE " OUT-STAT.
           MOVE 3 TO OUT-CNT.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND    " OUT-STAT " "
               OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           STOP RUN.
