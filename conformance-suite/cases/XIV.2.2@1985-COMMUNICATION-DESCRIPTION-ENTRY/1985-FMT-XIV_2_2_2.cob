      *>--- conformance-case
      *> id: 1985-FMT-XIV_2_2_2
      *> rules: [1985-FMT-XIV.2.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.2 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 11 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STATUS
           ERROR KEY IS OUT-ERR
           SYMBOLIC QUEUE IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
