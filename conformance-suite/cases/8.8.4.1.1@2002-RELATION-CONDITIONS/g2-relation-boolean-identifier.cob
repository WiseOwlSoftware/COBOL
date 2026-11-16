      *>--- conformance-case
      *> id: g2-relation-boolean-identifier
      *> rules: [2002-SR-8.8.4.1.1.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.1.2 SR2 (cite.py OK): "All identifiers shall be
      *> of class alphabetic, alphanumeric, national, or numeric or
      *> shall be strongly-typed group items." B1 = W is no Format 2
      *> relation (W is no boolean expression), so it is Format 1 and
      *> B1, of class boolean, violates SR2: refused at its IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B1 PIC 1 VALUE B"1".
       01  W  PIC X VALUE "1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF B1 = W DISPLAY "EQ".
           STOP RUN.
