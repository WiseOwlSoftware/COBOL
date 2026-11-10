      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-4
      *> rules: [1985-GR-XIV.2.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR4 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 4: the queue and sub-queue
      * items hold symbolic names that are defined to the MCS.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR4.
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
           MOVE "{""queues"":[{""name"":""PAYQ"",""subQueues"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[{""name"":""HOURLY"",""messages"":[""A"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """B""]},{""name"":""SALARY"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """C""]}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "PAYQ" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "HOURLY" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "PAYQ HOURLY " IN-STAT " " IN-CNT.
           MOVE "SALARY" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "PAYQ SALARY " IN-STAT " " IN-CNT.
           MOVE "BONUS" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "PAYQ BONUS  " IN-STAT.
           MOVE "NOQ" TO IN-Q.
           MOVE SPACES TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "NOQ         " IN-STAT.
           STOP RUN.
