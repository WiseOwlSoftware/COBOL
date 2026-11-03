      *>--- conformance-case
      *> id: pb2104_expand_skeleton_from_repository
      *> rules: [SR-12.3.8.3-4, SR-12.3.8.3-6, GR-12.3.8.4-5, DOC-A.1-138]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2104_skeleton_lib.cbl]
      *> build: ["pb2104_skeleton_lib.cbl > P2104SKL", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104 (DESIGN-external-repository 10.5). 9.3.12: "An
      *> expansion of a parameterized class is treated in all respects the
      *> same as if it were a class that is not a parameterized class."
      *> 12.3.8.4 general rule 5: "The class object-class-name-1 is
      *> created from the parameterized class object-class-name-2 by
      *> replacing each specification of the formal parameter by the
      *> corresponding actual parameter." The parameterized class
      *> P2104-Hold and the class P2104-Box are defined by another
      *> compilation group (the build step): this program names them in
      *> its REPOSITORY (12.3.8.3 syntax rule 6 b: information in the
      *> external repository for each) and expands the one over the
      *> other. HOLD-BOX is therefore P2104-Hold with ELEM read as
      *> P2104-Box: PUT stores the Box it is given and SHOW-IT invokes
      *> the Box's SHOW, which shows the number the Box was set to, so
      *> "BOX 042". Each line can fail: the skeleton cannot be found,
      *> the expansion cannot be made from its recorded text, or the
      *> expansion does not name the Box the actual names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104EXP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P2104-Box"
           CLASS HOLD AS "P2104-Hold"
           CLASS HOLD-BOX EXPANDS HOLD USING BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B  USAGE OBJECT REFERENCE BOX.
       01 HB USAGE OBJECT REFERENCE HOLD-BOX.
       PROCEDURE DIVISION.
           INVOKE BOX "NEW" RETURNING B
           INVOKE B "SETV" USING 42
           INVOKE HOLD-BOX "NEW" RETURNING HB
           INVOKE HB "PUT" USING B
           INVOKE HB "SHOW-IT"
           STOP RUN.
       END PROGRAM P2104EXP.
