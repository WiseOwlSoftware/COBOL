      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-15
      *> rules: [1985-SR-XIV.2.2.3-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR15 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 7 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985
      *> audit's drafts pass (a draft that mismatched on an older build,
      *> re-run to MATCH with its expectation unchanged, witnesses-85g)
      *> by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDDUP.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  COMM-IO FOR I-O
           MESSAGE DATE IS CD-DATE
           MESSAGE TIME IS CD-DATE
           SYMBOLIC TERMINAL IS CD-TERM
           TEXT LENGTH IS CD-LEN
           END KEY IS CD-END
           STATUS KEY IS CD-STAT.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
