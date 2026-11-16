      *>--- conformance-case
      *> id: g2002-cd-index-data-item-contexts
      *> rules: [2002-SR-13.16.58.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> At 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2), so IX, a
      *> USAGE INDEX item (§2002:13.16.58.3 GR9) of the CD record CR,
      *> is an index data item like one in working storage.
      *> §2002:13.16.58.2 SR9 - "An index data item may be referenced
      *>   explicitly only in a SEARCH or SET statement, a relation
      *>   condition, an intrinsic function argument, ..." IX is
      *>   referenced in three of the permitted contexts:
      *>   SET - X1 set to 3, IX set to X1, X1 reset to 1, X1 set to IX:
      *>     X1 is 3 again (line SET OK);
      *>   relation condition - IX = X1 holds (line REL OK);
      *>   SEARCH VARYING - §2002:14.8.34.2 SR3 admits an index data
      *>     item as identifier-2, and §2002:14.8.34.3 GR3b: "If
      *>     identifier-2 references an index data item, that data item
      *>     is incremented by the same amount as, and at the same time
      *>     as, the search index." From occurrence 1 the search stops
      *>     at E(3) = "C" (line FOUND C); X1 and IX each moved two
      *>     occurrences, so X2 set to IX is 3 (line VARY OK).
      *> It fails if a CD-record index data item is refused in these
      *> contexts, loses its value across SET, or is not varied.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABCDE".
           05  E PIC X OCCURS 5 INDEXED BY X1 X2.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR.
           05  IX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           SET X1 TO 3
           SET IX TO X1
           SET X1 TO 1
           SET X1 TO IX
           IF X1 = 3
               DISPLAY "SET OK"
           ELSE
               DISPLAY "SET BAD"
           END-IF
           IF IX = X1
               DISPLAY "REL OK"
           ELSE
               DISPLAY "REL BAD"
           END-IF
           SET X1 TO 1
           SET IX TO X1
           SEARCH E VARYING IX
               AT END DISPLAY "NOT FOUND"
               WHEN E (X1) = "C" DISPLAY "FOUND " E (X1)
           END-SEARCH
           SET X2 TO IX
           IF X2 = 3
               DISPLAY "VARY OK"
           ELSE
               DISPLAY "VARY BAD"
           END-IF
           STOP RUN.
