      *>--- conformance-case
      *> id: 1985-SR-VI_6_10_3-1
      *> rules: [1985-SR-VI.6.10.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.10.3 SR1 (THE DISPLAY STATEMENT). A refusal: the
      *> rule forbids line 8 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0817 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISPUPON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DEV PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "HELLO" UPON WS-DEV.
           STOP RUN.
