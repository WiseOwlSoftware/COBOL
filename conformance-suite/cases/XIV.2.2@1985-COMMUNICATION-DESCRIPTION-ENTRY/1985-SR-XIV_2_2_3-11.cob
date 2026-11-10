      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-11
      *> rules: [1985-SR-XIV.2.2.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR11 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 14 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2096 there; a compiler that
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
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "TERM1" TO OUT-DEST (1).
           STOP RUN.
