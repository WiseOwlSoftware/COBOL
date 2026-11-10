      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-15
      *> rules: [1985-GR-VI.6.18.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR15 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP15.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X(4) VALUE "ABCD".
       01  W2 PIC X(4) VALUE "ABAB".
       01  W3 PIC X(4) VALUE "AABA".
       01  W4 PIC X(4) VALUE "BAAB".
       01  W5 PIC X(4) VALUE "BABA".
       01  W6 PIC X(4) VALUE "BAAB".
       01  W7 PIC X(4) VALUE "BABA".
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 REPLACING CHARACTERS BY "Z".
           DISPLAY W1.
           INSPECT W2 REPLACING ALL "A" BY "X".
           DISPLAY W2.
           INSPECT W3 REPLACING LEADING "A" BY "X".
           DISPLAY W3.
           INSPECT W4 REPLACING LEADING "A" BY "X".
           DISPLAY W4.
           INSPECT W5 REPLACING FIRST "A" BY "X".
           DISPLAY W5.
           INSPECT W6 REPLACING LEADING "A" BY "X" AFTER INITIAL "B".
           DISPLAY W6.
           INSPECT W7 REPLACING FIRST "A" BY "X" FIRST "A" BY "Y".
           DISPLAY W7.
           STOP RUN.
