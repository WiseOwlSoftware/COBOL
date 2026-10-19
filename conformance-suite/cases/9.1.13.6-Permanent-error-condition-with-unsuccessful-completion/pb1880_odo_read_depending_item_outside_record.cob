*>--- conformance-case
*> id: pb1880_odo_read_depending_item_outside_record
*> rules: [GR-9.1.13.6-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §9.1.13.6 item 4 b) when the DEPENDING ON item LIES OUTSIDE THE RECORD. The rule makes the READ
      *> unsuccessful when "the associated DEPENDING ON item contains a value that makes the number of bytes in the record
      *> exceed the value specified or implied by the file description entry" - it does not require that item to be a
      *> field of the record, and a record whose table is counted by a WORKING-STORAGE item is judged by that item's
      *> value when the READ completes (docs/CONFORMANCE.md D-ODO2). The READ itself does not set
      *> the item, so the program does; the record on the medium is the same 12 bytes in every case.
      *> The record is 2 bytes of header plus a table of 1 TO 10 characters; the file description implies the largest
      *> description, 12 bytes (§13.18.43.4 GR5 a)), and the record's size at the count N is 2 + N (GR13 c)). So:
      *>   WS-N = 03  ->  5 bytes   '00'
      *>   WS-N = 10  -> 12 bytes   '00'
      *>   WS-N = 11  -> 13 bytes   '34'
      *> The permanent error REMAINS IN EFFECT (§9.1.13.1), so the READ after it answers '34' although its own count (03)
      *> would fit, and CLOSE ends it (Annex A.1 item 105, DOC-A.1-105). The record area is not used to find the count:
      *> the header "99" read from the medium changes nothing about the verdict.
      *> WHY EACH LEG CAN FAIL: a check that looked only at a count held INSIDE the record would read the header "99" and
      *> answer '34' at the first READ; one that did not read the outside item after the READ would answer '00' at the third.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880OU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OU ASSIGN TO "pb1880ou.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT OUW ASSIGN TO "pb1880ou.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  OU.
       01  OU-R.
           05 OU-H PIC XX.
           05 OU-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON WS-N.
       FD  OUW.
       01  OUW-R PIC X(12).
       WORKING-STORAGE SECTION.
       01  FS   PIC XX.
       01  WS-N PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT OUW.
           MOVE "99ABCDEFGHIJ" TO OUW-R.
           WRITE OUW-R.
           WRITE OUW-R.
           WRITE OUW-R.
           WRITE OUW-R.
           CLOSE OUW.
           OPEN INPUT OU.
           MOVE 3 TO WS-N.
           READ OU NOT AT END DISPLAY "OU1 NAE H=" OU-H END-READ.
           DISPLAY "OU1 " FS.
           MOVE 10 TO WS-N.
           READ OU NOT AT END DISPLAY "OU2 NAE H=" OU-H END-READ.
           DISPLAY "OU2 " FS.
           MOVE 11 TO WS-N.
           READ OU NOT AT END DISPLAY "OU3 NAE" END-READ.
           DISPLAY "OU3 " FS.
           MOVE 3 TO WS-N.
           READ OU NOT AT END DISPLAY "OU4 NAE" END-READ.
           DISPLAY "OU4 " FS.
           CLOSE OU.
           DISPLAY "CLOSE " FS.
           OPEN INPUT OU.
           MOVE 3 TO WS-N.
           READ OU NOT AT END DISPLAY "OU5 NAE H=" OU-H END-READ.
           DISPLAY "OU5 " FS.
           CLOSE OU.
           STOP RUN.
