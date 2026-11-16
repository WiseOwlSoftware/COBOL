      *>--- conformance-case
      *> id: g2-rgd-in-cd-refused
      *> rules: [2002-SR-13.13.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.13.2 SR1 - "Report group description entries may
      *>  appear only in the report section." CREC, a report group
      *>  description entry (TYPE DETAIL, LINE, COLUMN), is written under
      *>  an input CD in the COMMUNICATION SECTION, where a 2002 program
      *>  writes record description entries (a 2002-only reach, the
      *>  facility obsolete at 2002). Expected: refused at line 21, the
      *>  TYPE clause of CREC. The accepted placement is
      *>  g2-rgd-elementary-clauses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RGD03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CI FOR INPUT
           SYMBOLIC QUEUE IS Q1.
       01  CREC TYPE DETAIL LINE PLUS 1.
           05  F COLUMN 1 PIC X(87) VALUE SPACES.
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED"
           STOP RUN.
