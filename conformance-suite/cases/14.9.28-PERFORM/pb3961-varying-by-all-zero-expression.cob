*>--- conformance-case
*> id: pb3961-varying-by-all-zero-expression
*> rules: [FMT-14.9.28.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3961 - the 14.9.28.2 varying-phrase prints BY { identifier-4
      *> | literal-2 }; ALL ZERO + 1 is an arithmetic expression whose first
      *> operand is the figurative ZERO with its optional word ALL (8.3.3.6.2
      *> Format 1). The refusal quotes the operand as written: it used to
      *> print 'ALL0+1' (the rewritten token's text, no separators). Fails if
      *> the message spells anything but the source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3961ZV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC S9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING X FROM 1 BY ALL ZERO + 1 UNTIL X > 2
               DISPLAY X
           END-PERFORM
           STOP RUN.
