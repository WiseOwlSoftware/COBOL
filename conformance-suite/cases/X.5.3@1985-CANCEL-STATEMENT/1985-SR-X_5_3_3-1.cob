      *>--- conformance-case
      *> id: 1985-SR-X_5_3_3-1
      *> rules: [1985-SR-X.5.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.3 SR1 (THE CANCEL STATEMENT). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBPGM".
           CANCEL 12.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       PROCEDURE DIVISION.
       SUB-PARA.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
       END PROGRAM MAINPGM.
