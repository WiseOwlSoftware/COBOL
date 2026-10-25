*>--- conformance-case
*> id: pb929-set-sr10-factory-sender-not-implementing
*> rules: [SR-14.9.39.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 10 b) 1.: for an interface-name receiver, a sender described with an
      *> object-class-name and WITH a FACTORY phrase is permitted only if "the factory object of the specified class shall
      *> implement int-1".  FF is described FACTORY OF P9B1C.  P9B1C's INSTANCE definition carries IMPLEMENTS P9B1I and
      *> its FACTORY definition carries none, so SET RI TO FF violates SR10 b) 1. and shall be refused.
      *> DISCRIMINATOR: the instance half implements the interface and the factory half does not, so a check that asked
      *> the instance-side relation (SR10 b) 2.'s question) for a FACTORY sender would accept this program.  The
      *> conforming twin is conformance:cases/9.3.6-Method-invocation/pb389_object_reference_descriptor (SET R TO F).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P9B1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P9B1C
           INTERFACE P9B1I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RI USAGE OBJECT REFERENCE P9B1I.
       01 FF USAGE OBJECT REFERENCE FACTORY OF P9B1C.
       PROCEDURE DIVISION.
       MAIN.
           SET FF TO P9B1C.
           SET RI TO FF.
           STOP RUN.
       END PROGRAM P9B1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. P9B1I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE P9B1I.

       IDENTIFICATION DIVISION.
       CLASS-ID. P9B1C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE P9B1I.
       IDENTIFICATION DIVISION.
       FACTORY.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS P9B1I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING".
       END METHOD PING.
       END OBJECT.
       END CLASS P9B1C.
