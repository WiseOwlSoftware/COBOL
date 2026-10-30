*>--- conformance-case
*> id: pb3845-extended-letter-unparseable-85
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3845 - ISO 8.3.2.1: the COBOL-85 word is basic letters, digits and hyphen, so an extended letter is the
      *> COBOL-2002 introduction gate. The statement below does not parse; the gate is asked of the word the lexer
      *> read, not of the tree, so it is named all the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3845B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CAFÉ PIC X(5) VALUE "CREME".
       PROCEDURE DIVISION.
           DISPLAY CAFÉ MOVE MOVE.
           STOP RUN.
