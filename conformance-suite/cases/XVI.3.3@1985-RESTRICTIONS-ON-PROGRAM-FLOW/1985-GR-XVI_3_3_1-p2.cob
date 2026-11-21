      *>--- conformance-case
      *> id: 1985-GR-XVI_3_3_1-p2
      *> rules: [1985-GR-XVI.3.3.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.1 p2 (RESTRICTIONS ON PROGRAM FLOW). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p10: other uses of ALTER are valid and performed (1985
      * XVI.3.3.1 p2): SEGMENT-LIMIT IS 10; SECTION 0 alters a GO TO
      * in fixed overlayable SECTION 20; independent SECTION 60
      * alters a GO TO in fixed permanent SECTION 5 and one in fixed
      * overlayable SECTION 20. Expect: O-ALT1 P-ALT O-ALT2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P10.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X SEGMENT-LIMIT IS 10.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           ALTER OS TO PROCEED TO OA1.
           PERFORM OVL-SEC.
           PERFORM IND-SEC.
           PERFORM PERM-SEC.
           PERFORM OVL-SEC.
           STOP RUN.
       PERM-SEC SECTION 5.
       PS.
           GO TO PN.
       PN.
           DISPLAY "P-INIT".
           GO TO PX.
       PA.
           DISPLAY "P-ALT".
       PX.
           EXIT.
       OVL-SEC SECTION 20.
       OS.
           GO TO ON1.
       ON1.
           DISPLAY "O-INIT".
           GO TO OX.
       OA1.
           DISPLAY "O-ALT1".
           GO TO OX.
       OA2.
           DISPLAY "O-ALT2".
       OX.
           EXIT.
       IND-SEC SECTION 60.
       I-1.
           ALTER PS TO PROCEED TO PA.
           ALTER OS TO PROCEED TO OA2.
