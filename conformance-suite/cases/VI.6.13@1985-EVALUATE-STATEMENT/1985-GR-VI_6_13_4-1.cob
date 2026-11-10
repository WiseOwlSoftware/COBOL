      *>--- conformance-case
      *> id: 1985-GR-VI_6_13_4-1
      *> rules: [1985-GR-VI.6.13.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.4 GR1 (THE EVALUATE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EVALT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           EVALUATE N
               WHEN 1 THRU 5 DISPLAY "LOW"
               WHEN NOT 7 DISPLAY "NOT7"
               WHEN 6 THRU 9 DISPLAY "MID"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           EVALUATE TRUE ALSO N
               WHEN N > 5 ALSO NOT 1 THRU 3 DISPLAY "BOTH"
               WHEN OTHER DISPLAY "NONE"
           END-EVALUATE.
           STOP RUN.
