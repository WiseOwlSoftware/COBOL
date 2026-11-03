      *>--- conformance-case
      *> id: pb2104_expansion_is_one_class_across_groups
      *> rules: [GR-12.3.8.4-5, DOC-A.1-138, DOC-A.1-201]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2104_skeleton_lib.cbl, pb2104_expansion_callee.cbl]
      *> build: ["pb2104_skeleton_lib.cbl > P2104SKL", "pb2104_expansion_callee.cbl > P2104CAL", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104 (DESIGN-external-repository 10.5, decision D3). 9.3.12:
      *> "Within a run unit, two classes with the same externalized object-
      *> class-name that are created by expanding the same parameterized
      *> class with the same actual parameters are the same class instance."
      *> This program and the program it calls (the build step P2104CAL) are
      *> two compilation groups; each writes the same expansion HOLD-BOX of
      *> P2104-Hold over P2104-Box. If each made a class of its own the
      *> two would be two CLR types and the object this program passes would
      *> not be an object of the class the callee's formal names. The group
      *> compiled second finds the first's expansion in the external
      *> repository (same name, same definition and actual, same key) and
      *> names that class, so one class exists and the callee's formal
      *> accepts the argument: the activation conforms (14.8.2.3.2 rule 3,
      *> "the same object-class-name"), EC-PROGRAM-ARG-MISMATCH sets no
      *> exception, and the callee's SHOW-IT shows the Box that was set to 7.
      *> Each line can fail: BOX 007 is missing if the callee did not
      *> receive the object (two classes), and 02 reads ARG MISMATCH if
      *> the activation did not conform.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104ONE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P2104-Box"
           CLASS HOLD AS "P2104-Hold"
           CLASS HOLD-BOX EXPANDS HOLD USING BOX
           PROGRAM P2104CAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B  USAGE OBJECT REFERENCE BOX.
       01 HB USAGE OBJECT REFERENCE HOLD-BOX.
       PROCEDURE DIVISION.
           INVOKE BOX "NEW" RETURNING B
           INVOKE B "SETV" USING 7
           INVOKE HOLD-BOX "NEW" RETURNING HB
           INVOKE HB "PUT" USING B
           CALL P2104CAL USING HB
               ON EXCEPTION DISPLAY "02 ARG MISMATCH"
               NOT ON EXCEPTION DISPLAY "02 CONFORMS"
           END-CALL
           STOP RUN.
       END PROGRAM P2104ONE.
