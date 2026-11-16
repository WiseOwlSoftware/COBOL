      *>--- conformance-case
      *> id: g85_bf09_07
      *> rules: [1985-SR-XIII.3.14.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.14.3 SR1 - a level-number is required as the first
      *> element in each data description entry of the Report Section.
      *>   cite.py: OK  §1985:XIII.3.14.3 SR1 "A level-number is
      *>     required as the first element in each data description
      *>     entry"
      *> A refusal: the COLUMN entry under DE-1 has no level-number,
      *> which the rule forbids; WiseOwl refuses the program at 85 at
      *> that line with COBOL0001. A compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0907.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf0907.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       REPORT SECTION.
       RD  R-1.
       01  DE-1 TYPE DE LINE PLUS 1.
           COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
