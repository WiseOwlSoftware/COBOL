      *>--- conformance-case
      *> id: pb5924-rd-without-group-85
      *> rules: [1985-FMT-IV.6.3.2.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XIII.3.4 (cite.py: OK): "must begin with a report description entry (RD entry) and be followed by one or more report group description entries"
      *> An RD followed by no report group description entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOGRP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "nogrp85.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       REPORT SECTION.
       RD  R-1.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
