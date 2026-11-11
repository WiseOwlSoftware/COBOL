      *>--- conformance-case
      *> id: pb5121_search_index_below_table_85
      *> rules: [GR-14.9.37.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5121 - a format 1 SEARCH entered with the index BELOW the table, at COBOL-85.
      *>
      *> §1985:VI.6.22.4 GR2 (checked with scripts/spec/cite.py --check): an index that "is greater than the highest
      *> permissible occurrence number for identifier-1, the search is terminated immediately"; otherwise the search
      *> "operates by evaluating the conditions in the order that they are written" and, when none holds, the index
      *> "is incremented to obtain reference to the next occurrence". §1985:VI.6.23.4 GR2: a PERFORM may leave the
      *> index "outside the range of its associated table". Only the upper bound ends the search at the start, so the
      *> PERFORM below leaves IX at occurrence 0 (not greater than 5), the scan evaluates IX = 2 at occurrence 0
      *> (false), 1 (false) and 2 (true), and the output is FOUND AT 2.
      *> From 2002 the start test also refuses an index below 1 (pb5121_search_index_below_table).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5121A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-INIT PIC X(5) VALUE "ABCDE".
       01  TBL REDEFINES TBL-INIT.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM NULL-PARA VARYING IX FROM 2 BY -1 UNTIL IX < 1.
           SEARCH ELT AT END DISPLAY "AT END"
               WHEN IX = 2 DISPLAY "FOUND AT 2".
           STOP RUN.
       NULL-PARA.
           EXIT.
