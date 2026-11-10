      *>--- conformance-case
      *> id: 1985-SR-VI_6_14_3-1
      *> rules: [1985-SR-VI.6.14.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.14.3 SR1 (THE EXIT STATEMENT). A refusal: the rule
      *> forbids line 15 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2966 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXITT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM P-START THRU P-END.
           DISPLAY "DONE".
           STOP RUN.
       P-START.
           DISPLAY "START".
       P-END.
           MOVE 1 TO N EXIT.
