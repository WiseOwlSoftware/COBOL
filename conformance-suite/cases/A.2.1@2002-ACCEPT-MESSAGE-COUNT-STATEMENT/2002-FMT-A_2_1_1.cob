      *>--- conformance-case
      *> id: 2002-FMT-A_2_1_1
      *> rules: [2002-FMT-A.2.1.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.1.1 - ACCEPT cd-name-1 MESSAGE COUNT, where MESSAGE
      *> is an optional word (printed without underline on 2002 PDF
      *> page 689) and COUNT a key word: both spellings, MESSAGE
      *> COUNT and COUNT alone, are written and run.
      *> §2002:A.2.1.3 GR1 - the message count item is updated to the
      *> number of complete messages in the queue named by SYMBOLIC
      *> QUEUE; table A.1 gives status 00 for ACCEPT MESSAGE COUNT.
      *> The queue Q1 is seeded with two messages, Q2 with none.
      *> Each leg presets status XX and count 999999, so a spelling
      *> that is refused fails the compile, and a statement that does
      *> not update the count or status shows the preset.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AMC03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[""A"",""B""]},{""name"":""Q2""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE "XX" TO IN-STAT. MOVE 999999 TO IN-CNT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "MESSAGE COUNT Q1 " IN-STAT " " IN-CNT.
           MOVE "XX" TO IN-STAT. MOVE 999999 TO IN-CNT.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT Q1 " IN-STAT " " IN-CNT.
           MOVE "Q2" TO IN-Q.
           MOVE "XX" TO IN-STAT. MOVE 999999 TO IN-CNT.
           ACCEPT CIN COUNT.
           DISPLAY "COUNT Q2 " IN-STAT " " IN-CNT.
           STOP RUN.
