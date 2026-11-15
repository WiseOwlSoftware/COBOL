      *>--- conformance-case
      *> id: pb5641-table-sort-nested-subject-needs-all
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5641. §2002:14.8.36.2 SR13: "subscripts shall be specified for all levels with the exception that the
      *> rightmost subscript shall be the word ALL." SORT E (2) names the enclosing ROW and writes no rightmost ALL.
      *> Expected at 2002: COBOL7369. From 2014 the subject omits its own level (pb5641_table_sort_nested_subject_omits_its_level).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5641-NOALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "CBAFED".
          05 ROW OCCURS 2.
             10 E PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           SORT E (2) ASCENDING.
           DISPLAY T.
           STOP RUN.
