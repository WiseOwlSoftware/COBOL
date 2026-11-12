      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-9
      *> rules: [1985-GR-XII.2.4-9]
      *> accept: [85]
      *> files: [CPYLIB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR9 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CPYAREA.
000600 ENVIRONMENT DIVISION.
000700 DATA DIVISION.
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     PERFORM PARA-A.
001100     STOP RUN.
001200     COPY CPYLIB.
