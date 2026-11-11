      *>--- conformance-case
      *> id: pb5079-source-group-of-control-in-footing
      *> rules: [SR-13.18.53.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.20.3 SR5 a: in a control footing SOURCE clauses must not reference "Group data items containing a
      *> control data item." (cite.py: OK). WS-G holds the control data item WS-KEY. COBOL-2002 and later carry no such rule.
      *> conformance:pb5079-source-group-of-control-in-footing-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079EN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079EN.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  WS-KEY PIC XXX VALUE "AAA".
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC XXX SOURCE WS-G.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
