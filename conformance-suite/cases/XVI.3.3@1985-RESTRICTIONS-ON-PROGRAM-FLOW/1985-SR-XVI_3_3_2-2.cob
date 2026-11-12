      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_2-2
      *> rules: [1985-SR-XVI.3.3.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.2 SR2 (RESTRICTIONS ON PROGRAM FLOW). A refusal:
      *> the rule forbids line 8 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL6877 there; a compiler that accepts
      *> it fails. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGPRF.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           PERFORM A-SEC THRU B-SEC.
           DISPLAY "DONE".
           STOP RUN.
       A-SEC SECTION 60.
       A-1.
           DISPLAY "A".
       B-SEC SECTION 70.
       B-1.
           DISPLAY "B".
