*>--- conformance-case
*> id: pb2746_carrier_store_both_lanes
*> rules: [GR-14.6.9.2-2, GR-14.9.25.4-9]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2746. One variable-length group MOVE, received by a group of WORKING-STORAGE (a declared
      *> record) and by an EXTERNAL group (a shared storage cell): both lanes store each carried component
      *> through the same runtime operation, so they must agree.
      *> X = the EXCESS PART. 14.9.25.4 GR9: "each location that occupies the excess part is space filled",
      *> and 14.6.9.4: "the current capacity of the dynamic table is unaffected, and each element of the
      *> dynamic table is space-filled" - capacity 2 stays and both elements are spaces (never recreated at
      *> capacity zero, which is what an empty CARRIED table means).
      *> L = a CARRIED pair. 8.5.1.10.4: "If the maximum length is reached, the value is truncated on the
      *> right as necessary" - ABCDEFG into LIMIT 4 is ABCD; 14.6.9.2 recreates the table from the sender's
      *> one element ZZZ and fills it to its FROM 2 minimum with space-filled elements, so the capacity is 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2746LANES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EA PIC ZZ9.
       01 SFIX.
          05 SF-A PIC X(2) VALUE "PQ".
       01 RTAB.
          05 RT-A PIC X(2).
          05 RT-T PIC X(3) OCCURS DYNAMIC CAPACITY IN RT-C FROM 2.
       01 XTAB EXTERNAL.
          05 XT-A PIC X(2).
          05 XT-T PIC X(3) OCCURS DYNAMIC CAPACITY IN XT-C FROM 2.
       01 SDYN.
          05 SD-A PIC X(2) VALUE "MN".
          05 SD-L PIC X DYNAMIC LENGTH LIMIT 8.
          05 SD-T PIC X(3) OCCURS DYNAMIC CAPACITY IN SD-C FROM 1.
       01 RVAR.
          05 RV-A PIC X(2).
          05 RV-L PIC X DYNAMIC LENGTH LIMIT 4.
          05 RV-T PIC X(3) OCCURS DYNAMIC CAPACITY IN RV-C FROM 2.
       01 XVAR EXTERNAL.
          05 XV-A PIC X(2).
          05 XV-L PIC X DYNAMIC LENGTH LIMIT 4.
          05 XV-T PIC X(3) OCCURS DYNAMIC CAPACITY IN XV-C FROM 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "111" TO RT-T (1).
           MOVE "222" TO RT-T (2).
           MOVE "111" TO XT-T (1).
           MOVE "222" TO XT-T (2).
           MOVE SFIX TO RTAB.
           MOVE RT-C TO EA.
           DISPLAY "DX=[" RT-A "]" EA "[" RT-T (1) "][" RT-T (2) "]".
           MOVE SFIX TO XTAB.
           MOVE XT-C TO EA.
           DISPLAY "CX=[" XT-A "]" EA "[" XT-T (1) "][" XT-T (2) "]".
           MOVE "ABCDEFG" TO SD-L.
           MOVE "ZZZ" TO SD-T (1).
           MOVE SDYN TO RVAR.
           MOVE RV-C TO EA.
           DISPLAY "DL=[" RV-A "][" RV-L "]" EA "[" RV-T (1) "]".
           MOVE SDYN TO XVAR.
           MOVE XV-C TO EA.
           DISPLAY "CL=[" XV-A "][" XV-L "]" EA "[" XV-T (1) "]".
           STOP RUN.
