*>--- conformance-case
*> id: pb4359_numval_c_anycase_2002
*> rules: [FMT-15.68.2, FMT-15.94.2, AR-15.68.3-4, AR-15.94.3-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ANYCASE, THE KEYWORD OF FUNCTION NUMVAL-C / TEST-NUMVAL-C, IS A 2002 INTRODUCTION (kb/Work PB4359).
      *> 15.68.2 prints  FUNCTION NUMVAL-C ( argument-1 { LOCALE [ locale-name-1 ] / argument-2 } [ ANYCASE ] )  and
      *> 15.94.2 the same for TEST-NUMVAL-C; ISO/IEC 1989:2002 (15.55 / 15.76) printed [ ANYCASE ] already, and
      *> ANYCASE is a reserved word from 2002 (8.9) - below 2002 it is an ordinary user-defined word. This test is the
      *> introducing edition's witness; its refusal below 2002 is pb4359-numval-c-anycase-below-2002.
      *> The non-LOCALE form (the one that is NOT also a locale feature): 15.68.3 r4 f folds BOTH argument-1 and the
      *> currency string to lowercase before the currency string is matched.
      *>   ANYSTR - NUMVAL-C("usd 1,234.56" "USD" ANYCASE): argument-2 "USD" matches the written "usd" after the
      *>            fold, so the value is 1234.56 (15.68.4 r1, r2: the currency string and the grouping separator
      *>            are ignored).
      *>   ANYDEF - NUMVAL-C("$1,234.56" ANYCASE): neither argument-2 nor LOCALE, so 15.68.3 r3 supplies the
      *>            one currency string of the unit, the default sign '$'; a sign has no case, so the fold changes
      *>            nothing: 1234.56.
      *>   TANY   - TEST-NUMVAL-C("usd 1,234.56" "USD" ANYCASE): the argument conforms, so 15.94.4 r1 a is 0.
      *>   TPLAIN - the same call WITHOUT ANYCASE: the match is character for character (15.68.3 r4 a), the 'u' at
      *>            position 1 matches no currency string, and 15.94.4 r1 b returns the position of the first
      *>            character in error: 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4359ANY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(7)V99.
       01 T PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION NUMVAL-C("usd 1,234.56" "USD" ANYCASE)
           IF R = 1234.56 DISPLAY "ANYSTR OK" ELSE DISPLAY "ANYSTR BAD " R END-IF
           COMPUTE R = FUNCTION NUMVAL-C("$1,234.56" ANYCASE)
           IF R = 1234.56 DISPLAY "ANYDEF OK" ELSE DISPLAY "ANYDEF BAD " R END-IF
           MOVE FUNCTION TEST-NUMVAL-C("usd 1,234.56" "USD" ANYCASE) TO T
           IF T = 0 DISPLAY "TANY OK" ELSE DISPLAY "TANY BAD " T END-IF
           MOVE FUNCTION TEST-NUMVAL-C("usd 1,234.56" "USD") TO T
           IF T = 1 DISPLAY "TPLAIN OK" ELSE DISPLAY "TPLAIN BAD " T END-IF
           STOP RUN.
