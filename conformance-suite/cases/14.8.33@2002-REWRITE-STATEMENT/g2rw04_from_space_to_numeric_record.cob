      *>--- conformance-case
      *> id: g2rw04_from_space_to_numeric_record
      *> rules: [2002-SR-14.8.33.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.2 SR6 - identifier-1 or literal-1 "shall be
      *>   valid as a sending operand in a MOVE statement specifying
      *>   record-name-1 as the receiving operand" (cite.py: OK).
      *>   §2002:14.8.24.2 SR5: "The figurative constant SPACE shall
      *>   not be moved to a numeric or numeric-edited item." (cite.py:
      *>   OK). The record G2RW04-N is PIC 9(5), so REWRITE G2RW04-N
      *>   FROM SPACE is invalid and shall be refused at the REWRITE
      *>   line. The accepted side of SR6 is g2rw03. Fails if it
      *>   compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw04.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  G2RW04-N PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F.
           READ F AT END CONTINUE END-READ.
           REWRITE G2RW04-N FROM SPACE.
           CLOSE F.
           STOP RUN.
