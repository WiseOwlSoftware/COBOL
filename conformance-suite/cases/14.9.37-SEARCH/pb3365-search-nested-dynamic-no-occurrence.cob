*>--- conformance-case
*> id: pb3365-search-nested-dynamic-no-occurrence
*> rules: [SR-14.9.37.3-3, GR-14.9.37.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3365 - a dynamic-capacity table inside another table is bounded by the current capacity (ISO
      *> 8.5.1.9.1) of ONE occurrence, and that occurrence is named "in the WHEN phrases" (14.9.37.4 GR1) or on
      *> identifier-1 (14.9.37.3 SR2 permits the enclosing subscripts there). This SEARCH names it in neither: identifier-1
      *> is bare and its only WHEN phrase references no element of EP, so there is no occurrence whose capacity could
      *> bound the scan. (A fixed or occurs-depending table has one bound for every occurrence of its enclosing tables,
      *> which is why the same shape over such a table is legal.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3365NOOCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BR.
          05 E OCCURS 3.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP INDEXED BY EX.
       01 J PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH EP
               AT END DISPLAY "END"
               WHEN J = 5 DISPLAY "FOUND"
           END-SEARCH.
           STOP RUN.
