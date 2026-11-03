      *>--- conformance-case
      *> id: pb2104_expand_skeleton_refused_at_85
      *> rules: [SR-12.3.8.3-4, GR-12.3.8.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104. The class-specifier with its EXPANDS phrase (12.3.8.2)
      *> and the object reference to a class are COBOL-2002 introductions, so
      *> the program that expands a skeleton in pb2104_expand_skeleton_from_
      *> repository is refused below that edition: wiseowl/diagnostics/
      *> pb2104_expand_skeleton_refused_at_85.json.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104E85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P2104-Box"
           CLASS HOLD AS "P2104-Hold"
           CLASS HOLD-BOX EXPANDS HOLD USING BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B  USAGE OBJECT REFERENCE BOX.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM P2104E85.
