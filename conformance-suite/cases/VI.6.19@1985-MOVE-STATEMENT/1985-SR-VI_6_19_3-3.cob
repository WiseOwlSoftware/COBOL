      *>--- conformance-case
      *> id: 1985-SR-VI_6_19_3-3
      *> rules: [1985-SR-VI.6.19.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.19.3 SR3 (THE MOVE STATEMENT). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MCORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GRP-A.
           05 FLD-X PIC X(3) VALUE "ABC".
       01 GRP-B.
           05 FLD-X PIC X(3).
       01 ELEM-C PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE CORRESPONDING GRP-A TO ELEM-C.
           STOP RUN.
