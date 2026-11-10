      *>--- conformance-case
      *> id: 1985-GR-XVI_2_3_4-1
      *> rules: [1985-GR-XVI.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.3.4 GR1 (THE SEGMENT-LIMIT CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGLIM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X SEGMENT-LIMIT IS 10.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 00.
       M1.
           PERFORM OVL-SEC.
           ALTER SW TO PROCEED TO B-PARA.
           PERFORM OVL-SEC.
           STOP RUN.
       OVL-SEC SECTION 20.
       SW.
           GO TO A-PARA.
       A-PARA.
           DISPLAY "A".
           GO TO OVL-EXIT.
       B-PARA.
           DISPLAY "B".
       OVL-EXIT.
           EXIT.
