      *>--- conformance-case
      *> id: 1985-GR-XVI_2_2_4-3
      *> rules: [1985-GR-XVI.2.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.2.4 GR3 (THE OBJECT-COMPUTER PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGLIM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1.
       OBJECT-COMPUTER. COMP1
           SEGMENT-LIMIT IS 20.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 10.
       P1.
           DISPLAY "FIXED".
           PERFORM P2.
           DISPLAY "DONE".
           STOP RUN.
       OVL-SEC SECTION 30.
       P2.
           DISPLAY "OVERLAYABLE".
