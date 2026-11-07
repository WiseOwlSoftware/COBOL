      *>--- conformance-case
      *> id: pb4856-debug-evaluate-phrase-removed
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4856. The USE FOR DEBUGGING declarative the sibling case
      *> pb4856-debug-evaluate-consecutive-when exercises belongs to the
      *> X3.23-1985 debug module, which ISO 2002 deleted (VCR Table 7 row
      *> 7.17), so a later edition refuses the declarative (COBOL0902).
      *> Why it can fail: an edition gate lost to the per-phrase flags
      *> would let the 1985 declarative compile under 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4856ER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9 VALUE 5.
       01 K PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB0 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF D.
       DB0-B.
           DISPLAY "  DB0".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           EVALUATE K
             WHEN 1
             WHEN D DISPLAY "ARM"
           END-EVALUATE.
           STOP RUN.
