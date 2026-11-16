      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-34
      *> rules: [2002-GR-A.1.2.3-34]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR34 - the STATUS KEY (data-name-6) of an
      *> input-output CD holds the status of the previously executed
      *> ENABLE, RECEIVE, SEND, PURGE and DISABLE, coded per table A.1
      *> (§2002:A.1.2.4). Each statement is preceded by MOVE "XX" to
      *> the status key, so a statement that failed to set it shows
      *> XX; a wrong table A.1 association shows another code.
      *> Expected (table A.1, column per statement):
      *>   ENABLE I-O TERMINAL of a disabled terminal ...... 00
      *>   ENABLE I-O TERMINAL again (already enabled) ..... 15
      *>   RECEIVE of the seeded message ................... 00
      *>   SEND ... WITH EMI ................................ 00
      *>   PURGE of the partial message of a plain SEND ..... 00
      *>   DISABLE I-O TERMINAL ............................. 00
      *>   SEND input-output-cd after DISABLE ............... 10
      *>     ("Destination disabled. No action taken.")
      *> The program first defines its terminal to the message
      *> control system (the file mcs.json of the MCS directory):
      *> one terminal T1, starting disabled, holding message HELLO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD34.
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
           DISPLAY "RECEIVE " IO-STAT " " BUF.
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
