      *>--- conformance-case
      *> id: 1985-GR-III_2-p205
      *> rules: [1985-GR-III.2-p205]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p205 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P205OUTM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P205.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RES PIC X(40) VALUE SPACES.
       PROCEDURE DIVISION.
       P-MAIN.
      *    OUTPUT MODE: OPEN OUTPUT, WRITE SUCCEEDS
           OPEN OUTPUT F.
           MOVE "AAAA" TO R.
           WRITE R.
           MOVE FS TO RES(1:2).
      *    CLOSE REEL LEAVES THE FILE IN OUTPUT MODE
           CLOSE F REEL.
           MOVE FS TO RES(4:2).
           MOVE "BBBB" TO R.
           WRITE R.
           MOVE FS TO RES(7:2).
           CLOSE F.
           MOVE FS TO RES(10:2).
      *    AFTER CLOSE WITHOUT REEL: NOT IN OUTPUT MODE
           WRITE R.
           MOVE FS TO RES(13:2).
      *    EXTEND IS ALSO OUTPUT MODE
           OPEN EXTEND F.
           MOVE "CCCC" TO R.
           WRITE R.
           MOVE FS TO RES(16:2).
           CLOSE F.
           MOVE FS TO RES(19:2).
           DISPLAY "P205 " RES.
           IF RES = "00 07 00 00 48 00 00"
               DISPLAY "P205 OK"
           ELSE
               DISPLAY "P205 FAIL".
           STOP RUN.
