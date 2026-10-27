      *>--- conformance-case
      *> id: pb3846-value-list-all-zero-signed
      *> rules: [SR-8.3.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3846. ISO 8.3.3.6.3 SR1 a): "If the literal is
      *> restricted to a numeric literal, the only figurative constant
      *> permitted is ZERO (ZEROS, ZEROES) without the ALL phrase"
      *> (cite.py --check 8.3.3.6.3 -> OK), and 13.18.63.3 SR2 restricts
      *> each literal of a numeric subject's VALUE clause to a numeric
      *> literal. Here a signed literal follows ALL ZERO in the level-88
      *> list, so the ZERO reaches the VALUE clause as the arithmetic
      *> zero the token rewriter marks; it is still the figurative ALL
      *> ZERO and is barred from 2002 as VALUE ALL ZERO is (kb/Work PB1415,
      *> pb1415-value-all-zero-numeric). Accepted at 85 by
      *> pb3846_all_zero_in_expression_85. Before the fix it parsed only
      *> after PB3846's grammar arm, and then failed in the C# backend
      *> (CS0103 'ALL0L').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3846V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V    PIC S9 VALUE 0.
          88 C VALUE ALL ZERO -5.
       PROCEDURE DIVISION.
       MAIN-P.
           IF C
               DISPLAY "C TRUE"
           END-IF
           STOP RUN.
