      *>--- conformance-case
      *> id: 1985-GR-XIV_3_2_4-7
      *> rules: [1985-GR-XIV.3.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR7 (THE DISABLE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.2.4 general rule 7: DISABLE is honored only
      * with the system password, of one to ten characters; else
      * status.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISGR7.
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
       01  GOOD-KEY PIC X VALUE "K".
       01  BAD-KEY PIC X VALUE "Z".
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
           MOVE """enabled"":true,""key"":""ABCDEFGHIJ""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D2"",""enabled"":true,""key"":""K"""
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           MOVE "XX" TO OUT-STAT.
           DISABLE OUTPUT COUT WITH KEY "ABCDEFGHIX".
           DISPLAY "D1 BAD  " OUT-STAT.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "D1 SEND " OUT-STAT.
           DISABLE OUTPUT COUT WITH KEY "ABCDEFGHIJ".
           DISPLAY "D1 GOOD " OUT-STAT.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "D1 SEND " OUT-STAT " " OUT-ERR (1).
           MOVE "D2" TO OUT-DEST (1).
           DISABLE OUTPUT COUT WITH KEY BAD-KEY.
           DISPLAY "D2 BAD  " OUT-STAT.
           DISABLE OUTPUT COUT WITH KEY GOOD-KEY.
           DISPLAY "D2 GOOD " OUT-STAT.
           DISABLE OUTPUT COUT WITH KEY GOOD-KEY.
           DISPLAY "D2 GOOD " OUT-STAT.
           STOP RUN.
