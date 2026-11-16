      *>--- conformance-case
      *> id: g2-accept-sr3-index
      *> rules: [2002-SR-14.8.1.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR3 - "Identifier-2 shall not reference a data
      *> item of class alphabetic, boolean, index, object, or pointer."
      *> IX is a USAGE INDEX item (class index); ACCEPT IX FROM DATE
      *> is format 2.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if the compiler accepts that format 2 receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS3I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  IX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT IX FROM DATE
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS3I.
