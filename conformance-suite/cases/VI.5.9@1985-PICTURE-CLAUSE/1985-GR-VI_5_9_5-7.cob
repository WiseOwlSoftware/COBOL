      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_5-7
      *> rules: [1985-GR-VI.5.9.5-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR7 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F-CUR   PIC $$$$.99.
       01  F-EMB   PIC $$,$$$.99.
       01  F-MIN   PIC ----.99.
       01  F-PLS   PIC ++,+++.++.
       01  F-ALL   PIC $$$$.$$.
       01  F-CM    PIC $$$$-.
       01  F-CP    PIC $$$$+.
       01  F-CCR   PIC $$$$CR.
       01  F-CDB   PIC $$$$DB.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12.34 TO F-CUR.
           DISPLAY "A1[" F-CUR "]".
           MOVE .05 TO F-CUR.
           DISPLAY "A2[" F-CUR "]".
           MOVE 12345.67 TO F-CUR.
           DISPLAY "A3[" F-CUR "]".
           MOVE 1234 TO F-EMB.
           DISPLAY "B1[" F-EMB "]".
           MOVE 12 TO F-EMB.
           DISPLAY "B2[" F-EMB "]".
           MOVE -1.5 TO F-MIN.
           DISPLAY "C1[" F-MIN "]".
           MOVE 1.5 TO F-MIN.
           DISPLAY "C2[" F-MIN "]".
           MOVE -.05 TO F-MIN.
           DISPLAY "C3[" F-MIN "]".
           MOVE 1234.5 TO F-PLS.
           DISPLAY "D1[" F-PLS "]".
           MOVE 234.5 TO F-PLS.
           DISPLAY "D2[" F-PLS "]".
           MOVE -12.3 TO F-PLS.
           DISPLAY "D3[" F-PLS "]".
           MOVE .07 TO F-PLS.
           DISPLAY "D4[" F-PLS "]".
           MOVE ZERO TO F-PLS.
           DISPLAY "D5[" F-PLS "]".
           MOVE 1.5 TO F-ALL.
           DISPLAY "E1[" F-ALL "]".
           MOVE ZERO TO F-ALL.
           DISPLAY "E2[" F-ALL "]".
           MOVE -123 TO F-CM.
           DISPLAY "F1[" F-CM "]".
           MOVE 45 TO F-CM.
           DISPLAY "F2[" F-CM "]".
           MOVE ZERO TO F-CM.
           DISPLAY "F3[" F-CM "]".
           MOVE -5 TO F-CP.
           DISPLAY "G1[" F-CP "]".
           MOVE 5 TO F-CP.
           DISPLAY "G2[" F-CP "]".
           MOVE -67 TO F-CCR.
           DISPLAY "H1[" F-CCR "]".
           MOVE 67 TO F-CCR.
           DISPLAY "H2[" F-CCR "]".
           MOVE -8 TO F-CDB.
           DISPLAY "I1[" F-CDB "]".
           MOVE 8 TO F-CDB.
           DISPLAY "I2[" F-CDB "]".
           STOP RUN.
