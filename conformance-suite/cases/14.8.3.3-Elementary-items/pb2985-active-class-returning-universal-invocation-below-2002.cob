      *>--- conformance-case
      *> id: pb2985-active-class-returning-universal-invocation-below-2002
      *> rules: [GR-14.8.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2985 - the edition floor of the positive golden
      *> pb2985_active_class_returning_universal_invocation: an INVOKE
      *> (14.9.23) through a universal object reference, a CLASS-ID,
      *> USAGE OBJECT REFERENCE and its ACTIVE-CLASS phrase (13.18.60)
      *> are COBOL 2002 features, so the 14.8.3.3 rule 2 b) 4. crossing
      *> cannot be written at --std 85 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2985N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2985N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 OP USAGE OBJECT REFERENCE P2985N.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE P2985N "NEW" RETURNING U
           INVOKE U "RA" RETURNING OP
           STOP RUN.
       END PROGRAM PB2985N.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2985N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF
           GOBACK.
       END METHOD RA.
       END OBJECT.
       END CLASS P2985N.
