      *>--- conformance-case
      *> id: pb5275-sort-merge-in-debugging-section
      *> rules: [1985-GR-XV.3.2.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5275 - §1985:XI.4.4.3 SR1: "A SORT statement may
      *>   appear anywhere in the Procedure Division except in the
      *>   declarative portion", and §1985:XI.4.1.3 SR1 says the same
      *>   of MERGE ("except in the declaratives portion"). A USE FOR
      *>   DEBUGGING section is a declarative section (§1985:XV.3.2.3
      *>   SR1: debugging sections follow "the DECLARATIVES header").
      *>   Expected: COBOL2700 at 85 for the MERGE and the SORT in
      *>   DBG-PARA. Fails if a debugging section is not asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5275DB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "P5275I1".
           SELECT IN2 ASSIGN TO "P5275I2".
           SELECT OUT1 ASSIGN TO "P5275O1".
           SELECT SW ASSIGN TO "P5275SW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(4).
       FD  IN2.
       01  IN2-REC PIC X(4).
       FD  OUT1.
       01  OUT1-REC PIC X(4).
       SD  SW.
       01  SW-REC.
           05  SK PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SEC SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       DBG-PARA.
           MERGE SW ON ASCENDING KEY SK USING IN1 IN2 GIVING OUT1.
           SORT SW ON ASCENDING KEY SK USING IN1 GIVING OUT1.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           STOP RUN.
