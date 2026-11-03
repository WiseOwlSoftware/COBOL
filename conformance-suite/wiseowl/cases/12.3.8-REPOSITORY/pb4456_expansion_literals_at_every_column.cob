      *>--- conformance-case
      *> id: pb4456_expansion_literals_at_every_column
      *> rules: [GR-6.5-8, SR-12.3.8.3-4, SR-12.3.8.3-6, GR-12.3.8.4-5, DOC-A.1-138]
      *> accept: [2002, 2014, 2023]
      *> files: [pb4456_literal_run_lib.cbl]
      *> build: ["pb4456_literal_run_lib.cbl > P4456LIB", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4456 (DESIGN-external-repository 10.5). 6.4.2 "At least
      *> one alphanumeric character, national character, or hexadecimal
      *> digit of the literal content shall be specified on the continued
      *> line and on each continuation line", and "A pair of quotation
      *> symbols indicating a single quotation symbol within a literal
      *> shall be specified on the same line". 12.3.8.4 general rule 5:
      *> "The class object-class-name-1 is created from the parameterized
      *> class object-class-name-2 by replacing each specification of the
      *> formal parameter by the corresponding actual parameter." The
      *> parameterized class P4456-Hold (the build step) has a method that
      *> displays a long run of short literals (alphanumeric in both
      *> delimiters, with a doubled delimiter first, inside, last and
      *> alone, and hexadecimal) and then invokes the element. The
      *> expansion names only the external class P4456-Box, so it is
      *> compiled in an assembly of its own from its text, which is
      *> written as free-form lines of at most 200 positions: over the run
      *> a literal opens at every column of a line, including the last
      *> positions where its first content unit does not fit before the
      *> floating continuation indicator. SHOW-IT therefore shows each
      *> statement's literals joined, one line each, and then "BOX 042".
      *> Each line can fail: a literal read back wrong, the expansion text
      *> refused for a continued line with no content (COBOL2690), or the
      *> element not invoked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4456EXP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P4456-Box"
           CLASS HOLD AS "P4456-Hold"
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
       END PROGRAM P4456EXP.
