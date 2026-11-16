      *>--- conformance-case
      *> id: g85_bf24_07
      *> rules: [1985-GR-VIII.4.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR3 - "When a file is not in an open mode, no
      *> statement may be executed which references the file ...
      *> except for a MERGE statement with the USING or GIVING phrase,
      *> an OPEN statement, or a SORT statement with the USING or
      *> GIVING phrase." The relative file RLF, dynamic access:
      *>   OPEN OUTPUT RLF on a not-open file -> 00; records 1 CCC3,
      *>   2 AAA1, 3 BBB2 are written, RLF is closed.
      *>   SORT ... DESCENDING ... USING RLF GIVING SQ with both not
      *>   open; it leaves both not open (§1985:XI.4.4.4 GR6), so
      *>   OPEN I-O RLF and OPEN INPUT SQ each give 00 (an open file
      *>   gives 41); record 2 is still AAA1; SQ holds CCC3 BBB2 AAA1.
      *> Last, the forbidden form: a START on RLF after it is closed.
      *> §1985:VIII.1.3.4 4f sets I-O status 47 for a READ or START on
      *> a file not open in the input or I-O mode, shown by the USE
      *> procedure (USE-RLF=47). A 4x status is critical, so what
      *> follows the USE procedure is the implementor's; nothing after
      *> it is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2407.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "g85bf2407r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT SQ ASSIGN TO "g85bf2407s.dat"
               FILE STATUS IS ST-S.
           SELECT SW ASSIGN TO "g85bf2407w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  RLF.
       01  RLF-REC PIC X(4).
       FD  SQ.
       01  SQ-REC PIC X(4).
       SD  SW.
       01  SW-REC.
           05 SW-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK   PIC 9(4).
       01  ST-R PIC XX.
       01  ST-S PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       R-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RLF.
       R-ERR-PARA.
           DISPLAY "USE-RLF=" ST-R.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RLF.
           DISPLAY "OPEN-RLF=" ST-R.
           MOVE 1 TO RK.
           MOVE "CCC3" TO RLF-REC.
           WRITE RLF-REC INVALID KEY DISPLAY "W1-INVALID".
           MOVE 2 TO RK.
           MOVE "AAA1" TO RLF-REC.
           WRITE RLF-REC INVALID KEY DISPLAY "W2-INVALID".
           MOVE 3 TO RK.
           MOVE "BBB2" TO RLF-REC.
           WRITE RLF-REC INVALID KEY DISPLAY "W3-INVALID".
           CLOSE RLF.
           SORT SW ON DESCENDING KEY SW-KEY USING RLF GIVING SQ.
           OPEN I-O RLF.
           DISPLAY "OPEN-RLF2=" ST-R.
           MOVE 2 TO RK.
           READ RLF INVALID KEY DISPLAY "R2-INVALID".
           DISPLAY "RLF2=" RLF-REC.
           CLOSE RLF.
           OPEN INPUT SQ.
           DISPLAY "OPEN-SQ=" ST-S.
           READ SQ AT END DISPLAY "SQ-EOF-EARLY".
           DISPLAY "SQ1=" SQ-REC.
           READ SQ AT END DISPLAY "SQ-EOF-EARLY".
           DISPLAY "SQ2=" SQ-REC.
           READ SQ AT END DISPLAY "SQ-EOF-EARLY".
           DISPLAY "SQ3=" SQ-REC.
           READ SQ AT END DISPLAY "SQ-EOF".
           CLOSE SQ.
           MOVE 1 TO RK.
           START RLF KEY IS EQUAL TO RK.
           STOP RUN.
