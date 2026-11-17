      *>--- conformance-case
      *> id: pb5805-write-advancing-plus-sign-85
      *> rules: [1985-SR-VII.4.7.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5805. §1985:III.2: "Where the term 'integer' appears
      *> in the general formats, integer must be a numeric literal which
      *> is an integer, and it must be neither signed nor zero unless
      *> explicitly allowed by the rules for that format."
      *> §1985:VII.4.7.3 SR5: "Integer-1 may be positive or zero, but
      *> must not be negative." SR5 allows zero; it allows no sign (where
      *> 1985 allows one it says so: §1985:IV.4.3.8.2.3 SR6 "Integer-1
      *> may be signed"). So the written '+' of '+0' below is refused,
      *> though the value is zero; a compiler that judges the value
      *> alone accepts it and fails. Refused with COBOL2365.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5805A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5805a.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "X" TO R1.
           WRITE R1 AFTER ADVANCING +0 LINES.
           CLOSE F1.
           STOP RUN.
