      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-10
      *> rules: [1985-SR-XII.2.3-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR10 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the audit's draft, confirmed; run with
      *> its library texts (drafts-85n, kb/Work PB5067) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYCMT.
       AUTHOR. COPY NOSUCHTEXT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COPY TREATED AS COMMENT".
           STOP RUN.
