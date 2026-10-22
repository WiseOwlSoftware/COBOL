      *>--- conformance-case
      *> id: pb3510-depending-on-the-table-itself
      *> rules: [SR-13.18.38.3-2, SR-13.18.38.3-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3510 - DEPENDING ON names the table's own entry WS-T.
      *> 13.18.38.3 SR2, "Data-name-1 and data-name-2 shall not be
      *> subscripted": WS-T is a table, so a reference to it is subscripted
      *> (COBOLNET0853, the code this refusal expects - before PB3510 only an
      *> item WITHIN a table drew it, never the table itself); and SR20 - WS-T
      *> occupies the table's own first byte (COBOLNET0857).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3510N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
           05 WS-H.
               10 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-T PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO WS-T (1)
           DISPLAY WS-G
           STOP RUN.
