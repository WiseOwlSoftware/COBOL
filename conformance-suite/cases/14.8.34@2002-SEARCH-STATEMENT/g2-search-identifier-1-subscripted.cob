      *>--- conformance-case
      *> id: g2-search-identifier-1-subscripted
      *> rules: [2002-SR-14.8.34.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR1 - "Identifier-1 shall not be subscripted
      *> or reference modified and its data description entry shall
      *> contain an OCCURS clause including an INDEXED phrase."
      *> Leg: identifier-1 subscripted (an enclosing subscript): refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABCDEF".
          05 ROW OCCURS 2 INDEXED BY OX.
             10 E PIC X OCCURS 3 INDEXED BY IX.
       01 U VALUE "ABC".
          05 NE PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           SET OX TO 2.
           SET IX TO 1.
           SEARCH E(OX)
               AT END DISPLAY "NOT-FOUND"
               WHEN E(OX, IX) = "E" DISPLAY "FOUND"
           END-SEARCH.
           STOP RUN.
