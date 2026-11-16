      *>--- conformance-case
      *> id: g85_f07_06
      *> rules: [1985-SR-VI.5.8.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR5 - "integer-1 must be greater than or equal
      *> to zero and integer-2 must be greater than integer-1." OCCURS
      *> 0 TO 3 is the boundary the rule admits (integer-1 equal to
      *> zero, integer-2 greater), so the compiler must accept it.
      *> §1985:VI.5.8.4 GR2b: the number of occurrences is the current
      *> value of WS-CNT; GR3a: WS-CNT is outside the group, so a
      *> reference to WS-TBL uses only that part of the table. With
      *> WS-CNT 3 the group holds the three moved (line 1 "[PQR]"),
      *> with WS-CNT 1 only the first (line 2 "[P]"). A compiler that
      *> refuses integer-1 of zero, or uses the maximum length, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0706.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CNT  PIC 9 VALUE 0.
       01  WS-TBL.
           05  WS-EL  PIC X OCCURS 0 TO 3 TIMES DEPENDING ON WS-CNT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 3 TO WS-CNT.
           MOVE "P" TO WS-EL (1).
           MOVE "Q" TO WS-EL (2).
           MOVE "R" TO WS-EL (3).
           DISPLAY "[" WS-TBL "]".
           MOVE 1 TO WS-CNT.
           DISPLAY "[" WS-TBL "]".
           STOP RUN.
