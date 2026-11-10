      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-3
      *> rules: [1985-GR-VI.6.3.1.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 GR3 (Simple Conditions). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALPLOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(6) VALUE "abc de".
       01 WS-B PIC X(6) VALUE "abCde ".
       PROCEDURE DIVISION.
       P1.
           IF WS-A IS ALPHABETIC-LOWER
               DISPLAY "A LOWER"
           ELSE
               DISPLAY "A NOT LOWER".
           IF WS-B IS ALPHABETIC-LOWER
               DISPLAY "B LOWER"
           ELSE
               DISPLAY "B NOT LOWER".
           STOP RUN.
