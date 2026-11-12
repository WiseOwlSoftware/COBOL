      *>--- conformance-case
      *> id: 1985-FMT-XII_2_2
      *> rules: [1985-FMT-XII.2.2]
      *> files: [TEXTA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.2 (THE COPY STATEMENT). A refusal: the rule forbids
      *> line 9 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL2449 there; a compiler that accepts it fails. Placed from
      *> written and run by the triage of the drafts that did not match
      *> (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CPYTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  Y PIC X VALUE "N".
000700 PROCEDURE DIVISION.
000800 P1.
000900     COPY TEXTA REPLACING ==X== ==Y==.
001000     DISPLAY Y.
001100     STOP RUN.
