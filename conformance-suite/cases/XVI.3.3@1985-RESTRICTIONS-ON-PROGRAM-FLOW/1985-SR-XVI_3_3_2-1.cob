      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_2-1
      *> rules: [1985-SR-XVI.3.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.2 SR1 (RESTRICTIONS ON PROGRAM FLOW). A refusal:
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
       MAIN-SEC SECTION 10.
       MAIN-PARA.
           PERFORM WORK-SEC THRU WORK2-SEC.
           DISPLAY "DONE".
           STOP RUN.
       WORK-SEC SECTION 20.
       W1.
           DISPLAY "W1".
       WORK2-SEC SECTION 60.
       W2.
           DISPLAY "W2".
