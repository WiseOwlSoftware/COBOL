      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-3
      *> rules: [1985-GR-XIV.2.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR3 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 3: a sub-queue item not
      * being used contains spaces; a name in it is a sub-queue asked
      * for.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR3.
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
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """ONE""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE SPACES TO BUF.
           MOVE "XX" TO IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "SPACES " IN-STAT " " IN-CNT.
           MOVE "NOSUB" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "NAMED  " IN-STAT.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "XX" TO IN-STAT.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "SPACES " IN-STAT " [" BUF "]".
           STOP RUN.
