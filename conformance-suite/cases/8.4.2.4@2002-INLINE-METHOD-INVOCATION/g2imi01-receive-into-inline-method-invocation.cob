      *>--- conformance-case
      *> id: g2imi01-receive-into-inline-method-invocation
      *> rules: [2002-SR-8.4.2.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.4.2 SR1 - "Inline method invocation shall not be
      *> specified as a receiving operand." RECEIVE ... INTO identifier-1
      *> (§2002:A.2.5.1) is a communication-facility receiving operand:
      *> the message "is transferred to the receiving character positions
      *> of the area referenced by identifier-1" (§2002:A.2.5.3 GR3).
      *> Writing OB :: "GETX" there is the violation. Everything else is
      *> legal: CDIN is an input CD (§2002:A.2.5.2 SR1) and GETX returns
      *> PIC X(10), category alphanumeric (§2002:A.2.5.2 SR2), so the only
      *> ground for refusing the RECEIVE is SR1. The refusal is expected
      *> on the RECEIVE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IMI01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2IMI01C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OB USAGE OBJECT REFERENCE G2IMI01C.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2IMI01C "NEW" RETURNING OB.
           RECEIVE CDIN MESSAGE INTO OB :: "GETX"
               NO DATA DISPLAY "NO DATA"
           END-RECEIVE.
           STOP RUN.
       END PROGRAM G2IMI01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2IMI01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-X PIC X(10).
       PROCEDURE DIVISION RETURNING LK-X.
       MAIN-PARA.
           MOVE "ABC" TO LK-X.
       END METHOD GETX.
       END OBJECT.
       END CLASS G2IMI01C.
