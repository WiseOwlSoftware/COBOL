      *>--- conformance-case
      *> id: pb5641_table_sort_nested_subject_omits_its_level
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5641. From COBOL-2014 a Format-2 SORT subject names the table it sorts and writes a subscript for each
      *> ENCLOSING table only (§2023:8.4.2.3.3 SR5 e, SR6), so SORT E (2) sorts the E of ROW (2). At COBOL-2002 the same
      *> source is refused (§2002:14.8.36.2 SR13: the rightmost subscript shall be the word ALL), pinned by the refusal
      *> pb5641-table-sort-nested-subject-needs-all. T holds CBA FED; sorting row 2 gives CBA DEF, and the ALL spelling of
      *> row 1 gives ABC DEF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5641-OMIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "CBAFED".
          05 ROW OCCURS 2.
             10 E PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           SORT E (2) ASCENDING.
           DISPLAY T.
           SORT E (1, ALL) ASCENDING.
           DISPLAY T.
           STOP RUN.
