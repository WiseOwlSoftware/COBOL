      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-4
      *> rules: [1985-GR-VII.4.7.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR4 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO DISKFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-DATA PIC X(10) VALUE "HELLO-1985".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           WRITE OUT-REC FROM WS-DATA.
           DISPLAY WS-DATA.
           CLOSE OUT-FILE.
           STOP RUN.
