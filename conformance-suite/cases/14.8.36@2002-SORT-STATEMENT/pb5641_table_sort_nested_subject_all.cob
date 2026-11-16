      *>--- conformance-case
      *> id: pb5641_table_sort_nested_subject_all
      *> rules: [2002-SR-14.8.36.2-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.36.2 SR13: "If data-name-2 is subject to more than one level of OCCURS clauses, subscripts shall be
      *> specified for all levels with the exception that the rightmost subscript shall be the word ALL." E lies in
      *> ROW OCCURS 2, so SORT E (2, ALL) sorts row 2: CBA FED becomes CBA DEF, then SORT E (1, ALL) gives ABC DEF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5641-ALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "CBAFED".
          05 ROW OCCURS 2.
             10 E PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           SORT E (2, ALL) ASCENDING.
           DISPLAY T.
           SORT E (1, ALL) ASCENDING.
           DISPLAY T.
           STOP RUN.
