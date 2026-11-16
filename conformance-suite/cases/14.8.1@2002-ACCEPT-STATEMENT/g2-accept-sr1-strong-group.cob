      *>--- conformance-case
      *> id: g2-accept-sr1-strong-group
      *> rules: [2002-SR-14.8.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR1 - "Identifier-1 shall reference neither a
      *> strongly-typed group item nor a data item of class index,
      *> object, or pointer."
      *> V is a strongly-typed group item (TYPE TPT, TPT TYPEDEF
      *> STRONG); ACCEPT V is format 1.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS1G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TPT TYPEDEF STRONG.
           05 F1 PIC 9(4).
       01  V TYPE TPT.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT V
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS1G.
