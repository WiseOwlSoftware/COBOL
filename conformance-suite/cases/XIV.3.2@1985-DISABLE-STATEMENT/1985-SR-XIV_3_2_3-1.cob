      *>--- conformance-case
      *> id: 1985-SR-XIV_3_2_3-1
      *> rules: [1985-SR-XIV.3.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.3 SR1 (THE DISABLE STATEMENT). A refusal: the
      *> rule forbids line 12 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6170 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT.
       01  IN-REC PIC X(87).
       CD  CDOUT FOR OUTPUT.
       01  OUT-REC PIC X(23).
       PROCEDURE DIVISION.
       P1.
           DISABLE INPUT CDOUT WITH KEY "PASSWORD".
           STOP RUN.
