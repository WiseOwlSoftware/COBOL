      *>--- conformance-case
      *> id: 1985-GR-VI_6_27_4-18
      *> rules: [1985-GR-VI.6.27.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.4 GR18 (THE UNSTRING STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC PIC X(7) VALUE "AB,CD,E".
       01  F1  PIC X(2).
       01  F2  PIC X(2).
       PROCEDURE DIVISION.
       P1.
           UNSTRING SRC DELIMITED BY "," INTO F1 F2
               ON OVERFLOW DISPLAY "OVERFLOW"
               NOT ON OVERFLOW DISPLAY "NO OVERFLOW"
           END-UNSTRING
           DISPLAY "F1=" F1 " F2=" F2
           DISPLAY "END"
           STOP RUN.
