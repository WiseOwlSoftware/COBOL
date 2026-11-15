      *>--- conformance-case
      *> id: g2_write_from_all_long_literal_neg
      *> rules: [2002-SR-14.8.47.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR6 - "identifier-1 or literal-1 shall be valid
      *> as a sending operand in a MOVE statement specifying
      *> record-name-1 as the receiving operand." The figurative
      *> constant ALL "12" is not: §2002:8.3.1.2.5.2 SR3, "If the length
      *> of literal-1 is greater than one, it is not permitted to be
      *> associated with a numeric or numeric-edited item." R1 is
      *> numeric, so the WRITE at the violating line shall be refused
      *> (its one-character sibling ALL "7" is accepted:
      *> g2_write_from_valid_move_sender).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR01M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2wr01m.tmp" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1 PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           WRITE R1 FROM ALL "12"
           CLOSE F
           STOP RUN.
