      *>--- conformance-case
      *> id: pb3334_start_indexed_tree_operators
      *> rules: [GR-14.9.41.4-17, GR-14.9.41.4-7, GR-14.9.30.4-21,
      *>    GR-14.9.30.4-26, GR-14.9.30.4-27, GR-9.1.14-2, GR-9.1.14-3,
      *>    GR-9.1.14-4, GR-14.9.49.4-6, GR-9.1.13.7-6]
      *> accept: [2002, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3334 - START on an INDEXED file whose keys are B+
      *> trees (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4):
      *> every relational operator, a FULL key and a PARTIAL one (a
      *> data item at the key's leftmost positions, shorter than the
      *> key), found and not found, on the prime key and on an
      *> alternate key WITH DUPLICATES, with and without a FILE STATUS
      *> item and a USE declarative. LESS and its kin are 2002's.
      *> The rules (each quotation checked with scripts/spec/cite.py):
      *>   14.9.41.4 GR17 b) - the first temporary area has "the
      *>     length of data-name-1"; d) "This second temporary area is
      *>     truncated to the same length as the first"; e) the first
      *>     record whose key satisfies the comparison, the LESS
      *>     relations searching "in reverse order"; e) 1. "The file
      *>     position indicator is set to the value of the key of
      *>     reference in the first logical record whose key satisfies
      *>     the comparison". GR7 - a failed START leaves "no valid
      *>     record position", which 9.1.13.7 6) a) reads back as 46.
      *>   14.9.30.4 GR21 d) - after a START the next READ compares the
      *>     key ALONE, so it delivers the START's record (NEXT) or the
      *>     last of its duplicates (PREVIOUS); GR26 - duplicates come
      *>     in the order they were released; GR27 a) - '02' when the
      *>     next record in the walk has the same alternate key value.
      *>   9.1.14 2) - with an INVALID KEY phrase "any applicable
      *>     exception processing statements are not executed"; 3) -
      *>     without one they ARE; 4) - with neither, control goes to
      *>     the end of the statement. 14.9.49.4 GR6 a) - the USE
      *>     procedure runs "unless an AT END or INVALID KEY phrase
      *>     takes precedence".
      *> WHY EACH LEG CAN FAIL. P1-P5 walk the five operators with a
      *> 2-position partial key: a seek that compared whole keys finds
      *> nothing for "=" (no key is "AB") and the wrong record for the
      *> LESS ones. F1-F4 are whole keys: F1/F3 must answer 23 with
      *> INVALID KEY taken and NOT INVALID KEY not, and the READ after
      *> each must answer 46, not deliver a stale position. D1 walks
      *> the DDDD duplicates in release order (AA10 AB20 AB10), not
      *> prime order; D2 walks them back from the LAST released. U1-U3
      *> use the second connector, which has no FILE STATUS: U1 is
      *> found (no declarative), U2 is not found and has no INVALID
      *> KEY phrase, so the USE procedure runs and control continues
      *> (9.1.14 3)), U3 is not found WITH the phrase, which takes
      *> precedence over the procedure (9.1.14 2)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3334ST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb3334st.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K1
               ALTERNATE RECORD KEY IS A1 WITH DUPLICATES
               FILE STATUS IS FS1.
           SELECT F2 ASSIGN TO "pb3334st.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K2
               ALTERNATE RECORD KEY IS A2 WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1.
          05 K1.
             10 K1H PIC XX.
             10 K1T PIC XX.
          05 A1     PIC X(4).
       FD F2.
       01 R2.
          05 K2.
             10 K2H PIC XX.
             10 K2T PIC XX.
          05 A2     PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS1       PIC XX.
       01 TAG       PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       F2-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F2.
       F2-ERR-P.
           DISPLAY TAG " USE F2".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1
           MOVE "AA10DDDD" TO R1 WRITE R1
           MOVE "AB20DDDD" TO R1 WRITE R1
           MOVE "AA20CCCC" TO R1 WRITE R1
           MOVE "AB10DDDD" TO R1 WRITE R1
           MOVE "AC10EEEE" TO R1 WRITE R1
           CLOSE F1
           OPEN INPUT F1
           MOVE "P1" TO TAG MOVE "AB" TO K1H
           START F1 KEY IS EQUAL TO K1H
               INVALID KEY DISPLAY "P1 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "P2" TO TAG MOVE "AB" TO K1H
           START F1 KEY IS GREATER THAN K1H
               INVALID KEY DISPLAY "P2 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "P3" TO TAG MOVE "AB" TO K1H
           START F1 KEY IS NOT LESS THAN K1H
               INVALID KEY DISPLAY "P3 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "P4" TO TAG MOVE "AB" TO K1H
           START F1 KEY IS LESS THAN K1H
               INVALID KEY DISPLAY "P4 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "P5" TO TAG MOVE "AB" TO K1H
           START F1 KEY IS LESS THAN OR EQUAL TO K1H
               INVALID KEY DISPLAY "P5 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "F1" TO TAG MOVE "AB15" TO K1
           START F1 KEY IS EQUAL TO K1
               INVALID KEY DISPLAY "F1 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           PERFORM SHOW-NEXT
           MOVE "F2" TO TAG MOVE "AB15" TO K1
           START F1 KEY IS GREATER THAN OR EQUAL TO K1
               INVALID KEY DISPLAY "F2 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "F3" TO TAG MOVE "AA10" TO K1
           START F1 KEY IS LESS THAN K1
               INVALID KEY DISPLAY "F3 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           PERFORM SHOW-NEXT
           MOVE "F4" TO TAG MOVE "AB15" TO K1
           START F1 KEY IS LESS THAN K1
               INVALID KEY DISPLAY "F4 INVALID " FS1
               NOT INVALID KEY PERFORM SHOW-NEXT
           END-START
           MOVE "D1" TO TAG MOVE "DDDD" TO A1
           START F1 KEY IS EQUAL TO A1
               INVALID KEY DISPLAY "D1 INVALID " FS1
           END-START
           PERFORM SHOW-NEXT 3 TIMES
           MOVE "D2" TO TAG MOVE "DDDD" TO A1
           START F1 KEY IS LESS THAN OR EQUAL TO A1
               INVALID KEY DISPLAY "D2 INVALID " FS1
           END-START
           PERFORM SHOW-PREVIOUS 4 TIMES
           CLOSE F1
           OPEN INPUT F2
           MOVE "U1" TO TAG MOVE "AC" TO K2H
           START F2 KEY IS EQUAL TO K2H
           DISPLAY "U1 CONTINUES"
           MOVE "U2" TO TAG MOVE "ZZ" TO K2H
           START F2 KEY IS EQUAL TO K2H
           DISPLAY "U2 CONTINUES"
           MOVE "U3" TO TAG MOVE "ZZ" TO K2H
           START F2 KEY IS EQUAL TO K2H
               INVALID KEY DISPLAY "U3 INVALID KEY"
           END-START
           CLOSE F2
           STOP RUN.
       SHOW-NEXT.
           READ F1 NEXT RECORD
               AT END DISPLAY TAG " READ AT END " FS1
               NOT AT END DISPLAY TAG " READ " R1 " " FS1
           END-READ
           PERFORM SHOW-FAILURE.
       SHOW-PREVIOUS.
           READ F1 PREVIOUS RECORD
               AT END DISPLAY TAG " PREV AT END " FS1
               NOT AT END DISPLAY TAG " PREV " R1 " " FS1
           END-READ
           PERFORM SHOW-FAILURE.
       SHOW-FAILURE.
           IF FS1 NOT = "00" AND FS1 NOT = "02" AND FS1 NOT = "10"
               DISPLAY TAG " READ STATUS " FS1
           END-IF.
