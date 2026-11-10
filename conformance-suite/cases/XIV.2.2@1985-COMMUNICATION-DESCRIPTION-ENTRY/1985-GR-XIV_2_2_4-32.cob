      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-32
      *> rules: [1985-GR-XIV.2.2.4-32]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR32 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 32: the input-output CD's
      * text length: the RECEIVE's count, and the SEND's leftmost
      * positions.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR32.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT DEVICE ASSIGN TO "T1.out".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       FD  DEVICE.
       01  DEV-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       01  OUT-AREA PIC X(10) VALUE "ABCDEFGHIJ".
       COMMUNICATION SECTION.
       CD  CIO FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""terminals"":[{""name"":""T1"",""enabled"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "true,""messages"":[""HELLO""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE ZERO TO IO-LEN.
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IO-STAT " " IO-LEN " [" BUF "]".
           MOVE 3 TO IO-LEN.
           SEND CIO FROM OUT-AREA WITH EMI.
           DISPLAY "SEND 03 " IO-STAT.
           MOVE 10 TO IO-LEN.
           SEND CIO FROM OUT-AREA WITH EMI.
           DISPLAY "SEND 10 " IO-STAT.
           MOVE 11 TO IO-LEN.
           SEND CIO FROM OUT-AREA WITH EMI.
           DISPLAY "SEND 11 " IO-STAT.
           OPEN INPUT DEVICE.
           READ DEVICE AT END DISPLAY "DEVICE EMPTY".
           DISPLAY "DEVICE [" DEV-REC "]".
           CLOSE DEVICE.
           STOP RUN.
