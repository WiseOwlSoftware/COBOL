      *> reject-at: 2002 2014 2023
      *> ISO 15.52.3 r3: "Locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph." NOPE is no
      *> LOCALE clause's name - the ONE undeclared-locale-name diagnostic (COBOLNET1664) citing this function's rule
      *> (kb/Work PB64 T4; DESIGN-locale-facility 7 rule a: one code, every site).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T4UND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION LOCALE-DATE("20260819" NOPE) TO S.
           STOP RUN.
