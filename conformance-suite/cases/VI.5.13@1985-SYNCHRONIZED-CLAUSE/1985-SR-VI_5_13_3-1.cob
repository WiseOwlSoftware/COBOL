      *>--- conformance-case
      *> id: 1985-SR-VI_5_13_3-1
      *> rules: [1985-SR-VI.5.13.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.13.3 SR1 (THE SYNCHRONIZED CLAUSE). A refusal: the
      *> rule forbids line 6 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0900 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SYNCTEST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  GRP SYNCHRONIZED.
000700     05  FLD PIC 9(4) VALUE 1234.
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     DISPLAY FLD.
001100     STOP RUN.
