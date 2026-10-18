      *>--- conformance-case
      *> id: pb1515_io_status_07_open_close_only
      *> rules: [GR-9.1.13.2-6, GR-14.9.27.4-11, GR-14.9.27.4-2,
      *>    GR-14.9.27.4-25, GR-14.9.6.4-1, GR-14.9.6.4-3, GR-9.1.13.4-1,
      *>    GR-9.1.13.6-5, GR-9.1.13.7-1, GR-9.1.13.7-2, GR-9.1.13.7-6,
      *>    GR-14.9.49.4-6, DOC-A.1-103]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1515OC.
      *> kb/Work PB1515. WHICH STATEMENTS MAY SET I-O STATUS '07', AND
      *> WHAT THEY ANSWER WHEN THEY FAIL. ISO 9.1.13.2 item 6: "I-O
      *> status = 07. An OPEN or CLOSE statement is successfully
      *> executed but a CLOSE statement with the NO REWIND, REEL/UNIT,
      *> or FOR REMOVAL phrase or an OPEN statement with the NO REWIND
      *> phrase references a physical file on a non-reel/unit medium."
      *> Annex E.2 item 16: "The setting of I-O Status '07' is now
      *> restricted to the execution of the OPEN and CLOSE statements."
      *> The repository holds no text of an edition before 2023, and
      *> E.2 names neither the other statements nor the condition they
      *> reported, so docs/CONFORMANCE.md section 3 records the
      *> determination that the two statements are the only setters at
      *> EVERY edition (the surveyed implementation, GnuCOBOL 3.2,
      *> sets '07' from CLOSE alone); this test accepts the same
      *> output at 85, 2002, 2014 and 2023 and pins it.
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULES BELOW, NEVER
      *> COPIED FROM A RUN. Each line is one statement and the status
      *> it leaves; USE counts how many times the Format 1 USE
      *> declarative has run (14.9.49.4 GR6: it runs "upon the
      *> unsuccessful execution of an input-output operation unless an
      *> AT END or INVALID KEY phrase takes precedence"; a successful
      *> '07' is no unsuccessful operation, and EC-I-O-WARNING is off
      *> because nothing turns it on, 9.1.13.1). The failures continue
      *> the run unit because the program is equipped to see them: a
      *> FILE STATUS clause and the declarative (DOC-A.1-103).
      *>   ABSENT   35 USE 1  OPEN INPUT WITH NO REWIND of a file that
      *>                      is neither optional nor present: 9.1.13.6
      *>                      item 5. GR11's '07' rides a SUCCESSFUL
      *>                      open only; an unsuccessful OPEN keeps the
      *>                      status that caused it (14.9.27.4 GR25 a).
      *>   OUT      00 USE 1  OPEN OUTPUT creates the file.
      *>   W1,W2    00 USE 1  WRITE sets no '07': it is no OPEN or CLOSE.
      *>   REOPEN   41 USE 2  OPEN OUTPUT WITH NO REWIND of the file
      *>                      connector that is open: 9.1.13.7 item 1.
      *>   W3       00 USE 2  the failed OPEN left F open: "the file is
      *>                      not affected" (14.9.27.4 GR25); the file
      *>                      now holds AAAA, BBBB and CCCC.
      *>   CLOSE-NR 07 USE 2  CLOSE WITH NO REWIND, Table 14 cell c,g.
      *>   CLOSE-NR2 42 USE 3 the same CLOSE on a connector that is not
      *>                      open: 14.9.6.4 GR1 comes before every
      *>                      symbol, so no '07'.
      *>   CLOSE-RL 42 USE 4  CLOSE REEL, the same GR1.
      *>   CLOSE-UR 42 USE 5  CLOSE UNIT FOR REMOVAL, the same GR1.
      *>   IN-NR    07 USE 5  OPEN INPUT WITH NO REWIND, now succeeding.
      *>   R1       00 AAAA   READ sets no '07'; neither the OPEN's
      *>                      '07' nor any later status carries over.
      *>   R2       00 BBBB   READ AT END: the at end condition does
      *>                      not exist, the status is '00'.
      *>   R3       00 CCCC   the third record.
      *>   R4       10 USE 6  READ with no AT END phrase at the end of
      *>                      the file: 9.1.13.4 item 1 a); the USE
      *>                      declarative runs.
      *>   R5       46 USE 7  READ AT END after the at end condition:
      *>                      9.1.13.7 item 6 (the preceding READ
      *>                      caused the at end condition), a logic
      *>                      error and NOT the at end condition, so
      *>                      the AT END imperative does not run and
      *>                      the USE declarative does.
      *>   UNIT-FR  07 USE 7  CLOSE UNIT FOR REMOVAL keeps the file
      *>                      open and its position (14.9.6.4 GR3 e).
      *>   R6       46 USE 8  the unchanged position: still no valid
      *>                      next record.
      *>   CLOSE    00 USE 8  a plain CLOSE is successful: no phrase,
      *>                      no '07' (9.1.13.2 item 1).
      *>   IN2      00        a plain OPEN INPUT.
      *>   S1-S3    00        three READs: AAAA, BBBB, CCCC.
      *>   S4       10 USE 8  READ AT END: the AT END imperative runs
      *>                      and the USE declarative does not (9.1.13.1
      *>                      "no other applicable exception processing
      *>                      statements shall be executed").
      *>   CLOSE2   00 USE 8
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1515oc.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-F PIC XX.
       01 WS-USE PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F.
       IO-ERR-P.
           ADD 1 TO WS-USE.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN INPUT F WITH NO REWIND
           DISPLAY "ABSENT=" WS-F " USE=" WS-USE
           OPEN OUTPUT F
           DISPLAY "OUT=" WS-F " USE=" WS-USE
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           DISPLAY "W1=" WS-F " USE=" WS-USE
           MOVE "BBBB" TO F-REC
           WRITE F-REC
           DISPLAY "W2=" WS-F " USE=" WS-USE
           OPEN OUTPUT F WITH NO REWIND
           DISPLAY "REOPEN=" WS-F " USE=" WS-USE
           MOVE "CCCC" TO F-REC
           WRITE F-REC
           DISPLAY "W3=" WS-F " USE=" WS-USE
           CLOSE F WITH NO REWIND
           DISPLAY "CLOSE-NR=" WS-F " USE=" WS-USE
           CLOSE F WITH NO REWIND
           DISPLAY "CLOSE-NR2=" WS-F " USE=" WS-USE
           CLOSE F REEL
           DISPLAY "CLOSE-RL=" WS-F " USE=" WS-USE
           CLOSE F UNIT FOR REMOVAL
           DISPLAY "CLOSE-UR=" WS-F " USE=" WS-USE
           OPEN INPUT F WITH NO REWIND
           DISPLAY "IN-NR=" WS-F " USE=" WS-USE
           READ F
           DISPLAY "R1=" WS-F " " F-REC " USE=" WS-USE
           READ F AT END DISPLAY "R2 AT END RAN" END-READ
           DISPLAY "R2=" WS-F " " F-REC " USE=" WS-USE
           READ F
           DISPLAY "R3=" WS-F " " F-REC " USE=" WS-USE
           READ F
           DISPLAY "R4=" WS-F " USE=" WS-USE
           READ F AT END DISPLAY "R5 AT END RAN" END-READ
           DISPLAY "R5=" WS-F " USE=" WS-USE
           CLOSE F UNIT FOR REMOVAL
           DISPLAY "UNIT-FR=" WS-F " USE=" WS-USE
           READ F
           DISPLAY "R6=" WS-F " USE=" WS-USE
           CLOSE F
           DISPLAY "CLOSE=" WS-F " USE=" WS-USE
           OPEN INPUT F
           DISPLAY "IN2=" WS-F " USE=" WS-USE
           READ F
           DISPLAY "S1=" WS-F " " F-REC " USE=" WS-USE
           READ F
           DISPLAY "S2=" WS-F " " F-REC " USE=" WS-USE
           READ F
           DISPLAY "S3=" WS-F " " F-REC " USE=" WS-USE
           READ F AT END DISPLAY "S4 AT END RAN" END-READ
           DISPLAY "S4=" WS-F " USE=" WS-USE
           CLOSE F
           DISPLAY "CLOSE2=" WS-F " USE=" WS-USE
           STOP RUN.
