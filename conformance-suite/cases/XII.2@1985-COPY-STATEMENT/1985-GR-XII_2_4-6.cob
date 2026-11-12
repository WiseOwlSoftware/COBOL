      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-6
      *> rules: [1985-GR-XII.2.4-6]
      *> accept: [85]
      *> files: [LB46.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR6 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           COPY LB46 REPLACING ==MOVE 1
      *    PSEUDO-TEXT COMMENT

               TO WS-X== BY ==MOVE 2 TO WS-X==.
           DISPLAY WS-X.
           STOP RUN.
