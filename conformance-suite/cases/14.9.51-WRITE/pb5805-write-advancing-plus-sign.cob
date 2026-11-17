      *>--- conformance-case
      *> id: pb5805-write-advancing-plus-sign
      *> rules: [SR-14.9.51.3-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5805. §2023:5.5: "When the term 'integer-n' (n = 1,
      *> 2, ...) is used in a general format and associated rules, it
      *> refers to a fixed-point integer literal that shall be unsigned
      *> and nonzero unless otherwise specified in the associated
      *> rules." §2023:14.9.51.3 SR15: "Integer-1 shall be positive or
      *> zero." SR15 lifts the nonzero default only; integer-1 stays
      *> unsigned, so '+3' below is refused although its value is
      *> positive; a compiler that judges the value alone accepts it
      *> and fails. Refused with COBOL2365 (2014: §2014:5.5 and
      *> §2014:14.9.47.2 SR15, the same words).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5805B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5805b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "X" TO R1.
           WRITE R1 AFTER ADVANCING +3 LINES.
           CLOSE F1.
           STOP RUN.
