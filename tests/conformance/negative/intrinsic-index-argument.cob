*> reject-at: 2002 2014 2023
      *> kb/Work R27 (ledger F82) - 8.5.2.1 Table 2 makes class INDEX distinct from class numeric,
      *> and 15.44.3 r1 requires class numeric - but an index DATA item's PicInfo carries category
      *> NUMERIC for storage, so it passed every class-numeric screen, and an index-NAME fell into
      *> the computed-operand-is-numeric arm: FUNCTION INTEGER(IX) computed the occurrence number
      *> silently. The usage-keyed CobolClass.Index arm now rejects both shapes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R27NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       77 IX-ITEM USAGE INDEX.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
           SET IX TO 2.
           SET IX-ITEM TO IX.
           COMPUTE R = FUNCTION INTEGER(IX-ITEM).
           COMPUTE R = FUNCTION INTEGER(IX).
           DISPLAY R.
           STOP RUN.
