      *>--- conformance-case
      *> id: 1985-GR-X_5_1-4
      *> rules: [1985-GR-X.5.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 GR4 (PROCEDURE DIVISION IN THE INTER-PROGRAM
      *> COMMUNICATION MODULE). A refusal: the rule forbids line 10 of
      *> the program below, and WiseOwl refuses it at 85 with COBOL2746
      *> there; a compiler that accepts it fails. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SUBPGM.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 LINKAGE SECTION.
000600 01  LK-A PIC X(4).
000700 01  LK-B PIC X(4).
000800 PROCEDURE DIVISION USING LK-A.
000900 P1.
001000     MOVE "ABCD" TO LK-B.
001100     EXIT PROGRAM.
