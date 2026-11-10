      *>--- conformance-case
      *> id: pb5079-format-2-without-format-3-legal
      *> rules: [SR-13.15.3-11]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.9.3 SR4 c: "At least one format 3 entry must be immediately subordinate to a format 2 entry."
      *> (cite.py: OK). `02  LINE PLUS 1` has a level-number and a LINE NUMBER clause and no PICTURE clause, so it is a format 2 entry
      *> with nothing subordinate. COBOL-2002 and later have one entry format.
      *> conformance:pb5079-format-2-without-format-3 is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079FL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079FL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DET TYPE IS DETAIL.
           02  LINE PLUS 1 COLUMN 1 PIC X(5) VALUE "HELLO".
           02  LINE PLUS 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
