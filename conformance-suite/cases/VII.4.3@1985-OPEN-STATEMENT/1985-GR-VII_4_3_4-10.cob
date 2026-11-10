      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-10
      *> rules: [1985-GR-VII.4.3.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR10 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOREW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO DISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F WITH NO REWIND.
           MOVE "REC1" TO F-REC.
           WRITE F-REC.
           MOVE "REC2" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN INPUT F WITH NO REWIND.
           READ F
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY F-REC
           END-READ.
           CLOSE F.
           STOP RUN.
