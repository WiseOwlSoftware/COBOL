      *>--- conformance-case
      *> id: pb5302_search_advance_below_table_85
      *> rules: [GR-14.9.37.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5302 - a format 1 SEARCH entered with the index TWO below the table, at COBOL-85.
      *>
      *> §1985:VI.6.22.4 GR2a (checked with scripts/spec/cite.py --check): only an index greater than the highest
      *> permissible occurrence number ends the search at its start, so the scan starts at IX = -1 (kb/Work PB5121).
      *> §1985:VI.6.22.4 GR2b: after an unsuccessful pass the index is incremented and the process repeats "unless
      *> the new value of the index-name settings for identifier-1 corresponds to a table element outside the
      *> permissible range of occurrence values, in which case the search terminates as indicated in 2a above".
      *> The new value 0 is outside the range, so the search ends with AT END. (GR10: the Figure 1 flow, which
      *> loops back to the "greater than" test only, "is not intended to dictate implementation".) An index of 0
      *> at the start advances to occurrence 1 instead (pb5121_search_index_below_table_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5302A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-INIT PIC X(5) VALUE "ABCDE".
       01  TBL REDEFINES TBL-INIT.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM NULL-PARA VARYING IX FROM 3 BY -2 UNTIL IX < 1.
           SEARCH ELT AT END DISPLAY "AT END"
               WHEN IX = 2 DISPLAY "FOUND AT 2".
           STOP RUN.
       NULL-PARA.
           EXIT.
