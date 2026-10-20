*>--- conformance-case
*> id: pb3350_read_previous_varying_span
*> rules: [GR-14.9.30.4-19, GR-14.9.30.4-21]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3350 - READ ... PREVIOUS over a RECORD VARYING sequential file of 4100 records, whose
      *> frames are NOT uniformly wide (lengths 4 to 8). WiseOwl COBOL finds a frame by a checkpoint per
      *> 4096 frames and a window of the exact starts of the span being read, so 4100 records put the file's
      *> last four records in the SECOND span and every backward walk below crosses the span boundary
      *> (ordinal 4097 is the first record of span 2). The rule does not know of any span: it is the same
      *> record number as for a three-record file.
      *>
      *> THE RULES, and every expected value derived from them (no observation):
      *>  14.9.30.4 GR19 - "An implicit or explicit NEXT phrase or a PREVIOUS phrase results in a
      *>    sequential read".
      *>  14.9.30.4 GR21 c) - "If the file position indicator was established by a prior successful READ
      *>    statement, the first existing record in the physical file whose relative key number is greater
      *>    than the file position indicator if NEXT is specified or is less than the file position
      *>    indicator if PREVIOUS is specified" is made available. After the 4100th READ NEXT the indicator
      *>    is 4100, so the first PREVIOUS makes record 4099 available, then 4098 and so on to 1.
      *>  14.9.30.4 GR21 e) - "If no record is found that satisfies the above rules, the at end condition
      *>    exists": the PREVIOUS after record 1 is the at end condition, not a failure.
      *>  13.18.43 GR15 restores each record's length into the DEPENDING item (4 + MOD (n, 5) here).
      *>
      *> THE FILE: record n holds the 4-digit number n then "zzzz", written with the length
      *> 4 + MOD (n, 5), so record n's 4 digits are always whole and its tail is cut at 0 to 4 characters.
      *> WHY EACH LEG CAN FAIL: BACK counts the PREVIOUS reads that delivered the expected record (the
      *> number and the length); a frame index that mis-numbered a record at a span boundary makes BAD
      *> non-zero. SPAN is the walk that PREVIOUS makes from record 4098 across the boundary; it fails
      *> when ordinal 4097 or 4096 is taken from the wrong span.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3350A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VRF ASSIGN TO "pb3350a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-V.
       DATA DIVISION.
       FILE SECTION.
       FD  VRF RECORD IS VARYING IN SIZE FROM 4 TO 8
               DEPENDING ON WS-LEN.
       01  VR-REC.
           05  VR-NUM        PIC 9(4).
           05  VR-PAD        PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-V              PIC XX.
       01  WS-LEN            PIC 9(4) COMP.
       01  WS-N              PIC 9(5).
       01  WS-EXP            PIC 9(5).
       01  WS-BAD            PIC 9(5) VALUE 0.
       01  WS-BACK           PIC 9(5) VALUE 0.
       01  WS-GO             PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT VRF
           PERFORM VARYING WS-N FROM 1 BY 1 UNTIL WS-N > 4100
               MOVE WS-N TO VR-NUM
               MOVE ALL "z" TO VR-PAD
               COMPUTE WS-LEN = 4 + FUNCTION MOD (WS-N, 5)
               WRITE VR-REC
           END-PERFORM
           CLOSE VRF
      *> Phase 1 - read every record forward, then walk back over all of them.
           OPEN INPUT VRF
           PERFORM 4100 TIMES
               READ VRF NEXT RECORD
           END-PERFORM
           DISPLAY "FWD=" VR-NUM "|" WS-LEN "|" WS-V
           MOVE 4099 TO WS-EXP
           PERFORM UNTIL WS-GO = "N"
               READ VRF PREVIOUS RECORD
                   AT END
                       MOVE "N" TO WS-GO
                   NOT AT END
                       PERFORM CHECK-BACK
               END-READ
           END-PERFORM
           DISPLAY "BACK=" WS-BACK "|BAD=" WS-BAD "|" WS-V
           CLOSE VRF
      *> Phase 2 - the walk across the span boundary: forward to record 4098, then three backward.
           OPEN INPUT VRF
           PERFORM 4098 TIMES
               READ VRF NEXT RECORD
           END-PERFORM
           PERFORM 3 TIMES
               READ VRF PREVIOUS RECORD
               DISPLAY "SPAN=" VR-NUM "|" WS-LEN "|" WS-V
           END-PERFORM
           READ VRF NEXT RECORD
           DISPLAY "SPAN=" VR-NUM "|" WS-LEN "|" WS-V
           CLOSE VRF
           STOP RUN.
       CHECK-BACK.
           IF VR-NUM NOT = WS-EXP OR WS-LEN NOT = 4 + FUNCTION MOD (WS-EXP, 5)
               ADD 1 TO WS-BAD
           END-IF
           ADD 1 TO WS-BACK
           SUBTRACT 1 FROM WS-EXP.
