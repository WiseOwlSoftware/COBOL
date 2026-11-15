      *>--- conformance-case
      *> id: g2rw08_alt_key_order
      *> rules: [2002-GR-14.8.33.3-23]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.3 GR23 (cite.py: OK) - REWRITE of a record with
      *>   alternate keys: a) "When the value of a specific alternate
      *>   record key is not changed, the order of retrieval when that
      *>   key is the key of reference remains unchanged." b) when it
      *>   is changed and duplicates are permitted, "the record is
      *>   logically positioned last within the set of duplicate
      *>   records" with that value.
      *> §2002:14.8.29.3 GR23 (cite.py: OK): duplicates under the key
      *>   of reference are read "in the same order ... in which they
      *>   are released by execution of WRITE statements, or by
      *>   execution of REWRITE statements that create such duplicate
      *>   values"; GR29 (cite.py: OK): a random READ by an alternate
      *>   key finds "the first record in a sequence of duplicates
      *>   that was released to the operating environment".
      *> Written: 01/AA/PP, 02/BB/PP, 03/BB/PP (key/A1/A2).
      *>   BASE  A1 walk from AA: 01 02 03; A2 walk: 01 02 03.
      *>   LEG B REWRITE 01 with A1 AA -> BB: b) puts 01 LAST in the
      *>         BB set, after 02 and 03 although its prime key is
      *>         lowest: A1 walk 02 03 01. A2 (PP) is unchanged: a)
      *>         keeps 01 02 03. Random READ KEY A1 = BB: 02.
      *>   LEG C REWRITE 02 changing only R-D: neither alternate key
      *>         changes, so both orders remain (a): A1 02 03 01, A2
      *>         01 02 03. Random READ KEY A1 = BB: 02 still.
      *>   LEG D CLOSE and OPEN INPUT: the orders are the file's:
      *>         A1 02 03 01, A2 01 02 03.
      *> Fails if a changed key is placed by prime key (A1 01 02 03),
      *> or if a REWRITE that keeps a key repositions the record in
      *> that key's duplicate set (LEG B A2 02 03 01, or LEG C A1
      *> 03 01 02).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw08.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-A1 WITH DUPLICATES
               ALTERNATE RECORD KEY IS R-A2 WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R.
           05 R-KEY PIC X(2).
           05 R-A1  PIC X(2).
           05 R-A2  PIC X(2).
           05 R-D   PIC X(1).
       WORKING-STORAGE SECTION.
       01  W-TAG  PIC X(4).
       01  W-LINE.
           05 W-K1 PIC X(3).
           05 W-K2 PIC X(3).
           05 W-K3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "01AAPPx" TO R. WRITE R.
           MOVE "02BBPPx" TO R. WRITE R.
           MOVE "03BBPPx" TO R. WRITE R.
           CLOSE F.
           OPEN I-O F.
           MOVE "BASE" TO W-TAG.
           PERFORM WALK-A1. PERFORM WALK-A2.
           MOVE "01" TO R-KEY.
           READ F KEY IS R-KEY INVALID KEY DISPLAY "B-READ-INV"
           END-READ.
           MOVE "BB" TO R-A1.
           REWRITE R INVALID KEY DISPLAY "B-RW-INV" END-REWRITE.
           MOVE "B" TO W-TAG.
           PERFORM WALK-A1. PERFORM WALK-A2. PERFORM RANDOM-A1.
           MOVE "02" TO R-KEY.
           READ F KEY IS R-KEY INVALID KEY DISPLAY "C-READ-INV"
           END-READ.
           MOVE "y" TO R-D.
           REWRITE R INVALID KEY DISPLAY "C-RW-INV" END-REWRITE.
           MOVE "C" TO W-TAG.
           PERFORM WALK-A1. PERFORM WALK-A2. PERFORM RANDOM-A1.
           CLOSE F.
           OPEN INPUT F.
           MOVE "D" TO W-TAG.
           PERFORM WALK-A1. PERFORM WALK-A2.
           CLOSE F.
           STOP RUN.
       WALK-A1.
           MOVE "AA" TO R-A1.
           START F KEY IS NOT LESS THAN R-A1
               INVALID KEY DISPLAY "A1-START-INV"
           END-START.
           PERFORM READ-THREE.
           DISPLAY W-TAG " A1 " W-LINE.
       WALK-A2.
           MOVE "PP" TO R-A2.
           START F KEY IS NOT LESS THAN R-A2
               INVALID KEY DISPLAY "A2-START-INV"
           END-START.
           PERFORM READ-THREE.
           DISPLAY W-TAG " A2 " W-LINE.
       READ-THREE.
           MOVE SPACES TO W-LINE.
           READ F NEXT AT END MOVE "EOF" TO W-K1 END-READ.
           IF W-K1 = SPACES MOVE R-KEY TO W-K1 END-IF.
           READ F NEXT AT END MOVE "EOF" TO W-K2 END-READ.
           IF W-K2 = SPACES MOVE R-KEY TO W-K2 END-IF.
           READ F NEXT AT END MOVE "EOF" TO W-K3 END-READ.
           IF W-K3 = SPACES MOVE R-KEY TO W-K3 END-IF.
       RANDOM-A1.
           MOVE "BB" TO R-A1.
           READ F KEY IS R-A1 INVALID KEY DISPLAY "R-INV" END-READ.
           DISPLAY W-TAG " R  " R-KEY.
