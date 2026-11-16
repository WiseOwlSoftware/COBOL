      *>--- conformance-case
      *> id: g2002-boolean-literal-zero-length
      *> rules: [2002-FMT-8.3.1.2.3.1, 2002-SR-8.3.1.2.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.3.1 writes the content of every boolean literal
      *> in
      *> braces, one or more occurrences, and §2002:8.3.1.2.3.2 SR1
      *> requires
      *> its length to "be greater than zero": B"" is not a COBOL-2002
      *> boolean literal and the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BOL02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 B1 PIC 1(4).
       PROCEDURE DIVISION.
           MOVE B"" TO B1
           STOP RUN.
