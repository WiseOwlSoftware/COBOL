      *>--- conformance-case
      *> id: g2-search-identifier-1-no-indexed
      *> rules: [2002-SR-14.8.34.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR1 - "Identifier-1 shall not be subscripted
      *> or reference modified and its data description entry shall
      *> contain an OCCURS clause including an INDEXED phrase."
      *> Leg: identifier-1's OCCURS has no INDEXED phrase: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U VALUE "ABC".
          05 NE PIC X OCCURS 3.
       01 N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           SEARCH NE VARYING N
               AT END DISPLAY "NOT-FOUND"
               WHEN NE(N) = "B" DISPLAY "FOUND"
           END-SEARCH.
           STOP RUN.
