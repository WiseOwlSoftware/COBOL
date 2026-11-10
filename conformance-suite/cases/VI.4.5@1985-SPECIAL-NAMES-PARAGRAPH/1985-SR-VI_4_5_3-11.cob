      *>--- conformance-case
      *> id: 1985-SR-VI_4_5_3-11
      *> rules: [1985-SR-VI.4.5.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.3 SR11 (THE SPECIAL-NAMES PARAGRAPH). A refusal:
      *> the rule forbids line 6 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL1671 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CLSTST.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SPECIAL-NAMES.
000600     CLASS MYCLASS IS "AB" THROUGH "Z".
000700 DATA DIVISION.
000800 WORKING-STORAGE SECTION.
000900 01  X PIC X VALUE "B".
001000 PROCEDURE DIVISION.
001100 P1.
001200     DISPLAY "OK".
001300     STOP RUN.
