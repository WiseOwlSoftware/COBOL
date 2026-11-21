      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_1-p1
      *> rules: [1985-SR-XVI.3.3.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.1 p1 (RESTRICTIONS ON PROGRAM FLOW). A refusal:
      *> the rule forbids line 8 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL6877 there; a compiler that accepts
      *> it fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p09: an ALTER in SECTION 0 of a GO TO in SECTION 60
      * (1985 XVI.3.3.1 p1: must not). Control: s19_p09_alter_ok.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P09B.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           ALTER SW TO PROCEED TO I-ALT.
           PERFORM IND-SEC.
           STOP RUN.
       IND-SEC SECTION 60.
       I-0.
           DISPLAY "I-0".
       SW.
           GO TO I-NORM.
       I-NORM.
           DISPLAY "NORMAL".
           GO TO I-X.
       I-ALT.
           DISPLAY "ALTERED".
       I-X.
           EXIT.
