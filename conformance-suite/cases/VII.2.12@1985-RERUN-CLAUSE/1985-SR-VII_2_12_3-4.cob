      *>--- conformance-case
      *> id: 1985-SR-VII_2_12_3-4
      *> rules: [1985-SR-VII.2.12.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.3 SR4 (THE RERUN CLAUSE). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6531 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO DISK1.
           SELECT OUT-FILE ASSIGN TO DISK2.
       I-O-CONTROL.
           RERUN ON SYSCKPT EVERY 100 RECORDS OF IN-FILE
           RERUN ON SYSCKPT EVERY 200 RECORDS OF IN-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE
           LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(80).
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
