      *>--- conformance-case
      *> id: 1985-FMT-XIV_3_2_2
      *> rules: [1985-FMT-XIV.3.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.2 (THE DISABLE STATEMENT). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0901 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CD-1 FOR OUTPUT
           STATUS KEY IS CD-STAT.
       PROCEDURE DIVISION.
       P1.
           DISABLE OUTPUT TERMINAL CD-1.
           STOP RUN.
