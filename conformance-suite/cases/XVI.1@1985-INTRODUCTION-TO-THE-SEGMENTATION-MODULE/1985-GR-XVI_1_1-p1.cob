      *>--- conformance-case
      *> id: 1985-GR-XVI_1_1-p1
      *> rules: [1985-GR-XVI.1.1-p1, 1985-GR-XVI.1.4.2-p1, 1985-GR-XVI.1.4.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.1 p1; §1985:XVI.1.4.2 p1; §1985:XVI.1.4.2 p2
      *> (INTRODUCTION TO THE SEGMENTATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p07: the fixed portion (1985 XVI.1.4.2 p1, p2). With
      * SEGMENT-LIMIT IS 10, SECTION 5 is fixed permanent and
      * SECTION 20 fixed overlayable; each is always made available
      * in its last used state, also after independent SECTION 60
      * has run. Expect: P-INIT O-INIT I-INIT P-ALT O-ALT I-INIT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X SEGMENT-LIMIT IS 10.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM PERM-SEC.
           PERFORM OVL-SEC.
           PERFORM IND-SEC.
           PERFORM PERM-SEC.
           PERFORM OVL-SEC.
           PERFORM IND-SEC.
           STOP RUN.
       PERM-SEC SECTION 5.
       PS.
           GO TO PN.
       PN.
           DISPLAY "P-INIT".
           ALTER PS TO PROCEED TO PA.
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
           ALTER OS TO PROCEED TO OA.
           GO TO OX.
       OA.
           DISPLAY "O-ALT".
       OX.
           EXIT.
       IND-SEC SECTION 60.
       IS1.
           GO TO IN1.
       IN1.
           DISPLAY "I-INIT".
           ALTER IS1 TO PROCEED TO IA.
           GO TO IX.
       IA.
           DISPLAY "I-ALT".
       IX.
           EXIT.
