      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-13
      *> rules: [1985-GR-VI.6.18.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR13 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP13.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1   PIC X(6) VALUE "ABBCAB".
       01  W3   PIC X    VALUE "B".
       01  W4   PIC X    VALUE "C".
       01  CNT  PIC 99   VALUE 0.
       01  CN2  PIC 99   VALUE 0.
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 TALLYING CNT FOR ALL W3 BEFORE INITIAL W4.
           DISPLAY CNT.
           INSPECT W1 TALLYING CN2 FOR ALL W3 AFTER INITIAL W4.
           DISPLAY CN2.
           DISPLAY W1 W3 W4.
           STOP RUN.
