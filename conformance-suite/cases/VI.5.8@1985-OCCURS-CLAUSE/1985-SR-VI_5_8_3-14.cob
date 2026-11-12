      *>--- conformance-case
      *> id: 1985-SR-VI_5_8_3-14
      *> rules: [1985-SR-VI.5.8.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR14 (THE OCCURS CLAUSE). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2692 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. OCCIX.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  IX PIC 9.
000600 01  TAB.
000700     05  ELT PIC X OCCURS 3 TIMES INDEXED BY IX.
000800 PROCEDURE DIVISION.
000900 P1.
001000     DISPLAY "OK".
001100     STOP RUN.
