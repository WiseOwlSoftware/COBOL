      *>--- conformance-case
      *> id: 1985-GR-VI_6_13_4-2
      *> rules: [1985-GR-VI.6.13.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.4 GR2 (THE EVALUATE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EVALT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9 VALUE 5.
       77  B PIC X VALUE "Q".
       PROCEDURE DIVISION.
       P1.
           EVALUATE A ALSO B ALSO TRUE
             WHEN 1 THRU 4 ALSO ANY ALSO TRUE
               DISPLAY "W1"
             WHEN 3 THRU 6 ALSO "Q" ALSO A > 9
               DISPLAY "W2"
             WHEN 5 ALSO ANY ALSO A = 5
               DISPLAY "W3"
             WHEN 4 THRU 7 ALSO "Q" ALSO TRUE
               DISPLAY "W4"
             WHEN OTHER
               DISPLAY "W5"
           END-EVALUATE.
           STOP RUN.
