      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-L2_8
      *> rules: [1985-SR-IV.4.3.8.1-L2.8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 SRL2.8 (Uniqueness of Reference). A refusal:
      *> the rule forbids line 17 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL5997 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LCQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE1 ASSIGN TO PRINTA.
           SELECT PFILE2 ASSIGN TO PRINTB.
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE1 LINAGE IS 20 LINES.
       01  P1-REC PIC X(20).
       FD  PFILE2 LINAGE IS 30 LINES.
       01  P2-REC PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PFILE1 PFILE2.
           DISPLAY LINAGE-COUNTER.
           CLOSE PFILE1 PFILE2.
           STOP RUN.
