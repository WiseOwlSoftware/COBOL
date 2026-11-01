      *>--- conformance-case
      *> id: pb4363-debug-all-procedure-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The identifier-1 of the X3.23-1985 operand ALL [REFERENCES OF] identifier-1 is a data item (VCR
      *> Table 7 row 7.17; kb/Work PB4363). P1 is a paragraph, and a paragraph-name is a procedure-name,
      *> "a word used to refer to a paragraph or section" (ISO/IEC 1989:2023 14.4.1), so it identifies no
      *> data item (8.4.2.1: "a statement shall contain a reference that uniquely identifies that
      *> resource"). WITH DEBUGGING MODE makes the declarative live; it is refused at 85 with COBOL5848
      *> naming the procedure, and from 2002 the whole facility is COBOL0902
      *> (pb4237-debug-all-identifier-removed-2002).
      *> Why it can fail: a binder that never resolves the operand calls P1 the legal-but-unmodeled
      *> data-item subject (COBOL1571), which says the program is right.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4363DBGALLP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T     PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL P1.
       DBG-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           MOVE "Z" TO T.
           STOP RUN.
