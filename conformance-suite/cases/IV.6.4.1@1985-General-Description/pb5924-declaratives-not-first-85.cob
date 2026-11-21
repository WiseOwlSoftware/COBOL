      *>--- conformance-case
      *> id: pb5924-declaratives-not-first-85
      *> rules: [1985-SR-IV.6.4.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.4.1.1 (cite.py: OK): "Declarative sections must be grouped at the beginning of the Procedure Division"
      *> The DECLARATIVES portion follows a nondeclarative section; it is not at the beginning: refused.
      *> DEFECT (misleads_diagnosis): refused today as "reserved word ... user-defined word"; fails until the
      *> diagnostic names the placement rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLLATE.
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
       MAIN-S SECTION.
       P1.
           DISPLAY "MAIN".
           STOP RUN.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P.
           DISPLAY "ERR".
       END DECLARATIVES.
