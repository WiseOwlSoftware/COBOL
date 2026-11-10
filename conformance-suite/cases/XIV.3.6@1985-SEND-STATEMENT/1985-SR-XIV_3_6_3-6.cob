      *>--- conformance-case
      *> id: 1985-SR-XIV_3_6_3-6
      *> rules: [1985-SR-XIV.3.6.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.3 SR6 (THE SEND STATEMENT). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6171 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEND85SR6.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A                PIC X(4) VALUE "abcd".
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT
           DESTINATION COUNT IS CD-COUNT
           TEXT LENGTH IS CD-LENGTH
           STATUS KEY IS CD-STATUS
           ERROR KEY IS CD-ERROR
           SYMBOLIC DESTINATION IS CD-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO CD-COUNT.
           MOVE 4 TO CD-LENGTH.
           MOVE "DEST85" TO CD-DEST.
           SEND CD-OUT FROM FUNCTION UPPER-CASE (WS-A) WITH EGI.
           SEND CD-OUT FROM FUNCTION ORD (WS-A (1:1)) WITH EGI.
           DISPLAY "DONE".
           STOP RUN.
