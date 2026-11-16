      *>--- conformance-case
      *> id: g2trn-sr4-file-repeated
      *> rules: [2002-SR-7.2.19.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.2 SR4 - "No file-name shall be specified more than
      *>   once for one exception condition."
      *>   cite.py: OK  §2002:7.2.19.2 SR4
      *> NEGATIVE: the directive names F1 twice after the one
      *> exception-name EC-I-O-AT-END, so F1 is specified twice for that
      *> exception condition. The prohibition grants no form; the
      *> compiler shall refuse the directive line.
       >>TURN EC-I-O-AT-END F1 F1 CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TRNS4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2TRNS41.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       PROCEDURE DIVISION.
       M1.
           DISPLAY "NOT ACCEPTED".
           STOP RUN.
