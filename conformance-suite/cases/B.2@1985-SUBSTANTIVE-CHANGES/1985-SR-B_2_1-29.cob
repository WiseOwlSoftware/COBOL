      *>--- conformance-case
      *> id: 1985-SR-B_2_1-29
      *> rules: [1985-SR-B.2.1-29]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:B.2.1 SR29 (SUBSTANTIVE CHANGES). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 B.2.1 item 29 / VII.2.10.3 SR1: the order of clauses
      * in I-O-CONTROL is immaterial. MULTIPLE FILE TAPE is written
      * before SAME (the format prints SAME first) and RERUN last.
      * Self-checking: expects "IOCORD OK".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOCORD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "IOCORD1.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "IOCORD2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS F1 F2
           SAME RECORD AREA FOR F1 F2
           RERUN EVERY END OF REEL OF F1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1               PIC X(4).
       FD  F2.
       01  R2               PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "WXYZ" TO R1.
           IF R2 = "WXYZ"
               DISPLAY "IOCORD OK"
           ELSE
               DISPLAY "IOCORD BAD " R2.
           CLOSE F1.
           STOP RUN.
