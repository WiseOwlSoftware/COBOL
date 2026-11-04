      *>--- conformance-case
      *> id: pb4577_search_bare_nested_table_85
      *> rules: [SR-14.9.37.3-2, SR-14.9.37.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4577 - the spelling of a nested table's SEARCH that is legal at EVERY edition: identifier-1 BARE. The
      *> 2002 text, the 2009 committee draft and the 2014 FCD 1.0 say "Identifier-1 shall not be subscripted or reference modified", which
      *> this satisfies; COBOL-2023 also admits the enclosing tables' subscripts (pb4577_search_enclosing_subscript_2023).
      *> ISO/IEC 1989:2023 14.9.37.4 GR1 (cite.py --check 14.9.37.4 "The subscript that is used to determine the occurrence
      *> of each superordinate table to search is specified by the user in the WHEN phrases" -> OK) puts the enclosing
      *> occurrence in the WHEN phrases, so GI (2) selects the second row 7 8 9 through H (GI, HI), exactly as the subscripted
      *> spelling would, and the answers are the same as that golden's:
      *>   SEARCH H WHEN H (GI, HI) > 7           - FOUND 8.
      *>   SEARCH ALL H WHEN H (GI, HI) = 9       - FOUND 9.
      *> This is the form the CCVS-85 suite writes (NC233A: SEARCH ALL GRP2-ENTRY ... WHEN SEC (IDX-1, IDX-2)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4577B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-VALS PIC X(6) VALUE "123789".
       01  T REDEFINES T-VALS.
           05  G OCCURS 2 TIMES INDEXED BY GI.
               10  H PIC 9 OCCURS 3 TIMES ASCENDING KEY IS H
                   INDEXED BY HI.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET GI TO 2.
           SET HI TO 1.
           SEARCH H
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) > 7
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           SEARCH ALL H
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) = 9
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           STOP RUN.
