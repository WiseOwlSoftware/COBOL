      *>--- conformance-case
      *> id: 1985-SR-VI_6_22_3-4
      *> rules: [1985-SR-VI.6.22.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.3 SR4 (THE SEARCH STATEMENT). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1965 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCHALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL-DATA PIC X(6) VALUE "1A2B3C".
       01 TBL REDEFINES TBL-DATA.
          05 ENT OCCURS 3 TIMES ASCENDING KEY IS K INDEXED BY IX.
             10 K PIC 9.
             10 V PIC X.
       PROCEDURE DIVISION.
       P1.
           SEARCH ALL ENT AT END DISPLAY "NOT FOUND" WHEN V (IX) = "B"
                   DISPLAY V (IX).
           STOP RUN.
