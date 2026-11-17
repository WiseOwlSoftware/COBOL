      *>--- conformance-case
      *> id: pb5695-group-beyond-size-limit
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> AN IMPLEMENTOR-DEFINED LIMIT, refused BY NAME (kb/Work PB5695;
      *> docs/CONFORMANCE.md section 3 D-ITEM-SIZE). No edition bounds
      *> the size of a data item; WiseOwl COBOL holds one item,
      *> elementary or group, to 134217728 (2^27) character positions,
      *> the limit a PICTURE already meets (COBOL2148). Each integer
      *> below is within its own limit, but each group is the sum of
      *> its subordinates times their OCCURS counts:
      *>   T  = 1000 x 2000000000 = 2000000000000 positions (WS);
      *>   FR = 100 x 20000000    = 2000000000 positions (FILE);
      *>   L  = 100 x 20000000    = 2000000000 positions (LINKAGE).
      *> Each is COBOL7641 at every edition. MEASURED BEFORE the fix:
      *> each compiled clean, its size wrapped in the compiler's 32-bit
      *> model, and the program committed memory until the host ran
      *> out. Never run this program on a compiler without the limit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695SZ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5695.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 FE PIC X(100) OCCURS 20000000.
       WORKING-STORAGE SECTION.
       01  T.
           05 E PIC X(1000) OCCURS 2000000000.
       LINKAGE SECTION.
       01  L.
           05 LE PIC X(100) OCCURS 20000000.
       PROCEDURE DIVISION USING L.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
