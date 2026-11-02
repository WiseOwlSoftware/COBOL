*>--- conformance-case
*> id: pb4359-numval-c-anycase-below-2002
*> rules: [FMT-15.68.2]
*> source-format: free
*> portable: unverified
*>---
      *> The control of pb4359_numval_c_anycase_2002 (kb/Work PB4359). ANYCASE joins the NUMVAL-C format with 2002
      *> (15.68.2: ( argument-1 { LOCALE [ locale-name-1 ] / argument-2 } [ ANYCASE ] )); below 2002 it is a
      *> user-defined word (reserved-words.json: r85 false), so the keyword is refused by edition (COBOL0900) at 85.
      *> (TEST-NUMVAL-C is itself a 2002 function, so NUMVAL-C carries the 85 refusal.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4359BEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(7)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C("usd 1,234.56" "USD" ANYCASE)
           STOP RUN.
