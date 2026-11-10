      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-8
      *> rules: [1985-GR-XIV.2.2.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR8 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 8: the input CD's IN-DATE:
      * each RECEIVE sets it; ACCEPT, DISABLE and ENABLE do not.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR8.
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
           MOVE "{""queues"":[{""name"":""Q1"",""enabled"":true"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE ",""messages"":[{""segments"":[""ONE""],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """date"":""851231"",""time"":""23595999""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """segments"":[""TWO""],""date"":""860101"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """time"":""00000001""}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE ZERO TO IN-DATE.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IN-STAT " " IN-DATE.
           MOVE 111111 TO IN-DATE.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "ACCEPT  " IN-STAT " " IN-DATE.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE " IN-STAT " " IN-DATE.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE  " IN-STAT " " IN-DATE.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IN-STAT " " IN-DATE.
           STOP RUN.
