*>--- conformance-case
*> id: pb3597-initialize-with-filler-replacing-boolean-below-2002
*> rules: [GR-14.9.20.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of conformance-suite/cases/14.9.20-INITIALIZE/
      *> pb3597_initialize_with_filler_replacing_boolean_in_a_table_pad and pb3597_bit_run_tail_beside_a_redefines_canonical.
      *> The filler bits those two cases reset are 8.5.1.6.3's, and their antecedent is a data item of USAGE BIT - boolean
      *> data, which with the PICTURE symbol 1 is a COBOL-2002 introduction.  Below 2002 the group is refused at its DATA
      *> DIVISION entry, so no bit table exists and no filler between its occurrences can.
      *> Witness for kb/Work PB3597.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3597NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G-T PIC 1(3) USAGE BIT ALIGNED OCCURS 2.
           05  G-X PIC X.
       PROCEDURE DIVISION.
           INITIALIZE G WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           STOP RUN.
