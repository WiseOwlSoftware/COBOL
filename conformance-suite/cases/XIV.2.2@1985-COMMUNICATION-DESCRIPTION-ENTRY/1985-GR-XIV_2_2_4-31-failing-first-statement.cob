      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-31-failing-first-statement
      *> rules: [1985-GR-XIV.2.2.4-31]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR31 (THE COMMUNICATION DESCRIPTION ENTRY): "After
      *> executing the first statement referencing cd-name-1, the content of
      *> the data item referenced by data-name-3 must not be changed by the
      *> program. If this content is changed, the execution of any statement
      *> referencing cd-name-1 is unsuccessful". The first statement of each
      *> CD fails (SEND: text length 9 of a 5-character item, status 50;
      *> RECEIVE: a terminal the MCS does not know, status 21), and it still
      *> fixes the terminal: after SYMBOLIC TERMINAL changes, a SEND is
      *> unsuccessful (status 20, nothing sent), an ENABLE I-O TERMINAL too
      *> (21), and a RECEIVE of the second CD too (21), though T1 is defined.
      *> Statuses: Table 1 (§1985:XIV.2.2.5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR31FAIL.
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
       01  BUF PIC X(5).
       COMMUNICATION SECTION.
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       CD  IOCD2 FOR I-O
           MESSAGE DATE IS IO2-DATE
           MESSAGE TIME IS IO2-TIME
           SYMBOLIC TERMINAL IS IO2-TERM
           TEXT LENGTH IS IO2-LEN
           END KEY IS IO2-END
           STATUS KEY IS IO2-STAT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""terminals"":[{""name"":""T1""},{""name"":""T2""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE 9 TO IO-LEN.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "FIRST " IO-STAT.
           MOVE "T2" TO IO-TERM.
           MOVE 5 TO IO-LEN.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SECOND " IO-STAT.
           ENABLE I-O TERMINAL IOCD.
           DISPLAY "ENABLE " IO-STAT.
           MOVE "NOWHERE" TO IO2-TERM.
           RECEIVE IOCD2 MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE 1 " IO2-STAT.
           MOVE "T1" TO IO2-TERM.
           RECEIVE IOCD2 MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE 2 " IO2-STAT.
           STOP RUN.
