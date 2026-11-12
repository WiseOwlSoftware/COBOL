      *>--- conformance-case
      *> id: 1985-SR-XI_4_1_3-6
      *> rules: [1985-SR-XI.4.1.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.3 SR6 (THE MERGE STATEMENT). A refusal: the rule
      *> forbids line 28 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6815 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGMFR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "IN1".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT OUT1 ASSIGN TO "OUT1".
           SELECT EXTRA ASSIGN TO "EXTRA".
           SELECT SW ASSIGN TO "SW".
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS IN1 IN2.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(10).
       FD  IN2.
       01  IN2-REC PIC X(10).
       FD  OUT1.
       01  OUT1-REC PIC X(10).
       FD  EXTRA.
       01  EXTRA-REC PIC X(10).
       SD  SW.
       01  SW-REC.
           05  SW-KEY PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MERGE SW ON ASCENDING KEY SW-KEY USING IN1 IN2
               GIVING OUT1.
           STOP RUN.
