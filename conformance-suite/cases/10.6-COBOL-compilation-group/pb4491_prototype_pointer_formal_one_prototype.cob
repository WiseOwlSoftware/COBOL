      *>--- conformance-case
      *> id: pb4491_prototype_pointer_formal_one_prototype
      *> rules: [SR-10.6.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4491 - a prototype and its definition whose pointer
      *> formals name ONE prototype have the same signature, however
      *> each spells it. 10.6.2 SR2 (cite.py --check 10.6.2 -> OK
      *> 10.6.2 2)): "the signatures of these two compilation units
      *> shall be the same"; 8.13 (OK 8.13): the signature is "the
      *> description of the parameters"; a pointer formal's
      *> description is its USAGE clause, whose TO phrase names a
      *> prototype by its externalized name (docs/CONFORMANCE.md
      *> D-RP1). The type-name a formal is described through is not
      *> its description (an elementary TYPEDEF, 13.18.58).
      *> DERIVATION:
      *>   PX's prototype: USING a pointer TO Q1 through type T1.
      *>     PX's definition: USING a pointer TO QQ through type T9,
      *>     where its REPOSITORY entry PROGRAM QQ AS "Q1" (12.3.8.4
      *>     GR2, OK 12.3.8.4 2)) names the prototype known as Q1: one
      *>     USAGE clause, one signature - legal.
      *>   CALL PX USING A (A TO Q1, BY REFERENCE) - 14.8.2.3.2 (OK
      *>     14.8.2.3.2 4)) "both shall be restricted and of the same
      *>     type": A and the prototype's formal are of type T1 - legal;
      *>     PX sees NULL: px null.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM Q1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 L-P TYPE T1.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4491M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           PROGRAM PX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 A TYPE T1.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           CALL PX USING A
           STOP RUN.
       END PROGRAM PB4491M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QQ AS "Q1".
       DATA DIVISION.
       LINKAGE SECTION.
       01 T9 IS TYPEDEF USAGE PROGRAM-POINTER TO QQ.
       01 L-P TYPE T9.
       PROCEDURE DIVISION USING L-P.
       PX-P.
           IF L-P = NULL DISPLAY "px null" END-IF
           GOBACK.
       END PROGRAM PX.
