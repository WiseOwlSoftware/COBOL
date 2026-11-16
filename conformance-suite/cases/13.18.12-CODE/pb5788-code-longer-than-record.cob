      *>--- conformance-case
      *> id: pb5788-code-longer-than-record
      *> rules: [GR-13.18.12.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5788. §1985:XIII.3.6.4 GR1: "literal-1 is automatically
      *> placed in the first two character positions of each report writer
      *> logical record" (§2023:13.18.12.4 GR1 likewise). RECORD CONTAINS 1
      *> gives a record that cannot hold the two characters of CODE "AB":
      *> refused at every edition. conformance:pb5788_code_record_is_code_alone
      *> is the legal side, a record of exactly the code's size.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5788B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5788B.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 1 CHARACTERS
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           CODE "AB".
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
