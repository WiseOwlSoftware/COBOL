      *>--- conformance-case
      *> id: pb5386_arith_receiver_identified_at_store
      *> rules: [GR-14.7.7-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386. The result is stored in each resulting data
      *> item "in the left-to-right order in which the receiving data
      *> items are specified in the statement. Item identification for
      *> the receiving data items is done as each data item is
      *> accessed" (ISO 14.7.7 4) b); 1985 VI.6.4.6). For DIVIDE
      *> format 5, "If identifier-4 is subscripted, the subscript is
      *> evaluated immediately before the remainder is stored" (1985
      *> VI.6.11.4 GR4). I is in a record a REDEFINES shares, the
      *> subscript form that was evaluated once before the statement
      *> ran (ADD stored into WE (1)): each receiver's subscript reads
      *> I after the receivers to its left were stored.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386AR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 I PIC 9 VALUE 1.
       01  B REDEFINES A PIC X.
       01  WT.
           05 WE PIC 9 OCCURS 3 VALUE 0.
       01  WR.
           05 WQ PIC 9 OCCURS 3 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO I, WE (I).
           DISPLAY "ADD WT[" WT "] I=" A.
           MOVE 1 TO I. MOVE ZERO TO WT.
           COMPUTE I, WE (I) = 3.
           DISPLAY "COMPUTE WT[" WT "] I=" A.
           MOVE 1 TO I.
           DIVIDE 7 BY 2 GIVING I REMAINDER WQ (I).
           DISPLAY "DIVIDE WR[" WR "] I=" A.
           STOP RUN.
