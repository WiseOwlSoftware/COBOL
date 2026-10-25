*>--- conformance-case
*> id: pb929-set-sr10-active-class-sender-not-implementing
*> rules: [SR-14.9.39.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 10 c) 2.: for an interface-name receiver, a sender described with an ACTIVE-CLASS
      *> phrase and WITHOUT a FACTORY phrase is permitted only if "the objects of the class containing the data item
      *> referenced by identifier-4 shall implement int-1".  A is described ACTIVE-CLASS inside method MK of the
      *> instance definition of P9C2C; that instance definition carries no IMPLEMENTS clause (it only names P9C2I in
      *> the REPOSITORY), so SET IR TO A (IR described with the interface P9C2I) violates SR10 c) 2. and shall be refused.
      *> The conforming twin is conformance:cases/9.3.6-Method-invocation/pb389_object_reference_descriptor (SET IR TO A
      *> inside method MK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P9C2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P9C2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OY USAGE OBJECT REFERENCE P9C2C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE P9C2C "NEW" RETURNING OY.
           INVOKE OY "MK".
           STOP RUN.
       END PROGRAM P9C2.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. P9C2I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE P9C2I.

       IDENTIFICATION DIVISION.
       CLASS-ID. P9C2C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE P9C2I.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01 IR USAGE OBJECT REFERENCE P9C2I.
       PROCEDURE DIVISION.
       MAIN.
           SET A TO SELF.
           SET IR TO A.
       END METHOD MK.
       END OBJECT.
       END CLASS P9C2C.
