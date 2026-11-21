      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_1-p1
      *> rules: [1985-GR-VI.5.9.1-p1, 1985-SR-VI.5.9.6-p1, 1985-SR-VI.5.9.6-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.1 p1; §1985:VI.5.9.6 p1; §1985:VI.5.9.6 p3 (THE
      *> PICTURE CLAUSE). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: PICTURE symbols in their right-of-the-decimal-point
      * uses (VI.5.9.6 precedence chart): Z, *, cs and - floating to the
      * right of the point, P right of V, nonfloating + and - at either
      * end. Expect:
      * [  1.50][      ][**1.50][***.**][ $1.50][ -1.50][1200][.005]
      * (DISPLAY of the P items is implementor-defined, VI.6.10.4 GR1)
      * [1.50-][-1.50]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09PIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  E1 PIC ZZZ.ZZ.
       77  E1Z PIC ZZZ.ZZ.
       77  E2 PIC ***.**.
       77  E2Z PIC ***.**.
       77  E3 PIC $$$.$$.
       77  E4 PIC ---.--.
       77  P1 PIC 99PP.
       77  P2 PIC VPP9.
       77  E5 PIC 9.99+.
       77  E6 PIC -9.99.
       PROCEDURE DIVISION.
       P0.
           MOVE 1.5 TO E1.
           MOVE 0 TO E1Z.
           MOVE 1.5 TO E2.
           MOVE 0 TO E2Z.
           MOVE 1.5 TO E3.
           MOVE -1.5 TO E4.
           MOVE 1234 TO P1.
           MOVE .005 TO P2.
           MOVE -1.5 TO E5.
           MOVE -1.5 TO E6.
           DISPLAY "[" E1 "][" E1Z "][" E2 "][" E2Z "][" E3 "][" E4
               "][" P1 "][" P2 "]".
           DISPLAY "[" E5 "][" E6 "]".
           STOP RUN.
