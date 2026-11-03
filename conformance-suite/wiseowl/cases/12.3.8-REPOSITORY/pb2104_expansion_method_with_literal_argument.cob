      *>--- conformance-case
      *> id: pb2104_expansion_method_with_literal_argument
      *> rules: [GR-12.3.8.4-5, DOC-A.1-138]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2104_skeleton_lib.cbl]
      *> build: ["pb2104_skeleton_lib.cbl > P2104SKL", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104 (DESIGN-external-repository 10.5). 9.3.12: "An
      *> expansion of a parameterized class is treated in all respects the
      *> same as if it were a class that is not a parameterized class."
      *> A class that is not parameterized, defined by another compilation
      *> group, accepts a numeric literal as the argument of one of its
      *> methods: with none of BY REFERENCE, BY CONTENT or BY VALUE written,
      *> the manner of passing is determined by general rule 6 of 14.9.23.4
      *> and the formal receives the literal's value. So HOLD-BOX, the expansion of
      *> P2104-Hold over P2104-Box that lives in an assembly of its own,
      *> does the same: INVOKE HB "SETN" USING 42 shows "N 042". The line
      *> fails to compile if the literal is crossed through the owner's
      *> internal profile, which is a member of the other assembly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104LIT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P2104-Box"
           CLASS HOLD AS "P2104-Hold"
           CLASS HOLD-BOX EXPANDS HOLD USING BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HB USAGE OBJECT REFERENCE HOLD-BOX.
       PROCEDURE DIVISION.
           INVOKE HOLD-BOX "NEW" RETURNING HB
           INVOKE HB "SETN" USING 42
           STOP RUN.
       END PROGRAM P2104LIT.
