      *>--- conformance-case
      *> id: pb5117_random_read_dup_status
      *> rules: [GR-14.9.30.4-27]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5117 - a RANDOM READ of an alternate key that has duplicates, from COBOL-2002: I-O status 00.
      *>
      *> §2023:14.9.30.4 GR27 (checked with scripts/spec/cite.py --check): I-O status '02' is set when the READ is
      *> successful, "an indexed file is being sequentially accessed", the key of reference is an alternate key and the
      *> next record has the same key; a READ with a KEY phrase is a random access, so it sets '00'.  (COBOL-85's rule
      *> names no access mode and answers 02: see pb5117_random_read_dup_status_1985.)
      *>
      *> Records 0001AA 0002AA 0003AA 0004BB.  READ KEY AA: 00.  READ NEXT 0002AA: next 0003AA equals it: 02.
      *> READ NEXT 0003AA: next 0004BB differs: 00.  READ KEY BB: 00.  READ KEY IS R-KEY: 00.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5117B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5117b.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-AK WITH DUPLICATES
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(4).
          05 R-AK  PIC X(2).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "0001AA" TO R.
           WRITE R.
           MOVE "0002AA" TO R.
           WRITE R.
           MOVE "0003AA" TO R.
           WRITE R.
           MOVE "0004BB" TO R.
           WRITE R.
           CLOSE F.
           OPEN INPUT F.
           MOVE "AA" TO R-AK.
           READ F KEY IS R-AK INVALID KEY DISPLAY "RA-INV".
           DISPLAY "READ ALT AA " R " " F-ST.
           READ F NEXT AT END DISPLAY "N1-END".
           DISPLAY "READ NEXT " R " " F-ST.
           READ F NEXT AT END DISPLAY "N2-END".
           DISPLAY "READ NEXT " R " " F-ST.
           MOVE "BB" TO R-AK.
           READ F KEY IS R-AK INVALID KEY DISPLAY "RB-INV".
           DISPLAY "READ ALT BB " R " " F-ST.
           MOVE "0001" TO R-KEY.
           READ F KEY IS R-KEY INVALID KEY DISPLAY "RP-INV".
           DISPLAY "READ PRIME 0001 " R " " F-ST.
           CLOSE F.
           STOP RUN.
