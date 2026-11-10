      *>--- conformance-case
      *> id: 1985-GR-VI_6_11_4-5
      *> rules: [1985-GR-VI.6.11.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.4 GR5 (THE DIVIDE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVFIVE.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 17.
       01 B PIC 9 VALUE 5.
       01 Q PIC 99.
       01 R PIC 99.
       PROCEDURE DIVISION.
       P1.
           DIVIDE A BY B GIVING Q REMAINDER R.
           DISPLAY Q.
           DISPLAY R.
           STOP RUN.
