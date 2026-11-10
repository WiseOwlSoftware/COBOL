      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-11
      *> rules: [1985-SR-XV.3.2.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.3 SR11 (THE USE FOR DEBUGGING STATEMENT). A
      *> refusal: the rule forbids line 18 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6442 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(30).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON P.
       DBG-1.
           MOVE DEBUG-ITEM TO W.
       END DECLARATIVES.
       MAIN SECTION.
       P.
           MOVE DEBUG-ITEM TO W.
           DISPLAY "OK".
           STOP RUN.
