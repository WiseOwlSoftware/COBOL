      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_2-L2_2
      *> rules: [1985-SR-XVI.3.3.2-L2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.2 SRL2.2 (RESTRICTIONS ON PROGRAM FLOW). A
      *> refusal: the rule forbids line 12 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6877 there; a compiler that
      *> accepts it fails. Placed from written and run by the
      *> confirmation pass (the redraft of a WEAK or WRONG record, whose
      *> filed defect was fixed; re-run to MATCH with its expectation
      *> unchanged, witnesses-85g) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGPRF.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           GO TO A-1.
       IND-A SECTION 50.
       A-1.
           DISPLAY "IN A".
           PERFORM IND-B.
           DISPLAY "BACK IN A".
           STOP RUN.
       IND-B SECTION 60.
       B-1.
           DISPLAY "IN B".
