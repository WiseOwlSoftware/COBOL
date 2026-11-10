      *>--- conformance-case
      *> id: pb5079-format-2-no-optional-clause-legal
      *> rules: [SR-13.15.3-11]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.9.3 SR6: "A format 2 entry must contain at least one optional clause." (cite.py: OK). The
      *> entry `05.` is a bare level-number, which format 2 (1985:XIII.3.9.2) prints with every other part optional.
      *> conformance:pb5079-format-2-no-optional-clause is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079GL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079GL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DET TYPE IS DETAIL.
           05.
               10  LINE PLUS 1 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
