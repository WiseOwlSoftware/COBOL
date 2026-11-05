      *>--- conformance-case
      *> id: pb4650_unused_actual_expansion_refused_at_85
      *> rules: [SR-12.3.8.3-4, GR-12.3.8.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4650. The class-specifier with its EXPANDS phrase (12.3.8.2)
      *> is a COBOL-2002 introduction, so the program that expands COUNTER in
      *> pb4650_unused_actual_expansion_is_one_class is refused below that
      *> edition: wiseowl/diagnostics/pb4650_unused_actual_expansion_refused_at_85.json.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4650E85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P4650-Box"
           CLASS COUNTER AS "P4650-Counter"
           CLASS COUNTER-BOX EXPANDS COUNTER USING BOX.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM P4650E85.
