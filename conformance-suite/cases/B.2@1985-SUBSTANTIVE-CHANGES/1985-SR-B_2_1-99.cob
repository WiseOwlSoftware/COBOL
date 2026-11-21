      *>--- conformance-case
      *> id: 1985-SR-B_2_1-99
      *> rules: [1985-SR-B.2.1-99]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:B.2.1 SR99 (SUBSTANTIVE CHANGES). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 B.2.1 item 99 (and VI STRING): the INTO identifier
      * may be a group item. Self-checking: expects "STRGRP OK".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRGRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  RCV.
           05  RCV-A        PIC X(3).
           05  RCV-B        PIC X(5).
       01  PTR              PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "*" TO RCV.
           STRING "AB" "CDE" DELIMITED BY SIZE INTO RCV
               WITH POINTER PTR.
           IF RCV = "ABCDE***" AND RCV-A = "ABC" AND PTR = 6
               DISPLAY "STRGRP OK"
           ELSE
               DISPLAY "STRGRP BAD " RCV " " PTR.
           STOP RUN.
