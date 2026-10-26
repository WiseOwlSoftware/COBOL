*>--- conformance-case
*> id: pb3598-odo-bit-run-tail-variable-group-below-2014
*> rules: [GR-14.9.25.4-9]
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of conformance-suite/cases/14.9.25-MOVE/
      *> pb3598_odo_bit_run_tail_variable_group.  A variable-length group needs a dynamic-length item (DYNAMIC LENGTH, 8.5.1.10),
      *> a COBOL-2014 introduction, and the bit run needs USAGE BIT (2002), so below 2014 the group is refused at its DATA DIVISION
      *> entry by the edition gate (COBOL0900) and no variable-length group move exists.
      *> Witness for kb/Work PB3598.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3598VLGOLD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NN PIC 9.
       01 G1.
          05 D1 PIC X DYNAMIC LENGTH LIMIT 5.
          05 B1 PIC 1(2) USAGE BIT.
          05 T1 PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO NN
           DISPLAY "[" G1 "]"
           STOP RUN.
