      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-7
      *> rules: [1985-GR-VI.6.18.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR7 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPGR7.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1   PIC X(5) VALUE "ABABA".
       01  W2   PIC X(4) VALUE "AAAA".
       01  W3   PIC X(5) VALUE "AABAA".
       01  W4   PIC X(5) VALUE "ABABA".
       01  W5   PIC X(5) VALUE "ABABA".
       01  W6   PIC X(5) VALUE "ABABA".
       01  C1   PIC 99 VALUE 0.
       01  C2   PIC 99 VALUE 0.
       01  C3   PIC 99 VALUE 0.
       01  C4   PIC 99 VALUE 0.
       01  C5   PIC 99 VALUE 0.
       01  C6   PIC 99 VALUE 0.
       01  C7   PIC 99 VALUE 0.
       01  C8   PIC 99 VALUE 0.
       01  C9   PIC 99 VALUE 0.
       01  C10  PIC 99 VALUE 0.
       01  C11  PIC 99 VALUE 0.
       01  C12  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 TALLYING C1 FOR ALL "AB" C2 FOR ALL "A".
           DISPLAY C1 " " C2.
           INSPECT W1 TALLYING C3 FOR ALL "A" C4 FOR ALL "AB".
           DISPLAY C3 " " C4.
           INSPECT W2 TALLYING C5 FOR ALL "AAA".
           DISPLAY C5.
           INSPECT W3 TALLYING C6 FOR ALL "AB".
           DISPLAY C6.
           INSPECT W1 TALLYING C7 FOR ALL "B" C8 FOR CHARACTERS.
           DISPLAY C7 " " C8.
           INSPECT W1 TALLYING C9 FOR CHARACTERS C10 FOR ALL "B".
           DISPLAY C9 " " C10.
           INSPECT W3 TALLYING C11 FOR LEADING "A" C12 FOR ALL "A".
           DISPLAY C11 " " C12.
           INSPECT W4 REPLACING ALL "AB" BY "XY" ALL "A" BY "Z".
           DISPLAY W4.
           INSPECT W5 REPLACING ALL "A" BY "B" ALL "B" BY "A".
           DISPLAY W5.
           INSPECT W6 REPLACING FIRST "B" BY "X" ALL "B" BY "Y".
           DISPLAY W6.
           STOP RUN.
