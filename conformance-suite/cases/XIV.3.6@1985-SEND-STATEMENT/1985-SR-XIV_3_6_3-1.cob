      *>--- conformance-case
      *> id: 1985-SR-XIV_3_6_3-1
      *> rules: [1985-SR-XIV.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.3 SR1 (THE SEND STATEMENT). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6170 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-AREA PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           MESSAGE COUNT IS IN-COUNT
           STATUS KEY IS IN-STATUS.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STATUS
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEND IN-CD FROM MSG-AREA WITH EMI.
           STOP RUN.
