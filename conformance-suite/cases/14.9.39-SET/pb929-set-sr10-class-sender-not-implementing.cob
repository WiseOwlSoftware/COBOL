*>--- conformance-case
*> id: pb929-set-sr10-class-sender-not-implementing
*> rules: [SR-14.9.39.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 10 b) 2.: for an interface-name receiver, a sender described with an
      *> object-class-name and WITHOUT a FACTORY phrase is permitted only if "the objects of the specified class shall
      *> implement int-1".  OX is described with the class P9B2C, whose instance definition carries no IMPLEMENTS
      *> clause, so SET RI TO OX (RI described with the interface P9B2I) violates SR10 b) 2. and shall be refused.
      *> The conforming twin is conformance:cases/9.3.6-Method-invocation/pb389_object_reference_descriptor (SET R TO B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P9B2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P9B2C
           INTERFACE P9B2I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RI USAGE OBJECT REFERENCE P9B2I.
       01 OX USAGE OBJECT REFERENCE P9B2C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE P9B2C "NEW" RETURNING OX.
           SET RI TO OX.
           STOP RUN.
       END PROGRAM P9B2.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. P9B2I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE P9B2I.

       IDENTIFICATION DIVISION.
       CLASS-ID. P9B2C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS P9B2C.
