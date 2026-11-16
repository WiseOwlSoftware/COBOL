      *>--- conformance-case
      *> id: g85_f14_03
      *> rules: [1985-GR-VI.6.26.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.4 GR4 - 'enough places are carried so as not to
      *> lose significant digits'. L1 (format 1, §1985:VI.6.26.4 GR1):
      *> the sum 9+9=18 needs two places though each operand has one;
      *> 20-18=2 prints 02, a one-place temporary gives 12. L2: the sum
      *> 0.5+0.25=0.75 needs two decimal places though D has one;
      *> 1.00-0.75 = 0.25 (shown through PIC 9.99), a sum kept at D's
      *> scale gives 0.30.
      *> L3 (format 2, §1985:VI.6.26.4 GR2): 1000-999=1 into PIC 9; an
      *> intermediate held at the receiver's one place loses 1000. L4:
      *> 100-(9+9)=82 into PIC 99.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1403.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A  PIC 9    VALUE 9.
       77  B  PIC 9    VALUE 9.
       77  C  PIC 99   VALUE 20.
       77  D  PIC 9V9  VALUE 0.5.
       77  E  PIC V99  VALUE .25.
       77  F  PIC 9V99 VALUE 1.
       77  G  PIC 9.
       77  H  PIC 99.
       77  FE PIC 9.99.
       PROCEDURE DIVISION.
       P1.
           SUBTRACT A B FROM C.
           DISPLAY "L1 " C.
           SUBTRACT D E FROM F.
           MOVE F TO FE.
           DISPLAY "L2 " FE.
           SUBTRACT 999 FROM 1000 GIVING G.
           DISPLAY "L3 " G.
           SUBTRACT A B FROM 100 GIVING H.
           DISPLAY "L4 " H.
           STOP RUN.
