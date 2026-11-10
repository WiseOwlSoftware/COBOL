      *>--- conformance-case
      *> id: 1985-GR-VI_2_3_3-1
      *> rules: [1985-GR-VI.2.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.3.3 GR1 (STRUCTURE). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVEND.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           DISPLAY W.
           STOP RUN.
