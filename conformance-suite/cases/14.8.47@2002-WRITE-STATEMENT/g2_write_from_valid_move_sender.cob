      *>--- conformance-case
      *> id: g2_write_from_valid_move_sender
      *> rules: [2002-SR-14.8.47.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR6 - "If record-name-1 is specified,
      *> identifier-1 or literal-1 shall be valid as a sending operand
      *> in a MOVE statement specifying record-name-1 as the receiving
      *> operand." The 2002 MOVE rules decide validity: §2002:14.8.24.2
      *> SR5 bars only the figurative constant SPACE from a numeric
      *> receiver, and §2002:8.3.1.2.5.2 SR3 bars ALL literal-1 only
      *> when literal-1 is longer than one character. So ALL "7" is a
      *> valid sender to the numeric record R1; §2002:14.8.47.3 GR3
      *> makes the WRITE a MOVE then a WRITE, and §2002:8.3.1.2.5.3
      *> GR2b repeats the "7" to the three digit positions: 777.
      *> A numeric item of a CD record (a COMMUNICATION SECTION entry,
      *> a 2002 element; obsolete, COBOL0903 warns) is a numeric
      *> integer sender, valid per Table 17 (§2002:14.8.24.2 SR8): 456.
      *> Each leg fails if the compiler refuses the sender (the 2023
      *> MOVE rules refuse ALL "7" to a numeric item) or moves the
      *> wrong value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2wr01.tmp" ORGANIZATION SEQUENTIAL.
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
           MOVE 456 TO CN
           OPEN OUTPUT F
           WRITE R1 FROM ALL "7"
           WRITE R1 FROM CN
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "ALL-7=" R1
           READ F
           DISPLAY "CD-CN=" R1
           CLOSE F
           STOP RUN.
