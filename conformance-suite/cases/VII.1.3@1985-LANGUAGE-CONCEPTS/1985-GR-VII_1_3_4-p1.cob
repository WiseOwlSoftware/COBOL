      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_4-p1
      *> rules: [1985-GR-VII.1.3.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.4 p1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P07FPI.
      * 1985 VII.1.3.4 p1: the file position indicator is affected
      * only by CLOSE, OPEN and READ: a REWRITE between two READs
      * leaves it, so the second READ gets the second record.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P07FPI.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "R1R1" TO R. WRITE R.
           MOVE "R2R2" TO R. WRITE R.
           MOVE "R3R3" TO R. WRITE R.
           CLOSE F.
           OPEN I-O F.
           READ F AT END DISPLAY "EOF".
           DISPLAY "READ1 " R " " FS.
           MOVE "XXXX" TO R.
           REWRITE R.
           DISPLAY "REWRITE " FS.
           READ F AT END DISPLAY "EOF".
           DISPLAY "READ2 " R " " FS.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF".
           DISPLAY "AGAIN " R.
           READ F AT END DISPLAY "EOF".
           DISPLAY "AGAIN " R.
           CLOSE F.
           STOP RUN.
