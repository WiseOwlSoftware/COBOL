      *>--- conformance-case
      *> id: g2-class-alpha-on-numeric-edited
      *> rules: [2002-SR-8.8.4.1.3.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.3.2 SR5: "ALPHABETIC, ALPHABETIC-LOWER,
      *> ALPHABETIC-UPPER, or class-name-1 shall not be specified if
      *> the category of the data item referenced by identifier-1 is
      *> boolean, numeric, or numeric-edited."     cite.py: OK SR5
      *> A prohibiting syntax rule that grants no form, so a refusal
      *> is its evidence. Every operand below is usage display, so
      *> SR3 does not bar it; only SR5 does. The four phrases each
      *> appear once and the three categories each at least once:
      *>   NE (numeric-edited) IS ALPHABETIC
      *>   NU (numeric)        IS ALPHABETIC-LOWER
      *>   BB (boolean)        IS ALPHABETIC-UPPER
      *>   NE (numeric-edited) IS G2DIG (class-name-1)
      *> Every line is refused (COBOL6000). The case fails if any of
      *> them compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLR01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS G2DIG IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NE PIC ZZ9.
       01  NU PIC 9(3) VALUE 7.
       01  BB PIC 1(4) VALUE B"0101".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF NE IS ALPHABETIC DISPLAY "1" END-IF
           IF NU IS ALPHABETIC-LOWER DISPLAY "2" END-IF
           IF BB IS ALPHABETIC-UPPER DISPLAY "3" END-IF
           IF NE IS G2DIG DISPLAY "4" END-IF
           STOP RUN.
