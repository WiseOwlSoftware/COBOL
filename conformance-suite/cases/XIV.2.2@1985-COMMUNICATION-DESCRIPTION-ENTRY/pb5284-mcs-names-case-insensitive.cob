      *>--- conformance-case
      *> id: pb5284-mcs-names-case-insensitive
      *> rules: [1985-GR-XIV.2.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5284. §1985:XIV.2.2.4 GR4: the queue and sub-queue
      *> names "must follow the rules for the formation of system-names",
      *> and in those rules "Each lowercase letter is considered to be
      *> equivalent to its corresponding uppercase letter" (§1985:p.A-9,
      *> Amendment 1), so a lowercase name designates the uppercase
      *> queue the MCS holds. The MCS defines Q with sub-queues A and B
      *> and one message in each: Q alone counts both (00, 2), the
      *> sub-queue "b" counts one (00, 1) and a RECEIVE of it takes the
      *> message of B. The source name is folded the same way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5284.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DEF-FILE ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  DEF-FILE.
       01  DEF-REC PIC X(70).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT DEF-FILE.
           MOVE
           "{""queues"":[{""name"":""Q"",""subQueues"":["
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""name"":""A"",""messages"":[""MA""]},"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""name"":""B"",""messages"":[""MB""]}]}],"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           """sources"":[{""name"":""S1""}]}"
               TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE "q" TO IN-Q. MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "LOWER-Q COUNT " IN-STAT " " IN-CNT.
           MOVE "Q" TO IN-Q. MOVE "b" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "LOWER-B COUNT " IN-STAT " " IN-CNT.
           MOVE SPACES TO BUF.
           RECEIVE CIN MESSAGE INTO BUF NO DATA DISPLAY "NO DATA".
           DISPLAY "LOWER-B RECV " IN-STAT " [" BUF "]".
           MOVE "Q" TO IN-Q. MOVE SPACES TO IN-S1.
           MOVE "s1" TO IN-SRC.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DIS-TERM LOWER-SRC " IN-STAT.
           STOP RUN.
