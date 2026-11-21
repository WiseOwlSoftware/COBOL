      *>--- conformance-case
      *> id: pb6025-page-footing-absolute-past-page-limit-85
      *> rules: [1985-GR-XIII.3.10.9-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6025. 1985:XIII.3.10.9 GR4a: "the NEXT GROUP
      *> integer must not be greater than the line number specified by
      *> integer-1 of the PAGE clause." The page footing ends on line
      *> 19 and NEXT GROUP 21 exceeds the page limit 20: refused at 85
      *> (COBOL2284) by the page footing's own rule, the bound that
      *> COBOL-2002 states for every group (2002 13.16.35.2 SR1). Fails
      *> if the 85 arm leaves an absolute page footing integer past the
      *> page limit unbounded.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6025B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb6025b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES FIRST DETAIL 2
           LAST DETAIL 15 FOOTING 18.
       01  D1 TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "ABC".
       01  TYPE PAGE FOOTING LINE 19 NEXT GROUP 21.
           05  COLUMN 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
