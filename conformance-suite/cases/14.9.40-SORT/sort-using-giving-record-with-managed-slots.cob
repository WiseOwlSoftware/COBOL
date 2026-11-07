*>--- conformance-case
*> id: sort-using-giving-record-with-managed-slots
*> rules: [GR-14.9.40.4-2, GR-14.9.24.4-7, GR-14.9.24.4-12, GR-13.18.60.4-9, DOC-A.1-212, DOC-A.1-216]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4838 - SORT and MERGE over an SD record that is a strongly-typed group holding a data-pointer and a message-tag
      *> leaf (13.18.60.3 SR14), with USING and GIVING files whose records hold the same leaves.
      *> 14.9.40.4 GR12 b): each USING record "is obtained as if a READ statement ... had been executed" and released; GR15: the
      *> returned records are written to each GIVING file as if a WRITE statement without optional phrases had been executed, in key
      *> order; 14.9.24.4 GR7 and GR12 say the same of MERGE for its USING files and its GIVING file. A record is whole through all
      *> of it, so the pointer a record was written with is the pointer it comes back with, whichever place the key order gives
      *> it - a file carries a pointer and a message tag as the 8-position token of DOC-A.1-216 and DOC-A.1-212, which READ and
      *> RETURN read back as the same reference within the run unit. (cite.py --check OK for 14.9.40.4 and 14.9.24.4.)
      *> Expected: USING IN-F (keys 3, 1, 2) GIVING OUT-F reads back 1 AAAAA, 2 BBBBB, 3 CCCCC with the tag NULL; the SORT with an
      *> OUTPUT PROCEDURE returns the same order and the based item read through each returned pointer; the MERGE of the sorted
      *> M1 (keys 1, 3) and M2 (keys 2, 4) gives 1 to 4 with AAAAA to DDDDD; a SORT of an empty USING file gives an empty
      *> GIVING file (its first READ is at end, status 10); and a record whose pointer eight bytes were not produced by this run
      *> unit crosses the sort byte for byte (the GIVING file holds the same eight bytes) and its pointer is not NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SORTUSINGSLOTS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-F ASSIGN TO "sortusing.in"
              ORGANIZATION SEQUENTIAL FILE STATUS FS1.
           SELECT OUT-F ASSIGN TO "sortusing.out"
              ORGANIZATION SEQUENTIAL FILE STATUS FS2.
           SELECT M1-F ASSIGN TO "sortusing.m1"
              ORGANIZATION SEQUENTIAL FILE STATUS FS1.
           SELECT M2-F ASSIGN TO "sortusing.m2"
              ORGANIZATION SEQUENTIAL FILE STATUS FS1.
           SELECT RAW-F ASSIGN TO "sortusing.in"
              ORGANIZATION SEQUENTIAL FILE STATUS FS1.
           SELECT RAW-OUT ASSIGN TO "sortusing.out"
              ORGANIZATION SEQUENTIAL FILE STATUS FS2.
           SELECT SF ASSIGN TO "sortusing.wrk".
       DATA DIVISION.
       FILE SECTION.
       FD IN-F.
       01 IN-REC TYPE T-REC.
       FD OUT-F.
       01 OUT-REC TYPE T-REC.
       FD M1-F.
       01 M1-REC TYPE T-REC.
       FD M2-F.
       01 M2-REC TYPE T-REC.
       FD RAW-F.
       01 RAW-REC.
          05 RAW-KEY PIC X(2).
          05 RAW-PTR PIC X(8).
          05 RAW-TAG PIC X(8).
       FD RAW-OUT.
       01 RAW-OUT-REC PIC X(18).
       SD SF.
       01 SREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 SKEY PIC 9(2).
          05 SPTR USAGE POINTER.
          05 STAG USAGE MESSAGE-TAG.
       01 WS-A PIC X(5) VALUE "AAAAA".
       01 WS-B PIC X(5) VALUE "BBBBB".
       01 WS-C PIC X(5) VALUE "CCCCC".
       01 WS-D PIC X(5) VALUE "DDDDD".
       01 BX PIC X(5) BASED.
       01 WS-I PIC 9(2).
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M-P.
           OPEN OUTPUT IN-F
           PERFORM VARYING WS-I FROM 3 BY -1 UNTIL WS-I = 0
              MOVE WS-I TO SKEY OF IN-REC
              EVALUATE WS-I
                 WHEN 1 SET SPTR OF IN-REC TO ADDRESS OF WS-A
                 WHEN 2 SET SPTR OF IN-REC TO ADDRESS OF WS-B
                 WHEN 3 SET SPTR OF IN-REC TO ADDRESS OF WS-C
              END-EVALUATE
              SET STAG OF IN-REC TO NULL
              WRITE IN-REC
           END-PERFORM
           CLOSE IN-F
           SORT SF ON ASCENDING KEY SKEY OF SREC
               USING IN-F GIVING OUT-F
           DISPLAY "SORT " FS1 " " FS2
           OPEN INPUT OUT-F
           PERFORM 3 TIMES
              READ OUT-F
              SET ADDRESS OF BX TO SPTR OF OUT-REC
              DISPLAY SKEY OF OUT-REC " " BX
              IF STAG OF OUT-REC = NULL
                 DISPLAY "tag null"
              END-IF
           END-PERFORM
           CLOSE OUT-F
           SORT SF ON DESCENDING KEY SKEY OF SREC
               USING IN-F OUTPUT PROCEDURE IS OUTP
           OPEN OUTPUT M1-F
           MOVE 1 TO SKEY OF M1-REC
           SET SPTR OF M1-REC TO ADDRESS OF WS-A
           WRITE M1-REC
           MOVE 3 TO SKEY OF M1-REC
           SET SPTR OF M1-REC TO ADDRESS OF WS-C
           WRITE M1-REC
           CLOSE M1-F
           OPEN OUTPUT M2-F
           MOVE 2 TO SKEY OF M2-REC
           SET SPTR OF M2-REC TO ADDRESS OF WS-B
           WRITE M2-REC
           MOVE 4 TO SKEY OF M2-REC
           SET SPTR OF M2-REC TO ADDRESS OF WS-D
           WRITE M2-REC
           CLOSE M2-F
           MERGE SF ON ASCENDING KEY SKEY OF SREC
               USING M1-F M2-F GIVING OUT-F
           DISPLAY "MERGE " FS1 " " FS2
           OPEN INPUT OUT-F
           PERFORM 4 TIMES
              READ OUT-F
              SET ADDRESS OF BX TO SPTR OF OUT-REC
              DISPLAY SKEY OF OUT-REC " " BX
           END-PERFORM
           CLOSE OUT-F
           OPEN OUTPUT IN-F
           CLOSE IN-F
           SORT SF ON ASCENDING KEY SKEY OF SREC
               USING IN-F GIVING OUT-F
           OPEN INPUT OUT-F
           READ OUT-F
           DISPLAY "EMPTY " FS2
           CLOSE OUT-F
           OPEN OUTPUT RAW-F
           MOVE "05" TO RAW-KEY
           MOVE X"7F00000000000001" TO RAW-PTR
           MOVE X"0000000000000000" TO RAW-TAG
           WRITE RAW-REC
           CLOSE RAW-F
           SORT SF ON ASCENDING KEY SKEY OF SREC
               USING IN-F GIVING OUT-F
           OPEN INPUT RAW-OUT
           READ RAW-OUT
           IF RAW-OUT-REC = "05" & X"7F00000000000001" & X"0000000000000000"
              DISPLAY "BYTES KEPT"
           END-IF
           CLOSE RAW-OUT
           OPEN INPUT OUT-F
           READ OUT-F
           IF SPTR OF OUT-REC = NULL
              DISPLAY "FOREIGN NULL"
           ELSE
              DISPLAY "FOREIGN NOT NULL"
           END-IF
           CLOSE OUT-F
           STOP RUN.
       OUTP SECTION.
           PERFORM 3 TIMES
              RETURN SF AT END EXIT PERFORM END-RETURN
              SET ADDRESS OF BX TO SPTR OF SREC
              DISPLAY SKEY OF SREC " " BX " returned"
           END-PERFORM.
