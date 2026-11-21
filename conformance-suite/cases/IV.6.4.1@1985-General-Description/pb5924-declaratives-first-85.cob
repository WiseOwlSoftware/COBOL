      *>--- conformance-case
      *> id: pb5924-declaratives-first-85
      *> rules: [1985-SR-IV.6.4.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.4.1.1 (cite.py: OK): "Declarative sections must be grouped at the beginning of the Procedure Division"
      *> DECLARATIVES ... END DECLARATIVES at the beginning of the Procedure Division; execution begins
      *> at the first nondeclarative statement, so the declarative never runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLFIRST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5924DCL.DAT"
           ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC  PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P.
           DISPLAY "ERR".
       END DECLARATIVES.
       MAIN-S SECTION.
       P1.
           DISPLAY "MAIN".
           STOP RUN.
