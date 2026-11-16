      *>--- conformance-case
      *> id: g2-accept-sr1-cd-index
      *> rules: [2002-SR-14.8.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR1 - "Identifier-1 shall reference neither a
      *> strongly-typed group item nor a data item of class index,
      *> object, or pointer."  CI is a USAGE INDEX item of a record
      *> under a CD entry (§2002:A.1.1.2); ACCEPT CI is format 1.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if a class-index communication-section receiver is
      *> accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS1C.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CX PIC X(10).
           05 CI USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT CI
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS1C.
