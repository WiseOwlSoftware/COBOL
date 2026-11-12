      *>--- conformance-case
      *> id: 1985-GR-XIV_3_1_4-1-partly-received
      *> rules: [1985-GR-XIV.3.1.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.1.4 GR1 (THE ACCEPT MESSAGE COUNT STATEMENT): the count is
      *> "the number of complete messages that exist in the queue structure".
      *> The MCS removes a message from the queue only when it is complete
      *> (§1985:II.7.5) and a message partly received by the run unit is still
      *> there: STOP RUN must "eliminate from the queue any message partially
      *> received by that run unit" (§1985:VI.6.24.4 GR3). Q1 holds the
      *> messages ABCDEFGHIJ and SECND; the 5-character item takes the first
      *> in two RECEIVEs, so the count stays 2 between the portions and
      *> drops to 1 with the RECEIVE that returns the last portion.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ACC1PART.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(5).
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
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[""ABCDEFGHIJ"",""SECND""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "CNT0 " IN-STAT " " IN-CNT.
           PERFORM RCV-ONE.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "CNT1 " IN-STAT " " IN-CNT.
           PERFORM RCV-ONE.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "CNT2 " IN-STAT " " IN-CNT.
           PERFORM RCV-ONE.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "CNT3 " IN-STAT " " IN-CNT.
           STOP RUN.
       RCV-ONE.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "RCV " IN-STAT " " BUF " " IN-END
                   " " IN-LEN.
