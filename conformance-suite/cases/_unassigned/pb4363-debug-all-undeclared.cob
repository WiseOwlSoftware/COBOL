      *>--- conformance-case
      *> id: pb4363-debug-all-undeclared
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The operand ALL [REFERENCES OF] identifier-1 of the X3.23-1985 USE FOR DEBUGGING names NOSUCH,
      *> which nothing in the program declares (VCR Table 7 row 7.17; kb/Work PB4363). ISO/IEC 1989:2023
      *> 8.4.2.1 requires "a statement shall contain a reference that uniquely identifies that resource",
      *> so the program is wrong, and it is refused at 85 with COBOL1639 (not defined). From 2002 the
      *> whole facility is COBOL0902 (pb4237-debug-all-identifier-removed-2002).
      *> Why it can fail: a binder that never resolves the operand answers COBOL1571, "recognized but not
      *> modeled", about a name that does not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4363DBGALLU.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T     PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL NOSUCH.
       DBG-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           MOVE "Z" TO T.
           STOP RUN.
