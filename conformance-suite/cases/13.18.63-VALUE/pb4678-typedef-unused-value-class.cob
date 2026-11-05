*>--- conformance-case
*> id: pb4678-typedef-unused-value-class
*> rules: [SR-13.18.63.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR7: "If the item is of category numeric-edited and the literal is of class alphanumeric or
      *> national, the class of the literal shall conform to that of the data item".  8.5.2.1 Table 2 makes a
      *> numeric-edited item of usage national of class national, so the alphanumeric literal "ABC" does not
      *> conform.  The entry is a type declaration (13.18.58.4 GR1) that no item references: the rule is a
      *> property of the declaration, and the compiler answered it only at a TYPE user (kb/Work PB4678).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4678C1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NT TYPEDEF PIC ZZ9 USAGE NATIONAL VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
