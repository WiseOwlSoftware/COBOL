      *>--- conformance-case
      *> id: 1985-GR-VI_5_14_4-6
      *> rules: [1985-GR-VI.5.14.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR6 (THE USAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USGDSP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  GRP-NUM  PIC 9(3) VALUE 123.
           05  GRP-ALF  PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY GRP.
           STOP RUN.
