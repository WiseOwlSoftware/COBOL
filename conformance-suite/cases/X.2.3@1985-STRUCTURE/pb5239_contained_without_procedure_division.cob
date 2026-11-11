      *>--- conformance-case
      *> id: pb5239_contained_without_procedure_division
      *> rules: [1985-FMT-X.2.3.1, 1985-GR-X.2.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *>---
      * X3.23-1985 X 2.3.1 prints a source program as identification-
      * division [ environment-division ] [ data-division ]
      * [ procedure-division ] [ source-program ] ... [ end-program-
      * header ]: the Procedure Division and the contained programs
      * are separate brackets, and GR1 lets a division end at "An
      * Identification Division header which indicates the start of
      * another source program". So PB5239O, with an Environment and
      * a Data Division and no Procedure Division, may contain
      * PB5239I (kb/Work PB5239); ISO 2002 and 2023 nest the slot in
      * the procedure-division bracket and refuse it (COBOL2274,
      * pb1507-contained-program-without-procedure-division). The
      * test fails if the program is refused; it has nothing to run,
      * so it displays nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5239O.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "A".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5239I.
       PROCEDURE DIVISION.
       IP.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM PB5239I.
       END PROGRAM PB5239O.
