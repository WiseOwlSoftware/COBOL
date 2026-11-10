      *>--- conformance-case
      *> id: 1985-SR-XIV_3_1_3-1
      *> rules: [1985-SR-XIV.3.1.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.1.3 SR1 (THE ACCEPT MESSAGE COUNT STATEMENT). A
      *> refusal: the rule forbids line 10 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6170 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ACCMC.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 COMMUNICATION SECTION.
000600 CD  CDIN FOR INPUT MESSAGE COUNT IS IN-CNT.
000700 CD  CDOUT FOR OUTPUT STATUS KEY IS OUT-ST.
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     ACCEPT CDOUT MESSAGE COUNT.
001100     STOP RUN.
