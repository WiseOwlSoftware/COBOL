      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-1
      *> rules: [1985-GR-VI.6.3.1.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 GR1 (Simple Conditions). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1 PIC X(3) VALUE "123".
       01  A2 PIC X(3) VALUE "12A".
       01  N1 PIC S9(3) VALUE -123.
       PROCEDURE DIVISION.
       P1.
           IF A1 IS NUMERIC
               DISPLAY "A1 NUMERIC"
           ELSE
               DISPLAY "A1 NOT NUMERIC".
           IF A2 IS NUMERIC
               DISPLAY "A2 NUMERIC"
           ELSE
               DISPLAY "A2 NOT NUMERIC".
           IF N1 IS NUMERIC
               DISPLAY "N1 NUMERIC"
           ELSE
               DISPLAY "N1 NOT NUMERIC".
           STOP RUN.
