      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-7
      *> rules: [1985-GR-VI.6.18.4-7]
      *> accept: [85]
      *> source-format: fixed
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
       01  C1-E PIC Z9.
       01  C2-E PIC Z9.
       01  C3-E PIC Z9.
       01  C4-E PIC Z9.
       01  C5-E PIC Z9.
       01  C6-E PIC Z9.
       01  C7-E PIC Z9.
       01  C8-E PIC Z9.
       01  C9-E PIC Z9.
       01  C10-E PIC Z9.
       01  C11-E PIC Z9.
       01  C12-E PIC Z9.
       PROCEDURE DIVISION.
       P1.
           INSPECT W1 TALLYING C1 FOR ALL "AB" C2 FOR ALL "A".
           MOVE C1 TO C1-E
           MOVE C2 TO C2-E
           DISPLAY C1-E " " C2-E.
           INSPECT W1 TALLYING C3 FOR ALL "A" C4 FOR ALL "AB".
           MOVE C3 TO C3-E
           MOVE C4 TO C4-E
           DISPLAY C3-E " " C4-E.
           INSPECT W2 TALLYING C5 FOR ALL "AAA".
           MOVE C5 TO C5-E
           DISPLAY C5-E.
           INSPECT W3 TALLYING C6 FOR ALL "AB".
           MOVE C6 TO C6-E
           DISPLAY C6-E.
           INSPECT W1 TALLYING C7 FOR ALL "B" C8 FOR CHARACTERS.
           MOVE C7 TO C7-E
           MOVE C8 TO C8-E
           DISPLAY C7-E " " C8-E.
           INSPECT W1 TALLYING C9 FOR CHARACTERS C10 FOR ALL "B".
           MOVE C9 TO C9-E
           MOVE C10 TO C10-E
           DISPLAY C9-E " " C10-E.
           INSPECT W3 TALLYING C11 FOR LEADING "A" C12 FOR ALL "A".
           MOVE C11 TO C11-E
           MOVE C12 TO C12-E
           DISPLAY C11-E " " C12-E.
           INSPECT W4 REPLACING ALL "AB" BY "XY" ALL "A" BY "Z".
           DISPLAY W4.
           INSPECT W5 REPLACING ALL "A" BY "B" ALL "B" BY "A".
           DISPLAY W5.
           INSPECT W6 REPLACING FIRST "B" BY "X" ALL "B" BY "Y".
           DISPLAY W6.
           STOP RUN.
