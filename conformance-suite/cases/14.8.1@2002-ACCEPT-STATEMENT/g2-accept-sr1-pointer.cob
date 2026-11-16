      *>--- conformance-case
      *> id: g2-accept-sr1-pointer
      *> rules: [2002-SR-14.8.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR1 - "Identifier-1 shall reference neither a
      *> strongly-typed group item nor a data item of class index,
      *> object, or pointer."
      *> PT is a USAGE POINTER item (class pointer); ACCEPT PT is format
      *> 1.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS1P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PT USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT PT
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS1P.
