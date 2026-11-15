      *>--- conformance-case
      *> id: pb5386_move_property_refmod_function
      *> rules: [GR-14.9.25.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386, review R1-1. A receiving object property whose
      *> reference modifier holds a function carries two brackets: the
      *> property's (identifier-3 and the GET/SET of ISO 2023
      *> 8.4.3.9.4 GR1, GR2) and the one that evaluates the modifier
      *> where MOVE identifies the receiver (14.9.25.4 GR1). The
      *> property's encloses the other: NM OF AR(1) (FUNCTION
      *> INTEGER(K):2) with K = 2 replaces positions 2-3 of "ABCDEF",
      *> and the compiler once refused to emit it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386PR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5386PC
           PROPERTY NM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE PB5386PC OCCURS 2.
       01 R TYPE RT.
       01 K PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB5386PC "NEW" RETURNING AR(1)
           MOVE "QQ" TO NM OF AR(1) (FUNCTION INTEGER(K):2)
           DISPLAY "[" NM OF AR(1) "]"
           STOP RUN.
       END PROGRAM PB5386PR.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5386PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB5386PC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB5386PC.
