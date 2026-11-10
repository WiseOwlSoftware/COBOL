      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_2_3-4
      *> rules: [1985-SR-IV.4.3.8.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.3 SR4 (Subscripting). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2270 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 3 TIMES.
       01  X PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE ELEM TO X.
           STOP RUN.
