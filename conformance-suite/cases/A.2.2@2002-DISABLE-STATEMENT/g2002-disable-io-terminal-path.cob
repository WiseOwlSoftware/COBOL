      *>--- conformance-case
      *> id: g2002-disable-io-terminal-path
      *> rules: [2002-GR-A.2.2.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.3 GR5 - DISABLE I-O TERMINAL deactivates "the
      *> logical path between the source (as defined by the content of
      *> the data item referenced by data-name-3 (SYMBOLIC TERMINAL))
      *> and the program". Table A.1 (§2002:A.1.2.4): DISABLE I-O
      *> TERMINAL 00, 15 already disabled, 21 unknown; SEND
      *> input-output-cd 10 "Destination disabled. No action taken."
      *> DESK1 is a terminal of
      *> this MCS; a SEND to it while disabled is 10, and after ENABLE
      *> I-O TERMINAL (§2002:A.2.3.3 GR4) 00. NOTERM is unknown: 21. A
      *> path left open (SEND 00 while disabled), a second DISABLE that
      *> reports 00, or 00 for an unknown terminal fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS05.
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
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       CD  IOX FOR I-O
           MESSAGE DATE IS IX-DATE
           MESSAGE TIME IS IX-TIME
           SYMBOLIC TERMINAL IS IX-TERM
           TEXT LENGTH IS IX-LEN
           END KEY IS IX-END
           STATUS KEY IS IX-STAT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""terminals"":[{""name"":""DESK1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "DESK1" TO IO-TERM.
           MOVE "NOTERM" TO IX-TERM.
           MOVE 5 TO IO-LEN.
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL IOCD.
           DISPLAY "DIS DESK1     " IO-STAT.
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL IOCD.
           DISPLAY "DIS DESK1 AGN " IO-STAT.
           MOVE "XX" TO IO-STAT.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SEND DISABLED " IO-STAT.
           MOVE "XX" TO IO-STAT.
           ENABLE I-O TERMINAL IOCD.
           DISPLAY "ENA DESK1     " IO-STAT.
           MOVE "XX" TO IO-STAT.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SEND ENABLED  " IO-STAT.
           MOVE "XX" TO IX-STAT.
           DISABLE I-O TERMINAL IOX.
           DISPLAY "DIS NOTERM    " IX-STAT.
           STOP RUN.
