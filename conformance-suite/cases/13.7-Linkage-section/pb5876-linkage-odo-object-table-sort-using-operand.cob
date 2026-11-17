*>--- conformance-case
*> id: pb5876-linkage-odo-object-table-sort-using-operand
*> rules: [SR-13.7.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> The control of pb5876-linkage-odo-object-table-sort: L-2, the record of L-E's OCCURS DEPENDING ON object
      *> L-N, is an operand of the USING phrase, so ISO §2023:13.7.3 SR4 admits L-N and the SORT reads it. The SORT
      *> orders the table's current occurrences (§2023:14.9.40.4 GR20: "The number of occurrences of table elements
      *> referenced by data-name-2 is determined by the rules in the OCCURS clause"), the first 3 of "EDCBA", into
      *> ascending order, and leaves the 4th and 5th characters, which are no occurrence of the table, as they
      *> were: W-1 becomes "CDEBA".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876UM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1.
          05 W-E PIC X OCCURS 5 TIMES.
       01 W-2.
          05 W-N PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           MOVE "EDCBA" TO W-1
           CALL "PB5876UN" USING W-1 W-2
           DISPLAY "W-1=" W-1
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876UN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-1.
          05 L-E PIC X OCCURS 1 TO 5 DEPENDING ON L-N.
       01 L-2.
          05 L-N PIC 9.
       PROCEDURE DIVISION USING L-1 L-2.
           SORT L-E ASCENDING
           GOBACK.
       END PROGRAM PB5876UN.
       END PROGRAM PB5876UM.
