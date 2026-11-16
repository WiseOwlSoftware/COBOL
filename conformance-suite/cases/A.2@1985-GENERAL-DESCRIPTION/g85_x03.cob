      *>--- conformance-case
      *> id: g85_x03
      *> rules: [1989-GR-A.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.3 GR2: "A numeric function may not be referenced
      *> where an integer operand is required, even though a particular
      *> reference may yield an integer value." CHAR's argument-1 must be
      *> an integer, and SQRT is a numeric function, so line 8 of the
      *> program below is refused although SQRT (4) is 2. WiseOwl refuses
      *> it at 85 with COBOL1627; a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION CHAR (FUNCTION SQRT (4)) TO C.
           STOP RUN.
