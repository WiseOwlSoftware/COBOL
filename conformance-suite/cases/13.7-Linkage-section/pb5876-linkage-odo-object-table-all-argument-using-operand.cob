*>--- conformance-case
*> id: pb5876-linkage-odo-object-table-all-argument-using-operand
*> rules: [SR-13.7.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> The control of pb5876-linkage-odo-object-table-all-argument: L-2, the record of L-E's OCCURS DEPENDING ON
      *> object L-N, is an operand of the USING phrase, so ISO §2023:13.7.3 SR4 admits L-N and the argument reads it.
      *> L-E (ALL) is "as if each table element associated with that subscript position were specified" (§2023:15.3),
      *> the current occurrences of the table: the first 3 of "ABCDE". Their maximum is "C", never the "E" of the
      *> table's 5th character, which is no occurrence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876GM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1.
          05 W-E PIC X OCCURS 5 TIMES.
       01 W-2.
          05 W-N PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           MOVE "ABCDE" TO W-1
           CALL "PB5876GN" USING W-1 W-2
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876GN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-1.
          05 L-E PIC X OCCURS 1 TO 5 DEPENDING ON L-N.
       01 L-2.
          05 L-N PIC 9.
       PROCEDURE DIVISION USING L-1 L-2.
           DISPLAY "MAX=" FUNCTION MAX (L-E (ALL))
           GOBACK.
       END PROGRAM PB5876GN.
       END PROGRAM PB5876GM.
