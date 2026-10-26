*>--- conformance-case
*> id: pb1951_table_sort_external_pointer_dynamic_table
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1951 - THE FORMAT-2 TABLE SORT OF AN EXTERNAL TABLE WHOSE ELEMENT HOLDS A DYNAMIC-CAPACITY
      *> POINTER TABLE.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 13.18.22.3 "When a record description is an external item, any associated type declaration
      *>     that is strongly typed shall also be external." -> OK §13.18.22.3 5)
      *> GR24 places the sorted ELEMENTS, each the whole of its description: an element's dynamic-capacity table keeps
      *> its own capacity and occurrences (§8.5.1.9), and every pointer in it keeps its value. In an EXTERNAL record
      *> the pointers ride the record's managed slots, one per occurrence of the table each element holds. The STRONG
      *> type that admits a pointer below level 1 (§13.18.60.3 SR14) must itself be EXTERNAL (§13.18.22.3 SR5), a
      *> COBOL-2023 description. Before this change the statement was deferred (COBOL1756) and aborted the run unit.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   keys 3, 1, 2; the key-3 element's table holds two pointers (AAAA, BBBB), the key-1 element's one (CCCC),
      *>   the key-2 element's none. SORT E ASCENDING KEY EK:
      *>     1 CCCC       - the key-1 element is first with its one pointer (an image-only move left AAAA here);
      *>     2            - the key-2 element holds no pointer;
      *>     3 AAAA BBBB  - the key-3 element is last with both, in their own order.
      *>
      *> THE CAPACITY (kb/Work PB2849). The table's capacity register lives inside the element (§13.18.38.3 SR30) and
      *> SR31 forbids subscripting it, so no reference names ECAP: the capacity shows in the element's contiguous
      *> extent (§8.5.1.9.2: a sending reference sees a table of the current capacity). FUNCTION LENGTH counts
      *> alphanumeric positions (§15.50.4 rule 3), so (LENGTH of the element - LENGTH of EK) / LENGTH of a pointer is
      *> the number of pointer occurrences; the pointer's own width is the implementor's and cancels out:
      *>   before SORT  key 3 -> 2, key 1 -> 1, key 2 -> 0   (the capacities the SET statements created)
      *>   after SORT   key 1 -> 1, key 2 -> 0, key 3 -> 2   (each element carries its own capacity, GR24)
      *> A capacity that failed to shrink at the front would print 2 and 1 there, and one that failed to grow at the
      *> back would print 0:
      *>   cite.py --check 15.50.4 "the returned value is an integer equal to the length of argument-1 in
      *>     alphanumeric character positions" -> OK §15.50.4 3)
      *>   cite.py --check 8.5.1.9.2 "When a data item in a dynamic-capacity table is referenced as a sending
      *>     operand, the result of the operation is the same as for a fixed-capacity table whose number of
      *>     occurrences is the current capacity of the table" -> OK §8.5.1.9.2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951PD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-TAB TYPEDEF STRONG EXTERNAL.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 EP USAGE POINTER OCCURS DYNAMIC CAPACITY IN ECAP.
       01 BR TYPE T-TAB EXTERNAL.
       01 W PIC X(4) BASED.
       01 WS-A PIC X(4) VALUE "AAAA".
       01 WS-B PIC X(4) VALUE "BBBB".
       01 WS-C PIC X(4) VALUE "CCCC".
       01 PTR USAGE POINTER.
       01 I PIC 9.
       01 CAP PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO EK OF E(1)
           MOVE 1 TO EK OF E(2)
           MOVE 2 TO EK OF E(3)
           SET EP(1, 1) TO ADDRESS OF WS-A
           SET EP(1, 2) TO ADDRESS OF WS-B
           SET EP(2, 1) TO ADDRESS OF WS-C
           DISPLAY "before"
           PERFORM SHOW-CAPACITIES
           SORT E ASCENDING KEY EK
           SET ADDRESS OF W TO EP(1, 1)
           DISPLAY EK OF E(1) " " W
           DISPLAY EK OF E(2)
           SET ADDRESS OF W TO EP(3, 1)
           DISPLAY EK OF E(3) " " W WITH NO ADVANCING
           SET ADDRESS OF W TO EP(3, 2)
           DISPLAY " " W
           DISPLAY "after"
           PERFORM SHOW-CAPACITIES
           STOP RUN.
       SHOW-CAPACITIES.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               COMPUTE CAP = (FUNCTION LENGTH(E(I))
                   - FUNCTION LENGTH(EK OF E(I))) / FUNCTION LENGTH(PTR)
               DISPLAY "key " EK OF E(I) " capacity " CAP
           END-PERFORM.
