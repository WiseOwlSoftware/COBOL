      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-13
      *> rules: [1985-GR-XIII.4.5.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR13 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFREEL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE-1 ASSIGN TO TAPE1.
           SELECT RPT-FILE-2 ASSIGN TO TAPE1.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS RPT-FILE-1 POSITION 1
               RPT-FILE-2 POSITION 2.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE-1 LABEL RECORDS ARE STANDARD
           REPORT IS RPT-1.
       FD  RPT-FILE-2 LABEL RECORDS ARE STANDARD
           REPORT IS RPT-2.
       REPORT SECTION.
       RD  RPT-1.
       01  DETL-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "FIRST".
       RD  RPT-2.
       01  DETL-2 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "SECND".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE-1.
           INITIATE RPT-1.
           GENERATE DETL-1.
           TERMINATE RPT-1.
           CLOSE RPT-FILE-1.
           DISPLAY "ONE CLOSED".
           OPEN OUTPUT RPT-FILE-2.
           INITIATE RPT-2.
           GENERATE DETL-2.
           TERMINATE RPT-2.
           CLOSE RPT-FILE-2.
           DISPLAY "TWO CLOSED".
           STOP RUN.
