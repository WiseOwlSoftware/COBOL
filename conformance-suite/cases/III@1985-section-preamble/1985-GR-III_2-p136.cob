      *>--- conformance-case
      *> id: 1985-GR-III_2-p136
      *> rules: [1985-GR-III.2-p136]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p136 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p136: input mode lasts from OPEN INPUT until a
      * CLOSE without REEL/UNIT. READ succeeds in input mode, and
      * after CLOSE the file is not in input mode (READ status 47).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P136.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p136.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F.
           MOVE "AAAA" TO R. WRITE R.
           MOVE "BBBB" TO R. WRITE R.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF1".
           DISPLAY "READ1 " FS " " R.
           CLOSE F REEL.
           DISPLAY "CLOSE-REEL " FS.
           READ F AT END DISPLAY "EOF2".
           DISPLAY "READ2 " FS " " R.
           CLOSE F.
           READ F AT END DISPLAY "EOF3".
           DISPLAY "READ3 " FS.
           IF FS = "47"
               DISPLAY "P136 OK"
           ELSE
               DISPLAY "P136 FAIL".
           STOP RUN.
