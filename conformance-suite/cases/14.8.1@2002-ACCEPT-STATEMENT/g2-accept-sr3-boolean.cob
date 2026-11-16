      *>--- conformance-case
      *> id: g2-accept-sr3-boolean
      *> rules: [2002-SR-14.8.1.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR3 - "Identifier-2 shall not reference a data
      *> item of class alphabetic, boolean, index, object, or pointer."
      *> BO is PIC 1(8) (class boolean); ACCEPT BO FROM TIME is format
      *> 2.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS3B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BO PIC 1(8).
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT BO FROM TIME
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS3B.
