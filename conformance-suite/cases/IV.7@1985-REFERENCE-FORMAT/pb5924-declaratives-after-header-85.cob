      *>--- conformance-case
      *> id: pb5924-declaratives-after-header-85
      *> rules: [1985-SR-IV.7.5-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.5 (cite.py: OK):
      *> "must each appear on a line by itself"
      *> DECLARATIVES on the line of the Procedure Division header is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DECL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5924D".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X.
       PROCEDURE DIVISION. DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       D1-P.
           DISPLAY "ERR".
       END DECLARATIVES.
       M SECTION.
       M-P.
           DISPLAY "DECL".
           STOP RUN.
