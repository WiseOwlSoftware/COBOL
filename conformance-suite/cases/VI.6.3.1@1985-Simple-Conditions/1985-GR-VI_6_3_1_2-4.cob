      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-4
      *> rules: [1985-GR-VI.6.3.1.2-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 GR4 (Simple Conditions). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. AUTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X(5) VALUE "AB CD".
       01  W2 PIC X(5) VALUE "AbCD ".
       01  W3 PIC X(5) VALUE "AB1CD".
       PROCEDURE DIVISION.
       P1.
           IF W1 IS ALPHABETIC-UPPER
               DISPLAY "W1 UPPER"
           ELSE
               DISPLAY "W1 NOT UPPER".
           IF W2 IS ALPHABETIC-UPPER
               DISPLAY "W2 UPPER"
           ELSE
               DISPLAY "W2 NOT UPPER".
           IF W3 IS ALPHABETIC-UPPER
               DISPLAY "W3 UPPER"
           ELSE
               DISPLAY "W3 NOT UPPER".
           STOP RUN.
