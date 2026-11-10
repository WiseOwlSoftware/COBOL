      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-1
      *> rules: [1985-SR-XIV.2.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR1 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 6 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL0001 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CDTEST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 CD  CMI FOR INPUT.
000700 01  CMI-REC PIC X(87).
000800 PROCEDURE DIVISION.
000900 P1.
001000     DISPLAY "OK".
001100     STOP RUN.
