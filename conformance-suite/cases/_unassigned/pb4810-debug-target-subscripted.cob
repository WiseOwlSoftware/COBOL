      *>--- conformance-case
      *> id: pb4810-debug-target-subscripted
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING syntax rule 10: "If the
      *> data description entry of the data item referenced by identifier-1
      *> contains an OCCURS clause or is subordinate to a data description
      *> entry that contains an OCCURS clause, identifier-1 must be specified
      *> without the subscripting or indexing normally required." The
      *> operand ELEM (2) is subscripted, so the program is refused
      *> (COBOL6334); ELEM written alone is the legal form
      *> (pb4810-debug-sub-and-name). Why it can fail: a binder that resolves
      *> the operand with its subscripts compiles a trigger for one
      *> occurrence, which the rule does not give.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810SS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           02  ELEM PIC 9 OCCURS 3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ELEM (2).
       DBG-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           MOVE 1 TO ELEM (2).
           STOP RUN.
