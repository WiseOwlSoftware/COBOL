      *>--- conformance-case
      *> id: 1985-GR-XVI_1_4_3-1
      *> rules: [1985-GR-XVI.1.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.4.3 GR1 (ORGANIZATION). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the 1985 audit's drafts pass (a draft that
      *> mismatched on an older build, re-run to MATCH with its
      *> expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-S SECTION 10.
       M1.
           MOVE 0 TO CNT.
       BACK-S SECTION 10.
       B1.
           DISPLAY "BACK".
       IND-S SECTION 50.
       I1.
           ADD 1 TO CNT.
           IF CNT > 2 GO TO F1.
       I2.
           GO TO I3.
       I3.
           DISPLAY "INITIAL".
           ALTER I2 TO PROCEED TO I4.
           GO TO I2.
       I4.
           DISPLAY "ALTERED".
           GO TO B1.
       FIN-S SECTION 10.
       F1.
           STOP RUN.
