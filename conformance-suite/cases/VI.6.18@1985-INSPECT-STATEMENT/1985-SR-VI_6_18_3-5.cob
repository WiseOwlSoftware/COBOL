      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-5
      *> rules: [1985-SR-VI.6.18.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR5 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-STR PIC X(8) VALUE "ABABCAAB".
       01  WS-CNT PIC 9(2) VALUE ZERO.
       01  WS-ID3 PIC XX VALUE "AB".
       01  WS-ID4 PIC XX VALUE "CA".
       01  WS-ID5 PIC XX VALUE "XY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-STR TALLYING WS-CNT
               FOR ALL "AB" BEFORE INITIAL "CA".
           DISPLAY WS-CNT.
           INSPECT WS-STR REPLACING ALL WS-ID3 BY WS-ID5
               AFTER INITIAL WS-ID4.
           DISPLAY WS-STR.
           INSPECT WS-STR REPLACING FIRST "BA" BY "ZZ".
           DISPLAY WS-STR.
           STOP RUN.
