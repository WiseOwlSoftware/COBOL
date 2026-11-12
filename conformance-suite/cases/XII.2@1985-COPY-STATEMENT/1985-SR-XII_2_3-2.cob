      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-2
      *> rules: [1985-SR-XII.2.3-2]
      *> files: [PDTEXT.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR2 (THE COPY STATEMENT). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2449 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected); run with its library
      *> texts (drafts-85n, kb/Work PB5067) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PDTEXT DISPLAY "COPY OK".
           STOP RUN.
