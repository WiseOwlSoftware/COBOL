      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-8
      *> rules: [2002-GR-A.1.2.3-8, 2002-GR-A.1.2.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR8 - RECEIVE provides in data-name-5 (MESSAGE
      *> DATE) "the date on which it recognized that the message was
      *> complete in the form 'YYMMDD'"; §2002:A.1.2.3 GR9 - and in
      *> data-name-6 (MESSAGE TIME) "the time at which it recognized
      *> that the message was complete in the form 'HHMMSSTT'". Each
      *> item "is not updated by the MCS other than as part of the
      *> execution of a RECEIVE statement."
      *> The two messages are defined to the MCS (mcs.json) with the
      *> date and time it recognized each complete, so the values are
      *> fixed inputs, never the clock. Legs: each RECEIVE sets both
      *> items from its message (fails if they stay zero or take the
      *> run's clock); ACCEPT MESSAGE COUNT, DISABLE INPUT and ENABLE
      *> INPUT leave the program's 111111/11111111 alone (fails if any
      *> other statement updates them). Column 2 is the status key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD08.
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
           MOVE """date"":""021231"",""time"":""23595999""},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """segments"":[""TWO""],""date"":""030101"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """time"":""00000001""}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE ZERO TO IN-DATE IN-TIME.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IN-STAT " " IN-DATE " " IN-TIME.
           MOVE 111111 TO IN-DATE.
           MOVE 11111111 TO IN-TIME.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "ACCEPT  " IN-STAT " " IN-DATE " " IN-TIME.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE " IN-STAT " " IN-DATE " " IN-TIME.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE  " IN-STAT " " IN-DATE " " IN-TIME.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVE " IN-STAT " " IN-DATE " " IN-TIME.
           STOP RUN.
