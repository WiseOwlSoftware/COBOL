      *>--- conformance-case
      *> id: 1985-SR-VI_5_3_3-3
      *> rules: [1985-SR-VI.5.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.3 SR3 (THE DATA DESCRIPTION ENTRY). A refusal: the
      *> rule forbids line 7 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0881 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICREQ.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  WS-A PIC X VALUE "A".
       77  WS-B USAGE DISPLAY.
       PROCEDURE DIVISION.
       P1.
           DISPLAY WS-A.
           STOP RUN.
