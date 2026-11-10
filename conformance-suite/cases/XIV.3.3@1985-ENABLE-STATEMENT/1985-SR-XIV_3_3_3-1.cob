      *>--- conformance-case
      *> id: 1985-SR-XIV_3_3_3-1
      *> rules: [1985-SR-XIV.3.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.3.3 SR1 (THE ENABLE STATEMENT). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6170 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ENBTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 COMMUNICATION SECTION.
000600 CD  CDOUT FOR OUTPUT.
000700 01  CDOUT-REC PIC X(23).
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     ENABLE INPUT CDOUT WITH KEY "PASSWORD".
001100     STOP RUN.
