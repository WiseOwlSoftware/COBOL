      *>--- conformance-case
      *> id: pb5924-linkage-77-required-clauses-85
      *> rules: [1985-SR-X.4.1.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.4.1.1 (cite.py: OK): "The following data clauses are required in each data description entry"
      *> Level-77 Linkage items with a data-name and a PICTURE clause or USAGE IS INDEX receive the caller's
      *> values: 123 and the index for occurrence 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  W-N PIC 9(3) VALUE 123.
       77  W-I USAGE IS INDEX.
       01  W-TBL.
           05  W-EL PIC X OCCURS 9 TIMES INDEXED BY W-IX.
       PROCEDURE DIVISION.
       P1.
           SET W-IX TO 7.
           SET W-I TO W-IX.
           CALL "SUBP" USING W-N W-I.
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  WS-K PIC 9(3).
       01  S-TBL.
           05  S-EL PIC X OCCURS 9 TIMES INDEXED BY S-IX.
       LINKAGE SECTION.
       77  LK-N PIC 9(3).
       77  LK-I USAGE IS INDEX.
       PROCEDURE DIVISION USING LK-N LK-I.
       P2.
           SET S-IX TO LK-I.
           SET WS-K TO S-IX.
           DISPLAY LK-N " " WS-K.
           EXIT PROGRAM.
       END PROGRAM SUBP.
