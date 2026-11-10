      *>--- conformance-case
      *> id: 1985-FMT-XIV_3_1_2
      *> rules: [1985-FMT-XIV.3.1.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.1.2 (THE ACCEPT MESSAGE COUNT STATEMENT). A
      *> refusal: the rule forbids line 22 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL5506 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ACMC.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT.
       01  CD-IN-AREA.
           02 IN-QUEUE   PIC X(12).
           02 IN-SUB1    PIC X(12).
           02 IN-SUB2    PIC X(12).
           02 IN-SUB3    PIC X(12).
           02 IN-DATE    PIC 9(6).
           02 IN-TIME    PIC 9(8).
           02 IN-SOURCE  PIC X(12).
           02 IN-LENGTH  PIC 9(4).
           02 IN-END-KEY PIC X.
           02 IN-STATUS  PIC XX.
           02 IN-COUNT   PIC 9(6).
       PROCEDURE DIVISION.
       P1.
           MOVE "QUEUEA" TO IN-QUEUE.
           ACCEPT CD-IN MESSAGE.
           STOP RUN.
