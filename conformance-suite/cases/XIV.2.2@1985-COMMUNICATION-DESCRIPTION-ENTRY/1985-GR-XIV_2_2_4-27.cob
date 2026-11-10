      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-27
      *> rules: [1985-GR-XIV.2.2.4-27]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR27 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 27: the input-output CD's
      * IO-TIME: the RECEIVE sets it; SEND, PURGE, DISABLE, ENABLE do
      * not.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR27.
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
           MOVE "true,""messages"":[{""segments"":[""HELLO""],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """date"":""851231"",""time"":""23595999""}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE ZERO TO IO-TIME.
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IO-STAT " " IO-TIME.
           MOVE 11111111 TO IO-TIME.
           MOVE 5 TO IO-LEN.
           SEND CIO FROM BUF WITH EMI.
           DISPLAY "SEND    " IO-STAT " " IO-TIME.
           SEND CIO FROM BUF.
           PURGE CIO.
           DISPLAY "PURGE   " IO-STAT " " IO-TIME.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " IO-STAT " " IO-TIME.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT " " IO-TIME.
           STOP RUN.
