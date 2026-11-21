      *>--- conformance-case
      *> id: 1985-GR-III_2-p245
      *> rules: [1985-GR-III.2-p245, 1985-GR-III.2-p320, 1985-GR-III.2-p327]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p245; §1985:III.2 p320; §1985:III.2 p327 ((section
      *> preamble)). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/g-99.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REELPRB.
      * 1985 III.2 p245/p320/p327: reel, unit and volume are one term,
      * a portion of a medium whose dimensions the implementor fixes.
      * This implementation's disk files are non-reel/unit media, so
      * CLOSE REEL and CLOSE UNIT alike complete 07 and leave the file
      * open with its position unchanged (VII.4.2.4 GR3 F, VII.1.3.5).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "reel.dat"
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       01 STS PIC X(12) VALUE SPACES.
       01 SEQ PIC X(12) VALUE SPACES.
       01 FAILS PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SF.
           MOVE "AAAA" TO SREC. WRITE SREC.
           CLOSE SF REEL.
           MOVE ST TO STS (1:2).
           MOVE "BBBB" TO SREC. WRITE SREC.
           MOVE ST TO STS (3:2).
           CLOSE SF UNIT.
           MOVE ST TO STS (5:2).
           MOVE "CCCC" TO SREC. WRITE SREC.
           MOVE ST TO STS (7:2).
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END ADD 1 TO FAILS.
           MOVE SREC TO SEQ (1:4).
           CLOSE SF UNIT.
           MOVE ST TO STS (9:2).
           READ SF AT END ADD 1 TO FAILS.
           MOVE SREC TO SEQ (5:4).
           CLOSE SF REEL.
           MOVE ST TO STS (11:2).
           READ SF AT END ADD 1 TO FAILS.
           MOVE SREC TO SEQ (9:4).
           CLOSE SF.
           DISPLAY "STATUS [" STS "] DATA [" SEQ "]".
           IF STS NOT = "070007000707" ADD 1 TO FAILS.
           IF SEQ NOT = "AAAABBBBCCCC" ADD 1 TO FAILS.
           IF FAILS = 0
               DISPLAY "REEL PASS"
           ELSE
               DISPLAY "REEL FAIL " FAILS.
           STOP RUN.
