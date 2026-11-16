      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-32
      *> rules: [2002-GR-A.1.2.3-32]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR32 - data-name-4 (TEXT LENGTH) of an
      *> input-output CD holds "the number of alphanumeric character
      *> positions filled as a result of the execution of the RECEIVE
      *> statement", and on SEND the MCS reads it as "the number of
      *> leftmost alphanumeric character positions of the data item
      *> referenced by the associated SEND identifier from which data
      *> is transferred". §2002:A.2.6.3 GR3: a text length beyond the
      *> identifier's size is "an error is indicated by the value of
      *> the status key data item of the area referenced by cd-name-1,
      *> and no data is transferred", status 50 (§2002:A.1.2.4 Table
      *> A.1, "Text length exceeds size of identifier-1").
      *> Legs: RECEIVE of HELLO into X(10) gives 0005 (fails if the
      *> area's size or zero is stored); SEND of X(10) with 3 sends
      *> ABC (fails if the whole area goes), 10 is 00 and 11 is 50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD32.
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
