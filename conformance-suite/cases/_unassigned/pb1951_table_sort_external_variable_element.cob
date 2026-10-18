*>--- conformance-case
*> id: pb1951_table_sort_external_variable_element
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1951 (sibling) - THE FORMAT-2 TABLE SORT OF AN EXTERNAL TABLE WHOSE ELEMENT HOLDS
      *> VARIABLE-LENGTH COMPONENTS: two dynamic-length items and a dynamic-capacity table.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2." -> OK §14.9.40.4 24)
      *>   cite.py --check 8.5.1.11.2 "behaves in all respects as though it were in fact contiguous with its
      *>     neighbors" -> OK §8.5.1.11.2
      *> A table element is the whole of its description, and GR24 places the sorted ELEMENTS: each element's
      *> dynamic-length items keep their own content and its dynamic-capacity table keeps its occurrences. An
      *> EXTERNAL record's variable-length components live apart from its fixed run (the record's storage cell),
      *> so the sort must move each element's components with it. Before this change the element was moved as one
      *> contiguous image and cut back by length: the first dynamic-length item took every character of the
      *> second, and the dynamic-capacity table's occurrences were dropped.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   keys 3, 1, 2; ED1/ED2 hold "THREE"/"3", "ONE"/"1", "TWO"/"2"; EP holds "Z" "Y" in the key-3 element and
      *>   "Q" in the key-1 element, none in the key-2 element. SORT E ASCENDING KEY EK places the elements in key
      *>   order, each with its components:
      *>     1 [ONE] [1], 2 [TWO] [2], 3 [THREE] [3]  - a sort cutting the image back printed [ONE1] [] and so on.
      *>     EP 1 Q, EP 3 ZY                          - the table's occurrences travel with their element (an
      *>                                                image move left them empty).
      *>
      *> THE CAPACITY (kb/Work PB2849). Each element's dynamic-capacity table has a capacity register in the element
      *> (§13.18.38.3 SR30), which SR31 forbids to subscript, so no reference names ECAP; the capacity shows in the
      *> element's contiguous extent instead (§8.5.1.11.2; FUNCTION LENGTH counts alphanumeric positions, §15.50.4
      *> rule 3): 1 (EK) + the two dynamic-length items + the table's current capacity. Before the sort the elements
      *> are 9 (THREE, 3, capacity 2), 6 (ONE, 1, capacity 1) and 5 (TWO, 2, capacity 0); each moves with its own
      *> capacity (GR24), so after it they read 6, 5, 9. A capacity that stayed in its POSITION (2, 1, 0) while the
      *> items moved would give the key-1 element 1+3+1+2 = 7, the key-2 element 1+3+1+1 = 6 and the key-3 element
      *> 1+5+1+0 = 7, so the line would read LEN 7 6 7 (an image move that ignored the components would keep the
      *> positions' own lengths, 9 6 5):
      *>   cite.py --check 15.50.4 "the returned value is an integer equal to the length of argument-1 in
      *>     alphanumeric character positions" -> OK §15.50.4 3)
      *>   cite.py --check 13.18.38.3 "Data-name-3 shall not be subscripted." -> OK §13.18.38.3 31)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951EV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BR EXTERNAL.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 ED1 PIC X DYNAMIC LENGTH.
             10 ED2 PIC X DYNAMIC LENGTH.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP.
       01 I PIC 9.
       PROCEDURE DIVISION.
       MAIN.
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
           PERFORM SHOW-LENGTHS
           SORT E ASCENDING KEY EK
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY EK OF E(I) " [" ED1(I) "] [" ED2(I) "]"
           END-PERFORM
           DISPLAY "EP 1 " EP(1, 1)
           DISPLAY "EP 3 " EP(3, 1) EP(3, 2)
           PERFORM SHOW-LENGTHS
           STOP RUN.
       SHOW-LENGTHS.
           DISPLAY "LEN " FUNCTION LENGTH(E(1)) " " FUNCTION LENGTH(E(2))
               " " FUNCTION LENGTH(E(3)).
