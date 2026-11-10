      *>--- conformance-case
      *> id: 1985-SR-VIII_3_2_3-1
      *> rules: [1985-SR-VIII.3.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.3.2.3 SR1 (THE FILE DESCRIPTION ENTRY). A refusal:
      *> the rule forbids line 11 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL0001 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFILE ASSIGN TO DISK1
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       RELFILE.
       01  REL-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
