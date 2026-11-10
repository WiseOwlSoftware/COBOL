      *>--- conformance-case
      *> id: 1985-SR-X_5_4_3-2
      *> rules: [1985-SR-X.5.4.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.3 SR2 (THE EXIT PROGRAM STATEMENT). A refusal: the
      *> rule forbids line 8 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2102 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. GLOBEX.
000300 PROCEDURE DIVISION.
000400 DECLARATIVES.
000500 ERR-SECT SECTION.
000600     USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON INPUT.
000700 ERR-PARA.
000800     EXIT PROGRAM.
000900 END DECLARATIVES.
001000 MAIN-SECT SECTION.
001100 MAIN-PARA.
001200     DISPLAY "OK".
001300     STOP RUN.
