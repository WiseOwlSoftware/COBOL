      *>--- conformance-case
      *> id: g2-accept-sr3-alphabetic
      *> rules: [2002-SR-14.8.1.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR3 - "Identifier-2 shall not reference a data
      *> item of class alphabetic, boolean, index, object, or pointer."
      *> AL is PIC A(6) (class alphabetic); ACCEPT AL FROM DATE is
      *> format 2.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS3A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AL PIC A(6).
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT AL FROM DATE
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS3A.
