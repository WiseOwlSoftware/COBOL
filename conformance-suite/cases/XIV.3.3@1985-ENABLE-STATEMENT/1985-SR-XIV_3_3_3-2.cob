      *>--- conformance-case
      *> id: 1985-SR-XIV_3_3_3-2
      *> rules: [1985-SR-XIV.3.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.3.3 SR2 (THE ENABLE STATEMENT). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6170 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE.
       CD  IO-CD FOR I-O
           SYMBOLIC TERMINAL IS IO-TERM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ENABLE I-O TERMINAL IN-CD WITH KEY "KEY1".
           STOP RUN.
