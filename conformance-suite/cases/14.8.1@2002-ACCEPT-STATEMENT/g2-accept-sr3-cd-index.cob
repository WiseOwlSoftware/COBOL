      *>--- conformance-case
      *> id: g2-accept-sr3-cd-index
      *> rules: [2002-SR-14.8.1.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.2 SR3 - "Identifier-2 shall not reference a data
      *> item of class alphabetic, boolean, index, object, or pointer."
      *> CI is a USAGE INDEX item (class index) of a record under a
      *> CD entry (§2002:A.1.1.2); ACCEPT CI FROM TIME is format 2.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if the compiler accepts that format 2 receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS3C.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CX PIC X(10).
           05 CI USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT CI FROM TIME
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ACS3C.
