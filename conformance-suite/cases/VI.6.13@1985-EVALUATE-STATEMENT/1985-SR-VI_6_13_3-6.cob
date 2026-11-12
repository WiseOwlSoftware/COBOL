      *>--- conformance-case
      *> id: 1985-SR-VI_6_13_3-6
      *> rules: [1985-SR-VI.6.13.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.3 SR6 (THE EVALUATE STATEMENT). A refusal: the
      *> rule forbids line 8 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1634 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EVTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE WS-A WHEN TRUE
                   DISPLAY "YES"
               WHEN OTHER
                   DISPLAY "NO"
           END-EVALUATE.
           STOP RUN.
