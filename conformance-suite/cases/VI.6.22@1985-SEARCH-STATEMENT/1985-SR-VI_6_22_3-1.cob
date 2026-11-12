      *>--- conformance-case
      *> id: 1985-SR-VI_6_22_3-1
      *> rules: [1985-SR-VI.6.22.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.3 SR1 (THE SEARCH STATEMENT). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2075 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL VALUE "ABC".
           05  ENT PIC X OCCURS 3 TIMES.
       01  N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           SEARCH ENT VARYING N
               AT END DISPLAY "NOT FOUND"
               WHEN ENT (N) = "B" DISPLAY "FOUND".
           STOP RUN.
