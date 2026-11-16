      *>--- conformance-case
      *> id: g85_z08
      *> rules: [1985-GR-VI.6.5.4-2, 1985-GR-VI.6.5.4-3,
      *>    1985-GR-VI.6.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> stdin: g85_z08.in
      *>---
      *> §1985:VI.6.5.4 GR2 - the implementor defines the transfer
      *> size: one 80-character record, a shorter line padded with
      *> spaces, a longer line consecutive records, end of input
      *> leaving spaces (docs/conformance/7/DOC-A.1-5.md). Line 3:
      *> a 90-character line is two records, the second padded;
      *> line 4: 'WXYZ' is a whole padded record before 'XY' (a
      *> line-sized transfer would give WXYZXY); line 7: end of input.
      *> §1985:VI.6.5.4 GR3 - an 80-character receiver takes an
      *> 80-character line whole (line 1).
      *> §1985:VI.6.5.4 GR4 a - a wider receiver stores each record
      *> left-aligned in the positions not yet occupied and requests
      *> more (lines 3, 4); b - where the record exceeds the receiver
      *> or the portion left, the leftmost characters are stored and
      *> the rest ignored (lines 2, 3, 4, 5). Line 6 is the
      *> determination: every ACCEPT begins at a new line (DOC-A.1-5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Z08.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R80   PIC X(80).
       01  R5    PIC X(5).
       01  R170  PIC X(170).
       01  R100  PIC X(100).
       PROCEDURE DIVISION.
       P1.
           ACCEPT R80.
           DISPLAY "1[" R80 "]".
           ACCEPT R5.
           DISPLAY "2[" R5 "]".
           ACCEPT R170.
           DISPLAY "3[" R170 "]".
           ACCEPT R100.
           DISPLAY "4[" R100 "]".
           ACCEPT R5.
           DISPLAY "5[" R5 "]".
           ACCEPT R5.
           DISPLAY "6[" R5 "]".
           MOVE "QQQQQ" TO R5.
           ACCEPT R5.
           DISPLAY "7[" R5 "]".
           STOP RUN.
