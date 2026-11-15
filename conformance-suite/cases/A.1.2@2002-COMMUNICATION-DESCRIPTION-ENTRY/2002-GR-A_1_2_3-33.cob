      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-33
      *> rules: [2002-GR-A.1.2.3-33]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR33 - data-name-5 (END KEY) of an input-output
      *> CD "is set only by the MCS as part of the execution of a
      *> RECEIVE statement": with MESSAGE, 3 at an end of group, 2 at
      *> an end of message, 0 for less than a message; with SEGMENT, 3,
      *> 2, then 1 at an end of segment, 0 for less than a segment;
      *> and "the rule first satisfied in the order listed determines
      *> the content of the data item referenced by data-name-5."
      *> §2002:A.2.5.3 GR8: with MESSAGE "end of segment indicators are
      *> ignored"; a message larger than the area fills it and the
      *> remainder "is treated as a new message" (GR9 the same for a
      *> segment); an end of group implies an end of message, and an
      *> end of message or group an end of segment.
      *> Terminal T1 holds, in order (mcs.json): PQ (segments P, Q);
      *> AB, CD (two segments); EF ending a group; HELLOWORLD;
      *> ABCDEFGH, IJ (two segments); X, Y ending a group.
      *> Legs and the value each pins: SEND leaves the program's 9
      *> (set only by RECEIVE); MESSAGE over segments 2 (not 1); a
      *> first segment 1; a last segment 2 (end of message before end
      *> of segment); a group's message 3 (before 2); HELLO of
      *> HELLOWORLD 0, WORLD 2; ABCDE of a segment 0, FGH 1, IJ 2; X 1;
      *> Y ending the group 3 (before 2 and 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD33.
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
       01  BUF5 PIC X(5).
       01  ONE-CHAR PIC X VALUE "Z".
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
           MOVE "{""terminals"":[{""name"":""T1"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""segments"":[""P"",""Q""]},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""segments"":[""AB"",""CD""]},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""segments"":[""EF""],""group"":true},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """HELLOWORLD"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""segments"":[""ABCDEFGH"",""IJ""]},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""segments"":[""X"",""Y""],""group"":true}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE "9" TO IO-END.
           MOVE 1 TO IO-LEN.
           SEND CIO FROM ONE-CHAR WITH EMI.
           DISPLAY "SEND " IO-STAT " " IO-END.
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "M1 " IO-STAT " " IO-END " [" BUF "]".
           MOVE SPACES TO BUF.
           RECEIVE CIO SEGMENT INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "S1 " IO-STAT " " IO-END " [" BUF "]".
           MOVE SPACES TO BUF.
           RECEIVE CIO SEGMENT INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "S2 " IO-STAT " " IO-END " [" BUF "]".
           MOVE SPACES TO BUF.
           RECEIVE CIO MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "M3 " IO-STAT " " IO-END " [" BUF "]".
           MOVE SPACES TO BUF5.
           RECEIVE CIO MESSAGE INTO BUF5 NO DATA DISPLAY "NO DATA".
           DISPLAY "M4 " IO-STAT " " IO-END " [" BUF5 "]".
           MOVE SPACES TO BUF5.
           RECEIVE CIO MESSAGE INTO BUF5 NO DATA DISPLAY "NO DATA".
           DISPLAY "M4 " IO-STAT " " IO-END " [" BUF5 "]".
           MOVE SPACES TO BUF5.
           RECEIVE CIO SEGMENT INTO BUF5 NO DATA DISPLAY "NO DATA".
           DISPLAY "S5 " IO-STAT " " IO-END " [" BUF5 "]".
           MOVE SPACES TO BUF5.
           RECEIVE CIO SEGMENT INTO BUF5 NO DATA DISPLAY "NO DATA".
           DISPLAY "S5 " IO-STAT " " IO-END " [" BUF5 "]".
           MOVE SPACES TO BUF5.
           RECEIVE CIO SEGMENT INTO BUF5 NO DATA DISPLAY "NO DATA".
           DISPLAY "S5 " IO-STAT " " IO-END " [" BUF5 "]".
           MOVE SPACES TO BUF.
           RECEIVE CIO SEGMENT INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "S6 " IO-STAT " " IO-END " [" BUF "]".
           MOVE SPACES TO BUF.
           RECEIVE CIO SEGMENT INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "S6 " IO-STAT " " IO-END " [" BUF "]".
           STOP RUN.
