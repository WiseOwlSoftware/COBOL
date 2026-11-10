      *>--- conformance-case
      *> id: 1985-GR-VI_2_4_4-2
      *> rules: [1985-GR-VI.2.4.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.4.4 GR2 (END PROGRAM HEADER). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0001 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PROG1.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     DISPLAY "HELLO".
000600     STOP RUN.
000700 END PROGRAM PROG1.
000800     DISPLAY "EXTRA".
