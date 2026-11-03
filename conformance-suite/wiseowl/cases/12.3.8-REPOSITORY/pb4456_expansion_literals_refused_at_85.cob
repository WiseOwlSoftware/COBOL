      *>--- conformance-case
      *> id: pb4456_expansion_literals_refused_at_85
      *> rules: [SR-12.3.8.3-4, GR-12.3.8.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4456. The class-specifier with its EXPANDS phrase (12.3.8.2)
      *> and the object reference to a class are COBOL-2002 introductions, so
      *> the program that expands the skeleton of pb4456_expansion_literals_at_
      *> every_column is refused below that edition: wiseowl/diagnostics/
      *> pb4456_expansion_literals_refused_at_85.json.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4456E85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P4456-Box"
           CLASS HOLD AS "P4456-Hold"
           CLASS HOLD-BOX EXPANDS HOLD USING BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B  USAGE OBJECT REFERENCE BOX.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM P4456E85.
