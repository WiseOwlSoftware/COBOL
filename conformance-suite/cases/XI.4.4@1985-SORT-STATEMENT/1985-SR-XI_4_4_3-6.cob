      *>--- conformance-case
      *> id: 1985-SR-XI_4_4_3-6
      *> rules: [1985-SR-XI.4.4.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.3 SR6 (THE SORT STATEMENT). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTSR6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO SRTWK.
           SELECT IN-F ASSIGN TO INFILE.
           SELECT OUT-F ASSIGN TO OUTFILE.
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05 SK PIC X(5).
       FD  IN-F LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(5).
       FD  OUT-F LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ON ASCENDING KEY SK USING SF GIVING OUT-F.
           STOP RUN.
