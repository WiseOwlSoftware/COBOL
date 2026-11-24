      *>--- conformance-case
      *> id: pb6075-source-index-cites-2014
      *> rules: [SR-13.18.53.3-2, SR-13.18.60.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6075. §2014:13.18.53.2 SR2: identifier-1 "shall be described such that a MOVE statement is valid", and
      *> §2014:14.9.24.2 SR1: "The class of identifier-1 or identifier-2 shall not be index, object, or pointer".
      *> SOURCE IS IX is refused (COBOL2770) and the message names the 2014 rules, not COBOL-2023's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6075S14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB6075S14".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  IX USAGE IS INDEX.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE DETAIL LINE PLUS 1.
           10  COLUMN 1 PIC 9(4) SOURCE IS IX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
