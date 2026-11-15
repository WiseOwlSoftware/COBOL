      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_3-3-usage-index
      *> rules: [1985-GR-VI.6.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.3 GR3 (THE CORRESPONDING PHRASE): "The description of
      *> D1 and D2 must not contain level-number 66, 77, or 88 or the USAGE
      *> IS INDEX clause." The group GI below is described USAGE IS INDEX
      *> (the clause on a group applies to every item under it), and it is
      *> the receiving group of an ADD CORRESPONDING and the sending group
      *> of a SUBTRACT CORRESPONDING, the two arithmetic spellings of the
      *> rule. WiseOwl refuses each at 85 with COBOL1757; a compiler that
      *> accepts either one fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CORRIXG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GI USAGE IS INDEX.
           05  X1.
       01  G2.
           05  X1 PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           ADD CORRESPONDING G2 TO GI.
           SUBTRACT CORRESPONDING GI FROM G2.
           STOP RUN.
