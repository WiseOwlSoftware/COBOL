      *>--- conformance-case
      *> id: 1985-FMT-VI_6_10_2
      *> rules: [1985-FMT-VI.6.10.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.10.2 (THE DISPLAY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISPTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           DISPLAY WS-A "DEF" WS-A.
           STOP RUN.
