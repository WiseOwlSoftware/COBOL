*>--- conformance-case
*> id: pb5614-implied-dollar-national-refused-from-2014
*> rules: [SR-12.3.7.3-25, SR-12.3.7.3-28]
*> source-format: free
*>---
      *> kb/Work PB5614 - from COBOL-2014 §2014:12.3.6.2 SR25 says "If a source unit does not contain a CURRENCY SIGN
      *> clause that specifies '$' as the currency symbol (either as literal-7 or literal-8), the clause CURRENCY SIGN
      *> '$' PICTURE SYMBOL '$' is implied for that source unit", and its SR28 (the same rule as §2023:12.3.7.3 SR28)
      *> confines a symbol whose literal-7 is alphanumeric to a numeric-edited item with usage display. The implied
      *> clause's literal-7 is the alphanumeric '$', so the national item NC5 is refused at 2014 and 2023. At
      *> COBOL-2002 there is no implied clause and the same item is accepted
      *> (g2002-default-currency-sign-national-edited).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5614IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC5 PIC $9.99 USAGE NATIONAL.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED"
           STOP RUN.
