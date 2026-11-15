      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_3-3
      *> rules: [1985-GR-VI.6.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.3 GR3 (THE CORRESPONDING PHRASE): "The description of
      *> D1 and D2 must not contain level-number 66, 77, or 88 or the USAGE
      *> IS INDEX clause." The rule governs MOVE, ADD and SUBTRACT
      *> CORRESPONDING alike. This program MOVEs CORRESPONDING a level-66
      *> RENAMES (a group item, §1985:VI.5.11.4 GR1) to a group, which the
      *> rule forbids, and also MOVEs CORRESPONDING a group to a level-66
      *> receiver. WiseOwl refuses each at 85 with COBOL1757; a compiler
      *> that accepts either one fails. The control, G1 in place of R, is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CORR66G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  A PIC X VALUE "P".
           05  B PIC X VALUE "S".
       66  R RENAMES A OF G1 THRU B OF G1.
       01  G2.
           05  A PIC X VALUE "R".
       PROCEDURE DIVISION.
       P1.
           MOVE CORRESPONDING G1 TO G2.
           MOVE CORRESPONDING R TO G2.
           MOVE CORRESPONDING G2 TO R.
           DISPLAY A OF G2.
           STOP RUN.
