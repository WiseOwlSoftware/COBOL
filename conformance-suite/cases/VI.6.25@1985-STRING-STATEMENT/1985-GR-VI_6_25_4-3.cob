      *>--- conformance-case
      *> id: 1985-GR-VI_6_25_4-3
      *> rules: [1985-GR-VI.6.25.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR3 (THE STRING STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. STRFIG.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WS-OUT PIC X(10) VALUE ALL "-".
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     STRING "AB" ZEROS "CD" DELIMITED BY SIZE INTO WS-OUT.
000900     DISPLAY WS-OUT.
001000     STOP RUN.
