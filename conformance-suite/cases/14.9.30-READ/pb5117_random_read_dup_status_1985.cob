      *>--- conformance-case
      *> id: pb5117_random_read_dup_status_1985
      *> rules: [GR-14.9.30.4-27]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5117 - a RANDOM READ of an alternate key that has duplicates, at COBOL-85: I-O status 02.
      *>
      *> §1985:IX.1.3.4 GRL2.1 (checked with scripts/spec/cite.py --check): "For a READ statement, the key value
      *> for the current key of reference is equal to the value of the same key in the next record within the current
      *> key of reference." The rule names no access mode, so a READ with a KEY phrase is covered; the record made
      *> available is the first of the duplicates released.  From COBOL-2002 the rule is limited
      *> to a READ with NEXT or PREVIOUS (§2023:14.9.30.4 GR27): see pb5117_random_read_dup_status.
      *>
      *> Records 0001AA 0002AA 0003AA 0004BB.  READ KEY AA makes 0001AA available and 0002AA has the same key: 02.
      *> READ NEXT 0002AA: next 0003AA equals it: 02.  READ NEXT 0003AA: next 0004BB differs: 00.  READ KEY BB: only
      *> one record: 00.  READ KEY IS R-KEY (the prime key never repeats): 00.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5117A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5117a.dat"
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
