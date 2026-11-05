*>--- conformance-case
*> id: pb4678-typedef-unused-value-oversize-national
*> rules: [SR-13.18.63.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR5 sentence 2: "National literals in the VALUE clause of an elementary item shall not
      *> exceed the size indicated by an explicit PICTURE clause."  13.18.58.4 GR1 makes the entry a type
      *> declaration whose clauses are source like any other; a type declaration allocates no storage (GR2), but
      *> its VALUE clause is still a VALUE clause of an elementary item.  ZZ9 is three national positions and
      *> N"ABCDEF" is six.  No item is described with TYPE NT, so the rule is a property of the declaration and the
      *> compiler has to answer it THERE: it used to answer only at a TYPE user, and compiled this program clean
      *> (kb/Work PB4678).  The conforming boundary is pinned by pb4678_typedef_value_national_edited.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4678N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NT TYPEDEF PIC ZZ9 USAGE NATIONAL VALUE N"ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
