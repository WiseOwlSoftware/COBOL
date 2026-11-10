      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-2
      *> rules: [1985-GR-VI.6.3.1.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 GR2 (Simple Conditions). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALPHTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-MIXED  PIC X(10) VALUE "Ab Cd eF".
       01  W-DIGIT  PIC X(10) VALUE "Ab1".
       PROCEDURE DIVISION.
       P1.
           IF W-MIXED IS ALPHABETIC
               DISPLAY "MIXED ALPHABETIC"
           ELSE
               DISPLAY "MIXED NOT ALPHABETIC".
           IF W-DIGIT IS ALPHABETIC
               DISPLAY "DIGIT ALPHABETIC"
           ELSE
               DISPLAY "DIGIT NOT ALPHABETIC".
           STOP RUN.
