      *>--- conformance-case
      *> id: 1985-SR-XI_4_1_3-1
      *> rules: [1985-SR-XI.4.1.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.3 SR1 (THE MERGE STATEMENT). A refusal: the rule
      *> forbids line 26 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2700 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGDCL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "IN1".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT OUT1 ASSIGN TO "OUT1".
           SELECT SW ASSIGN TO "SW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(10).
       FD  IN2.
       01  IN2-REC PIC X(10).
       FD  OUT1.
       01  OUT1-REC PIC X(10).
       SD  SW.
       01  SW-REC.
           05  SK PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUT1.
       ERR-PARA.
           MERGE SW ON ASCENDING KEY SK USING IN1 IN2 GIVING OUT1.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           STOP RUN.
