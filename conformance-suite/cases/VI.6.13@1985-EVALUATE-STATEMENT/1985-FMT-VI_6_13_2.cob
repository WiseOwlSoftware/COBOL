      *>--- conformance-case
      *> id: 1985-FMT-VI_6_13_2
      *> rules: [1985-FMT-VI.6.13.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.2 (THE EVALUATE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EVALT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9 VALUE 5.
       77  B PIC 9 VALUE 2.
       77  N PIC X VALUE "M".
       PROCEDURE DIVISION.
       P1.
           EVALUATE A ALSO TRUE
               WHEN 1 THRU 4 ALSO B = 2
                   DISPLAY "LOW"
               WHEN 5 THRU 9 ALSO B = 2
                   DISPLAY "HIGH"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           EVALUATE A + B ALSO N ALSO B
               WHEN 6 ALSO ANY ALSO ANY
               WHEN 8 ALSO "M" ALSO ANY
                   DISPLAY "SIX OR EIGHT"
               WHEN 7 ALSO NOT "A" THROUGH "L" ALSO NOT 2
                   DISPLAY "NOT TWO"
               WHEN A + 2 ALSO NOT "A" THROUGH "L" ALSO B * 1
                   DISPLAY "SEVEN"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           EVALUATE FALSE ALSO A > 3
               WHEN B = 2 ALSO TRUE
                   DISPLAY "FIRST"
               WHEN B = 9 ALSO FALSE
                   DISPLAY "SECOND"
               WHEN B = 9 ALSO TRUE
                   DISPLAY "THIRD"
           END-EVALUATE.
           EVALUATE N
               WHEN "A" THRU "L"
               WHEN "N" THRU "Z"
                   DISPLAY "NOT M"
               WHEN ANY
                   DISPLAY "ANY"
           END-EVALUATE.
           EVALUATE B
               WHEN 3 DISPLAY "THREE"
               WHEN 4 DISPLAY "FOUR".
           DISPLAY "END".
           STOP RUN.
