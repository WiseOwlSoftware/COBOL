      *>--- conformance-case
      *> id: 1985-GR-VI_4_5_4-4
      *> rules: [1985-GR-VI.4.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR4 (THE SPECIAL-NAMES PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALPHTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-MACHINE.
       OBJECT-COMPUTER. ANY-MACHINE
           PROGRAM COLLATING SEQUENCE IS ALPHA.
       SPECIAL-NAMES.
           ALPHABET ALPHA IS "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "A".
       01  Y PIC X VALUE "B".
       01  Z PIC X VALUE "1".
       PROCEDURE DIVISION.
       P1.
           IF Y < X
               DISPLAY "B-LOW"
           ELSE
               DISPLAY "B-HIGH".
           IF Z > X
               DISPLAY "1-HIGH"
           ELSE
               DISPLAY "1-LOW".
           STOP RUN.
