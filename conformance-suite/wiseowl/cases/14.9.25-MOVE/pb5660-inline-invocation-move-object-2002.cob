      *>--- conformance-case
      *> id: pb5660-inline-invocation-move-object-2002
      *> rules: [SR-14.9.25.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5660. At --std 2002 the inline invocation of a method
      *> returning an ACTIVE-CLASS item is admitted (the ban is 2014's,
      *> kb/Work PB5598), but a MOVE operand shall not be of class
      *> object (§2002:14.8.24.2 SR1): both operands are refused under
      *> that rule, the sender named as the compiler temporary it is,
      *> never by its internal name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5660P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5660C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB5660C.
       01 W   USAGE OBJECT REFERENCE PB5660C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB5660C "NEW" RETURNING OBJ.
           MOVE OBJ :: "CLONEME" TO W.
           STOP RUN.
       END PROGRAM PB5660P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5660C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. CLONEME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-OBJ USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK-OBJ.
       MAIN.
           SET LK-OBJ TO SELF.
       END METHOD CLONEME.
       END OBJECT.
       END CLASS PB5660C.
