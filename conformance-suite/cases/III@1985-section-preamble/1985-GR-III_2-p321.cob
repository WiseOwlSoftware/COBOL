      *>--- conformance-case
      *> id: 1985-GR-III_2-p321
      *> rules: [1985-GR-III.2-p321]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p321 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSUCC.
      * 1985 III.2 Unsuccessful Execution: a WRITE that fails with a
      * duplicate key leaves the data it references unaffected and
      * sets the I-O status.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "UNSUCC.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS W-KEY
               FILE STATUS IS W-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RELF.
       01  R-REC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  W-KEY        PIC 9(4).
       01  W-FS         PIC XX.
       01  W-OK         PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RELF.
           MOVE 3 TO W-KEY.
           MOVE "AAAA" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "FIRST WRITE FAILED".
           MOVE "BBBB" TO R-REC.
           WRITE R-REC INVALID KEY ADD 1 TO W-OK.
           DISPLAY "FS=" W-FS " REC=" R-REC " KEY=" W-KEY.
           IF W-FS = "22" ADD 1 TO W-OK.
           IF R-REC = "BBBB" ADD 1 TO W-OK.
           IF W-KEY = 3 ADD 1 TO W-OK.
           CLOSE RELF.
           IF W-OK = 4 DISPLAY "UNSUCC PASS"
               ELSE DISPLAY "UNSUCC FAIL " W-OK.
           STOP RUN.
