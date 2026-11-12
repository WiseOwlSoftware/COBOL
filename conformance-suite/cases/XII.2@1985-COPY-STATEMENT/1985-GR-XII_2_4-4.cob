      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-4
      *> rules: [1985-GR-XII.2.4-4]
      *> accept: [85]
      *> files: [LBTX44.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR4 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * REQUIRED LIBRARY TEXT LBTX44 (ONE LINE, AREA B):
      *    DISPLAY "OLD" WS-A WS-AX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A  PIC X VALUE "A".
       01  WS-B  PIC X VALUE "B".
       01  WS-AX PIC X VALUE "X".
       PROCEDURE DIVISION.
       P1.
           COPY LBTX44 REPLACING "OLD" BY "NEW" WS-A BY WS-B.
           STOP RUN.
