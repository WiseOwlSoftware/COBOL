      *>--- conformance-case
      *> id: 1985-SR-XIV_3_2_3-2
      *> rules: [1985-SR-XIV.3.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.3 SR2 (THE DISABLE STATEMENT). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6170 there; a compiler that accepts it
      *> fails. Placed from written and run by the triage of the drafts
      *> that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DISTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 COMMUNICATION SECTION.
000600 CD  CD-IN FOR INPUT STATUS KEY IS IN-ST.
000700 CD  CD-IO FOR I-O STATUS KEY IS IO-ST.
000800 PROCEDURE DIVISION.
000900 P1.
001000     DISABLE I-O TERMINAL CD-IN.
001100     STOP RUN.
