      *>--- conformance-case
      *> id: g2002-hex-national-literal-zero-length
      *> rules: [2002-FMT-8.3.1.2.4.1, 2002-SR-8.3.1.2.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.4.1 writes the content of every national literal
      *> in
      *> braces, one or more occurrences, and §2002:8.3.1.2.4.2 SR1
      *> requires
      *> its length to "be greater than zero": NX'' is not a COBOL-2002
      *> national literal and the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NAT03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 N1 PIC N(2).
       PROCEDURE DIVISION.
           MOVE NX'' TO N1
           STOP RUN.
