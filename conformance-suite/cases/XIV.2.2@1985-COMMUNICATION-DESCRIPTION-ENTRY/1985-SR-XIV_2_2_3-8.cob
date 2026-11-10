      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-8
      *> rules: [1985-SR-XIV.2.2.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR8 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 5 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from the confirmation pass's redraft
      *> (WEAK original) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CDTEST.
000300 DATA DIVISION.
000400 COMMUNICATION SECTION.
000500 CD CD-OUT FOR OUTPUT.
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     STOP RUN.
