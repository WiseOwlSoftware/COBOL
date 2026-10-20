      *>--- conformance-case
      *> id: pb3350_open_reversed_varying_span_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3350R.
      *> kb/Work PB3350. OPEN INPUT ... REVERSED over a RECORD VARYING
      *> file of 4100 records (lengths 4 to 8). The phrase positions the
      *> file at its END and every READ makes the PRECEDING record
      *> available (kb/Work PB668, whose three-record golden pb668_open_
      *> reversed_85 derives it from 14.9.30.4 GR21 c) and e)); so the
      *> first READ makes record 4100 available, the 4100th makes
      *> record 1 available and the 4101st is the at end condition
      *> ('10', GR21 e).
      *> WiseOwl COBOL finds a frame by a checkpoint per 4096 frames and
      *> a window of the exact starts of the span being read, so this
      *> walk starts in the SECOND span (4097 to 4100) and crosses into
      *> the first at record 4096; it is also the one walk that reaches a
      *> record nothing has read forward, which is why the OPEN itself
      *> counts the frames (one pass that leaves the checkpoints).
      *>
      *> THE FILE: record n holds the 4-digit number n then "zzzz",
      *> written with the length 4 + MOD (n, 5). EXPECTED VALUES ARE
      *> COMPUTED FROM THE RULE: READ k (k = 1 to 4100) delivers record
      *> 4101 - k, so BAD counts the reads that delivered anything else.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3350r.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD IS VARYING IN SIZE FROM 4 TO 8
               DEPENDING ON WS-LEN.
       01 F-REC.
           05 F-NUM PIC 9(4).
           05 F-PAD PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-LEN PIC 9(4) COMP.
       01 WS-N PIC 9(5).
       01 WS-EXP PIC 9(5).
       01 WS-BAD PIC 9(5) VALUE 0.
       01 WS-READS PIC 9(5) VALUE 0.
       01 WS-GO PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           PERFORM VARYING WS-N FROM 1 BY 1 UNTIL WS-N > 4100
               MOVE WS-N TO F-NUM
               MOVE ALL "z" TO F-PAD
               COMPUTE WS-LEN = 4 + FUNCTION MOD (WS-N, 5)
               WRITE F-REC
           END-PERFORM
           CLOSE F
           OPEN INPUT F REVERSED
           DISPLAY "OPEN=" WS-ST
           MOVE 4100 TO WS-EXP
           PERFORM UNTIL WS-GO = "N"
               READ F
                   AT END
                       MOVE "N" TO WS-GO
                   NOT AT END
                       ADD 1 TO WS-READS
                       IF F-NUM NOT = WS-EXP
                          OR WS-LEN NOT = 4 + FUNCTION MOD (WS-EXP, 5)
                           ADD 1 TO WS-BAD
                       END-IF
                       SUBTRACT 1 FROM WS-EXP
               END-READ
           END-PERFORM
           DISPLAY "READS=" WS-READS "|BAD=" WS-BAD "|" WS-ST
           CLOSE F
           STOP RUN.
