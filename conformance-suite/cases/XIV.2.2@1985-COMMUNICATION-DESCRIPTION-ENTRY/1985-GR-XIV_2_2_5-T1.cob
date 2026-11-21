      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_5-T1
      *> rules: [1985-GR-XIV.2.2.5-T1, 1985-GR-XIV.2.2.5-p1, 1985-GR-XIV.2.2.6-T1, 1985-GR-XIV.2.2.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.5 Table 1; §1985:XIV.2.2.5 p1; §1985:XIV.2.2.6
      *> Table 1; §1985:XIV.2.2.6 p1 (THE COMMUNICATION DESCRIPTION
      *> ENTRY). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-18.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985 XIV.2.2.5 Table 1 / XIV.2.2.6 Table 2: SEND to a
      * disabled destination is status 10, error key 2 (message held);
      * past the destination's capacity (mcs.json capacity 1) it is
      * status 65, error key 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P05.
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
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """enabled"":false,""capacity"":1}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND 1 " OUT-STAT " " OUT-ERR (1).
           MOVE "XX" TO OUT-STAT.
           MOVE "X" TO OUT-ERR (1).
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "SEND 2 " OUT-STAT " " OUT-ERR (1).
           STOP RUN.
