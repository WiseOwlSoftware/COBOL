*>--- conformance-case
*> id: pb3845-underscore-unparseable-85
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3845 - ISO 8.3.2.1: "the basic special characters hyphen and underscore" - the underscore is a COBOL-2002
      *> introduction (the COBOL-85 set is hyphen-only). The statement below does not parse; the gate is asked of the
      *> word the lexer read, so it is named all the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3845C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MY_NAME PIC X(5) VALUE "CREME".
       PROCEDURE DIVISION.
           DISPLAY MY_NAME MOVE MOVE.
           STOP RUN.
