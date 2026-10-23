*>--- conformance-case
*> id: pb3564-group-move-keeps-bit-filler-below-2002
*> rules: [GR-14.9.25.4-4]
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of conformance-suite/cases/14.9.25-MOVE/pb3564_group_move_keeps_bit_filler
      *> (and of its two siblings in 13.18.1-ALIGNED and 13.18.60-USAGE).  The filler bits PB3564 stores are 8.5.1.6.3's,
      *> and their antecedent is a data item of USAGE BIT - boolean data, which with the PICTURE symbol 1 is a COBOL-2002
      *> introduction.  Below 2002 the group is refused at its DATA DIVISION entry, so no bit run exists and no filler can.
      *> Witness for kb/Work PB3564.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  G1-B PIC 1 USAGE BIT.
           05  G1-X PIC X.
       PROCEDURE DIVISION.
           MOVE "AB" TO G1
           DISPLAY "G1=[" G1 "]"
           STOP RUN.
