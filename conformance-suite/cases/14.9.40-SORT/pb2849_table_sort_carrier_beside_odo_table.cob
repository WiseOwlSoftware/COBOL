*>--- conformance-case
*> id: pb2849_table_sort_carrier_beside_odo_table
*> rules: [GR-14.9.40.4-20, GR-14.9.40.4-24, GR-13.18.38.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2849 - THE FORMAT-2 TABLE SORT OF A FIXED TABLE OF VARIABLE-LENGTH CARRIERS IN AN EXTERNAL RECORD
      *> THAT ALSO HOLDS AN OCCURS DEPENDING TABLE.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 13.18.38.4 "The value of the data item referenced by data-name-1 represents the current
      *>     number of occurrences of the subject of the entry" -> OK §13.18.38.4 7)
      *>   cite.py --check 8.5.1.11.2 "behaves in all respects as though it were in fact contiguous with its
      *>     neighbors" -> OK §8.5.1.11.2
      *> The record BR is EXTERNAL, so its variable-length components live apart from its fixed run and each element
      *> of E moves as its component carrier. The record's trailing storage is an OCCURS DEPENDING table T, counted
      *> by N (§13.18.38.3 SR22 makes it the record's last item): the table is NOT part of E's elements, so the sort
      *> must neither carry it nor disturb it, and the carrier of an element must not be cut by the record's count.
      *> (An element itself cannot hold an occurs-depending table: §13.18.38.3 SR1 b.)
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   keys 3, 1, 2; the key-3 element holds "THREE" "3" and two EP occurrences (Z Y), the key-1 element "ONE"
      *>   "1" and one (Q), the key-2 element "TWO" "2" and none. The element's contiguous extent is 1 + the two
      *>   dynamic-length items + the table's current capacity (§8.5.1.11.2), so before: 9, 6, 5; after SORT E
      *>   ASCENDING KEY EK the elements are in key order with their own components and capacities: 1ONE1Q (6),
      *>   2TWO2 (5), 3THREE3ZY (9). T keeps its two occurrences "A" "B" and N stays 2.
      *>   EXCEPTION PATH. N = 7 is above integer-2 (4), so the subject T and the items subordinate or superordinate
      *>   to it (BR) would raise EC-BOUND-ODO when referenced (§13.18.38.4 GR7, --check validated:
      *>   cite.py --check 13.18.38.4 "At the time the subject of entry is referenced or any data item subordinate
      *>   or superordinate to the subject of entry is referenced" -> OK §13.18.38.4 7)). E is a SIBLING of T, neither
      *>   subordinate nor superordinate to it, so under checking SORT E DESCENDING KEY EK with N = 7 raises nothing
      *>   and still sorts, carriers whole: the declarative stays silent (no RAISED line) and the elements read
      *>   3THREE3ZY (9), 2TWO2 (5), 1ONE1Q (6). A regression that applied the record's count bound to a reference
      *>   into BR, or cut the carriers at it, would print RAISED or an unsorted or truncated table. N is put back
      *>   to 2 before T is read, since reading T under N = 7 would itself raise the condition.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2849BO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 EXTERNAL.
       01 BR EXTERNAL.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 ED1 PIC X DYNAMIC LENGTH.
             10 ED2 PIC X DYNAMIC LENGTH.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP.
          05 T PIC X OCCURS 1 TO 4 DEPENDING ON N.
       01 I PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-ODO.
       D1P.
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE 2 TO N
           MOVE 3 TO EK OF E(1)
           MOVE 1 TO EK OF E(2)
           MOVE 2 TO EK OF E(3)
           MOVE "THREE" TO ED1(1)
           MOVE "3" TO ED2(1)
           MOVE "Z" TO EP(1, 1)
           MOVE "Y" TO EP(1, 2)
           MOVE "ONE" TO ED1(2)
           MOVE "1" TO ED2(2)
           MOVE "Q" TO EP(2, 1)
           MOVE "TWO" TO ED1(3)
           MOVE "2" TO ED2(3)
           MOVE "A" TO T(1)
           MOVE "B" TO T(2)
           DISPLAY "before"
           PERFORM SHOW
           SORT E ASCENDING KEY EK
           DISPLAY "after"
           PERFORM SHOW
           MOVE 7 TO N
           SORT E DESCENDING KEY EK
           DISPLAY "after N=7"
           PERFORM SHOW-E
           MOVE 2 TO N
           DISPLAY "N " N " T " T(1) T(2)
           STOP RUN.
       SHOW.
           PERFORM SHOW-E
           DISPLAY "N " N " T " T(1) T(2).
       SHOW-E.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "E(" I ") len " FUNCTION LENGTH(E(I)) " " E(I)
           END-PERFORM.
