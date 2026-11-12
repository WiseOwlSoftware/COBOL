      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-10
      *> rules: [1985-GR-XII.2.4-10]
      *> accept: [85]
      *> files: [LIBP.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR10 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYGR10.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ALPHA-FIELD-WITH-LONG-NAME  PIC X(5) VALUE "HELLO".
       01  WS-BETA-FIELD-WITH-A-LONG-NAME PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY LIBP REPLACING ==TAG== BY
               ==DISPLAY WS-ALPHA-FIELD-WITH-LONG-NAME DISPLAY WS-BETA-
      -          FIELD-WITH-A-LONG-NAME DISPLAY "DONE"==.
           STOP RUN.
