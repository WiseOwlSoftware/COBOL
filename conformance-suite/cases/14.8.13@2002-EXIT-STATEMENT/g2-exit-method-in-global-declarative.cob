      *>--- conformance-case
      *> id: g2-exit-method-in-global-declarative
      *> rules: [2002-SR-14.8.13.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR2 - "The EXIT statement shall not
      *>   be specified in a declarative procedure for which the GLOBAL
      *>   phrase is specified in the associated USE statement." EXIT
      *>   METHOD in the GLOBAL declarative of a method shall be
      *>   refused (COBOL2102). The accepted form is
      *>   g2_exit_method_ends_method.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXN2C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
       DECLARATIVES.
       DM SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON INPUT.
       DM-P.
           EXIT METHOD.
       END DECLARATIVES.
       MM SECTION.
       MM1.
           MOVE "GOOD" TO LR.
           EXIT METHOD.
       END METHOD GETV.
       END OBJECT.
       END CLASS G2EXN2C.
