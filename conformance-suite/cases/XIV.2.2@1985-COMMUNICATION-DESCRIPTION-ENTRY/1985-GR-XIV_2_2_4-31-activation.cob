      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-31-activation
      *> rules: [1985-GR-XIV.2.2.4-31]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR31: "After executing the first statement
      *> referencing cd-name-1, the content of the data item referenced by
      *> data-name-3 must not be changed by the program. If this content
      *> is changed, the execution of any statement referencing cd-name-1
      *> is unsuccessful" (status 20 for a SEND, Table 1, §1985:XIV.2.2.5).
      *> The rule binds the program in its activation: a program that is
      *> cancelled "ceases to have any logical relationship to the run
      *> unit" (§1985:X.5.3.4 GR2) and the next CALL finds it in its
      *> initial state with no statement having referenced the CD, as
      *> does each CALL of a program with the INITIAL attribute. The
      *> content, not the name: "t2" is not the content "T2".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR31ACT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  UPPER-2 PIC XX VALUE "T2".
       01  UPPER-3 PIC XX VALUE "T3".
       01  LOWER-2 PIC XX VALUE "t2".
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""terminals"":[{""name"":""T1""},{""name"":""T2""},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""T3""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           CALL "SUBC" USING UPPER-2.
           CALL "SUBC" USING UPPER-3.
           CALL "SUBC" USING LOWER-2.
           CALL "SUBC" USING UPPER-2.
           CANCEL "SUBC".
           CALL "SUBC" USING UPPER-3.
           CALL "SUBI" USING UPPER-2.
           CALL "SUBI" USING UPPER-3.
           STOP RUN.
       END PROGRAM GR31ACT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       LINKAGE SECTION.
       01  TERM-NAME PIC XX.
       COMMUNICATION SECTION.
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       PROCEDURE DIVISION USING TERM-NAME.
       SEND-PARA.
           MOVE TERM-NAME TO IO-TERM.
           MOVE 5 TO IO-LEN.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SUBC " TERM-NAME " " IO-STAT.
           EXIT PROGRAM.
       END PROGRAM SUBC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBI IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       LINKAGE SECTION.
       01  TERM-NAME PIC XX.
       COMMUNICATION SECTION.
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       PROCEDURE DIVISION USING TERM-NAME.
       SEND-PARA.
           MOVE TERM-NAME TO IO-TERM.
           MOVE 5 TO IO-LEN.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SUBI " TERM-NAME " " IO-STAT.
           EXIT PROGRAM.
       END PROGRAM SUBI.
