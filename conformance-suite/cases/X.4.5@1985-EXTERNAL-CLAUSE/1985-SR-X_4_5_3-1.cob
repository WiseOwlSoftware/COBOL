      *>--- conformance-case
      *> id: 1985-SR-X_4_5_3-1
      *> rules: [1985-SR-X.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.3 SR1 (THE EXTERNAL CLAUSE). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2404 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. EXTLNK.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  W-ITEM PIC X(4) VALUE "OK".
000600 LINKAGE SECTION.
000700 01  L-ITEM EXTERNAL PIC X(4).
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     DISPLAY W-ITEM.
001100     STOP RUN.
