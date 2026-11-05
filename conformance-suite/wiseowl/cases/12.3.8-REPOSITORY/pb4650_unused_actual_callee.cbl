      *> kb/Work PB4650: the callee of pb4650_unused_actual_expansion_is_one_class,
      *> compiled apart (its build step). It expands COUNTER over BOX in a
      *> compilation group of its own and bumps the factory's count once.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4650CAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P4650-Box"
           CLASS COUNTER AS "P4650-Counter"
           CLASS COUNTER-BOX EXPANDS COUNTER USING BOX.
       PROCEDURE DIVISION.
           INVOKE COUNTER-BOX "BUMP"
           GOBACK.
       END PROGRAM P4650CAL.
