      *>--- conformance-case
      *> id: pb3981-relation-zero-touching-sign
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB3981 - ISO 8.7.1 (as pb3981-compute-zero-touching-sign):
      * the operand of a relation condition is an arithmetic expression,
      * so in IF X = ZEROS +1 the +1 is a binary plus written without its
      * second space: COBOL2993, as for IF X = Y +1. The ZERO must not
      * stay a figurative operand-list item here. The sibling position
      * is the second operand, IF ZERO -1 = X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3981R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC S9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           IF X = ZEROS +1
               DISPLAY "A"
           END-IF.
           IF ZERO -1 = X
               DISPLAY "B"
           END-IF.
           STOP RUN.
       END PROGRAM PB3981R.
