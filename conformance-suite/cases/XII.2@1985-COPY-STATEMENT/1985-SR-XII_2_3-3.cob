      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-3
      *> rules: [1985-SR-XII.2.3-3]
      *> files: [CPY233.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR3 (THE COPY STATEMENT). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2572 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected); run with its library
      *> texts (drafts-85n, kb/Work PB5067) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYPT1.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       P1.
           COPY CPY233 REPLACING == == BY ==W-A==.
           DISPLAY W-A.
           STOP RUN.
