      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-7
      *> rules: [1985-SR-XII.2.3-7]
      *> files: [TXT37.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR7 (THE COPY STATEMENT). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2451 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected); run with its library
      *> texts (drafts-85n, kb/Work PB5067) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYNEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY TXT37 REPLACING ==ZZQ== BY
           ==COPY TEXTB.==.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DONE".
           STOP RUN.
