*>--- conformance-case
*> id: pb3666_dynamic_table_group_move_below_2014
*> rules: [GR-14.9.25.4-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3666, the negative of pb3666_dynamic_table_group_move: a dynamic-capacity table (OCCURS DYNAMIC) is a
*> COBOL-2014 introduction, so the program is refused at COBOL-85 and COBOL-2002 by name (COBOLNET0900) and accepted
*> from 2014 on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3666DTN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2.
          05 T2 PIC X OCCURS DYNAMIC CAPACITY IN C2 FROM 3.
       01 G1.
          05 T1 PIC 1(3) USAGE BIT OCCURS DYNAMIC CAPACITY IN C1 FROM 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE G2 TO G1
           STOP RUN.
