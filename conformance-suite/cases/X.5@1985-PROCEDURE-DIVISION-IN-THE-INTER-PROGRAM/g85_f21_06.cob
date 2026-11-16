      *>--- conformance-case
      *> id: g85_f21_06
      *> rules: [1985-GR-X.5.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 GR4 - Linkage Section items of the called program
      *> G85F2156 may be referenced when they are (a) operands of the
      *> USING phrase of its Procedure Division header (LK-A, LK-B,
      *> LK-C, LK-D, LK-E: five data-names, the level 1 minimum the rule
      *> states for both USING phrases), (b) subordinate to such an
      *> operand (LK-A1, LK-A2), (c) defined by a REDEFINES or RENAMES
      *> whose object is such an item (LK-R, LK-RN), (d) subordinate to
      *> an item of (c) (LK-R1, LK-R2), or (e) condition-names or
      *> index-names of such items (LK-A1-OK, LK-IX). Every reference
      *> must translate and resolve to the caller's storage (X.5.1 GR2,
      *> by reference): the lines show the caller's values, and the
      *> MOVE to LK-E is seen by the caller as E AFTER Z. A compiler
      *> that refuses any of these references, or resolves one to
      *> other storage, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2106.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(5) VALUE "OK123".
       01  WS-B PIC X(3) VALUE "BBB".
       01  WS-C PIC 9(3) VALUE 42.
       01  WS-D PIC X(2) VALUE "DD".
       01  WS-E PIC X VALUE "E".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "G85F2156" USING WS-A WS-B WS-C WS-D WS-E.
           DISPLAY "E AFTER " WS-E.
           STOP RUN.
       END PROGRAM G85F2106.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2156.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-A.
           05  LK-A1 PIC X(2).
               88  LK-A1-OK VALUE "OK".
           05  LK-A2 PIC 9 OCCURS 3 TIMES INDEXED BY LK-IX.
       66  LK-RN RENAMES LK-A1.
       01  LK-R REDEFINES LK-A.
           05  LK-R1 PIC X(3).
           05  LK-R2 PIC X(2).
       01  LK-B PIC X(3).
       01  LK-C PIC 9(3).
       01  LK-D PIC X(2).
       01  LK-E PIC X.
       PROCEDURE DIVISION USING LK-A LK-B LK-C LK-D LK-E.
       SUB-P.
           DISPLAY "A " LK-A.
           DISPLAY "A1 " LK-A1.
           IF LK-A1-OK
               DISPLAY "COND OK"
           ELSE
               DISPLAY "COND NOT OK".
           SET LK-IX TO 2.
           DISPLAY "A2 " LK-A2 (LK-IX).
           DISPLAY "R " LK-R.
           DISPLAY "R1 " LK-R1.
           DISPLAY "R2 " LK-R2.
           DISPLAY "RN " LK-RN.
           DISPLAY "B " LK-B.
           DISPLAY "C " LK-C.
           DISPLAY "D " LK-D.
           DISPLAY "E " LK-E.
           MOVE "Z" TO LK-E.
           EXIT PROGRAM.
       END PROGRAM G85F2156.
