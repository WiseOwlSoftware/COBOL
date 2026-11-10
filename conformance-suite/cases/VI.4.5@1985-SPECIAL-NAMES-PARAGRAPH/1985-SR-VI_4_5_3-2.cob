      *>--- conformance-case
      *> id: 1985-SR-VI_4_5_3-2
      *> rules: [1985-SR-VI.4.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.3 SR2 (THE SPECIAL-NAMES PARAGRAPH). A refusal:
      *> the rule forbids line 6 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2241 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SNTEST.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SPECIAL-NAMES.
000600     CONSOLE IS CONS ON STATUS IS CONS-ON.
000700 PROCEDURE DIVISION.
000800 P1.
000900     DISPLAY "OK" UPON CONS.
001000     STOP RUN.
