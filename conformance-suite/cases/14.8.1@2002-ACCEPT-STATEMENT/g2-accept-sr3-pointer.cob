      *>--- conformance-case
      *> id: g2-accept-sr3-pointer
      *> rules: [2002-SR-14.8.1.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR3 - "Identifier-2 shall not reference a data
      *> item of class alphabetic, boolean, index, object, or pointer."
      *> PT is a USAGE POINTER item (class pointer); ACCEPT PT FROM
      *> DAY is format 2.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if the compiler accepts that format 2 receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS3P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PT USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT PT FROM DAY
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS3P.
