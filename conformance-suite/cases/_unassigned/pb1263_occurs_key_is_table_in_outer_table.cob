      *>--- conformance-case
      *> id: pb1263_occurs_key_is_table_in_outer_table
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4609: accept is 2023, not 2014 (PB4577 had it at 2014). `SEARCH ALL T (2)` writes an enclosing table's subscript on SEARCH
      *> identifier-1, which the 2002 text, the 2009 committee draft and the 2014 FCD 1.0 (14.9.34.2 ALL FORMATS rule 1) refuse ("Identifier-1 shall
      *> not be subscripted or reference modified") and only COBOL-2023 permits (14.9.37.3 SR2, SR3): construct
      *> search-identifier-1-enclosing-subscript-2023. The legal spelling of the same search at every edition is the bare one
      *> (conformance:cases/14.9.37-SEARCH/pb4577_search_bare_nested_table_85).
      *> kb/Work PB1263 - SR4 asks only about a group BETWEEN the table entry and its KEY. When the
      *> KEY is the table's own subject (SR6 admits that), no group lies between them, so an
      *> OCCURS group that ENCLOSES the table is irrelevant: T is its own key inside OUTER OCCURS 2.
      *> Fails if the key is refused (COBOL2787) or the search misses the element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 OUTER OCCURS 2.
               10 T PIC 9 OCCURS 3 ASCENDING KEY IS T INDEXED BY IX.
       PROCEDURE DIVISION.
       PB4305-LEAD.
           MOVE "123456" TO R
           SEARCH ALL T (2) AT END DISPLAY "NONE"
               WHEN T (2 IX) = 5 DISPLAY "FOUND 5"
           END-SEARCH
           STOP RUN.
