      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-10
      *> rules: [1985-GR-XII.3.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR10 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REPL10.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       REPLACE ==SHOW-IT== BY ==DISPLAY
                   "A  B" "C"==.
       P1.
           SHOW-IT SHOW-IT SHOW-IT SHOW-IT SHOW-IT SHOW-IT SHOW-IT.
           STOP RUN.
