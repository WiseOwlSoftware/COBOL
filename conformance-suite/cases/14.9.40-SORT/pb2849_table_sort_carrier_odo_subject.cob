*>--- conformance-case
*> id: pb2849_table_sort_carrier_odo_subject
*> rules: [GR-14.9.40.4-18, GR-14.9.40.4-20, GR-14.9.40.4-24, GR-13.18.38.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2849 - THE FORMAT-2 TABLE SORT OF AN OCCURS DEPENDING TABLE WHOSE ELEMENTS ARE VARIABLE-LENGTH
      *> CARRIERS, ON ITS SUCCESS PATH AND ON ITS EC-BOUND-ODO EXCEPTION PATH.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 14.9.40.4 "The number of occurrences of table elements referenced by data-name-2 is
      *>     determined by the rules in the OCCURS clause" -> OK §14.9.40.4 20)
      *>   cite.py --check 13.18.38.4 "the value of the data item referenced by data-name-1 shall fall within the
      *>     bounds from integer-1 through integer-2. If the value of the data item does not fall within the
      *>     specified bounds, the EC-BOUND-ODO exception condition is set to exist." -> OK §13.18.38.4 7)
      *>   cite.py --check 14.9.33.4 "If the NEXT STATEMENT phrase is specified, control is transferred to an
      *>     implicit CONTINUE statement" -> OK §14.9.33.4 2)
      *>   cite.py --check 8.5.1.11.2 "behaves in all respects as though it were in fact contiguous with its
      *>     neighbors" -> OK §8.5.1.11.2
      *>   cite.py --check 15.50.4 "the returned value is an integer equal to the length of argument-1 in
      *>     alphanumeric character positions" -> OK §15.50.4 3)
      *> E is an OCCURS DEPENDING table (N, EXTERNAL) and each element holds two dynamic-length items and a
      *> dynamic-capacity table: a variable-length element (§8.5.1.11.2), which the statement moves whole as its
      *> component carrier. An element cannot hold an OCCURS DEPENDING table of its own (§13.18.38.3 SR1 b: the OCCURS
      *> clause shall not be specified in an entry that has an occurs-depending table subordinate to it), so the
      *> table SORT meets a depending table only as the SUBJECT, as here.
      *>
      *> THE OBSERVER. A dynamic-capacity table nested in E has its capacity register inside E (§13.18.38.3 SR30),
      *> which SR31 forbids to subscript, so no reference names ECAP. The capacity shows in the element's own length:
      *> LENGTH of a variable-length element is its contiguous current extent (§8.5.1.11.2; §15.50.4 rule 3 counts
      *> alphanumeric positions) = 1 (EK) + the two dynamic-length items + the table's current capacity, and the
      *> element DISPLAYs as that same contiguous image.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   N = 3. Keys 3, 1, 2. Before: 3THREE3ZY (9; capacity 2), 1ONE1Q (6; capacity 1), 2TWO2 (5; capacity 0).
      *>   SORT E ASCENDING KEY EK places the elements, each with its own items and capacity, in key order:
      *>     1ONE1Q (6), 2TWO2 (5), 3THREE3ZY (9)
      *>   a capacity that stayed in its POSITION (2, 1, 0) while the items moved would give the key-1 element
      *>   1+3+1+2 = 7, the key-2 element 1+3+1+1 = 6 and the key-3 element 1+5+1+0 = 7: lengths 7 6 7.
      *>   EXCEPTION PATH. N = 7 is above integer-2 (4): under EC-BOUND-ODO checking the condition is set to exist
      *>   when the subject is referenced (GR7), the declarative displays it and RESUME NEXT STATEMENT continues
      *>   after the SORT (§14.9.33.4 GR2): the statement placed nothing, so the descending order that would have
      *>   reversed the three elements never appears.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2849OD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 EXTERNAL.
       01 BR EXTERNAL.
          05 E OCCURS 1 TO 4 DEPENDING ON N.
             10 EK PIC 9.
             10 ED1 PIC X DYNAMIC LENGTH.
             10 ED2 PIC X DYNAMIC LENGTH.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP.
       01 I PIC 9.
       01 SAVED-N PIC 9.
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
           MOVE 3 TO N
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
           DISPLAY "before"
           PERFORM SHOW
           SORT E ASCENDING KEY EK
           DISPLAY "after N=3"
           PERFORM SHOW
           MOVE 7 TO N
           SORT E DESCENDING KEY EK
           DISPLAY "after N=7"
           PERFORM SHOW
           STOP RUN.
       SHOW.
           MOVE N TO SAVED-N
           MOVE 3 TO N
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "E(" I ") len " FUNCTION LENGTH(E(I)) " " E(I)
           END-PERFORM
           MOVE SAVED-N TO N.
