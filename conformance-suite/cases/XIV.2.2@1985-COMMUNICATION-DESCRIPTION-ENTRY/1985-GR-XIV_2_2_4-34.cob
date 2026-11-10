      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-34
      *> rules: [1985-GR-XIV.2.2.4-34]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR34 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 34: the input-output CD's
      * status key after each of ENABLE, RECEIVE, SEND, PURGE and
      * DISABLE.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR34.
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
           MOVE "false,""messages"":[""HELLO""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE SPACES TO BUF.
           MOVE "XX" TO IO-STAT.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT.
           MOVE "XX" TO IO-STAT.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE  " IO-STAT.
           MOVE "XX" TO IO-STAT.
           RECEIVE CIO MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IO-STAT.
           MOVE 5 TO IO-LEN.
           MOVE "XX" TO IO-STAT.
           SEND CIO FROM BUF WITH EMI.
           DISPLAY "SEND    " IO-STAT.
           SEND CIO FROM BUF.
           MOVE "XX" TO IO-STAT.
           PURGE CIO.
           DISPLAY "PURGE   " IO-STAT.
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "DISABLE " IO-STAT.
           MOVE "XX" TO IO-STAT.
           SEND CIO FROM BUF WITH EMI.
           DISPLAY "SEND    " IO-STAT.
           STOP RUN.
