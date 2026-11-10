      *>--- conformance-case
      *> id: pb5107_enable_io_both_ways
      *> rules: []
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5107 - §1985:XIV.3.3.4 GR4: ENABLE I-O TERMINAL activates the
      *> logical path between the terminal and the program, both ways. Until then
      *> a SEND takes no action (status 10) and a RECEIVE is given no message: the
      *> message the MCS holds for the terminal stays queued for the ENABLE.
      *> The program first defines its terminal and destination to this
      *> implementation's message control system (the file mcs.json).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5107EN.
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
       01  BUF PIC X(10).
       COMMUNICATION SECTION.
       CD  CIO FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
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
           MOVE "{""terminals"":[{""name"":""T1"",""enabled"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "false}],""destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """enabled"":true,""deliverTo"":""terminal:T1"""
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE 5 TO IO-LEN.
           MOVE SPACES TO BUF.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           SEND CIO FROM MSG WITH EMI.
           DISPLAY "SEND    " IO-STAT.
           SEND COUT FROM MSG WITH EMI.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "RECEIVE NO DATA"
               WITH DATA DISPLAY "RECEIVE DATA".
           MOVE "XX" TO IO-STAT.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT.
           MOVE 5 TO IO-LEN.
           SEND CIO FROM MSG WITH EMI.
           DISPLAY "SEND    " IO-STAT.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "RECEIVE NO DATA"
               WITH DATA DISPLAY "RECEIVE DATA".
           DISPLAY "[" BUF "]".
           MOVE "XX" TO IO-STAT.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT.
           STOP RUN.
