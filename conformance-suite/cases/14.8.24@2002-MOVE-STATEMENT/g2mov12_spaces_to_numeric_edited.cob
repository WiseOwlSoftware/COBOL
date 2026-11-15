      *>--- conformance-case
      *> id: g2mov12_spaces_to_numeric_edited
      *> rules: [2002-SR-14.8.24.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR5 - "The figurative constant SPACE shall
      *>   not be moved to a numeric or numeric-edited item." SPACES
      *>   (the same figurative constant, §2002:8.3.1.2.5.3 GR1) moved to
      *>   the numeric-edited item E (PIC ZZ9.99) violates it and is
      *>   refused at the MOVE line. Fails if the compiler accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E  PIC ZZ9.99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACES TO E.
           STOP RUN.
