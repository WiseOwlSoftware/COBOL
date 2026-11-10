      *>--- conformance-case
      *> id: 1985-SR-XIV_3_4_3-1
      *> rules: [1985-SR-XIV.3.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.4.3 SR1 (THE PURGE STATEMENT). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6170 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PRGTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 COMMUNICATION SECTION.
000600 CD IN-CD FOR INPUT SYMBOLIC QUEUE IS IN-Q.
000700 CD OUT-CD FOR OUTPUT STATUS KEY IS OUT-ST.
000800 PROCEDURE DIVISION.
000900 P1.
001000     PURGE IN-CD.
001100     STOP RUN.
