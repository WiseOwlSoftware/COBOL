      *>--- conformance-case
      *> id: pb5121_search_index_below_table
      *> rules: [GR-14.9.37.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5121 - a format 1 SEARCH entered with the index BELOW the table, from COBOL-2002.
      *>
      *> §2023:14.9.37.4 GR4 (checked with scripts/spec/cite.py --check): if the search index at the start "corresponds
      *> to an occurrence number that is negative, zero, or greater than the highest permissible occurrence number for
      *> identifier-1, the search operation is unsuccessful". The PERFORM below leaves IX at occurrence 0, so the AT END
      *> phrase runs and no WHEN condition is evaluated. COBOL-85 starts the scan instead
      *> (pb5121_search_index_below_table_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5121B.
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
