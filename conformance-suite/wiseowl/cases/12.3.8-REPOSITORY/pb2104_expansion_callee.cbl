      *> kb/Work PB2104: the callee of pb2104_expansion_is_one_class_across_groups,
      *> compiled apart (its build step). It expands the same skeleton over the
      *> same actual as the caller does, in a compilation group of its own, and
      *> takes an object of the expansion.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104CAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P2104-Box"
           CLASS HOLD AS "P2104-Hold"
           CLASS HOLD-BOX EXPANDS HOLD USING BOX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 H USAGE OBJECT REFERENCE HOLD-BOX.
       PROCEDURE DIVISION USING H.
           INVOKE H "SHOW-IT"
           GOBACK.
       END PROGRAM P2104CAL.
