      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-24
      *> rules: [1985-GR-XIV.2.2.4-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR24 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDIO.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  TERM-CD FOR I-O
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC TERMINAL IS ST
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK.
       01  CD-REC PIC X(33).
       PROCEDURE DIVISION.
       P1.
           MOVE 123456 TO MD.
           MOVE 12345678 TO MT.
           MOVE "TERMINAL-ABC" TO ST.
           MOVE 42 TO TL.
           MOVE "E" TO EK.
           MOVE "99" TO SK.
           DISPLAY CD-REC.
           STOP RUN.
