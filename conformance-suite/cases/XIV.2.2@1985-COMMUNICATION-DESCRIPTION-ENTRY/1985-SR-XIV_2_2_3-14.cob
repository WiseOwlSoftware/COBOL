      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-14
      *> rules: [1985-SR-XIV.2.2.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR14 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 8 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD IOCD FOR I-O
           SYMBOLIC TERMINAL IS IOTERM.
       01  IOREC PIC X(32).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
