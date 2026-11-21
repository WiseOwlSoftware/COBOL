      *>--- conformance-case
      *> id: 1985-GR-III_2-p239
      *> rules: [1985-GR-III.2-p239]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p239 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P239RARE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P239.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON L.
       01  R PIC X(20).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  L PIC 99.
       01  W PIC X(20).
       01  RES PIC X(30) VALUE SPACES.
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT F.
           MOVE "ABCDEFGHIJKLMNOPQRST" TO R.
           MOVE 5 TO L.
           WRITE R.
           MOVE 12 TO L.
           WRITE R.
           CLOSE F.
           MOVE 0 TO L.
           OPEN INPUT F.
           MOVE ALL "*" TO W.
           READ F INTO W AT END MOVE "EOF" TO RES.
           MOVE L TO RES(1:2).
           MOVE W(1:8) TO RES(4:8).
           MOVE ALL "*" TO W.
           READ F INTO W AT END MOVE "EOF" TO RES.
           MOVE L TO RES(13:2).
           MOVE W(1:14) TO RES(16:14).
           CLOSE F.
           DISPLAY "P239 " RES.
           IF RES = "05 ABCDE    12 ABCDEFGHIJKL  "
               DISPLAY "P239 OK"
           ELSE
               DISPLAY "P239 FAIL".
           STOP RUN.
