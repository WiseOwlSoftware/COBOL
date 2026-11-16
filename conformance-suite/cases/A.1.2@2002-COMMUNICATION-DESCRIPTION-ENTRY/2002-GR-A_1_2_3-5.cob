      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-5
      *> rules: [2002-GR-A.1.2.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR5 - "A RECEIVE statement causes the serial
      *> return of the next message or portion of a message from the
      *> queue as specified by the entries in the CD." With only the
      *> queue named, "the MCS determines the next message or portion
      *> of a message to be accessed within the queue and/or sub-queue
      *> specified in the input CD", and afterwards "the contents of
      *> the data items referenced by data-name-1 through data-name-4
      *> will contain the symbolic names of all the levels of the
      *> queue structure."
      *> The program defines queue A with one sub-queue B holding
      *> FIRST then SECOND (the file mcs.json of the MCS directory).
      *> Legs: (1) A with spaces below: FIRST, and IN-S1 becomes B
      *> while IN-S2 stays spaces (A/B has two levels) - fails if the
      *> MCS needs every level, or leaves IN-S1 unset; (2) the same
      *> again: SECOND, the serial order - fails on a re-read or a
      *> reversed order; (3) A/B named in full on the empty sub-queue:
      *> NO DATA, the levels still named - fails if a full name is
      *> refused or data is invented. The facility is obsolete at 2002
      *> (§2002:G) and draws warning COBOL0903.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD05.
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
           MOVE "{""queues"":[{""name"":""A"",""subQueues"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""B"",""messages"":[""FIRST"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """SECOND""]}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "A" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE SPACES TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "1 " IN-STAT " [" BUF "] [" IN-Q "] [" IN-S1
               "] [" IN-S2 "]".
           MOVE SPACES TO IN-S1 BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "2 " IN-STAT " [" BUF "] [" IN-Q "] [" IN-S1 "]".
           MOVE "A" TO IN-Q.
           MOVE "B" TO IN-S1.
           MOVE SPACES TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "3 [" IN-Q "] [" IN-S1 "]".
           STOP RUN.
