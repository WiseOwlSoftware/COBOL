      *>--- conformance-case
      *> id: 1985-FMT-XIV_3_6_2
      *> rules: [1985-FMT-XIV.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.2 (THE SEND STATEMENT). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0001 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEND OUT-CD.
           STOP RUN.
