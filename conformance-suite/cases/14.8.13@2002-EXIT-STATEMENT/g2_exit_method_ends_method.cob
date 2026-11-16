      *>--- conformance-case
      *> id: g2_exit_method_ends_method
      *> rules: [2002-SR-14.8.13.2-8, 2002-SR-14.8.13.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.2 SR8 - "An EXIT METHOD statement may be
      *>   specified only in a method procedure division." The granted
      *>   form: EXIT METHOD in the procedure division of method GETV,
      *>   accepted and run. Beside it, SR6 ("The LAST phrase may be
      *>   specified only in a declarative procedure"): EXIT METHOD
      *>   RAISING LAST EXCEPTION in GETV's declarative is accepted (the
      *>   declarative does not run). The refusals are
      *>   g2-exit-method-in-program (COBOL0827) and
      *>   g2-exit-raising-last-method-outside-decl (COBOL2103).
      *> §2002:14.8.13.3 GR7 - "The execution of an EXIT METHOD
      *>   statement causes the executing method to terminate, and
      *>   control to return to the invoking statement"; "the value in
      *>   the data item referenced by the RETURNING phrase becomes the
      *>   result of the method invocation".
      *> EXPECTED (derived): GETV moves GOOD to LR and exits before the
      *>   MOVE "BAD "; R receives GOOD (R=GOOD) and control returns to
      *>   the INVOKE, so AFTER-INVOKE follows.
      *> FAILS IF: EXIT METHOD is refused in a method, does not end the
      *>   method (R=BAD ), or does not return the RETURNING item.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXT03C INHERITS FROM BASE.
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
           USE AFTER EXCEPTION CONDITION EC-SIZE-TRUNCATION.
       DM-P.
           EXIT METHOD RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MM SECTION.
       MM1.
           MOVE "GOOD" TO LR.
           EXIT METHOD.
           MOVE "BAD " TO LR.
       END METHOD GETV.
       END OBJECT.
       END CLASS G2EXT03C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2EXT03C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2EXT03C.
       01 R PIC X(4) VALUE "INIT".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2EXT03C "NEW" RETURNING O.
           INVOKE O "GETV" RETURNING R.
           DISPLAY "R=" R.
           DISPLAY "AFTER-INVOKE".
           STOP RUN.
       END PROGRAM G2EXT03.
