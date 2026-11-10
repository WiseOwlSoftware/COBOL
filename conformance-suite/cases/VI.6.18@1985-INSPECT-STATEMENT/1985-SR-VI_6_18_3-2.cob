      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-2
      *> rules: [1985-SR-VI.6.18.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR2 (THE INSPECT STATEMENT). A refusal: the
      *> rule forbids line 12 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0847 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC X(4) VALUE "ABCA".
       01 W-T PIC 99 VALUE 0.
       01 W-X PIC X VALUE "A".
       01 W-N PIC 9 COMP VALUE 1.
       PROCEDURE DIVISION.
       P1.
           INSPECT W-S TALLYING W-T FOR ALL W-N.
           DISPLAY W-T.
           STOP RUN.
