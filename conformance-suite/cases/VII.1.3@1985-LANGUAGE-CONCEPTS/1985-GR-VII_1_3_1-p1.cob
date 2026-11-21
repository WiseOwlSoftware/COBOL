      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_1-p1
      *> rules: [1985-GR-VII.1.3.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.1 p1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P06SUC.
      * 1985 VII.1.3.1 p1: the successor order is the WRITE order of
      * the file's creation and changes only when records are added
      * at the end (OPEN EXTEND).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P06SUC.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "CCCC" TO R. WRITE R.
           MOVE "AAAA" TO R. WRITE R.
           MOVE "BBBB" TO R. WRITE R.
           CLOSE F.
           OPEN EXTEND F.
           MOVE "DDDD" TO R. WRITE R.
           CLOSE F.
           OPEN INPUT F.
           PERFORM UNTIL EOF-SW = "Y"
               READ F AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY R
               END-READ
           END-PERFORM.
           CLOSE F.
           STOP RUN.
