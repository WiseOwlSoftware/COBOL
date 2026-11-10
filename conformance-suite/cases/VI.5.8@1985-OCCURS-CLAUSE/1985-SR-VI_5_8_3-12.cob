      *>--- conformance-case
      *> id: 1985-SR-VI_5_8_3-12
      *> rules: [1985-SR-VI.5.8.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR12 (THE OCCURS CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2787 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. OCCKEY.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  T.
000600     05  E OCCURS 3 TIMES ASCENDING KEY IS K INDEXED BY I.
000700         10  K2 PIC 9.
000800         10  G OCCURS 2 TIMES.
000900             15  K PIC 9.
001000 PROCEDURE DIVISION.
001100 P1.
001200     DISPLAY "X".
001300     STOP RUN.
