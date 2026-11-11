      *>--- conformance-case
      *> id: pb5167-empty-repository
      *> rules: [1985-FMT-IV.6.2.3.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 IV 6.2.3.1 prints the CONFIGURATION SECTION with
      * three paragraphs, SOURCE-COMPUTER, OBJECT-COMPUTER and
      * SPECIAL-NAMES; the REPOSITORY paragraph is ISO 2002's. Each
      * REPOSITORY entry was refused at 85, the empty paragraph was
      * not (kb/Work PB5167); it is refused with COBOL0900, the
      * construct row repository-paragraph-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5167R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "EMPTY REPOSITORY".
           STOP RUN.
