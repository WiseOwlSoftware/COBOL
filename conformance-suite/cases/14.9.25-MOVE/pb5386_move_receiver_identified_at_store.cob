      *>--- conformance-case
      *> id: pb5386_move_receiver_identified_at_store
      *> rules: [GR-14.9.25.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386. "Item identification for identifier-2 is
      *> performed immediately before the data is moved to the
      *> respective data item" (ISO 14.9.25.4 GR1; 1985 VI.6.19.4
      *> GR2: "Any length evaluation or subscripting associated with
      *> identifier-2 is evaluated immediately before the data is
      *> moved"). I is in a record a REDEFINES shares, so its value
      *> is read through the shared area, the subscript form that was
      *> evaluated once before the statement ran: MOVE 2 TO I, WE (I)
      *> stored into WE (1). Each receiver is identified after the
      *> receivers before it are stored, so WE (2) receives 2, and in
      *> the second MOVE WE (1) is stored before I changes and WF (3)
      *> after it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386MV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 I PIC 9 VALUE 1.
       01  B REDEFINES A PIC X.
       01  WT.
           05 WE PIC 9 OCCURS 3 VALUE 0.
       01  WU.
           05 WF PIC 9 OCCURS 3 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE 2 TO I, WE (I).
           DISPLAY "WT[" WT "] I=" A.
           MOVE 1 TO I. MOVE ZERO TO WT.
           MOVE 3 TO WE (I), I, WF (I).
           DISPLAY "WT[" WT "] WU[" WU "] I=" A.
           STOP RUN.
