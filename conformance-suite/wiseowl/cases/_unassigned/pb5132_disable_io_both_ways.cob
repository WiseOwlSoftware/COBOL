      *>--- conformance-case
      *> id: pb5132_disable_io_both_ways
      *> rules: []
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5132 item 3 - the DISABLE sibling of PB5107's ENABLE test.
      *> §1985:XIV.3.2.4 GR5: "When the I-O TERMINAL phrase is specified, the
      *> logical path between the source (as defined by the content of the data
      *> item referenced by data-name-3 (SYMBOLIC TERMINAL)) and the program is
      *> deactivated." So after DISABLE I-O TERMINAL a SEND takes no action
      *> (status 10) and a RECEIVE is given no message, though the message the
      *> MCS holds for the terminal stays queued until the next ENABLE. A second
      *> DISABLE finds it already deactivated: "No action is taken when a
      *> DISABLE statement is executed which specifies a source or
      *> destination which is already disconnected" (§1985:XIV.3.2.4 GR1),
      *> and the status key says so, 15 (the status key table, XIV-16).
      *> Derivation: ENABLE gives 00; SEND gives 00;
      *> DISABLE gives 00; SEND gives 10; SEND COUT queues HELLO for T1
      *> (destination D1 delivers to it); RECEIVE has no data; the second
      *> DISABLE gives 15; ENABLE gives 00 and the RECEIVE now delivers HELLO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5132DI.
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
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT.
           SEND CIO FROM MSG WITH EMI.
           DISPLAY "SEND    " IO-STAT.
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " IO-STAT.
           MOVE 5 TO IO-LEN.
           SEND CIO FROM MSG WITH EMI.
           DISPLAY "SEND    " IO-STAT.
           SEND COUT FROM MSG WITH EMI.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "RECEIVE NO DATA"
               WITH DATA DISPLAY "RECEIVE DATA".
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " IO-STAT.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "RECEIVE NO DATA"
               WITH DATA DISPLAY "RECEIVE DATA".
           DISPLAY "[" BUF "]".
           STOP RUN.
