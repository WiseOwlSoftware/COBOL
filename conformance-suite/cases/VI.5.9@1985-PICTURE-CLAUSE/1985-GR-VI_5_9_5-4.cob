      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_5-4
      *> rules: [1985-GR-VI.5.9.5-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR4 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIMPINS.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E-SPACE   PIC 99B99B99.
       01  E-SLANT   PIC 99/99/99.
       01  E-ZERO    PIC 9990.
       01  E-COMMA   PIC 9,999.
       01  E-LASTC   PIC 999,.
       PROCEDURE DIVISION.
       P1.
           MOVE 123456 TO E-SPACE.
           MOVE 251231 TO E-SLANT.
           MOVE 123 TO E-ZERO.
           MOVE 1234 TO E-COMMA.
           MOVE 123 TO E-LASTC.
           DISPLAY E-SPACE.
           DISPLAY E-SLANT.
           DISPLAY E-ZERO.
           DISPLAY E-COMMA.
           DISPLAY E-LASTC.
           STOP RUN.
