      *>--- conformance-case
      *> id: pb5315_disable_io_terminal_held_message
      *> rules: []
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5315 - §1985:XIV.3.2.4 GR1: "The logical path for the transfer of
      *> data between the COBOL programs and the MCS is not affected by the DISABLE
      *> statement", so after DISABLE I-O TERMINAL (GR5) a RECEIVE still takes a
      *> message the MCS already holds from the terminal. DISABLE acts at the
      *> terminal: a message that reaches the terminal's input after the DISABLE
      *> stays in the MCS until ENABLE I-O TERMINAL (§1985:XIV.3.3.4 GR4).
      *> The program first defines its terminal and destination to this
      *> implementation's message control system (the file mcs.json).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5315DI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "LATER".
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
           MOVE "{""terminals"":[{""name"":""T1"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """HELD1""]}],""destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""terminal:T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " IO-STAT.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IO-STAT " [" BUF "]".
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IO-STAT " [" BUF "]".
           ENABLE I-O TERMINAL CIO.
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IO-STAT " [" BUF "]".
           STOP RUN.
