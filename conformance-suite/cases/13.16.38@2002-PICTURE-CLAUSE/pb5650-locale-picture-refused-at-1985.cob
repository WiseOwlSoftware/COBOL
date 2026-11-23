      *>--- conformance-case
      *> id: pb5650-locale-picture-refused-at-1985
      *> rules: [2002-GR-13.16.38.4-13]
      *> source-format: fixed
      *>---
      *> The PICTURE LOCALE phrase and the SPECIAL-NAMES LOCALE clause
      *> are COBOL 2002 features (§2002:13.16.38.4 GR13); a 1985 compile
      *> refuses the program with COBOL0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5650N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE BRA IS "pt-BR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L7 PIC +ZZZ9.99 LOCALE IS BRA SIZE IS 14.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE -1234.5 TO L7
           STOP RUN.
