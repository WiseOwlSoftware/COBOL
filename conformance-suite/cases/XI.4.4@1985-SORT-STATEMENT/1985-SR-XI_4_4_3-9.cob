      *>--- conformance-case
      *> id: 1985-SR-XI_4_4_3-9
      *> rules: [1985-SR-XI.4.4.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.3 SR9 (THE SORT STATEMENT). A refusal: the rule
      *> forbids line 25 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTSAME.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO "INFILE".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT OUT-FILE ASSIGN TO "OUTFILE".
           SELECT SRT1 ASSIGN TO "SRT1".
       I-O-CONTROL.
           SAME SORT AREA FOR SRT1 IN-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(10).
       FD  IN2 LABEL RECORDS ARE STANDARD.
       01  IN2-REC PIC X(10).
       FD  OUT-FILE LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       SD  SRT1.
       01  S1-REC.
           05 S1-KEY PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRT1 ASCENDING S1-KEY USING IN-FILE GIVING OUT-FILE.
           STOP RUN.
