      *>--- conformance-case
      *> id: 1985-SR-VI_5_4_3-2
      *> rules: [1985-SR-VI.5.4.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.4.3 SR2 (THE BLANK WHEN ZERO CLAUSE). A refusal: the
      *> rule forbids line 5 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2405 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BWZTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-NUM PIC 9(4) USAGE IS COMPUTATIONAL BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO W-NUM.
           DISPLAY "DONE".
           STOP RUN.
