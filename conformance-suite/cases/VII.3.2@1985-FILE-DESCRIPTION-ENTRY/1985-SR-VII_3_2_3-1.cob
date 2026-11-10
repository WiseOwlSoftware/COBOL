      *>--- conformance-case
      *> id: 1985-SR-VII_3_2_3-1
      *> rules: [1985-SR-VII.3.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.2.3 SR1 (THE FILE DESCRIPTION ENTRY). A refusal:
      *> the rule forbids line 9 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL0001 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "OUTFILE".
       DATA DIVISION.
       FILE SECTION.
       OUT-FILE.
       01  OUT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
