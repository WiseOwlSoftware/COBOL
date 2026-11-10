      *>--- conformance-case
      *> id: 1985-SR-VI_6_27_3-2
      *> rules: [1985-SR-VI.6.27.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.3 SR2 (THE UNSTRING STATEMENT). A refusal: the
      *> rule forbids line 11 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1651 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. UNS1.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  WS-NUM PIC 9(4) VALUE 1234.
000700 01  WS-ALP PIC X(4) VALUE "1234".
000800 01  WS-OUT PIC X(4).
000900 PROCEDURE DIVISION.
001000 P1.
001100     UNSTRING WS-NUM DELIMITED BY "," INTO WS-OUT.
001200     DISPLAY WS-OUT.
001300     STOP RUN.
