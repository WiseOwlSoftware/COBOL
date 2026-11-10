      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-4
      *> rules: [1985-SR-XV.3.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.3 SR4 (THE USE FOR DEBUGGING STATEMENT). A
      *> refusal: the rule forbids line 11 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2376 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. DBGT.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SOURCE-COMPUTER. COMPUTER-X WITH DEBUGGING MODE.
000600 PROCEDURE DIVISION.
000700 DECLARATIVES.
000800 D1 SECTION.
000900     USE FOR DEBUGGING ON P1.
001000 D1-PARA.
001100     GO TO D2-PARA.
001200 D2 SECTION.
001300     USE FOR DEBUGGING ON P2.
001400 D2-PARA.
001500     EXIT.
001600 END DECLARATIVES.
001700 MAIN SECTION.
001800 P1.
001900     DISPLAY "A".
002000 P2.
002100     STOP RUN.
