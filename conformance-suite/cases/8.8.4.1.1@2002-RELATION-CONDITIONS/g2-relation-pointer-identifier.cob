      *>--- conformance-case
      *> id: g2-relation-pointer-identifier
      *> rules: [2002-SR-8.8.4.1.1.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.1.2 SR2 (cite.py OK): "All identifiers shall be
      *> of class alphabetic, alphanumeric, national, or numeric or
      *> shall be strongly-typed group items." P1 = N: N is numeric so
      *> this is no Format 3 relation (SR5, cite.py OK: "Identifier-3
      *> and identifier-4 shall reference data items of class pointer
      *> or object"), and as Format 1 the pointer P1 violates SR2:
      *> refused at its IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P1 USAGE POINTER.
       01  N  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF P1 = N DISPLAY "EQ".
           STOP RUN.
