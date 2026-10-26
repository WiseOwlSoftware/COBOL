*>--- conformance-case
*> id: pb3691-relation-literal-integer-function
*> rules: [GR-8.8.4.2.1-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.2.5: "The numeric integer operand shall be an integer literal or an integer numeric data item of
      *> usage display or national." A numeric, integer or index FUNCTION-IDENTIFIER is neither: it is not a
      *> literal (8.4.3.2.1: it "references the unique data item that results from the evaluation of a function"),
      *> and 15.2 4), 5) and 6) state no usage for the item such a function returns, so it is not "of usage display
      *> or national" (docs/CONFORMANCE.md D-FNUSE; kb/Work PB3691, owner 2026-10-25: "Refuse it per the spec").
      *> The pair has no defined comparison (COBOL2532, as for a USAGE COMP item). Before kb/Work PB3691 it
      *> compiled clean and compared the function's literal-form text: N = 7 against "0007" answered not equal,
      *> where the same relation on the USAGE DISPLAY item N answers equal.
      *> Shape: the same pair in the other operand order - the rule is symmetric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3691R2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 X PIC X(4) VALUE "0007".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF "0007" = FUNCTION INTEGER(N) DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           STOP RUN.
