      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_2_3-L3_1
      *> rules: [1985-SR-IV.4.2.2.2.3-L3.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 SRL3.1 (Literals). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0844 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LITTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
       P1.
           ADD SPACE TO X.
           DISPLAY X.
           STOP RUN.
