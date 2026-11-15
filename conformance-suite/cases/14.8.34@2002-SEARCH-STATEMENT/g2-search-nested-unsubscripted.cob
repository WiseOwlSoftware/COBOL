      *>--- conformance-case
      *> id: g2-search-nested-unsubscripted
      *> rules: [2002-SR-14.8.34.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR1 - "Identifier-1 shall not be subscripted
      *> or reference modified and its data description entry shall
      *> contain an OCCURS clause including an INDEXED phrase." The
      *> granted form for a table inside another table: SEARCH E with
      *> E unsubscripted; the outer occurrence comes from the WHEN
      *> subscripts (§2002:14.8.34.3 GR1 "each appropriate subscript
      *> shall be set to the desired value before the SEARCH statement
      *> is executed"), and only IX is varied ("only the setting of an
      *> index associated with identifier-1 ... is modified").
      *> ROW(1) = "ABC", ROW(2) = "DEF". With OX = 2, IX = 1:
      *>   "E" is E(2, 2): FOUND OX=2 IX=2.
      *>   "B" is only in ROW(1): row 2 is searched, AT END, OX=2.
      *> Fails if the unsubscripted nested form is refused at 2002, if
      *> the search ignores the WHEN outer subscript (row 1 would give
      *> IX=2 for "B"), or if SEARCH alters OX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SCH01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABCDEF".
          05 ROW OCCURS 2 INDEXED BY OX.
             10 E PIC X OCCURS 3 INDEXED BY IX.
       01 N1 PIC 9.
       01 N2 PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SET OX TO 2.
           SET IX TO 1.
           SEARCH E
               AT END DISPLAY "NOT-FOUND E"
               WHEN E(OX, IX) = "E"
                   SET N1 TO OX
                   SET N2 TO IX
                   DISPLAY "FOUND OX=" N1 " IX=" N2
           END-SEARCH.
           SET IX TO 1.
           SEARCH E
               AT END
                   SET N1 TO OX
                   DISPLAY "AT END B OX=" N1
               WHEN E(OX, IX) = "B"
                   SET N2 TO IX
                   DISPLAY "FOUND B IX=" N2
           END-SEARCH.
           STOP RUN.
