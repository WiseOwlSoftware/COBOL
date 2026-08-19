      *> reject-at: 2002 2014 2023
      *> ISO 15.97.2 / 15.97.4 r2: the LOCALE phrase of UPPER-CASE names a locale-name of the SPECIAL-NAMES LOCALE clause;
      *> NOPE is none - COBOLNET1664 (kb/Work PB64 T5; the phrase was refused by name with COBOLNET1518 before T5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5PUND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION UPPER-CASE("abc" LOCALE NOPE) TO S.
           STOP RUN.
