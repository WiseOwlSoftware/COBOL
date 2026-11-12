      *>--- conformance-case
      *> id: 1985-SR-VII_4_3_3-2
      *> rules: [1985-SR-VII.4.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.3 SR2 (THE OPEN STATEMENT). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6814 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFTEXT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO TAPE1.
           SELECT F2 ASSIGN TO TAPE2.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2 LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN EXTEND F1.
           CLOSE F1.
           STOP RUN.
