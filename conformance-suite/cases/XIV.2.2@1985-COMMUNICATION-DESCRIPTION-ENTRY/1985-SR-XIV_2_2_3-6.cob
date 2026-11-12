      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-6
      *> rules: [1985-SR-XIV.2.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR6 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 10 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985
      *> audit's drafts pass (a draft that mismatched on an older build,
      *> re-run to MATCH with its expectation unchanged, witnesses-85g)
      *> by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDUNIQ.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS CQ
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS EK.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
