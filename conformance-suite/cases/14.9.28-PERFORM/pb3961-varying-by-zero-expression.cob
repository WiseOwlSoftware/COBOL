*>--- conformance-case
*> id: pb3961-varying-by-zero-expression
*> rules: [FMT-14.9.28.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3961 - the 14.9.28.2 varying-phrase prints a BRACE GROUP in
      *> each operand slot: BY { identifier-4 | literal-2 }. An arithmetic
      *> expression is not one of them, and ZERO + 1 is one. The refusal
      *> quotes the operand AS THE PROGRAMMER WROTE IT: the token rewriter
      *> gives the ZERO the text "0" (it is the numeric zero there), and the
      *> message used to print '0+1'. Fails if the message spells the
      *> rewritten token instead of the source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3961YV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC S9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING X FROM 1 BY ZERO + 1 UNTIL X > 2
               DISPLAY X
           END-PERFORM
           STOP RUN.
