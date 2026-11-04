      *>--- conformance-case
      *> id: pb4577_search_enclosing_subscript_2023
      *> rules: [SR-14.9.37.3-2, SR-14.9.37.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4609 (PB4577) - SEARCH identifier-1 written WITH the subscript of the table that encloses it, a COBOL-2023
      *> permission (construct search-identifier-1-enclosing-subscript-2023). ISO/IEC 1989:2023 14.9.37.3 SR2 (cite.py
      *> --check 14.9.37.3 "identifier-1 shall not be subscripted at the level for which the SEARCH is applicable" -> OK 2))
      *> bans the SEARCHED level's subscript alone, and SR3 (cite.py --check 14.9.37.3 "Identifier-1 may be contained within
      *> one or more other tables, for which the subscripting is still required" -> OK 3)) keeps the enclosing tables'. The 2002
      *> text, the 2009 committee draft (CD 1.2) and the 2014 FCD 1.0 (14.9.34.2 ALL FORMATS rule 1) say "Identifier-1 shall not be subscripted or
      *> reference modified", so the form is refused below 2023 (conformance wiseowl/diagnostics pb4577-*-below-2023).
      *> The table is G (2) of H (3), the digits 1 2 3 and 7 8 9 in storage order. GI is 2, so H (GI) is the second row,
      *> 7 8 9, and the search varies HI from its starting value 1 (14.9.37.4 GR1):
      *>   SEARCH H (GI) WHEN H (GI, HI) > 7        - HI 1 holds 7 (no), HI 2 holds 8: FOUND 8.
      *>   SEARCH H (GI) WHEN H (GI, HI) > 9        - no occurrence qualifies: AT END, NOT FOUND.
      *>   SEARCH ALL H (GI) WHEN H (GI, HI) = 9    - the row is ascending (ASCENDING KEY IS H); the key 9 is found: FOUND 9.
      *> Leg that can fail: the enclosing subscript dropped (the search then runs over the first row, 1 2 3, and prints
      *> NOT FOUND for the first search), or SEARCH ALL refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4577A.
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
           SEARCH H (GI)
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) > 7
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           SET HI TO 1.
           SEARCH H (GI)
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) > 9
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           SEARCH ALL H (GI)
               AT END DISPLAY "NOT FOUND"
               WHEN H (GI, HI) = 9
                   DISPLAY "FOUND " H (GI, HI)
           END-SEARCH.
           STOP RUN.
