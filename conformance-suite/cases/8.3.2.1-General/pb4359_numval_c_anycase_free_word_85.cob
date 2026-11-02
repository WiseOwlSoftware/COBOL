      *>--- conformance-case
      *> id: pb4359_numval_c_anycase_free_word_85
      *> rules: [SR-8.3.2.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ANYCASE AND LOCALE ARE USER-DEFINED WORDS AT COBOL-85 (kb/Work PB4359, review round 1). Both are reserved
      *> words only from 2002 (8.9; reserved-words.json r85 false), and 8.3.2.1 rule 1 forbids a RESERVED word as a
      *> user-defined word - so at COBOL-85 a program may declare data items of those names, and NUMVAL-C's
      *> argument-2 slot (15.68.2) then names them as OPERANDS: the program is conforming and must not be refused
      *> as if it had written the 2002 keywords. 15.68.4 r1, r2: the currency string argument-2 and the grouping
      *> separator are ignored in the value.
      *>   BYANY - NUMVAL-C("USD1,234.56" ANYCASE) with 01 ANYCASE PIC X(3) VALUE "USD": argument-2 is "USD",
      *>           the value 1234.56.
      *>   BYLOC - NUMVAL-C("EUR9.99" LOCALE) with 01 LOCALE PIC X(3) VALUE "EUR": argument-2 is "EUR", 9.99.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4359FRE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ANYCASE PIC X(3) VALUE "USD".
       01 LOCALE PIC X(3) VALUE "EUR".
       01 R PIC S9(7)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION NUMVAL-C("USD1,234.56" ANYCASE)
           IF R = 1234.56
              DISPLAY "BYANY OK"
           ELSE
              DISPLAY "BYANY BAD " R
           END-IF
           COMPUTE R = FUNCTION NUMVAL-C("EUR9.99" LOCALE)
           IF R = 9.99
              DISPLAY "BYLOC OK"
           ELSE
              DISPLAY "BYLOC BAD " R
           END-IF
           STOP RUN.
