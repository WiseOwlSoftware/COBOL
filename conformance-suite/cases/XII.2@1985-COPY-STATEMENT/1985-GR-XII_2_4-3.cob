      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-3
      *> rules: [1985-GR-XII.2.4-3]
      *> accept: [85]
      *> files: [LIBTXT.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR3 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * LIBRARY TEXT LIBTXT (SEPARATE LIBRARY MEMBER) IS THE LINE:
      *            DISPLAY "OLD".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           COPY LIBTXT REPLACING "OLD" BY "NEW".
           STOP RUN.
