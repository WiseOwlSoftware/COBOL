      *>--- conformance-case
      *> id: 1985-GR-VI_6_2_2-T1_1
      *> rules: [1985-GR-VI.6.2.2-T1.1, 1985-GR-VI.6.2.2-T1.2, 1985-GR-VI.6.2.2-T1.3, 1985-GR-VI.6.2.2-T1.4, 1985-GR-VI.6.2.2-T1.5, 1985-GR-VI.6.2.2-T2.1, 1985-GR-VI.6.2.2-T2.2, 1985-SR-VI.6.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.2.2 Table 1 row 1; §1985:VI.6.2.2 Table 1 row 2;
      *> §1985:VI.6.2.2 Table 1 row 3; §1985:VI.6.2.2 Table 1 row 4;
      *> §1985:VI.6.2.2 Table 1 row 5; §1985:VI.6.2.2 Table 2 row 1;
      *> §1985:VI.6.2.2 Table 2 row 2; §1985:VI.6.2.1 p1 (ARITHMETIC
      *> EXPRESSIONS). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: every binary operator + - * / ** and both unary
      * operators of VI.6.2.2, the figurative constant ZERO as an
      * operand (VI.6.2.1), parentheses. A = 7, B = 2. Expect:
      * +0009.00 +0005.00 +0014.00 +0003.50 +0049.00 +0007.00
      * -0007.00 +0007.00 +0045.00 +0009.00 +0004.00
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09ARI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC S9(4) VALUE 7.
       77  B PIC S9(4) VALUE 2.
       77  R PIC S9(4)V99.
       77  D PIC +9(4).99.
       PROCEDURE DIVISION.
       P0.
           COMPUTE R = A + B.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = A - B.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = A * B.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = A / B.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = A ** B.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = + A.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = - A.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = ZERO + A.
           MOVE R TO D. DISPLAY D.
           COMPUTE R = ( A + B ) * ( A - B ).
           MOVE R TO D. DISPLAY D.
           COMPUTE R = A - ( - B ).
           MOVE R TO D. DISPLAY D.
           COMPUTE R = - B ** 2.
           MOVE R TO D. DISPLAY D.
           STOP RUN.
