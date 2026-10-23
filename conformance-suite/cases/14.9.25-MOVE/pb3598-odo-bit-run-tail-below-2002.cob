*>--- conformance-case
*> id: pb3598-odo-bit-run-tail-below-2002
*> rules: [GR-14.9.25.4-4]
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of conformance-suite/cases/14.9.25-MOVE/
      *> pb3598_odo_bit_run_tail_travels_with_the_group.  The filler bits that case moves are 8.5.1.6.3's, and their antecedent is a
      *> data item of USAGE BIT - boolean data, which with the PICTURE symbol 1 is a COBOL-2002 introduction.  Below 2002 the
      *> table is refused at its DATA DIVISION entry, so no bit run exists and no filler can.
      *> Witness for kb/Work PB3598.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3598NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NN PIC 9.
       01  G1.
           05  B1 PIC 1(2) USAGE BIT.
           05  T1 PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NN.
       PROCEDURE DIVISION.
           MOVE 1 TO NN
           MOVE "~" TO G1
           STOP RUN.
