      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-1
      *> rules: [1985-SR-XII.2.3-1]
      *> accept: [85]
      *> files: [MSGTXT.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR1 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CPYUNQ.
000300*    ASSUMES ONE COBOL LIBRARY IS AVAILABLE. IT HOLDS EXACTLY ONE
000400*    TEXT NAMED MSGTXT, WHOSE ENTIRE CONTENT IS THE LINE:
000500*               DISPLAY "COPIED TEXT"
000600 ENVIRONMENT DIVISION.
000700 DATA DIVISION.
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     COPY MSGTXT.
001100     STOP RUN.
