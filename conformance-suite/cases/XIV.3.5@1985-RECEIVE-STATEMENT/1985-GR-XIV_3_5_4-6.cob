      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-6
      *> rules: [1985-GR-XIV.3.5.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR6 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.5.4 general rule 6: the data items of the CD
      * are updated by the MCS at each execution of a RECEIVE
      * statement.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVGR6.
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
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """segments"":[""FIRST""],""source"":""T1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """date"":""851231"",""time"":""23595999""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """segments"":[""SECOND!""],""group"":true,"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """source"":""T2"",""date"":""860101"",""time"""
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE ":""00000100""}]}],""sources"":[{""name"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """T1""},{""name"":""T2""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE SPACES TO IN-SRC.
           MOVE ZERO TO IN-DATE IN-TIME IN-LEN.
           MOVE "X" TO IN-END.
           MOVE "XX" TO IN-STAT.
           PERFORM RCV-CD 2 TIMES.
           MOVE "NOQUEUE" TO IN-Q.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "UNKNOWN QUEUE " IN-STAT.
           STOP RUN.
       RCV-CD.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY IN-DATE " " IN-TIME " [" IN-SRC "] " IN-LEN " "
               IN-END " " IN-STAT.
