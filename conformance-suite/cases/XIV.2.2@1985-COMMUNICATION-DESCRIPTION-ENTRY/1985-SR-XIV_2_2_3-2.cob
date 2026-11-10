      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-2
      *> rules: [1985-SR-XIV.2.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR2 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 8 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDINIT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CD-A FOR INITIAL INPUT.
       01  CD-A-REC PIC X(87).
       CD  CD-B FOR INITIAL INPUT.
       01  CD-B-REC PIC X(87).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
