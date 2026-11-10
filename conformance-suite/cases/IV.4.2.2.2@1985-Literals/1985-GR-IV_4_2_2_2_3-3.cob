      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_3-3
      *> rules: [1985-GR-IV.4.2.2.2.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GR3 (Literals). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HVTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F PIC X(3).
       01  G PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           MOVE HIGH-VALUE TO F.
           IF F = ALL HIGH-VALUES
               DISPLAY "FILLED"
           ELSE
               DISPLAY "NOT FILLED".
           IF G NOT > F
               DISPLAY "HIGHEST"
           ELSE
               DISPLAY "NOT HIGHEST".
           STOP RUN.
