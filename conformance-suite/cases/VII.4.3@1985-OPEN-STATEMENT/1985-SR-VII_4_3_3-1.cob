      *>--- conformance-case
      *> id: 1985-SR-VII_4_3_3-1
      *> rules: [1985-SR-VII.4.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.3 SR1 (THE OPEN STATEMENT). A refusal: the rule
      *> forbids line 16 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2210 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REVTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-F ASSIGN TO RFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-F
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN INPUT REL-F REVERSED.
           CLOSE REL-F.
           STOP RUN.
