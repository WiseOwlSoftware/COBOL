      *>--- conformance-case
      *> id: pb5386_set_receiver_identified_at_store
      *> rules: [GR-14.9.39.4-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386. "item identification of the data item
      *> referenced by identifier-1 is done immediately before the
      *> value of that data item is changed" (ISO 14.9.39.4 GR2; 1985
      *> VI.6.23.4 GR3d). I is in a record a REDEFINES shares, the
      *> subscript form that was evaluated once before the statement
      *> ran (it set WE (1)): WE (I) is identified after I is set, so
      *> WE (2) receives the occurrence number 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386ST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 I PIC 9 VALUE 1.
       01  B REDEFINES A PIC X.
       01  WT.
           05 WE PIC 9 OCCURS 3 VALUE 0.
       01  XT.
           05 XE PIC 9 OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       P1.
           SET IX TO 2.
           SET I, WE (I) TO IX.
           DISPLAY "WT[" WT "] I=" A.
           STOP RUN.
