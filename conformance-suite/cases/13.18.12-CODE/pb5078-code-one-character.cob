      *>--- conformance-case
      *> id: pb5078-code-one-character
      *> rules: [SR-13.18.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5078 - 1985:XIII.3.6.3 SR1: "Literal-1 must be a two-character nonnumeric literal." (cite.py: OK).
      *> CODE "A" is one character. COBOL-2002 and later ask only for an alphanumeric literal (2023:13.18.12.3 SR1).
      *> conformance:pb5078-code-one-character-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5078DN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5078DN.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT CODE "A".
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
