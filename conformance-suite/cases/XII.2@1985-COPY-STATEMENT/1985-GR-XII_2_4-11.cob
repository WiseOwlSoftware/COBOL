      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-11
      *> rules: [1985-GR-XII.2.4-11]
      *> accept: [85]
      *> files: [TXT11.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR11 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * LIBRARY TEXT TXT11 (ONE LINE, AREA B):  DISPLAY XX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           COPY TXT11 REPLACING ==XX== BY
           =="12345678901234567890123456789012345678901234567890ABCDEFGH
      -    "IJKLMNOP"==.
           STOP RUN.
