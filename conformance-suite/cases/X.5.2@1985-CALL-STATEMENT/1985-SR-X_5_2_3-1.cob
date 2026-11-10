      *>--- conformance-case
      *> id: 1985-SR-X_5_2_3-1
      *> rules: [1985-SR-X.5.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.3 SR1 (THE CALL STATEMENT). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CALLTST.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     CALL 123.
000600     STOP RUN.
