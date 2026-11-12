      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-10
      *> rules: [1985-SR-XIV.2.2.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR10 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 8 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985
      *> audit's drafts pass (a draft that mismatched on an older build,
      *> re-run to MATCH with its expectation unchanged, witnesses-85g)
      *> by scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CDTEST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 COMMUNICATION SECTION.
000600 CD  OUT-CD FOR OUTPUT
000700         TEXT LENGTH IS CD-LEN
000800         STATUS KEY IS CD-LEN.
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     DISPLAY "OK".
001200     STOP RUN.
