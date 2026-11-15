      *>--- conformance-case
      *> id: g2_write_from_invalid_move_sender_neg
      *> rules: [2002-SR-14.8.47.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR6 - "identifier-1 or literal-1 shall be valid
      *> as a sending operand in a MOVE statement specifying
      *> record-name-1 as the receiving operand." CA, an alphabetic item
      *> of a CD record, is not a valid sender to the numeric record R1:
      *> Table 17 (§2002:14.8.24.2 SR8) gives Alphabetic to Numeric "No".
      *> The WRITE at the violating line shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR01N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2wr01n.tmp" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1 PIC 9(3).
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CA PIC A(3).
          05 CN PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           WRITE R1 FROM CA
           CLOSE F
           STOP RUN.
