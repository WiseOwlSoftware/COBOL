      *>--- conformance-case
      *> id: 1985-GR-VI_6_25_4-4
      *> rules: [1985-GR-VI.6.25.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR4 (THE STRING STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R PIC X(10) VALUE "XXXXXXXXXX".
       01  S PIC X(5) VALUE "AB*CD".
       PROCEDURE DIVISION.
       P1.
           STRING S DELIMITED BY "*"
                  "EFG" DELIMITED BY SIZE
               INTO R.
           DISPLAY R.
           STOP RUN.
