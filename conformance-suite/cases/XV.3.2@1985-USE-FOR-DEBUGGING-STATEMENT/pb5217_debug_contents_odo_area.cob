      *>--- conformance-case
      *> id: pb5217_debug_contents_odo_area
      *> rules: [1985-GR-XV.3.2.4-23]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5217. X3.23-1985 USE FOR DEBUGGING general rule 23 c:
      *> "For READ, DEBUG-CONTENTS contains the entire record read." The
      *> file's record R holds an OCCURS DEPENDING ON table whose DEPENDING
      *> item N is 1 when the READ runs, so R's own current extent is 5
      *> positions; the record read is the 10-position record the WRITE
      *> released at N = 6 (fixed length 10, the largest record, at the
      *> implementor's choice of 1985 II.2.1.4.3, docs/CONFORMANCE.md
      *> DOC-A.1-147). R's DEPENDING item sizes R, never the record read.
      *>
      *> EXPECTED VALUES (derived from the rules, not from a run): the
      *> section runs after each OPEN and CLOSE and after the READ (rule
      *> 2 a, b; a WRITE is not among its triggers). The OPENs and CLOSEs
      *> are other references to the file-name (rule 23 d: spaces); the
      *> READ gives ABCDEFGHIJ. Five lines, the fourth the READ's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5217DB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5217db.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R.
           05 RK PIC X(4).
           05 RT PIC X OCCURS 1 TO 6 DEPENDING ON N.
       01  R3 PIC X(7).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON F.
       D1-P.
           DISPLAY "DBG [" DEBUG-CONTENTS (1:10) "]".
       END DECLARATIVES.
       M SECTION.
       M1.
           OPEN OUTPUT F.
           MOVE 6 TO N.
           MOVE "ABCDEFGHIJ" TO R.
           WRITE R.
           CLOSE F.
           MOVE 1 TO N.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF".
           CLOSE F.
           STOP RUN.
