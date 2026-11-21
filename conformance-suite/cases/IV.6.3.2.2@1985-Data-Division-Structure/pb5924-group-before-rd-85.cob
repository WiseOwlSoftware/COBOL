      *>--- conformance-case
      *> id: pb5924-group-before-rd-85
      *> rules: [1985-FMT-IV.6.3.2.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XIII.3.4 (cite.py: OK): "must begin with a report description entry (RD entry) and be followed by one or more report group description entries"
      *> A report group description entry ahead of every RD is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NORD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "nord85.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       REPORT SECTION.
       01  DE-0 TYPE DETAIL LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "XXX".
       RD  R-1.
       01  DE-1 TYPE DETAIL LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
